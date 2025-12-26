using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Redis 分布式存储实现（继承基类）
/// </summary>
public class RedisChatMessageStore : ChatMessageStoreBase
{
    // private readonly IConnectionMultiplexer _redis;
    
    public RedisChatMessageStore(/* IConnectionMultiplexer redis */)
    {
        // _redis = redis;
    }

    public override async Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // var db = _redis.GetDatabase();
        // var json = await db.StringGetAsync($"chat:{sessionId}");
        // if (json.HasValue) { ... deserialize ... }
        return await Task.FromResult(new ChatHistory());
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default)
    {
        // var db = _redis.GetDatabase();
        // var item = JsonSerializer.Serialize(message);
        // await db.ListRightPushAsync($"chat_history:{sessionId}", item);
        await Task.CompletedTask;
    }

    public override async Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // var db = _redis.GetDatabase();
        // await db.KeyDeleteAsync($"chat_history:{sessionId}");
        await Task.CompletedTask;
    }
}

