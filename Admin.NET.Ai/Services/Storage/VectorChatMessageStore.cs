using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Rag;
using Admin.NET.Ai.Services.RAG;
using Microsoft.Extensions.AI;
using System.Security.Cryptography;
using System.Text;
using Admin.NET.Ai.Storage;

namespace Admin.NET.Ai.Services.Storage;

/// <summary>
/// 向量数据库存储实现 (MEAI-first, 对接 VectorSearchProvider)
/// </summary>
public class VectorChatMessageStore : ChatMessageStoreBase
{
    private readonly ITextSearchProvider _searchProvider;

    public VectorChatMessageStore(ITextSearchProvider searchProvider)
    {
        _searchProvider = searchProvider;
    }

    public override async Task<IList<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // Vector Store 通常不适合作为全量历史记录的 Source of Truth
        return await Task.FromResult<IList<ChatMessage>>(new List<ChatMessage>());
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
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

        await _searchProvider.ChunkAndIndexAsync(new[] { doc });
    }

    public override async Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // VectorSearchProvider 需要扩展 DeleteByFilter
        await Task.CompletedTask;
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
