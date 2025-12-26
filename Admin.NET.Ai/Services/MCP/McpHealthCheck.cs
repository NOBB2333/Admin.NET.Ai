using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Admin.NET.Ai.Services.MCP;

/// <summary>
/// MCP 服务健康检查
/// </summary>
public class McpHealthCheck : IHealthCheck
{
    private readonly ILogger<McpHealthCheck> _logger;
    private readonly IMcpConnectionPool _connectionPool;
    private readonly IOptions<LLMMcpConfig> _options;

    public McpHealthCheck(
        ILogger<McpHealthCheck> logger,
        IMcpConnectionPool connectionPool,
        IOptions<LLMMcpConfig> options)
    {
        _logger = logger;
        _connectionPool = connectionPool;
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var unhealthyServers = new List<string>();
        var degradedServers = new List<string>();

        foreach (var server in _options.Value.Servers)
        {
            try
            {
                var status = _connectionPool.GetStatus(server.Name);
                data[$"{server.Name}_status"] = status.ToString();

                switch (status)
                {
                    case McpConnectionStatus.Connected:
                        // 尝试 ping
                        var connection = await _connectionPool.GetConnectionAsync(server.Name, cancellationToken);
                        var lastActive = connection.LastActiveTime;
                        var inactiveTime = DateTime.UtcNow - lastActive;

                        data[$"{server.Name}_last_active"] = lastActive.ToString("O");
                        
                        if (inactiveTime > TimeSpan.FromMinutes(5))
                        {
                            degradedServers.Add(server.Name);
                            data[$"{server.Name}_warning"] = $"无活动时间: {inactiveTime.TotalMinutes:F1} 分钟";
                        }
                        break;

                    case McpConnectionStatus.Disconnected:
                    case McpConnectionStatus.Failed:
                        unhealthyServers.Add(server.Name);
                        break;

                    case McpConnectionStatus.Connecting:
                    case McpConnectionStatus.Reconnecting:
                        degradedServers.Add(server.Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP 健康检查失败: {Server}", server.Name);
                unhealthyServers.Add(server.Name);
                data[$"{server.Name}_error"] = ex.Message;
            }
        }

        // 汇总状态
        data["total_servers"] = _options.Value.Servers.Count;
        data["healthy_count"] = _options.Value.Servers.Count - unhealthyServers.Count - degradedServers.Count;
        data["degraded_count"] = degradedServers.Count;
        data["unhealthy_count"] = unhealthyServers.Count;

        if (unhealthyServers.Any())
        {
            return HealthCheckResult.Unhealthy(
                $"MCP 服务不可用: {string.Join(", ", unhealthyServers)}", 
                data: data);
        }

        if (degradedServers.Any())
        {
            return HealthCheckResult.Degraded(
                $"MCP 服务降级: {string.Join(", ", degradedServers)}", 
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
