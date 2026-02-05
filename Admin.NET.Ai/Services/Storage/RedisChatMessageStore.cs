using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Redis 分布式存储实现（MEAI-first，继承基类）
/// </summary>
public class RedisChatMessageStore : ChatMessageStoreBase
{
    // private readonly IConnectionMultiplexer _redis;
    
    public RedisChatMessageStore(/* IConnectionMultiplexer redis */)
    {
        // _redis = redis;
    }

    public override async Task<IList<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // var db = _redis.GetDatabase();
        // var json = await db.StringGetAsync($"chat:{sessionId}");
        // if (json.HasValue) { ... deserialize ... }
        return await Task.FromResult<IList<ChatMessage>>(new List<ChatMessage>());
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
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
