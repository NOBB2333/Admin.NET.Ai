using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 消息计数缩减器 (保留最近 N 条)
/// 对应需求: 5.1 消息计数压缩器
/// </summary>
/// <summary>
/// 消息计数缩减器 (保留最近 N 条)
/// 对应需求: 5.1 消息计数压缩器
/// </summary>
public class MessageCountingReducer(Microsoft.Extensions.Options.IOptions<Admin.NET.Ai.Options.CompressionConfig> configOptions) : IChatReducer
{
    private readonly Admin.NET.Ai.Options.CompressionConfig _config = configOptions.Value;

    public Task<IEnumerable<ChatMessageContent>> ReduceAsync(IEnumerable<ChatMessageContent> messages, CancellationToken ct = default)
    {
        var msgList = messages.ToList();
        // 使用配置中的阈值作为保留数量的基准，或者默认保留一部分
        // 这里假设 MessageCountThreshold 是触发阈值，我们保留该阈值数量的消息
        int maxMessageCount = _config.MessageCountThreshold;

        if (msgList.Count <= maxMessageCount)
        {
            return Task.FromResult<IEnumerable<ChatMessageContent>>(msgList);
        }

        // 策略:
        // 1. 总是保留 System Message (通常在头部)
        // 2. 保留最近的 (maxMessageCount - SystemMessagesCount) 条

        var systemMessages = msgList.Where(m => m.Role == AuthorRole.System).ToList();
        
        // 计算剩余可用槽位
        var slotsForOthers = maxMessageCount - systemMessages.Count;
        if (slotsForOthers < 0) slotsForOthers = 0;

        // 获取非 System 消息并保留最近的 N 条
        var otherMessages = msgList.Where(m => m.Role != AuthorRole.System);
        var keptOthers = otherMessages.TakeLast(slotsForOthers);

        var result = new List<ChatMessageContent>();
        result.AddRange(systemMessages);
        result.AddRange(keptOthers);

        return Task.FromResult<IEnumerable<ChatMessageContent>>(result);
    }
}
