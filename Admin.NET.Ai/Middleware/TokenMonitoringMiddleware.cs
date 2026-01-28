using Admin.NET.Ai.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// Token使用监控和费用控制中间件 (基于 DelegatingChatClient)
/// </summary>
public class TokenMonitoringMiddleware : DelegatingChatClient
{
    private readonly ITokenUsageStore _tokenStore;
    private readonly ILogger<TokenMonitoringMiddleware> _logger;
    private readonly ICostCalculator _costCalculator;
    private readonly IBudgetManager _budgetManager;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly string? _configuredModelName; // 构造时配置的模型名

    public TokenMonitoringMiddleware(
        IChatClient innerClient,
        ITokenUsageStore tokenStore,
        ILogger<TokenMonitoringMiddleware> logger,
        ICostCalculator costCalculator,
        IBudgetManager budgetManager,
        IHttpContextAccessor? httpContextAccessor = null,
        string? modelName = null) // 可选的模型名参数
        : base(innerClient)
    {
        _tokenStore = tokenStore;
        _logger = logger;
        _costCalculator = costCalculator;
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

        await CheckBudgetAsync(userId, modelName, requestId);

        var tokenUsage = await RecordStartAsync(requestId, userId, modelName, messagesList);

        try
        {
            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();
            
            // 直接使用 response.Usage (MEAI 标准)
            await RecordCompletionAsync(tokenUsage, response, modelName, requestId, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await RecordFailureAsync(tokenUsage, ex, requestId);
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

        await CheckBudgetAsync(userId, modelName, requestId);

        var tokenUsage = await RecordStartAsync(requestId, userId, modelName, chatMessages);
        
        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        try 
        {
             enumerator = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
             stopwatch.Stop();
             await RecordFailureAsync(tokenUsage, ex, requestId);
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
                    await RecordFailureAsync(tokenUsage, ex, requestId);
                    throw;
                }

                if (hasNext)
                {
                    var update = enumerator.Current;
                    responseBuilder.Add(update);
                    
                    // 尝试从流式更新中获取 Usage (某些 Provider 在最后一个 update 中包含)
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
            // 流结束，记录 Token
            await RecordStreamingCompletionAsync(tokenUsage, chatMessages, responseBuilder, streamUsage, modelName, requestId, stopwatch.ElapsedMilliseconds);
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


    private async Task CheckBudgetAsync(string userId, string modelName, string requestId)
    {
        var budgetCheck = await _budgetManager.CheckBudgetAsync(userId, modelName);
        if (!budgetCheck.IsWithinBudget)
        {
            _logger.LogWarning("🚫 [Request-{RequestId}] 用户 {UserId} 超出预算限制", requestId, userId);
            throw new InvalidOperationException($"本月预算已用尽: {budgetCheck.UsedAmount:C} / {budgetCheck.BudgetAmount:C}");
        }
    }

    private async Task<TokenUsageRecord> RecordStartAsync(string requestId, string userId, string modelName, IEnumerable<ChatMessage> messages)
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

        await _tokenStore.RecordStartAsync(tokenUsage);
        _logger.LogDebug("📊 [Request-{RequestId}] 开始Token监控 - 用户: {UserId}, 模型: {Model}", requestId, userId, modelName);
        return tokenUsage;
    }

    private async Task RecordCompletionAsync(TokenUsageRecord tokenUsage, ChatResponse response, string modelName, string requestId, long elapsedMs)
    {
        var responseText = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        
        // 优先使用 API 返回的 Usage，否则估算
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
            // Fallback: 估算 - 警告用户 API 未返回 Usage
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
            
        await FinalizeRecordAsync(tokenUsage, usage, modelName, requestId, responseText, source, elapsedMs);
    }
    
    private async Task RecordStreamingCompletionAsync(
        TokenUsageRecord tokenUsage, 
        IEnumerable<ChatMessage> requestMessages, 
        List<ChatResponseUpdate> updates, 
        UsageDetails? streamUsage,
        string modelName, 
        string requestId,
        long elapsedMs)
    {
        var fullText = string.Join("", updates.Where(u => !string.IsNullOrEmpty(u.Text)).Select(u => u.Text));
        
        int inputTokens, outputTokens;
        string source;
        
        // 优先使用流式返回的 Usage
        if (streamUsage != null && (streamUsage.InputTokenCount > 0 || streamUsage.OutputTokenCount > 0))
        {
            inputTokens = (int)(streamUsage.InputTokenCount ?? 0);
            outputTokens = (int)(streamUsage.OutputTokenCount ?? 0);
            source = "API(Stream)";
        }
        else
        {
            // Fallback: 估算
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
              
        await FinalizeRecordAsync(tokenUsage, usage, modelName, requestId, fullText, source, elapsedMs);
    }

    private async Task FinalizeRecordAsync(TokenUsageRecord tokenUsage, TokenUsage usage, string modelName, string requestId, string? responseText, string source, long elapsedMs = 0)
    {
        var cost = _costCalculator.CalculateCost(usage, modelName);

        tokenUsage.CompletionTime = DateTime.UtcNow;
        tokenUsage.PromptTokens = usage.PromptTokens;
        tokenUsage.CompletionTokens = usage.CompletionTokens;
        tokenUsage.Cost = cost;
        tokenUsage.Status = TokenUsageStatus.Completed;
        tokenUsage.ResponseMessage = responseText?.Length > 500 ? responseText[..500] : responseText; 

        await _tokenStore.RecordCompletionAsync(tokenUsage);

        var budgetStatus = await _budgetManager.GetBudgetStatusAsync(tokenUsage.UserId, modelName);
        if (budgetStatus.UsagePercentage >= 0.8m)
        {
            _logger.LogWarning("⚠️ [Request-{RequestId}] 用户 {UserId} 预算使用已达 {Percentage}%", 
                requestId, tokenUsage.UserId, budgetStatus.UsagePercentage * 100);
        }

        // 增强输出：包含模型、用户、Token、耗时、费用
        // 流式输出可能没有换行，确保日志在新行开始
        if (source.Contains("Stream"))
        {
            Console.WriteLine(); // 确保流式输出后换行
        }
        _logger.LogInformation(
            "✅ [{Model}] 用户:{User} | Token:{In}→{Out}({Source}) | 耗时:{Duration}ms | 费用:{Cost:C}", 
            modelName, tokenUsage.UserId, usage.PromptTokens, usage.CompletionTokens, source, elapsedMs, cost);
    }

    private async Task RecordFailureAsync(TokenUsageRecord tokenUsage, Exception ex, string requestId)
    {
        tokenUsage.CompletionTime = DateTime.UtcNow;
        tokenUsage.Status = TokenUsageStatus.Failed;
        tokenUsage.ErrorMessage = ex.Message;
        await _tokenStore.RecordCompletionAsync(tokenUsage);

        _logger.LogError(ex, "❌ [Request-{RequestId}] Token监控记录失败", requestId);
    }

    /// <summary>
    /// 估算 Token 数量 (当 API 不返回 Usage 时使用)
    /// </summary>
    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        // 中文: 约 1.2 token/字符, 英文: 约 0.75 token/word (1.3 * words)
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
