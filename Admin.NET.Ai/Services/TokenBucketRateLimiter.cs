using System.Collections.Concurrent;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services;

/// <summary>
/// 基于令牌桶的简单速率限制器实现
/// </summary>
public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, UserBucket> _buckets = new();
    private readonly ILogger<TokenBucketRateLimiter> _logger;
    
    // 默认配置
    private readonly int _maxTokens = 100; // 最大令牌数
    private readonly int _refillRate = 10; // 每秒补充令牌数
    private readonly TimeSpan _refillInterval = TimeSpan.FromSeconds(1);

    private record UserBucket(int Tokens, DateTime LastRefill);

    public TokenBucketRateLimiter(ILogger<TokenBucketRateLimiter> logger)
    {
        _logger = logger;
    }

    public Task<bool> CheckLimitAsync(string userId)
    {
        var bucket = _buckets.GetOrAdd(userId, _ => new UserBucket(_maxTokens, DateTime.UtcNow));
        
        // 补充令牌
        var now = DateTime.UtcNow;
        var timeSinceRefill = now - bucket.LastRefill;
        var tokensToAdd = (int)(timeSinceRefill.TotalSeconds * _refillRate);
        
        if (tokensToAdd > 0)
        {
            var newTokens = Math.Min(_maxTokens, bucket.Tokens + tokensToAdd);
            _buckets[userId] = bucket with { Tokens = newTokens, LastRefill = now };
        }

        var currentBucket = _buckets[userId];
        return Task.FromResult(currentBucket.Tokens > 0);
    }

    public Task RecordRequestAsync(string userId)
    {
        _buckets.AddOrUpdate(userId,
            _ => new UserBucket(_maxTokens - 1, DateTime.UtcNow),
            (_, bucket) => bucket with { Tokens = Math.Max(0, bucket.Tokens - 1) });
        
        return Task.CompletedTask;
    }
}
