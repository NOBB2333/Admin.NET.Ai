using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Middleware;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 中间件演示 - 展示各种中间件的使用
/// </summary>
public static class MiddlewareDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        Console.WriteLine("\n========== 中间件演示 ==========\n");

        // 1. 获取基础 ChatClient
        var baseClient = aiFactory.GetDefaultChatClient();

        // ===== 1. 日志中间件 =====
        Console.WriteLine("--- 1. 日志中间件 (LoggingMiddleware) ---");
        Console.WriteLine("LoggingMiddleware 会记录所有请求和响应的详细信息。");
        Console.WriteLine("在生产环境中，这有助于调试和审计。\n");

        // ===== 2. 重试中间件 =====
        Console.WriteLine("--- 2. 重试中间件 (RetryMiddleware) ---");
        Console.WriteLine("配置: MaxRetries=3, InitialDelay=1s, ExponentialBackoff=true");
        Console.WriteLine("当 API 调用失败时，会自动重试并使用指数退避策略。\n");

        // ===== 3. 缓存中间件 =====
        Console.WriteLine("--- 3. 缓存中间件 (CachingMiddleware) ---");
        Console.WriteLine("支持语义缓存 - 相似问题可以命中缓存。");
        Console.WriteLine("配置: SemanticSimilarityThreshold=0.85\n");

        // 演示缓存效果
        var question = "什么是机器学习？";
        Console.WriteLine($"第一次调用: {question}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response1 = await baseClient.GetResponseAsync(question);
        sw.Stop();
        Console.WriteLine($"响应时间: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"响应: {response1.Messages.LastOrDefault()?.Text?.Substring(0, Math.Min(100, response1.Messages.LastOrDefault()?.Text?.Length ?? 0))}...\n");

        Console.WriteLine($"第二次调用 (相同问题 - 应命中缓存): {question}");
        sw.Restart();
        var response2 = await baseClient.GetResponseAsync(question);
        sw.Stop();
        Console.WriteLine($"响应时间: {sw.ElapsedMilliseconds}ms (如果命中缓存会更快)");

        // ===== 4. 限流中间件 =====
        Console.WriteLine("\n--- 4. 限流中间件 (RateLimitingMiddleware) ---");
        Console.WriteLine("支持分布式限流 (Redis) 和本地限流。");
        Console.WriteLine("配置: TokensPerMinute=60, BucketSize=10");
        Console.WriteLine("当请求超出限制时，会等待或拒绝请求。\n");

        // ===== 5. Token 监控中间件 =====
        Console.WriteLine("--- 5. Token 监控中间件 (TokenMonitoringMiddleware) ---");
        Console.WriteLine("实时追踪 Token 使用量和成本。");
        Console.WriteLine("支持预算控制 - 当超出预算时发出警告或阻止请求。\n");

        // ===== 6. 审计中间件 =====
        Console.WriteLine("--- 6. 审计中间件 (AuditMiddleware) ---");
        Console.WriteLine("记录所有 AI 操作的审计日志，包括:");
        Console.WriteLine("- 用户ID、请求时间、请求内容");
        Console.WriteLine("- 响应内容、Token使用量");
        Console.WriteLine("- 错误信息（如有）\n");

        // ===== 7. 上下文注入中间件 =====
        Console.WriteLine("--- 7. 上下文注入中间件 (InstructionMiddleware) ---");
        Console.WriteLine("自动向每个请求注入系统指令、时间、用户信息等上下文。");
        Console.WriteLine("示例: 自动添加当前时间、用户角色等信息。\n");

        // ===== 8. 工具验证中间件 =====
        Console.WriteLine("--- 8. 工具验证中间件 (ToolValidationMiddleware) ---");
        Console.WriteLine("验证工具调用的参数和结果:");
        Console.WriteLine("- 权限检查: 用户是否有权调用该工具");
        Console.WriteLine("- 参数验证: 参数是否符合 Schema");
        Console.WriteLine("- 结果脱敏: 敏感信息自动脱敏\n");

        // ===== 中间件链示例 =====
        Console.WriteLine("--- 中间件链配置示例 ---");
        Console.WriteLine(@"
// 在 ChatClientBuilder 中配置中间件链
var client = new ChatClientBuilder(baseClient)
    .Use(loggingMiddleware)     // 最外层: 日志
    .Use(auditMiddleware)       // 审计
    .Use(retryMiddleware)       // 重试 (应在缓存之前)
    .Use(cachingMiddleware)     // 缓存
    .Use(rateLimitMiddleware)   // 限流
    .Use(tokenMonitor)          // Token 监控
    .Use(instructionMiddleware) // 上下文注入 (最内层)
    .Build();
");

        Console.WriteLine("========== 中间件演示结束 ==========");
    }
}
