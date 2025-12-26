using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Admin.NET.Ai.Storage;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Storage;

/// <summary>
/// 混合聊天消息存储 (Redis + Database)
/// </summary>
public class HybridChatMessageStore : ChatMessageStoreBase
{
    private readonly RedisChatMessageStore _redisStore;
    private readonly DatabaseChatMessageStore _dbStore;
    private readonly ILogger<HybridChatMessageStore> _logger;

    public HybridChatMessageStore(
        RedisChatMessageStore redisStore,
        DatabaseChatMessageStore dbStore,
        ILogger<HybridChatMessageStore> logger)
    {
        _redisStore = redisStore;
        _dbStore = dbStore;
        _logger = logger;
    }

    public override async Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // 1. 尝试从 Redis 获取 (热数据)
        try 
        {
            var cachedHistory = await _redisStore.GetHistoryAsync(sessionId, cancellationToken);
            if (cachedHistory != null && cachedHistory.Count > 0)
            {
                _logger.LogDebug("🔥 [HybridStore] Redis 命中: {SessionId}", sessionId);
                return cachedHistory;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis 读取失败，降级到 DB");
        }

        // 2. 从数据库获取 (冷数据)
        var dbHistory = await _dbStore.GetHistoryAsync(sessionId, cancellationToken);
        
        // 3. 回填 Redis
        if (dbHistory.Count > 0)
        {
            // 此处需要批量保存接口，或简单的循环保存 (暂略)
            // await _redisStore.SaveHistoryAsync(sessionId, dbHistory); 
        }

        return dbHistory;
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default)
    {
        // 双写：先写 DB 保证持久化，再写 Redis 保证高性能
        await _dbStore.SaveMessageAsync(sessionId, message, cancellationToken);
        
        try
        {
            await _redisStore.SaveMessageAsync(sessionId, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis 写入失败: {SessionId}", sessionId);
            // Redis 失败不影响主流程
        }
    }

    public override async Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(
            _redisStore.ClearHistoryAsync(sessionId, cancellationToken),
            _dbStore.ClearHistoryAsync(sessionId, cancellationToken)
        );
    }
}
