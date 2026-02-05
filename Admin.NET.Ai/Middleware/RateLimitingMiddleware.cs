using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 分布式限流中间件 (基于 DelegatingChatClient)
/// 注意：UserId 从 ChatOptions.AdditionalProperties["UserId"] 获取，
/// 框架不负责从 HttpContext 获取用户信息
/// </summary>
public class RateLimitingMiddleware : DelegatingChatClient
{
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    public RateLimitingMiddleware(
        IChatClient innerClient,
        IRateLimiter rateLimiter, 
        ILogger<RateLimitingMiddleware> logger)
        : base(innerClient)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(options);
        await CheckLimitAsync(userId);
        await _rateLimiter.RecordRequestAsync(userId);
        return await base.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(options);
        await CheckLimitAsync(userId);
        await _rateLimiter.RecordRequestAsync(userId);

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// 从 ChatOptions 获取 UserId
    /// 调用方应通过 ChatOptions.AdditionalProperties["UserId"] 传入
    /// </summary>
    private static string GetUserId(ChatOptions? options)
    {
        if (options?.AdditionalProperties?.TryGetValue("UserId", out var userId) == true 
            && userId is string userIdStr 
            && !string.IsNullOrEmpty(userIdStr))
        {
            return userIdStr;
        }
        return "anonymous";
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
