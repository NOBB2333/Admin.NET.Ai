using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using System.Text;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 智能摘要缩减器 - 实现 MEAI IChatReducer
/// 对应需求: 5.2 智能摘要压缩器
/// </summary>
public class SummarizingReducer(IAiService aiService, Microsoft.Extensions.Options.IOptions<Admin.NET.Ai.Options.CompressionConfig> configOptions) : IChatReducer
{
    private readonly IAiService _aiService = aiService;
    private readonly Admin.NET.Ai.Options.CompressionConfig _config = configOptions.Value;

    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var msgList = messages.ToList();
        
        // 使用配置阈值
        if (msgList.Count < _config.MessageCountThreshold)
        {
            return msgList;
        }

        // 策略:
        // 1. 保留 System Message
        // 2. split: [Old Messages to Summarize] + [Recent Messages to Keep]
        // 3. Summarize Old Messages -> 1 Summary Message
        // 4. Result = [System] + [Summary of Old] + [Recent]

        var systemMessages = msgList.Where(m => m.Role == ChatRole.System).ToList();
        var nonSystem = msgList.Where(m => m.Role != ChatRole.System).ToList();

        // 动态计算保留最近消息的数量 (例如保留阈值的 1/3)
        int keepRecent = Math.Max(2, _config.MessageCountThreshold / 3);
        
        if (nonSystem.Count <= keepRecent) return msgList;

        var toSummarize = nonSystem.Take(nonSystem.Count - keepRecent).ToList();
        var toKeep = nonSystem.TakeLast(keepRecent).ToList();

        // 生成摘要
        var sb = new StringBuilder();
        foreach (var msg in toSummarize)
        {
            sb.AppendLine($"{msg.Role}: {msg.Text}");
        }

        // 注入配置中的 Prompt
        var prompt = $"{_config.SummaryPromptTemplate}\n\n{sb}";
        
        // 调用 AI 服务生成摘要
        var options = new Dictionary<string, object?> { { "SkipCompression", true } };
        
        string summaryText;
        try 
        {
            summaryText = await _aiService.ExecuteAsync<string>(prompt, options) ?? "Summary generation returned null.";
        }
        catch (Exception ex)
        {
            summaryText = $"Summary generation failed: {ex.Message}";
        }

        var summaryMessage = new ChatMessage(ChatRole.System, $"[Conversation Summary]: {summaryText}");

        var result = new List<ChatMessage>();
        result.AddRange(systemMessages);
        result.Add(summaryMessage);
        result.AddRange(toKeep);

        return result;
    }
}
