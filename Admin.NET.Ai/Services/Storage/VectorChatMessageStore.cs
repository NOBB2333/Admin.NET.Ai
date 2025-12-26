using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Rag;
using Admin.NET.Ai.Services.RAG; // SearchOptions, Document
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Admin.NET.Ai.Storage; // For ChatMessageStoreBase

namespace Admin.NET.Ai.Services.Storage;

/// <summary>
/// 向量数据库存储实现 (对接 VectorSearchProvider)
/// </summary>
public class VectorChatMessageStore : ChatMessageStoreBase
{
    private readonly ITextSearchProvider _searchProvider; // Use Interface!

    public VectorChatMessageStore(ITextSearchProvider searchProvider)
    {
        _searchProvider = searchProvider;
    }

    public override async Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // Vector Store 通常不适合作为全量历史记录的 Source of Truth，因为排序可能不可靠。
        // 但这里为了演示，我们通过搜索该 sessionId 的所有记录来重建
        // 建议实践：仅将 Vector Store 用于 "相关历史检索"，而主历史记录使用 DB/Redis。
        
        // 模拟：VectorSearchProvider 目前接口不支持 "GetAllByFilter"，所以这里只是一个空实现或依赖 Search。
        // 为了完整性，我们假设有一个专门的方法 (这里简化返回空，因为 VectorSearchProvider 主要是 Search)
        return await Task.FromResult(new ChatHistory());
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default)
    {
        // 将聊天记录作为文档块索引
        var docId = GenerateId(sessionId, message);
        
        // Ensure using the correct Document type from Admin.NET.Ai.Services.RAG
        var doc = new Admin.NET.Ai.Services.RAG.Document
        {
            Content = $"{message.Role}: {message.Content}",
            Metadata = new Dictionary<string, object>
            {
                { "SessionId", sessionId },
                { "Role", message.Role.ToString() },
                { "Timestamp", DateTime.UtcNow },
                { "DocId", docId }
            }
        };

        // 使用 ChunkAndIndexAsync (虽然只需要一块)
        await _searchProvider.ChunkAndIndexAsync(new[] { doc });
    }

    public override async Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // VectorSearchProvider 需要扩展 DeleteByFilter
        // 目前暂不支持清理特定 Session
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
            Filters = new Dictionary<string, object> { { "SessionId", sessionId } } // 需要 Provider 支持 Filter
        };

        var results = await _searchProvider.SearchAsync(query, options);
        return results.Results.Select(r => r.Text);
    }

    private string GenerateId(string sessionId, ChatMessageContent message)
    {
        var input = $"{sessionId}:{message.Role}:{message.Content}:{DateTime.UtcNow.Ticks}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
