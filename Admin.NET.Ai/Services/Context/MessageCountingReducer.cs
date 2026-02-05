using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 消息计数缩减器 (保留最近 N 条) - 实现 MEAI IChatReducer
/// 对应需求: 5.1 消息计数压缩器
/// </summary>
public class MessageCountingReducer(Microsoft.Extensions.Options.IOptions<Admin.NET.Ai.Options.CompressionConfig> configOptions) : IChatReducer
{
    private readonly Admin.NET.Ai.Options.CompressionConfig _config = configOptions.Value;

    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var msgList = messages.ToList();
        int maxMessageCount = _config.MessageCountThreshold;

        if (msgList.Count <= maxMessageCount)
        {
            return Task.FromResult<IEnumerable<ChatMessage>>(msgList);
        }

        // 策略: 保留 System Messages + 最近的 N 条
        var systemMessages = msgList.Where(m => m.Role == ChatRole.System).ToList();
        var slotsForOthers = maxMessageCount - systemMessages.Count;
        if (slotsForOthers < 0) slotsForOthers = 0;

        var otherMessages = msgList.Where(m => m.Role != ChatRole.System);
        var keptOthers = otherMessages.TakeLast(slotsForOthers);

        var result = new List<ChatMessage>();
        result.AddRange(systemMessages);
        result.AddRange(keptOthers);

        return Task.FromResult<IEnumerable<ChatMessage>>(result);
    }
}
