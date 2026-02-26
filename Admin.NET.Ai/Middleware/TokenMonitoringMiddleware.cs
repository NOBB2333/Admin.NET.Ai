using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// Token使用监控中间件 (增强版)
/// 职责: Token 用量记录、成本计算、工具/Agent/Skill 使用追踪
/// 注意: 配额检查已移至 QuotaCheckMiddleware
/// UserId 必须通过 ChatOptions.AdditionalProperties["UserId"] 传入
/// </summary>
public class TokenMonitoringMiddleware : DelegatingChatClient
{
    private readonly ITokenUsageStore _tokenStore;
    private readonly ILogger<TokenMonitoringMiddleware> _logger;
    private readonly string? _configuredModelName;

    public TokenMonitoringMiddleware(
        IChatClient innerClient,
        ITokenUsageStore tokenStore,
        ILogger<TokenMonitoringMiddleware> logger,
        string? modelName = null)
        : base(innerClient)
    {
        _tokenStore = tokenStore;
        _logger = logger;
        _configuredModelName = modelName;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages?.ToList() ?? [];
        if (messagesList.Count == 0)
        {
            _logger.LogWarning("⚠️ TokenMonitoringMiddleware: 收到空消息列表");
            return new ChatResponse([]);
        }

        var context = CreateRequestContext(options, messagesList);
        var record = await RecordStartAsync(context, cancellationToken);

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();

            await RecordCompletionAsync(record, response, context, stopwatch.ElapsedMilliseconds, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(record, ex, cancellationToken);
            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages?.ToList() ?? [];
        if (messagesList.Count == 0)
        {
            _logger.LogWarning("⚠️ TokenMonitoringMiddleware: 收到空消息列表");
            yield break;
        }

        var context = CreateRequestContext(options, messagesList);
        var record = await RecordStartAsync(context, cancellationToken);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var updates = new List<ChatResponseUpdate>();
        UsageDetails? streamUsage = null;

        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        try
        {
            enumerator = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(record, ex, cancellationToken);
            throw;
        }

        await using (enumerator)
        {
            while (true)
            {
                bool hasNext;
                try { hasNext = await enumerator.MoveNextAsync(); }
                catch (Exception ex)
                {
                    await RecordFailureAsync(record, ex, cancellationToken);
                    throw;
                }
                
                if (!hasNext) break;

                var update = enumerator.Current;
                updates.Add(update);

                if (update.Contents?.FirstOrDefault(c => c is UsageContent) is UsageContent usageContent)
                {
                    streamUsage = usageContent.Details;
                }

                yield return update;
            }
        }

        stopwatch.Stop();

        // ✅ 修复换行问题: 流式输出后 Console.Write 没有换行，
        // 导致后续的 logger 输出粘在 AI 回复末尾
        Console.WriteLine();

        await RecordStreamingCompletionAsync(record, context, updates, streamUsage, stopwatch.ElapsedMilliseconds, cancellationToken);
    }

    #region Private Helpers

    private record RequestContext(string RequestId, string UserId, string ModelName, string? InputText);

    private RequestContext CreateRequestContext(ChatOptions? options, IEnumerable<ChatMessage> messages)
    {
        var userId = options?.AdditionalProperties?.TryGetValue("UserId", out var uid) == true 
            ? uid?.ToString() ?? "anonymous" 
            : "anonymous";
            
        return new RequestContext(
            RequestId: Guid.NewGuid().ToString("N")[..8],
            UserId: userId,
            ModelName: options?.ModelId ?? _configuredModelName ?? "unknown-model",
            InputText: messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text
        );
    }

    private async Task<TokenUsageRecord> RecordStartAsync(RequestContext context, CancellationToken ct)
    {
        var record = new TokenUsageRecord
        {
            RequestId = context.RequestId,
            UserId = context.UserId,
            Model = context.ModelName,
            StartTime = DateTime.UtcNow,
            InputMessage = context.InputText,
            Status = TokenUsageStatus.Running
        };

        await _tokenStore.RecordStartAsync(record, ct);
        _logger.LogDebug("📊 [{RequestId}] 开始监控 - {User}@{Model}", context.RequestId, context.UserId, context.ModelName);
        return record;
    }

    private async Task RecordCompletionAsync(
        TokenUsageRecord record, ChatResponse response, RequestContext context,
        long elapsedMs, CancellationToken ct)
    {
        var responseText = response.Messages?.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        var usage = ExtractUsage(response.Usage, record.InputMessage, responseText, out var source);
        
        await FinalizeRecordAsync(record, usage, context, responseText, source, elapsedMs, ct);
    }

    private async Task RecordStreamingCompletionAsync(
        TokenUsageRecord record, 
        RequestContext context,
        List<ChatResponseUpdate> updates, 
        UsageDetails? streamUsage,
        long elapsedMs,
        CancellationToken ct)
    {
        var fullText = string.Concat(updates.Where(u => !string.IsNullOrEmpty(u.Text)).Select(u => u.Text));
        var usage = ExtractUsage(streamUsage, record.InputMessage, fullText, out var source);
        
        await FinalizeRecordAsync(record, usage, context, fullText, source + "(Stream)", elapsedMs, ct);
    }

    private async Task FinalizeRecordAsync(
        TokenUsageRecord record, 
        TokenUsage usage, 
        RequestContext context,
        string? responseText, 
        string source, 
        long elapsedMs,
        CancellationToken ct)
    {
        var cost = _tokenStore.CalculateCost(usage, context.ModelName);

        record.CompletionTime = DateTime.UtcNow;
        record.PromptTokens = usage.PromptTokens;
        record.CompletionTokens = usage.CompletionTokens;
        record.Cost = cost;
        record.Status = TokenUsageStatus.Completed;
        record.ResponseMessage = responseText?.Length > 500 ? responseText[..500] : responseText;

        await _tokenStore.RecordCompletionAsync(record, ct);

        // ===== 增强日志输出 =====
        // 基础日志 (Token + 耗时 + 成本)
        _logger.LogInformation(
            "✅ [{Model}] {User} | Token:{In}→{Out}({Source}) | {Duration}ms | ¥{Cost:F4}", 
            context.ModelName, context.UserId, usage.PromptTokens, usage.CompletionTokens, source, elapsedMs, cost);

    }

    private async Task RecordFailureAsync(TokenUsageRecord record, Exception ex, CancellationToken ct)
    {
        record.CompletionTime = DateTime.UtcNow;
        record.Status = TokenUsageStatus.Failed;
        record.ErrorMessage = ex.Message;
        await _tokenStore.RecordCompletionAsync(record, ct);

        _logger.LogError(ex, "❌ [{RequestId}] Token监控记录失败", record.RequestId);
    }

    private TokenUsage ExtractUsage(UsageDetails? apiUsage, string? inputText, string? outputText, out string source)
    {
        if (apiUsage != null && (apiUsage.InputTokenCount > 0 || apiUsage.OutputTokenCount > 0))
        {
            source = "API";
            return new TokenUsage
            {
                PromptTokens = (int)(apiUsage.InputTokenCount ?? 0),
                CompletionTokens = (int)(apiUsage.OutputTokenCount ?? 0)
            };
        }

        source = "估算";
        return new TokenUsage
        {
            PromptTokens = EstimateTokens(inputText ?? ""),
            CompletionTokens = EstimateTokens(outputText ?? "")
        };
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        if (text.Any(c => c >= '\u4e00' && c <= '\u9fa5'))
        {
            return (int)Math.Ceiling(text.Length * 1.2);
        }
        return (int)Math.Ceiling(text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length * 1.3);
    }

    #endregion
}

