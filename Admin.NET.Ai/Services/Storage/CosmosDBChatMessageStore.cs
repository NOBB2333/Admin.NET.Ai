using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Cosmos DB 存储实现（MEAI-first，继承基类）
/// </summary>
public class CosmosDBChatMessageStore : ChatMessageStoreBase
{
    public CosmosDBChatMessageStore()
    {
    }

    public override async Task<IList<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // Query Cosmos DB
        return await Task.FromResult<IList<ChatMessage>>(new List<ChatMessage>());
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        // Upsert Item to Cosmos
        await Task.CompletedTask;
    }

    public override async Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // Delete items / partition
        await Task.CompletedTask;
    }
}
