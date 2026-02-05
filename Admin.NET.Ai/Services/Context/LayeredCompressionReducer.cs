using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using System.Text;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 分层压缩策略 - 实现 MEAI IChatReducer
/// 对应需求: 5.8 分层压缩策略
/// </summary>
public class LayeredCompressionReducer(
    IAiService aiService,
    FunctionCallPreservationReducer functionReducer
    ) : IChatReducer
{
    private readonly IAiService _aiService = aiService;
    private readonly FunctionCallPreservationReducer _functionReducer = functionReducer;

    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        
        if (messageList.Count <= 15) return messageList;

        var systemMsgs = messageList.Where(m => m.Role == ChatRole.System).ToList();
        var recentMsgs = messageList.TakeLast(5).ToList();
        
        var middleCandidates = messageList
            .Except(systemMsgs)
            .Except(recentMsgs)
            .ToList();

        var toSummarize = new List<ChatMessage>();
        var toKeepMiddle = new List<ChatMessage>();
        
        foreach (var msg in middleCandidates)
        {
            bool isFunc = msg.Contents.Any(i => i is FunctionCallContent || i is FunctionResultContent);
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
        
        var result = new List<ChatMessage>();
        result.AddRange(systemMsgs);
        if (summaryMsg != null) result.Add(summaryMsg);
        result.AddRange(toKeepMiddle);
        result.AddRange(recentMsgs);
        
        var final = messageList.Where(m => result.Contains(m) || m == summaryMsg).ToList();
        
        if (summaryMsg != null && !final.Contains(summaryMsg))
        {
            int lastSysIndex = final.FindLastIndex(m => m.Role == ChatRole.System);
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

    private async Task<ChatMessage?> GenerateSummaryAsync(List<ChatMessage> messages)
    {
        if (messages.Count == 0) return null;
        
        var sb = new StringBuilder();
        foreach (var m in messages)
        {
            sb.AppendLine($"{m.Role}: {m.Text}");
        }
        
        var prompt = $"Summarize these middle conversation usage contexts concisely:\n{sb}";
        var opts = new Dictionary<string, object?> { { "SkipCompression", true } };
        string summary = await _aiService.ExecuteAsync<string>(prompt, opts) ?? "";
        
        if (string.IsNullOrWhiteSpace(summary)) return null;
        
        return new ChatMessage(ChatRole.System, $"[Middle Summary]: {summary}");
    }
}
