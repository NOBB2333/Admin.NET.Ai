using System.Collections.Concurrent;
using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Admin.NET.Ai.Services.RateLimiting;

/// <summary>
/// 基于令牌桶的简单速率限制器实现 (内存版)
/// 使用 LLMCostControlConfig.RateLimiting 配置
/// </summary>
public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, UserBucket> _buckets = new();
    private readonly ILogger<TokenBucketRateLimiter> _logger;
    private readonly RateLimitConfig _config;

    private record UserBucket(int Tokens, DateTime LastRefill);

    public TokenBucketRateLimiter(
        ILogger<TokenBucketRateLimiter> logger,
        IOptions<LLMCostControlConfig> options)
    {
        _logger = logger;
        _config = options.Value.RateLimiting;
    }

    public Task<bool> CheckLimitAsync(string userId)
    {
        var bucket = _buckets.GetOrAdd(userId, _ => new UserBucket(_config.BucketCapacity, DateTime.UtcNow));
        
        // 补充令牌
        var now = DateTime.UtcNow;
        var timeSinceRefill = now - bucket.LastRefill;
        var tokensToAdd = (int)(timeSinceRefill.TotalSeconds * _config.TokensPerSecond);
        
        if (tokensToAdd > 0)
        {
            var newTokens = Math.Min(_config.BucketCapacity, bucket.Tokens + tokensToAdd);
            _buckets[userId] = bucket with { Tokens = newTokens, LastRefill = now };
        }

        var currentBucket = _buckets[userId];
        return Task.FromResult(currentBucket.Tokens > 0);
    }

    public Task RecordRequestAsync(string userId)
    {
        _buckets.AddOrUpdate(userId,
            _ => new UserBucket(_config.BucketCapacity - 1, DateTime.UtcNow),
            (_, bucket) => bucket with { Tokens = Math.Max(0, bucket.Tokens - 1) });
        
        return Task.CompletedTask;
    }
}
