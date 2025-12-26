using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.RAG;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Middleware;

public class RAGTracingMiddleware : IRunMiddleware
{
    private readonly ILogger<RAGTracingMiddleware> _logger;

    public RAGTracingMiddleware(ILogger<RAGTracingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<ChatResponse> InvokeAsync(RunMiddlewareContext context, NextRunMiddleware next)
    {
        var startTime = DateTime.Now;
        var query = context.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;

        _logger.LogInformation($"[RAG_TRACE] Start Processing Query: {query}");

        // 执行管道 (包括 ContextInjection -> RAG Search)
        var response = await next(context);

        // 可观测性：检查是否发生了检索
        // 假设 ContextInjectionMiddleware 将检索信息放在可访问的地方，
        // 或者 TextSearchProvider 将结果记录到 context.Metadata，如果我们扩展上下文以支持向上传递数据。
        // 目前 TextSearchProvider (简单) 注入消息。
        // 高级实现可能会将元数据附加到 ChatResponse？
        // 让我们假设 response.Metadata 包含 RAG 信息，如果由提供者或下游逻辑设置。
        
        // 为了演示，我们只记录完成情况。
        _logger.LogInformation($"[RAG_TRACE] Pipeline Completed in {(DateTime.Now - startTime).TotalMilliseconds}ms.");

        if (response.AdditionalProperties != null && response.AdditionalProperties.ContainsKey("RAG_Sources"))
        {
             _logger.LogInformation($"[RAG_TRACE] Sources Used: {response.AdditionalProperties["RAG_Sources"]}");
        }

        // 注意：MEAI 9.0 中的 ChatResponse 可能会使用 AdditionalProperties 作为元数据
        // 如果不可用，我们要么跳过附加/读取实现。
        // 目前，记录查询/时间足以进行跟踪。

        return response;
    }
}
