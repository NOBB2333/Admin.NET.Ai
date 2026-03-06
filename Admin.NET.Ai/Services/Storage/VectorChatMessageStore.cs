using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Rag;
using Admin.NET.Ai.Services.RAG;
using Admin.NET.Ai.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Admin.NET.Ai.Services.Storage;

/// <summary>
/// 向量数据库存储实现 (MEAI-first, 对接 VectorSearchProvider)
/// </summary>
public class VectorChatMessageStore : ChatMessageStoreBase
{
    private readonly ITextSearchProvider _searchProvider;
    private readonly FileChatMessageStore _fallbackStore;
    private readonly ILogger<VectorChatMessageStore> _logger;
    private int _clearWarningState;

    public VectorChatMessageStore(
        ITextSearchProvider searchProvider,
        FileChatMessageStore fallbackStore,
        ILogger<VectorChatMessageStore> logger)
    {
        _searchProvider = searchProvider;
        _fallbackStore = fallbackStore;
        _logger = logger;
    }

    public override Task<IList<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // 向量库用于语义检索，历史消息使用文件存储兜底，避免“查不到历史”。
        return _fallbackStore.GetHistoryAsync(sessionId, cancellationToken);
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        // 先写兜底存储，保证历史可恢复。
        await _fallbackStore.SaveMessageAsync(sessionId, message, cancellationToken);

        var docId = GenerateId(sessionId, message);
        
        var doc = new Document
        {
            Content = $"{message.Role}: {message.Text}",
            Metadata = new Dictionary<string, object>
            {
                { "SessionId", sessionId },
                { "Role", message.Role.Value },
                { "Timestamp", DateTime.UtcNow },
                { "DocId", docId }
            }
        };

        try
        {
            await _searchProvider.ChunkAndIndexAsync([doc]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vector 索引写入失败，已保留消息到兜底存储: {SessionId}", sessionId);
        }
    }

    public override async Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _fallbackStore.ClearHistoryAsync(sessionId, cancellationToken);

        if (Interlocked.Exchange(ref _clearWarningState, 1) == 0)
        {
            _logger.LogWarning("VectorChatMessageStore 尚未实现按 Session 清理向量索引，仅清理了兜底历史。");
        }
    }

    public override Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        return base.SaveMessagesAsync(sessionId, messages, cancellationToken);
    }

    public override Task ReplaceHistoryAsync(string sessionId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        return _fallbackStore.ReplaceHistoryAsync(sessionId, messages, cancellationToken);
    }

    /// <summary>
    /// 语义搜索历史记录
    /// </summary>
    public async Task<IEnumerable<string>> SearchAsync(string query, string sessionId, CancellationToken cancellationToken = default)
    {
        var options = new SearchOptions 
        { 
            MaxResults = 5, 
            MinScore = 0.7,
            Filters = new Dictionary<string, object> { { "SessionId", sessionId } }
        };

        var results = await _searchProvider.SearchAsync(query, options);
        return results.Results.Select(r => r.Text);
    }

    private string GenerateId(string sessionId, ChatMessage message)
    {
        var input = $"{sessionId}:{message.Role}:{message.Text}:{DateTime.UtcNow.Ticks}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
