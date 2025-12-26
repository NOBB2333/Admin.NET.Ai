using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 分层压缩策略
/// 对应需求: 5.8 分层压缩策略
/// 逻辑:
/// 1. 第一层: 保留 System 和 最近 N 条
/// 2. 第二层: 对中间部分进行摘要
/// 3. 第三层 (可选): 对摘要后的结果再次检查Token，如果还超，则丢弃最旧的摘要或中间层
/// </summary>
public class LayeredCompressionReducer(
    IAiService aiService,
    FunctionCallPreservationReducer functionReducer // 复用函数保护逻辑
    ) : IChatReducer
{
    private readonly IAiService _aiService = aiService;
    private readonly FunctionCallPreservationReducer _functionReducer = functionReducer;

    public async Task<IEnumerable<ChatMessageContent>> ReduceAsync(IEnumerable<ChatMessageContent> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        
        // 如果消息很少，直接返回
        if (messageList.Count <= 15) return messageList;

        // 1. 保留 System
        var systemMsgs = messageList.Where(m => m.Role == AuthorRole.System).ToList();
        
        // 2. 识别并保留 Function Calls (利用 FunctionReducer 的逻辑)
        // 注意：FunctionReducer 会返回 System + Functions + Recent。我们只需要它帮我们挑出 Functions
        // 这里手动实现简化版逻辑更可控：
        
        // 3. 定义分层
        // Layer 1: Recent (最后 5 条)
        var recentMsgs = messageList.TakeLast(5).ToList();
        
        // Layer 2: Middle (中间部分)
        // 排除掉 System 和 Recent
        var middleCandidates = messageList
            .Except(systemMsgs)
            .Except(recentMsgs)
            .ToList();

        // 对中间部分进行摘要，但不能破坏 Function Call 的完整性
        // 简单策略：如果中间部分包含 Function Call，则不摘要该段落，或者只摘要纯文本部分
        // 为了简化，我们只对纯文本 User/Assistant 消息进行摘要
        
        var toSummarize = new List<ChatMessageContent>();
        var toKeepMiddle = new List<ChatMessageContent>();
        
        foreach (var msg in middleCandidates)
        {
            bool isFunc = msg.Items.Any(i => i is FunctionCallContent || i is FunctionResultContent);
            if (isFunc)
            {
                toKeepMiddle.Add(msg);
            }
            else
            {
                toSummarize.Add(msg);
            }
        }

        var summaryMsg = await GenerateSummaryAsync(toSummarize);
        
        // 组装
        var result = new List<ChatMessageContent>();
        result.AddRange(systemMsgs);
        if (summaryMsg != null) result.Add(summaryMsg);
        result.AddRange(toKeepMiddle); // 这部分可能是散落在中间的 Function Call
        result.AddRange(recentMsgs);
        
        // 按实际时间排序
        // 将 result 里的消息按在原列表中的顺序排序
        var final = messageList.Where(m => result.Contains(m) || m == summaryMsg).ToList();
        
        // 如果 Summary 是新生成的，它不在 messageList 里，需要插在 System 之后
        if (summaryMsg != null && final.Contains(summaryMsg) == false)
        {
            // 找到最后一个 System 消息的位置
            int lastSysIndex = final.FindLastIndex(m => m.Role == AuthorRole.System);
            if (lastSysIndex >= 0 && lastSysIndex < final.Count - 1)
            {
                final.Insert(lastSysIndex + 1, summaryMsg);
            }
            else
            {
                final.Insert(0, summaryMsg);
            }
        }

        return final;
    }

    private async Task<ChatMessageContent?> GenerateSummaryAsync(List<ChatMessageContent> messages)
    {
        if (messages.Count == 0) return null;
        
        var sb = new StringBuilder();
        foreach (var m in messages)
        {
            sb.AppendLine($"{m.Role}: {m.Content}");
        }
        
        var prompt = $"Summarize these middle conversation usage contexts concisely:\n{sb}";
        
        // 防止递归配置
        var opts = new Dictionary<string, object?> { { "SkipCompression", true } };
        string summary = await _aiService.ExecuteAsync<string>(prompt, opts) ?? "";
        
        if (string.IsNullOrWhiteSpace(summary)) return null;
        
        return new ChatMessageContent(AuthorRole.System, $"[Middle Summary]: {summary}");
    }
}
