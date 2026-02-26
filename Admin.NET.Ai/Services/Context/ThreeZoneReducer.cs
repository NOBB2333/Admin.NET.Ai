using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using System.Text;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 三区保护压缩 Reducer — 借鉴 OpenCowork context-compression.ts
/// 
/// 三区保护策略:
///   Zone A: 用户首条真实消息 — 永远保留（保持任务上下文不丢失）
///   压缩区: 中间历史消息 — 用 LLM 摘要替代
///   Zone B: 最近 N 条消息 — 逐字保留（保持最新上下文完整）
/// 
/// 额外特性:
///   - 预压缩: Token 用量达 65% 时,清理旧工具结果(不调 LLM)
///   - 孤儿修复: 压缩后修复断裂的 FunctionCall/FunctionResult 配对
///   - 工具边界感知: Zone B 的起点不会切断工具调用对
/// </summary>
public class ThreeZoneReducer(
    IAiService aiService,
    Microsoft.Extensions.Options.IOptions<Admin.NET.Ai.Options.CompressionConfig> configOptions) : IChatReducer
{
    private readonly IAiService _aiService = aiService;
    private readonly Admin.NET.Ai.Options.CompressionConfig _config = configOptions.Value;

    /// <summary>
    /// 预压缩阈值 (总上下文占比)
    /// </summary>
    private const double PreCompressThreshold = 0.65;

    /// <summary>
    /// 完整压缩阈值 (总上下文占比)
    /// </summary>
    private const double FullCompressThreshold = 0.80;

    /// <summary>
    /// 最少保留的最近消息数
    /// </summary>
    private const int MinPreserveCount = 4;

    /// <summary>
    /// 最多保留的最近消息数
    /// </summary>
    private const int MaxPreserveCount = 10;

    /// <summary>
    /// 预压缩时保留工具结果的最近消息数
    /// </summary>
    private const int ToolResultKeepRecent = 6;

    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var msgList = messages.ToList();
        var totalTokens = EstimateTokens(msgList);
        var contextLength = _config.TokenCountThreshold > 0 ? _config.TokenCountThreshold : 128000;
        var ratio = (double)totalTokens / contextLength;

        // 情况 1: 未达阈值，不压缩
        if (ratio < PreCompressThreshold)
            return msgList;

        // 情况 2: 达到预压缩阈值但未达完整压缩 — 轻量清理
        if (ratio < FullCompressThreshold)
            return PreCompressMessages(msgList);

        // 情况 3: 达到完整压缩阈值 — 三区保护压缩
        return await FullCompressAsync(msgList, ct);
    }

    #region 预压缩（不调 LLM）

    /// <summary>
    /// 轻量预压缩: 清理旧消息中的大块工具结果
    /// </summary>
    private List<ChatMessage> PreCompressMessages(List<ChatMessage> messages)
    {
        if (messages.Count <= ToolResultKeepRecent)
            return messages;

        var cutoff = messages.Count - ToolResultKeepRecent;
        var result = new List<ChatMessage>();

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];

            // 最近的消息保持原样
            if (i >= cutoff)
            {
                result.Add(msg);
                continue;
            }

            // 清理旧消息中的大块 FunctionResultContent
            if (msg.Contents.Any(c => c is FunctionResultContent))
            {
                var newContents = new List<AIContent>();
                foreach (var content in msg.Contents)
                {
                    if (content is FunctionResultContent frc)
                    {
                        var text = frc.Result?.ToString() ?? "";
                        if (text.Length > 200)
                        {
                            // 替换为占位符
                            newContents.Add(new FunctionResultContent(frc.CallId, "[已压缩的工具输出]"));
                        }
                        else
                        {
                            newContents.Add(content);
                        }
                    }
                    else
                    {
                        newContents.Add(content);
                    }
                }
                result.Add(new ChatMessage(msg.Role, newContents));
            }
            else
            {
                result.Add(msg);
            }
        }

        return result;
    }

    #endregion

    #region 完整三区压缩

    /// <summary>
    /// 完整的三区保护压缩
    /// </summary>
    private async Task<List<ChatMessage>> FullCompressAsync(
        List<ChatMessage> messages, CancellationToken ct)
    {
        var originalCount = messages.Count;

        // 自适应计算保留数量
        var preserveCount = Math.Min(
            MaxPreserveCount,
            Math.Max(MinPreserveCount, originalCount / 5));

        // 消息太少不压缩
        if (originalCount <= preserveCount + 2)
            return messages;

        // --- Zone A: 找到用户首条真实消息 ---
        var zoneA = FindOriginalTaskMessage(messages);
        var zoneACount = zoneA != null ? 1 : 0;

        // --- Zone B: 最后 preserveCount 条消息（工具边界感知）---
        var zoneBStart = Math.Max(zoneACount, originalCount - preserveCount);
        zoneBStart = FindCleanBoundary(messages, zoneBStart, zoneACount);
        var zoneB = messages.Skip(zoneBStart).ToList();

        // --- 压缩区: Zone A 和 Zone B 之间 ---
        var compressionStart = zoneACount;
        var compressionEnd = zoneBStart;
        var toCompress = messages.Skip(compressionStart).Take(compressionEnd - compressionStart).ToList();

        if (toCompress.Count == 0)
            return messages;

        // 序列化并调用 LLM 摘要
        var serialized = SerializeMessages(toCompress);
        string summary;
        try
        {
            var prompt = $"""
                你是对话压缩专家。请将以下 {toCompress.Count} 条对话消息压缩为一段简洁的摘要。

                要求:
                1. 保留所有关键决定、代码变更、重要发现
                2. 保留所有文件路径、函数名、类名等技术细节
                3. 保留工具调用的关键结果(成功/失败及输出要点)
                4. 去除重复和冗余内容
                5. 使用原文语言

                对话内容:
                {serialized}
                """;

            var options = new Dictionary<string, object?> { { "SkipCompression", true } };
            summary = await _aiService.ExecuteAsync<string>(prompt, options) ?? "摘要生成失败";
        }
        catch (Exception ex)
        {
            summary = $"摘要生成失败: {ex.Message}";
        }

        // 组装结果: Zone A + 摘要 + Zone B
        var result = new List<ChatMessage>();
        if (zoneA != null)
            result.Add(zoneA);

        result.Add(new ChatMessage(ChatRole.System,
            $"[对话摘要 - 压缩了 {toCompress.Count} 条消息]\n{summary}"));

        result.AddRange(zoneB);

        // 修复孤儿工具块
        result = SanitizeOrphanedToolBlocks(result);

        return result;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 找到用户首条真实消息（不是工具结果）
    /// </summary>
    private static ChatMessage? FindOriginalTaskMessage(List<ChatMessage> messages)
    {
        foreach (var msg in messages)
        {
            if (msg.Role != ChatRole.User) continue;
            // 跳过纯工具结果消息
            if (msg.Contents.All(c => c is FunctionResultContent)) continue;
            // 有文本内容的才是真正的用户消息
            if (msg.Contents.Any(c => c is TextContent))
                return msg;
        }
        return null;
    }

    /// <summary>
    /// 找到不切断工具调用对的干净边界
    /// </summary>
    private static int FindCleanBoundary(List<ChatMessage> messages, int initialStart, int minStart)
    {
        var start = initialStart;

        for (int attempt = 0; attempt < 10 && start > minStart; attempt++)
        {
            // 收集 Zone B 中所有 FunctionCallContent 的 CallId
            var zoneBCallIds = new HashSet<string>();
            for (int i = start; i < messages.Count; i++)
            {
                foreach (var content in messages[i].Contents)
                {
                    if (content is FunctionCallContent fcc && !string.IsNullOrEmpty(fcc.CallId))
                        zoneBCallIds.Add(fcc.CallId);
                }
            }

            // 检查 Zone B 中是否有 FunctionResultContent 引用了 Zone B 外的 CallId
            bool hasOrphan = false;
            for (int i = start; i < messages.Count; i++)
            {
                foreach (var content in messages[i].Contents)
                {
                    if (content is FunctionResultContent frc &&
                        !string.IsNullOrEmpty(frc.CallId) &&
                        !zoneBCallIds.Contains(frc.CallId))
                    {
                        hasOrphan = true;
                        break;
                    }
                }
                if (hasOrphan) break;
            }

            if (!hasOrphan) return start;

            // 向前扩展 2 条（一对 assistant tool_use + user tool_result）
            start = Math.Max(minStart, start - 2);
        }

        return start;
    }

    /// <summary>
    /// 修复压缩后断裂的 FunctionCall/FunctionResult 配对
    /// </summary>
    private static List<ChatMessage> SanitizeOrphanedToolBlocks(List<ChatMessage> messages)
    {
        // 收集所有 CallId
        var callIds = new HashSet<string>();
        var resultCallIds = new HashSet<string>();

        foreach (var msg in messages)
        {
            foreach (var content in msg.Contents)
            {
                if (content is FunctionCallContent fcc && !string.IsNullOrEmpty(fcc.CallId))
                    callIds.Add(fcc.CallId);
                if (content is FunctionResultContent frc && !string.IsNullOrEmpty(frc.CallId))
                    resultCallIds.Add(frc.CallId);
            }
        }

        // 找孤儿
        var orphanedCalls = callIds.Except(resultCallIds).ToHashSet();
        var orphanedResults = resultCallIds.Except(callIds).ToHashSet();

        if (orphanedCalls.Count == 0 && orphanedResults.Count == 0)
            return messages;

        // 将孤儿转为文本
        return messages.Select(msg =>
        {
            var newContents = new List<AIContent>();
            bool changed = false;

            foreach (var content in msg.Contents)
            {
                if (content is FunctionCallContent fcc &&
                    !string.IsNullOrEmpty(fcc.CallId) &&
                    orphanedCalls.Contains(fcc.CallId))
                {
                    changed = true;
                    newContents.Add(new TextContent($"[历史工具调用: {fcc.Name}]"));
                }
                else if (content is FunctionResultContent frc &&
                         !string.IsNullOrEmpty(frc.CallId) &&
                         orphanedResults.Contains(frc.CallId))
                {
                    changed = true;
                    var resultText = frc.Result?.ToString() ?? "";
                    var truncated = resultText.Length > 200 ? resultText[..200] + "..." : resultText;
                    newContents.Add(new TextContent($"[历史工具结果: {truncated}]"));
                }
                else
                {
                    newContents.Add(content);
                }
            }

            return changed ? new ChatMessage(msg.Role, newContents) : msg;
        }).ToList();
    }

    /// <summary>
    /// 序列化消息为可读文本
    /// </summary>
    private static string SerializeMessages(List<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            var role = msg.Role.Value.ToUpper();
            var text = msg.Text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                // 截断过长的单条消息
                if (text.Length > 800)
                    text = text[..800] + $"\n... [已截断, 共 {text.Length} 字符]";
                sb.AppendLine($"[{role}]: {text}");
            }

            // 记录工具调用
            foreach (var content in msg.Contents)
            {
                if (content is FunctionCallContent fcc)
                    sb.AppendLine($"[{role}]: 调用工具 {fcc.Name}(...)");
                else if (content is FunctionResultContent frc)
                {
                    var result = frc.Result?.ToString() ?? "";
                    var truncated = result.Length > 500 ? result[..500] + "..." : result;
                    sb.AppendLine($"[{role}]: 工具结果: {truncated}");
                }
            }
        }
        return sb.ToString();
    }

    private static int EstimateTokens(List<ChatMessage> messages)
    {
        int chars = 0;
        foreach (var m in messages)
        {
            var text = m.Text;
            if (text != null) chars += text.Length;
            // 工具内容也计入
            foreach (var c in m.Contents)
            {
                if (c is FunctionResultContent frc)
                    chars += frc.Result?.ToString()?.Length ?? 0;
            }
        }
        return chars / 2; // 粗略估算
    }

    #endregion
}
