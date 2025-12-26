using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Cosmos DB 存储实现（继承基类）
/// </summary>
public class CosmosDBChatMessageStore : ChatMessageStoreBase
{
    public CosmosDBChatMessageStore()
    {
    }

    public override async Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // Query Cosmos DB
        return await Task.FromResult(new ChatHistory());
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default)
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

