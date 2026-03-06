using Admin.NET.Ai.Abstractions;
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
    private readonly bool _enableSensitiveDataLogging;

    public LoggingMiddleware(
        IChatClient innerClient,
        ILogger<LoggingMiddleware> logger,
        bool enableSensitiveDataLogging = false) // 可从配置中注入
        : base(innerClient)
    {
        _logger = logger;
        _enableSensitiveDataLogging = enableSensitiveDataLogging;
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var model = options?.ModelId ?? "default";
        var stopwatch = Stopwatch.StartNew();
        var logContext = CreateLogContext(options);
        
        LogRequest(requestId, model, chatMessages, logContext);

        try
        {
            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();

            LogResponse(requestId, stopwatch.ElapsedMilliseconds, response, logContext);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "❌ [Req:{RequestId}] 调用失败. 耗时: {Elapsed}ms. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
                requestId,
                stopwatch.ElapsedMilliseconds,
                logContext.TraceId,
                logContext.SpanId,
                logContext.SessionId);
            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var model = options?.ModelId ?? "default";
        var stopwatch = Stopwatch.StartNew();
        var logContext = CreateLogContext(options);

        LogRequest(requestId, model, chatMessages, logContext);

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
             _logger.LogError(
                 ex,
                 "❌ [Req:{RequestId}] 流式连接失败. 耗时: {Elapsed}ms. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
                 requestId,
                 stopwatch.ElapsedMilliseconds,
                 logContext.TraceId,
                 logContext.SpanId,
                 logContext.SessionId);
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
                    _logger.LogError(
                        ex,
                        "❌ [Req:{RequestId}] 流式读取异常. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
                        requestId,
                        logContext.TraceId,
                        logContext.SpanId,
                        logContext.SessionId);
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
        _logger.LogDebug(
            "⬅️ [Req:{RequestId}] 流式调用结束. 耗时: {Elapsed}ms. Chunks: {Count}. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
            requestId,
            stopwatch.ElapsedMilliseconds,
            updateCount,
            logContext.TraceId,
            logContext.SpanId,
            logContext.SessionId);
    }

    private void LogRequest(
        string requestId,
        string model,
        IEnumerable<ChatMessage> messages,
        (string TraceId, string SpanId, string SessionId) logContext)
    {
        _logger.LogDebug(
            "➡️ [Req:{RequestId}] 模型: {Model}. PromptLength: {Length}. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
            requestId,
            model,
            messages.Sum(m => m.Text?.Length ?? 0),
            logContext.TraceId,
            logContext.SpanId,
            logContext.SessionId);

        if (_enableSensitiveDataLogging)
        {
            _logger.LogDebug(
                "📝 [Req:{RequestId}] Messages: {Payload}. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
                requestId,
                JsonSerializer.Serialize(messages),
                logContext.TraceId,
                logContext.SpanId,
                logContext.SessionId);
        }
    }

    private void LogResponse(
        string requestId,
        long elapsed,
        ChatResponse response,
        (string TraceId, string SpanId, string SessionId) logContext)
    {
        int inputTokens = (int)(response.Usage?.InputTokenCount ?? 0);
        int outputTokens = (int)(response.Usage?.OutputTokenCount ?? 0);

        _logger.LogDebug(
            "⬅️ [Req:{RequestId}] 完成. 耗时: {Elapsed}ms. Tokens: {In}+{Out}={Total}. Finish: {Reason}. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
            requestId, 
            elapsed,
            inputTokens, 
            outputTokens, 
            inputTokens + outputTokens,
            response.FinishReason,
            logContext.TraceId,
            logContext.SpanId,
            logContext.SessionId);

        if (_enableSensitiveDataLogging)
        {
            var content = response.Messages.LastOrDefault()?.Text;
            _logger.LogDebug(
                "📄 [Req:{RequestId}] Response: {Content}... TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
                requestId,
                content?.Substring(0, Math.Min(100, content?.Length ?? 0)),
                logContext.TraceId,
                logContext.SpanId,
                logContext.SessionId);
        }
    }

    private static (string TraceId, string SpanId, string SessionId) CreateLogContext(ChatOptions? options)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        var spanId = Activity.Current?.SpanId.ToString() ?? "none";
        var sessionId = GetOptionString(options, "SessionId") ?? "none";

        return (traceId, spanId, sessionId);
    }

    private static string? GetOptionString(ChatOptions? options, string key)
    {
        if (options?.AdditionalProperties == null)
        {
            return null;
        }

        return options.AdditionalProperties.TryGetValue(key, out var value)
            ? value?.ToString()
            : null;
    }

}
