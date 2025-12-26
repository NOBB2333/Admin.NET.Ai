using Admin.NET.Ai.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 分布式限流中间件 (基于 DelegatingChatClient)
/// </summary>
public class RateLimitingMiddleware : DelegatingChatClient
{
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public RateLimitingMiddleware(
        IChatClient innerClient,
        IRateLimiter rateLimiter, 
        ILogger<RateLimitingMiddleware> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(innerClient)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        await CheckLimitAsync(userId);
        
        await _rateLimiter.RecordRequestAsync(userId); // 简单扣减，最好是 CheckAndConsume 原子操作
        
        return await base.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        await CheckLimitAsync(userId);
        
        await _rateLimiter.RecordRequestAsync(userId);

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private string GetUserId()
    {
        // 优先从 Options 获取 (如果是 Agent 内部调用)，否则从 HttpContext
        // 这里简化为 HttpContext
        var context = _httpContextAccessor.HttpContext;
        var userId = context?.User?.Identity?.Name 
                     ?? context?.Request.Headers["X-User-Id"].ToString();
                     
        return !string.IsNullOrEmpty(userId) ? userId : "anonymous";
    }

    private async Task CheckLimitAsync(string userId)
    {
        if (!await _rateLimiter.CheckLimitAsync(userId))
        {
            _logger.LogWarning("⚠️ 用户 {UserId} 触发限流", userId);
            throw new RateLimitExceededException($"API调用频率超限，请稍后重试");
        }
    }
}


/// <summary>
/// 分布式令牌桶限流器 (Redis-backed)
/// </summary>
public class DistributedRateLimiter : IRateLimiter
{
    private readonly IDistributedCache _cache;
    private readonly int _capacity;
    private readonly int _tokensPerInterval;
    private readonly TimeSpan _interval;

    public DistributedRateLimiter(IDistributedCache cache)
    {
        _cache = cache;
        // 配置通常应来自 IOptions
        _capacity = 20; 
        _tokensPerInterval = 5;
        _interval = TimeSpan.FromMinutes(1);
    }

    public async Task<bool> CheckLimitAsync(string key)
    {
        // 简单实现：使用滑动窗口计数器或令牌桶
        // 为了演示分布式特性，这里使用基于 Cache 的简单计数器 (固定窗口)
        // 生产环境建议使用 Redis+Lua 脚本实现原子性滑动窗口
        
        var cacheKey = $"ratelimit:{key}";
        
        // 获取当前 Token 数
        var tokenStr = await _cache.GetStringAsync(cacheKey);
        int tokens = tokenStr != null ? int.Parse(tokenStr) : _capacity;

        if (tokens > 0)
        {
            return true;
        }
        
        return false;
    }

    public async Task RecordRequestAsync(string key)
    {
        var cacheKey = $"ratelimit:{key}";
        // 原子递减 (需要 Redis 脚本支持，IDistributedCache 不支持原子 decr)
        // 这里模拟逻辑：
        // 实际操作应该使用 ConnectionMultiplexer 的 db.StringDecrementAsync
        
        // 简化版（非严格原子性，仅作演示适配 IDistributedCache）
        var tokenStr = await _cache.GetStringAsync(cacheKey);
        int tokens = tokenStr != null ? int.Parse(tokenStr) : _capacity;
        
        if (tokens > 0)
        {
            tokens--;
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _interval // 窗口重置时间
            };
            await _cache.SetStringAsync(cacheKey, tokens.ToString(), options);
        }
    }
}

// 占位异常
public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}

