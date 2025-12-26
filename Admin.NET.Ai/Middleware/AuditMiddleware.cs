using Admin.NET.Ai.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 审计日志中间件 (基于 DelegatingChatClient)
/// 用于持久化关键业务操作日志到数据库
/// </summary>
public class AuditMiddleware : DelegatingChatClient
{
    private readonly IAuditStore _auditStore;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditMiddleware(
        IChatClient innerClient,
        IAuditStore auditStore, 
        ILogger<AuditMiddleware> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(innerClient)
    {
        _auditStore = auditStore;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        var requestId = Guid.NewGuid().ToString("N");
        
        ChatResponse? response = null;
        Exception? error = null;

        try
        {
            response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            await SaveAuditLogAsync(requestId, userId, chatMessages, response, error, startTime);
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 审计流式请求：通常只记录请求开始和最终状态，不记录中间的所有 Chunk
        var startTime = DateTime.UtcNow;
        var userId = GetUserId();
        var requestId = Guid.NewGuid().ToString("N");

        await SaveAuditLogAsync(requestId, userId, chatMessages, null, null, startTime, isStreaming: true);

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private async Task SaveAuditLogAsync(string requestId, string userId, IEnumerable<ChatMessage> request, ChatResponse? response, Exception? error, DateTime startTime, bool isStreaming = false)
    {
        var duration = DateTime.UtcNow - startTime;
        
        try 
        {
            var requestContent = JsonSerializer.Serialize(request);
            var responseContent = isStreaming ? "[Streaming Response]" : (response != null ? JsonSerializer.Serialize(response) : null);
            
            await _auditStore.SaveAuditLogAsync(
                requestId, 
                requestContent, 
                responseContent,
                new Dictionary<string, object?>
                {
                    ["UserId"] = userId,
                    ["Success"] = error == null,
                    ["DurationMs"] = (long)duration.TotalMilliseconds,
                    ["ErrorMessage"] = error?.Message,
                    ["IpAddress"] = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "无法保存审计日志");
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

// 简单的审计实体 (如果 Abstractions 中未定义)
public class AuditLogEntry
{
    public string RequestId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestContent { get; set; }
    public string? ResponseContent { get; set; }
    public string? IpAddress { get; set; }
}
