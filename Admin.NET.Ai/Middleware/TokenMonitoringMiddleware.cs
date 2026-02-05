using Admin.NET.Ai.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// Token使用监控和费用控制中间件 (基于 DelegatingChatClient)
/// 使用 ITokenUsageStore 进行成本计算和记录
/// </summary>
public class TokenMonitoringMiddleware : DelegatingChatClient
{
    private readonly ITokenUsageStore _tokenStore;
    private readonly ILogger<TokenMonitoringMiddleware> _logger;
    private readonly IBudgetManager _budgetManager;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly string? _configuredModelName;

    public TokenMonitoringMiddleware(
        IChatClient innerClient,
        ITokenUsageStore tokenStore,
        ILogger<TokenMonitoringMiddleware> logger,
        IBudgetManager budgetManager,
        IHttpContextAccessor? httpContextAccessor = null,
        string? modelName = null)
        : base(innerClient)
    {
        _tokenStore = tokenStore;
        _logger = logger;
        _budgetManager = budgetManager;
        _httpContextAccessor = httpContextAccessor;
        _configuredModelName = modelName;
    }


    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages?.ToList() ?? [];
        if (messagesList.Count == 0)
        {
            _logger.LogWarning("⚠️ TokenMonitoringMiddleware: 收到空消息列表，返回空响应");
            return new ChatResponse([]);
        }

        var userId = GetUserId();
        var modelName = options?.ModelId ?? _configuredModelName ?? "unknown-model";
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await CheckBudgetAsync(userId, modelName, requestId, cancellationToken);

        var tokenUsage = await RecordStartAsync(requestId, userId, modelName, messagesList, cancellationToken);

