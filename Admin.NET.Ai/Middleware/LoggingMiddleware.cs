using Admin.NET.Ai.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 结构化日志中间件 (基于 DelegatingChatClient)
/// </summary>
public class LoggingMiddleware : DelegatingChatClient
{
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly bool _enableSensitiveDataLogging;

    public LoggingMiddleware(
        IChatClient innerClient,
        ILogger<LoggingMiddleware> logger,
        IHttpContextAccessor httpContextAccessor,
        bool enableSensitiveDataLogging = false) // 可从配置中注入
        : base(innerClient)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _enableSensitiveDataLogging = enableSensitiveDataLogging;
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var model = options?.ModelId ?? "default";
        var stopwatch = Stopwatch.StartNew();
        
        LogRequest(requestId, model, chatMessages);

        try
        {
            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();

            LogResponse(requestId, stopwatch.ElapsedMilliseconds, response);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ [Req:{RequestId}] 调用失败. 耗时: {Elapsed}ms", requestId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var model = options?.ModelId ?? "default";
        var stopwatch = Stopwatch.StartNew();

        LogRequest(requestId, model, chatMessages);

        int updateCount = 0;
        
        // 捕获异常需要包裹枚举
        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        try 
        {
             enumerator = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
             stopwatch.Stop();
             _logger.LogError(ex, "❌ [Req:{RequestId}] 流式连接失败. 耗时: {Elapsed}ms", requestId, stopwatch.ElapsedMilliseconds);
             throw;
        }

        await using (enumerator)
        {
            bool hasNext = true;
            while (hasNext)
            {
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [Req:{RequestId}] 流式读取异常", requestId);
                    throw;
                }

                if (hasNext)
                {
                    updateCount++;
                    yield return enumerator.Current;
                }
            }
        }
        
        stopwatch.Stop();
        _logger.LogDebug("⬅️ [Req:{RequestId}] 流式调用结束. 耗时: {Elapsed}ms. Chunks: {Count}", 
            requestId, stopwatch.ElapsedMilliseconds, updateCount);
    }

    private void LogRequest(string requestId, string model, IEnumerable<ChatMessage> messages)
    {
        var userId = GetUserId();
        _logger.LogDebug("➡️ [Req:{RequestId}] 用户: {UserId}, 模型: {Model}. PromptLength: {Length}", 
            requestId, userId, model, messages.Sum(m => m.Text?.Length ?? 0));

        if (_enableSensitiveDataLogging)
        {
            _logger.LogDebug("📝 [Req:{RequestId}] Messages: {Payload}", requestId, JsonSerializer.Serialize(messages));
        }
    }

    private void LogResponse(string requestId, long elapsed, ChatResponse response)
    {
        int inputTokens = (int)(response.Usage?.InputTokenCount ?? 0);
        int outputTokens = (int)(response.Usage?.OutputTokenCount ?? 0);

        _logger.LogDebug("⬅️ [Req:{RequestId}] 完成. 耗时: {Elapsed}ms. Tokens: {In}+{Out}={Total}. Finish: {Reason}",
            requestId, 
            elapsed,
            inputTokens, 
            outputTokens, 
            inputTokens + outputTokens,
            response.FinishReason);

        if (_enableSensitiveDataLogging)
        {
            var content = response.Messages.LastOrDefault()?.Text;
            _logger.LogDebug("📄 [Req:{RequestId}] Response: {Content}...", requestId, content?.Substring(0, Math.Min(100, content?.Length ?? 0)));
        }
    }

    private string GetUserId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.Identity?.Name 
               ?? context?.Request.Headers["X-User-Id"].ToString() 
               ?? "anonymous";
    }
}

