using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Options;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 关键词优先保护缩减器
/// 对应需求: 5.4 关键词优先保留
/// </summary>
public class KeywordAwareReducer(
    IOptions<Admin.NET.Ai.Options.CompressionConfig> configOptions) : IChatReducer
{
    private readonly Admin.NET.Ai.Options.CompressionConfig _config = configOptions.Value;

    public async Task<IEnumerable<ChatMessageContent>> ReduceAsync(IEnumerable<ChatMessageContent> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        
        // 分离包含关键词的消息和普通消息
        // 注意：System 消息通常也需要保护，这里假设 System 消息可能包含也可能不包含，策略上我们建议总是由于普通消息。
        // 为了安全起见，我们对 Role != System 的消息进行筛选。System 消息由外部或组合策略处理，或者我们在这里显式保留。
        
        var systemMessages = messageList.Where(m => m.Role == AuthorRole.System).ToList();
        var userAssistantMessages = messageList.Where(m => m.Role != AuthorRole.System).ToList();

        var (criticalMessages, normalMessages) = ClassifyMessages(userAssistantMessages);
        
        // 组装结果
        var result = new List<ChatMessageContent>();
        result.AddRange(systemMessages);
        result.AddRange(criticalMessages);
        
        // 对普通消息应用压缩（此处简化为截断，若需复杂压缩可注入 InnerReducer）
        // 假设我们允许的最大总消息数是 Threshold
        int maxTotal = _config.MessageCountThreshold;
        int currentCount = result.Count;
        int spaceLeft = maxTotal - currentCount;
        
        if (spaceLeft > 0 && normalMessages.Count > 0)
        {
            // 保留最近的 normal messages
            result.AddRange(normalMessages.TakeLast(spaceLeft));
        }

        // 也可以选择在这里对 Result 再次按原始顺序排序，以防打乱上下文（虽然 Critical 往往是旧的）
        // 若业务对顺序敏感（LLM 通常敏感），我们需要把 result 里的消息按原始出现顺序重排。
        // 简单方式：通过 messageList 对照。
        var finalSet = result.ToHashSet();
        var orderedResult = messageList.Where(m => finalSet.Contains(m)).ToList();
        
        return await Task.FromResult(orderedResult);
    }
    
    private (List<ChatMessageContent> critical, List<ChatMessageContent> normal) ClassifyMessages(List<ChatMessageContent> messages)
    {
        var critical = new List<ChatMessageContent>();
        var normal = new List<ChatMessageContent>();
        var keywords = _config.CriticalKeywords ?? Array.Empty<string>();

        foreach (var message in messages)
        {
            if (string.IsNullOrEmpty(message.Content))
            {
                normal.Add(message);
                continue;
            }

            if (keywords.Any(keyword => 
                message.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                critical.Add(message);
            }
            else
            {
                normal.Add(message);
            }
        }
        
        return (critical, normal);
    }
}