        try
        {
            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();
            
            await RecordCompletionAsync(tokenUsage, response, modelName, requestId, stopwatch.ElapsedMilliseconds, cancellationToken);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await RecordFailureAsync(tokenUsage, ex, requestId, cancellationToken);
            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages?.ToList() ?? [];
        if (messagesList.Count == 0)
        {
            _logger.LogWarning("⚠️ TokenMonitoringMiddleware: 收到空消息列表，跳过处理");
            yield break;
        }

        var userId = GetUserId();
        var modelName = options?.ModelId ?? _configuredModelName ?? "unknown-model";
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await CheckBudgetAsync(userId, modelName, requestId, cancellationToken);

        var tokenUsage = await RecordStartAsync(requestId, userId, modelName, chatMessages, cancellationToken);
        
        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        try 
        {
             enumerator = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
             stopwatch.Stop();
             await RecordFailureAsync(tokenUsage, ex, requestId, cancellationToken);
             throw;
        }

        await using (enumerator)
        {
            var responseBuilder = new List<ChatResponseUpdate>();
            UsageDetails? streamUsage = null;
            
            bool hasNext = true;
            while (hasNext)
            {
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    await RecordFailureAsync(tokenUsage, ex, requestId, cancellationToken);
                    throw;
                }

                if (hasNext)
                {
                    var update = enumerator.Current;
                    responseBuilder.Add(update);
                    
                    if (update.Contents != null)
                    {
                        foreach (var content in update.Contents)
                        {
                            if (content is UsageContent usageContent)
                            {
                                streamUsage = usageContent.Details;
                            }
                        }
                    }
                    
                    yield return update;
                }
            }
            
            stopwatch.Stop();
            await RecordStreamingCompletionAsync(tokenUsage, chatMessages, responseBuilder, streamUsage, modelName, requestId, stopwatch.ElapsedMilliseconds, cancellationToken);
        }
    }

    // --- Private Helpers ---

    private string GetUserId()
    {
        if (_httpContextAccessor == null)
            return "console-user";
            
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.Identity?.Name 
               ?? context?.Request.Headers["X-User-Id"].ToString() 
               ?? "anonymous";
    }


    private async Task CheckBudgetAsync(string userId, string modelName, string requestId, CancellationToken cancellationToken)
    {
        var budgetCheck = await _budgetManager.CheckBudgetAsync(userId, modelName, cancellationToken);
        if (!budgetCheck.IsWithinBudget)
        {
            _logger.LogWarning("🚫 [Request-{RequestId}] 用户 {UserId} 超出预算限制", requestId, userId);
            throw new InvalidOperationException($"本月预算已用尽: {budgetCheck.UsedAmount:C} / {budgetCheck.BudgetAmount:C}");
        }
    }

    private async Task<TokenUsageRecord> RecordStartAsync(string requestId, string userId, string modelName, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var tokenUsage = new TokenUsageRecord
        {
            RequestId = requestId,
            UserId = userId,
            Model = modelName,
            StartTime = DateTime.UtcNow,
            InputMessage = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text,
            Status = TokenUsageStatus.Running
        };

        await _tokenStore.RecordStartAsync(tokenUsage, cancellationToken);
        _logger.LogDebug("📊 [Request-{RequestId}] 开始Token监控 - 用户: {UserId}, 模型: {Model}", requestId, userId, modelName);
        return tokenUsage;
    }

    private async Task RecordCompletionAsync(TokenUsageRecord tokenUsage, ChatResponse response, string modelName, string requestId, long elapsedMs, CancellationToken cancellationToken)
    {
        var responseText = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        
        int inputTokens, outputTokens;
        string source;
        
        if (response.Usage != null && (response.Usage.InputTokenCount > 0 || response.Usage.OutputTokenCount > 0))
        {
            inputTokens = (int)(response.Usage.InputTokenCount ?? 0);
            outputTokens = (int)(response.Usage.OutputTokenCount ?? 0);
            source = "API";
        }
        else
        {
            inputTokens = EstimateTokens(tokenUsage.InputMessage ?? "");
            outputTokens = EstimateTokens(responseText ?? "");
            source = "估算";
            _logger.LogDebug("⚠️ [Request-{RequestId}] API 未返回 Token 用量数据，使用估算 (模型: {Model})", requestId, modelName);
        }
        
        var usage = new TokenUsage
        {
            PromptTokens = inputTokens,
            CompletionTokens = outputTokens
        };
            
        await FinalizeRecordAsync(tokenUsage, usage, modelName, requestId, responseText, source, elapsedMs, cancellationToken);
    }
    
    private async Task RecordStreamingCompletionAsync(
        TokenUsageRecord tokenUsage, 
        IEnumerable<ChatMessage> requestMessages, 
        List<ChatResponseUpdate> updates, 
        UsageDetails? streamUsage,
        string modelName, 
        string requestId,
        long elapsedMs,
        CancellationToken cancellationToken)
    {
        var fullText = string.Join("", updates.Where(u => !string.IsNullOrEmpty(u.Text)).Select(u => u.Text));
        
        int inputTokens, outputTokens;
        string source;
        
        if (streamUsage != null && (streamUsage.InputTokenCount > 0 || streamUsage.OutputTokenCount > 0))
        {
            inputTokens = (int)(streamUsage.InputTokenCount ?? 0);
            outputTokens = (int)(streamUsage.OutputTokenCount ?? 0);
            source = "API(Stream)";
        }
        else
        {
            var promptText = string.Join(" ", requestMessages.Select(m => m.Text));
            inputTokens = EstimateTokens(promptText);
            outputTokens = EstimateTokens(fullText);
            source = "估算";
            _logger.LogDebug("⚠️ [Request-{RequestId}] 流式 API 未返回 Token 用量数据，使用估算 (模型: {Model})", requestId, modelName);
        }
        
        var usage = new TokenUsage
        {
            PromptTokens = inputTokens,
            CompletionTokens = outputTokens
        };
              
        await FinalizeRecordAsync(tokenUsage, usage, modelName, requestId, fullText, source, elapsedMs, cancellationToken);
    }

    private async Task FinalizeRecordAsync(TokenUsageRecord tokenUsage, TokenUsage usage, string modelName, string requestId, string? responseText, string source, long elapsedMs, CancellationToken cancellationToken)
    {
        // 使用 ITokenUsageStore 计算成本
        var cost = _tokenStore.CalculateCost(usage, modelName);

        tokenUsage.CompletionTime = DateTime.UtcNow;
        tokenUsage.PromptTokens = usage.PromptTokens;
        tokenUsage.CompletionTokens = usage.CompletionTokens;
        tokenUsage.Cost = cost;
        tokenUsage.Status = TokenUsageStatus.Completed;
        tokenUsage.ResponseMessage = responseText?.Length > 500 ? responseText[..500] : responseText; 

        await _tokenStore.RecordCompletionAsync(tokenUsage, cancellationToken);

        var budgetStatus = await _budgetManager.GetBudgetStatusAsync(tokenUsage.UserId, modelName, cancellationToken);
        if (budgetStatus.UsagePercentage >= 0.8m)
        {
            _logger.LogWarning("⚠️ [Request-{RequestId}] 用户 {UserId} 预算使用已达 {Percentage}%", 
                requestId, tokenUsage.UserId, budgetStatus.UsagePercentage * 100);
        }

        if (source.Contains("Stream"))
        {
            Console.WriteLine();
        }
        _logger.LogInformation(
            "✅ [{Model}] 用户:{User} | Token:{In}→{Out}({Source}) | 耗时:{Duration}ms | 费用:{Cost:C}", 
            modelName, tokenUsage.UserId, usage.PromptTokens, usage.CompletionTokens, source, elapsedMs, cost);
    }

    private async Task RecordFailureAsync(TokenUsageRecord tokenUsage, Exception ex, string requestId, CancellationToken cancellationToken)
    {
        tokenUsage.CompletionTime = DateTime.UtcNow;
        tokenUsage.Status = TokenUsageStatus.Failed;
        tokenUsage.ErrorMessage = ex.Message;
        await _tokenStore.RecordCompletionAsync(tokenUsage, cancellationToken);

        _logger.LogError(ex, "❌ [Request-{RequestId}] Token监控记录失败", requestId);
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        if (ContainsChinese(text))
        {
            return (int)Math.Ceiling(text.Length * 1.2);
        }
        
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)Math.Ceiling(wordCount * 1.3);
    }

    private static bool ContainsChinese(string text)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u4e00-\u9fa5]");
    }
}
