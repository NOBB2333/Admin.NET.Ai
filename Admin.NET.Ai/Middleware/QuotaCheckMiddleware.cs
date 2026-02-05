using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Core.Exceptions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 配额检查中间件 - 在请求前验证用户配额
/// </summary>
public class QuotaCheckMiddleware : DelegatingChatClient
{
    private readonly IQuotaManager _quotaManager;
    private readonly ILogger<QuotaCheckMiddleware> _logger;

    public QuotaCheckMiddleware(
        IChatClient innerClient,
        IQuotaManager quotaManager,
        ILogger<QuotaCheckMiddleware> logger)
        : base(innerClient)
    {
        _quotaManager = quotaManager;
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        await CheckQuotaAsync(options, cancellationToken);
        return await base.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await CheckQuotaAsync(options, cancellationToken);
        
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private async Task CheckQuotaAsync(ChatOptions? options, CancellationToken cancellationToken)
    {
        var userId = GetUserId(options);
        var modelName = options?.ModelId ?? "unknown-model";
        
        var quotaCheck = await _quotaManager.CheckQuotaAsync(userId, modelName, cancellationToken);
        
        if (!quotaCheck.IsWithinQuota)
        {
            _logger.LogWarning("🚫 用户 {UserId} 配额超限: {Reason}", userId, quotaCheck.BlockReason);
            throw new QuotaExceededException(quotaCheck.BlockReason ?? "配额已用尽");
        }
        
        _logger.LogDebug("✅ 配额检查通过 - 用户: {UserId}, 模型: {Model}", userId, modelName);
    }
    
    private static string GetUserId(ChatOptions? options)
    {
        if (options?.AdditionalProperties?.TryGetValue("UserId", out var userId) == true)
        {
            return userId?.ToString() ?? "anonymous";
        }
        return "anonymous";
    }
}
