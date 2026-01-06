using Admin.NET.Ai.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Admin.NET.Ai.Services.MCP;

/// <summary>
/// MCP 服务健康检查 - 使用 McpToolFactory
/// </summary>
public class McpHealthCheck : IHealthCheck
{
    private readonly ILogger<McpHealthCheck> _logger;
    private readonly McpToolFactory _factory;
    private readonly IOptions<LLMMcpConfig> _options;

    public McpHealthCheck(
        ILogger<McpHealthCheck> logger,
        McpToolFactory factory,
        IOptions<LLMMcpConfig> options)
    {
        _logger = logger;
        _factory = factory;
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var unhealthyServers = new List<string>();
        var healthyServers = new List<string>();

        foreach (var server in _options.Value.Servers.Where(s => s.Enabled))
        {
            try
            {
                // 尝试获取客户端连接
                var client = await _factory.GetClientAsync(server.Name);
                
                if (client?.ServerCapabilities != null)
                {
                    healthyServers.Add(server.Name);
                    data[$"{server.Name}_status"] = "Connected";
                    data[$"{server.Name}_tools"] = client.ServerCapabilities.Tools != null;
                    data[$"{server.Name}_resources"] = client.ServerCapabilities.Resources != null;
                }
                else
                {
                    unhealthyServers.Add(server.Name);
                    data[$"{server.Name}_status"] = "NoCapabilities";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP 健康检查失败: {Server}", server.Name);
                unhealthyServers.Add(server.Name);
                data[$"{server.Name}_status"] = "Failed";
                data[$"{server.Name}_error"] = ex.Message;
            }
        }

        // 汇总状态
        var total = _options.Value.Servers.Count(s => s.Enabled);
        data["total_servers"] = total;
        data["healthy_count"] = healthyServers.Count;
        data["unhealthy_count"] = unhealthyServers.Count;

        if (unhealthyServers.Any())
        {
            return HealthCheckResult.Unhealthy(
                $"MCP 服务不可用: {string.Join(", ", unhealthyServers)}", 
                data: data);
        }

        return HealthCheckResult.Healthy("所有 MCP 服务正常", data);
    }
}

/// <summary>
/// MCP 健康检查扩展
/// </summary>
public static class McpHealthCheckExtensions
{
    /// <summary>
    /// 添加 MCP 健康检查
    /// </summary>
    public static IHealthChecksBuilder AddMcpHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "mcp",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddCheck<McpHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Unhealthy,
            tags ?? new[] { "mcp", "external" });
    }
}
