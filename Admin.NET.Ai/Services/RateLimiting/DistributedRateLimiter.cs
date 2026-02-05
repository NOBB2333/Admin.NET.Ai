using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Admin.NET.Ai.Services.RateLimiting;

/// <summary>
/// 分布式令牌桶限流器 (Redis-backed)
/// 使用 LLMCostControlConfig.RateLimiting 配置
/// </summary>
public class DistributedRateLimiter : IRateLimiter
{
    private readonly IDistributedCache _cache;
    private readonly RateLimitConfig _config;
    private readonly ILogger<DistributedRateLimiter> _logger;

    public DistributedRateLimiter(
        IDistributedCache cache,
        IOptions<LLMCostControlConfig> options,
        ILogger<DistributedRateLimiter> logger)
    {
        _cache = cache;
        _config = options.Value.RateLimiting;
        _logger = logger;
    }

    public async Task<bool> CheckLimitAsync(string key)
    {
        var cacheKey = $"ratelimit:{key}";
        var tokenStr = await _cache.GetStringAsync(cacheKey);
        int tokens = tokenStr != null ? int.Parse(tokenStr) : _config.BucketCapacity;
        return tokens > 0;
    }

    public async Task RecordRequestAsync(string key)
    {
        var cacheKey = $"ratelimit:{key}";
        var tokenStr = await _cache.GetStringAsync(cacheKey);
        int tokens = tokenStr != null ? int.Parse(tokenStr) : _config.BucketCapacity;
        
        if (tokens > 0)
        {
            tokens--;
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            };
            await _cache.SetStringAsync(cacheKey, tokens.ToString(), cacheOptions);
        }
    }
}
