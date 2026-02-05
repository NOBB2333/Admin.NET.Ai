using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Admin.NET.Ai.Extensions;

/// <summary>
/// AI 诊断端点扩展
/// </summary>
public static class AiDiagnosticsEndpoints
{
    /// <summary>
    /// 映射 AI 诊断端点 (中间件管道可视化等)
    /// </summary>
    public static IEndpointRouteBuilder MapAiDiagnostics(this IEndpointRouteBuilder endpoints, string prefix = "/ai")
    {
        var group = endpoints.MapGroup(prefix);

        // 中间件管道可视化
        group.MapGet("/middleware-pipeline", GetMiddlewarePipeline)
            .WithName("GetAiMiddlewarePipeline")
            .WithDescription("获取 AI 中间件管道配置");

        // 配额状态
        group.MapGet("/quota-status/{userId}", GetQuotaStatus)
            .WithName("GetAiQuotaStatus")
            .WithDescription("获取用户配额状态");

        return endpoints;
    }

    private static IResult GetMiddlewarePipeline()
    {
        // 返回推荐的中间件管道顺序
        var pipeline = new
        {
            Description = "推荐的 AI 中间件管道顺序",
            Pipeline = new[]
            {
                new { Order = 1, Name = "QuotaCheckMiddleware", Description = "配额检查 - 阻止超限请求" },
                new { Order = 2, Name = "RateLimitingMiddleware", Description = "限流 - 令牌桶算法" },
                new { Order = 3, Name = "CachingMiddleware", Description = "语义缓存 - 减少重复请求" },
                new { Order = 4, Name = "TokenMonitoringMiddleware", Description = "Token 记录和成本计算" },
                new { Order = 5, Name = "ContentSafetyMiddleware", Description = "内容安全过滤 (敏感词+PII)" },
                new { Order = 6, Name = "AuditMiddleware", Description = "审计日志" },
                new { Order = 7, Name = "InnerChatClient", Description = "实际 LLM 调用" }
            },
            Example = @"
// 构建管道示例
var client = chatClient
    .AsBuilder()
    .Use(sp => new QuotaCheckMiddleware(...))
    .Use(sp => new RateLimitingMiddleware(...))
    .Use(sp => new TokenMonitoringMiddleware(...))
    .Use(sp => new ContentSafetyMiddleware(...))
    .Build();
"
        };

        return Results.Ok(pipeline);
    }

    private static async Task<IResult> GetQuotaStatus(
        string userId,
        HttpContext context)
    {
        var quotaManager = context.RequestServices.GetService<Abstractions.IQuotaManager>();
        if (quotaManager == null)
        {
            return Results.Problem("IQuotaManager 未注册");
        }

        var daily = await quotaManager.GetStatusAsync(userId, Abstractions.QuotaPeriod.Daily);
        var monthly = await quotaManager.GetStatusAsync(userId, Abstractions.QuotaPeriod.Monthly);

        return Results.Ok(new
        {
            UserId = userId,
            Daily = new { daily.Used, daily.Limit, daily.UsagePercentage, daily.ResetTime },
            Monthly = new { monthly.Used, monthly.Limit, monthly.UsagePercentage, monthly.ResetTime }
        });
    }
}
