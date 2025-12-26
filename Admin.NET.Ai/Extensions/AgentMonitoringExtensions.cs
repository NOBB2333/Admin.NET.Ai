using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Admin.NET.Ai.Services.Monitoring;
using System.Diagnostics.Metrics;

namespace Admin.NET.Ai.Configuration;

public class AgentMonitoringConfiguration
{
    public bool EnableDistributedTracing { get; set; } = true;
    public bool EnablePerformanceMetrics { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan SlowResponseThreshold { get; set; } = TimeSpan.FromSeconds(30);
}

public static class AgentMonitoringExtensions
{
    public static IServiceCollection AddAgentMonitoring(
        this IServiceCollection services,
        Action<AgentMonitoringConfiguration> configure)
    {
        var config = new AgentMonitoringConfiguration();
        configure(config);
        
        services.AddSingleton(config);
        services.AddSingleton<WorkflowMonitor>();
        services.AddSingleton<ResilientAgentExecutor>();

        if (config.EnablePerformanceMetrics)
        {
             services.AddSingleton<PerformanceMetricsCollector>();
        }
        
        if (config.EnableDistributedTracing)
        {
            // 需要 OpenTelemetry.Extensions.Hosting 和 OpenTelemetry.Exporter.OpenTelemetryProtocol 包
            // services.AddOpenTelemetry()
            //    .WithTracing(tracing => tracing
            //        .AddSource("Microsoft.AgentFramework")
            //        .AddAspNetCoreInstrumentation()
            //        .AddHttpClientInstrumentation());
            Console.WriteLine("[Warning] OpenTelemetry packages not installed. Tracing disabled.");
        }
        
        return services;
    }

    /// <summary>
    /// 启用 DevUI (模拟实现)
    /// </summary>
    public static void MapDevUI(this WebApplication app)
    {
        app.MapGet("/dev-ui", async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<h1>Admin.NET AI DevUI</h1><p>Agent Monitoring Dashboard (Mock)</p>");
        });
    }
}
