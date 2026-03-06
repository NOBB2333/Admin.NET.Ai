using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Redis 分布式存储实现（MEAI-first，继承基类）
/// </summary>
public class RedisChatMessageStore : ChatMessageStoreBase
{
    // private readonly IConnectionMultiplexer _redis;
    private readonly FileChatMessageStore _fallbackStore;
    private readonly ILogger<RedisChatMessageStore> _logger;
    private int _fallbackLogState;
    
    public RedisChatMessageStore(
        FileChatMessageStore fallbackStore,
        ILogger<RedisChatMessageStore> logger
        /* IConnectionMultiplexer redis */)
    {
        _fallbackStore = fallbackStore;
        _logger = logger;
        // _redis = redis;
    }

    public override Task<IList<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        LogFallbackOnce();
        return _fallbackStore.GetHistoryAsync(sessionId, cancellationToken);
    }

    public override Task SaveMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        LogFallbackOnce();
        return _fallbackStore.SaveMessageAsync(sessionId, message, cancellationToken);
    }

    public override Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        LogFallbackOnce();
        return _fallbackStore.ClearHistoryAsync(sessionId, cancellationToken);
    }

    public override Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        LogFallbackOnce();
        return _fallbackStore.SaveMessagesAsync(sessionId, messages, cancellationToken);
    }

    public override Task ReplaceHistoryAsync(string sessionId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        LogFallbackOnce();
        return _fallbackStore.ReplaceHistoryAsync(sessionId, messages, cancellationToken);
    }

    private void LogFallbackOnce()
    {
        if (Interlocked.Exchange(ref _fallbackLogState, 1) == 0)
        {
            _logger.LogWarning("RedisChatMessageStore 尚未接入真实 Redis，已自动回退到 FileChatMessageStore。");
        }
    }
}
