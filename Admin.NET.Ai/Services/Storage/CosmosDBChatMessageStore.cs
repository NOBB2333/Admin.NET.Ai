using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Cosmos DB 存储实现（MEAI-first，继承基类）
/// </summary>
public class CosmosDBChatMessageStore : ChatMessageStoreBase
{
    private readonly FileChatMessageStore _fallbackStore;
    private readonly ILogger<CosmosDBChatMessageStore> _logger;
    private int _fallbackLogState;

    public CosmosDBChatMessageStore(
        FileChatMessageStore fallbackStore,
        ILogger<CosmosDBChatMessageStore> logger)
    {
        _fallbackStore = fallbackStore;
        _logger = logger;
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
            _logger.LogWarning("CosmosDBChatMessageStore 尚未接入真实 Cosmos DB，已自动回退到 FileChatMessageStore。");
        }
    }
}
