using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 关键词优先保护缩减器 - 实现 MEAI IChatReducer
/// 对应需求: 5.4 关键词优先保留
/// </summary>
public class KeywordAwareReducer(
    IOptions<Admin.NET.Ai.Options.CompressionConfig> configOptions) : IChatReducer
{
    private readonly Admin.NET.Ai.Options.CompressionConfig _config = configOptions.Value;

    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        
        var systemMessages = messageList.Where(m => m.Role == ChatRole.System).ToList();
        var userAssistantMessages = messageList.Where(m => m.Role != ChatRole.System).ToList();

        var (criticalMessages, normalMessages) = ClassifyMessages(userAssistantMessages);
        
        // 组装结果
        var result = new List<ChatMessage>();
        result.AddRange(systemMessages);
        result.AddRange(criticalMessages);
        
        // 对普通消息应用压缩（此处简化为截断）
        int maxTotal = _config.MessageCountThreshold;
        int currentCount = result.Count;
        int spaceLeft = maxTotal - currentCount;
        
        if (spaceLeft > 0 && normalMessages.Count > 0)
        {
            // 保留最近的 normal messages
            result.AddRange(normalMessages.TakeLast(spaceLeft));
        }

        // 按原始顺序排序
        var finalSet = result.ToHashSet();
        var orderedResult = messageList.Where(m => finalSet.Contains(m)).ToList();
        
        return Task.FromResult<IEnumerable<ChatMessage>>(orderedResult);
    }
    
    private (List<ChatMessage> critical, List<ChatMessage> normal) ClassifyMessages(List<ChatMessage> messages)
    {
        var critical = new List<ChatMessage>();
        var normal = new List<ChatMessage>();
        var keywords = _config.CriticalKeywords ?? Array.Empty<string>();

        foreach (var message in messages)
        {
            var text = message.Text;
            if (string.IsNullOrEmpty(text))
            {
                normal.Add(message);
                continue;
            }

            if (keywords.Any(keyword => 
                text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
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
