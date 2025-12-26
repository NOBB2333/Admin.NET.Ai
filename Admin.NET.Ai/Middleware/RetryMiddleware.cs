using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 智能重试中间件 (基于 DelegatingChatClient, Polly 8.x)
/// </summary>
public class RetryMiddleware : DelegatingChatClient
{
    private readonly ILogger<RetryMiddleware> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    public RetryMiddleware(IChatClient innerClient, ILogger<RetryMiddleware> logger) 
        : base(innerClient)
    {
        _logger = logger;

        // Polly 8.x 使用 ResiliencePipeline
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .Handle<IOException>()
                    .Handle<Exception>(ex => IsTransientError(ex)),
                OnRetry = args =>
                {
                    _logger.LogWarning("⚠️ 瞬态错误导致重试 [{RetryCount}/3] - 等待 {Duration}ms - 错误: {Message}", 
                        args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return default;
                }
            })
            .Build();
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async ct => 
            await base.GetResponseAsync(chatMessages, options, ct), cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 流式重试比较复杂，如果已经开始返回数据则不能重试
        // 这里仅对连接建立阶段进行重试。一旦 yield return 开始，不能重试整个流
        // 暂不实现复杂的流式重试，仅透传。建议流式错误由客户端处理。
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private static bool IsTransientError(Exception ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        return msg.Contains("429") || 
               msg.Contains("too many requests") || 
               msg.Contains("500") || 
               msg.Contains("502") || 
               msg.Contains("503") || 
               msg.Contains("504") ||
               msg.Contains("server busy") ||
               msg.Contains("timeout");
    }
}


