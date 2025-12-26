using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Monitoring;
using Admin.NET.Ai.Services.Processing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 监控与可观测性演示 - OpenTelemetry、指标、质量评估
/// </summary>
public static class MonitoringDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        Console.WriteLine("\n========== 监控与可观测性演示 ==========\n");

        // ===== 1. OpenTelemetry 分布式追踪 =====
        Console.WriteLine("--- 1. OpenTelemetry 分布式追踪 ---");
        Console.WriteLine(@"
AgentTelemetry 提供集成的 ActivitySource 和 Meter。

配置示例 (Program.cs):
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(AgentTelemetry.ActivitySourceName) // 'Admin.NET.Ai'
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter()) // 导出到 Jaeger/Aspire
    .WithMetrics(metrics => metrics
        .AddMeter(AgentTelemetry.MeterName)
        .AddPrometheusExporter());

追踪的操作:
- Agent.RunAsync
- Workflow.Execute
- Tool.Invoke
- MCP.Connect
");

        // ===== 2. 自定义 Span 追踪 =====
        Console.WriteLine("\n--- 2. 自定义 Span 追踪 ---");

        // 获取 telemetry 服务
        var telemetry = sp.GetRequiredService<AgentTelemetry>();

        // 创建追踪 Span
        using var activity = telemetry.StartAgentActivity("Demo", "CustomOperation");
        activity?.SetTag("demo.type", "monitoring");
        activity?.SetTag("demo.user", "test-user");

        Console.WriteLine("已创建追踪 Span: Agent.Demo");
        Console.WriteLine("  Tags: demo.type=monitoring, demo.user=test-user");

        // 模拟一些操作
        await Task.Delay(100);

        activity?.SetStatus(ActivityStatusCode.Ok);
        Console.WriteLine("  Status: OK");

        // ===== 3. 指标收集 =====
        Console.WriteLine("\n--- 3. 指标收集 (Metrics) ---");
        Console.WriteLine(@"
内置指标:
- agent_requests_total: 请求总数
- agent_request_duration_seconds: 请求延迟 (histogram)
- agent_tokens_used_total: Token 使用量
- agent_errors_total: 错误计数
- workflow_executions_total: 工作流执行数
- tool_invocations_total: 工具调用数

使用示例:
telemetry.RecordRequest(""chat"", duration, success: true);
telemetry.RecordTokenUsage(promptTokens: 100, completionTokens: 50);
");

        // 记录一些指标
        //telemetry.RecordRequest("chat", TimeSpan.FromMilliseconds(250), success: true);
        //telemetry.RecordTokenUsage(100, 50);
        Console.WriteLine("已记录指标: request_duration=250ms, tokens=150");

        // ===== 4. 工作流监控 =====
        Console.WriteLine("\n--- 4. 工作流监控 (WorkflowMonitor) ---");
        Console.WriteLine(@"
WorkflowMonitor 提供工作流执行的深度监控:

var monitor = sp.GetRequiredService<WorkflowMonitor>();

// 开始监控工作流
using var scope = monitor.BeginWorkflowScope(workflowId, ""MyWorkflow"");

// 记录步骤
scope.RecordStep(""Step1"", duration, success: true);
scope.RecordStep(""Step2"", duration, success: true);

// 获取执行报告
var report = scope.GetReport();
Console.WriteLine($""总耗时: {report.TotalDuration}"");
Console.WriteLine($""步骤数: {report.StepCount}"");
");

        // ===== 5. 批量处理监控 =====
        Console.WriteLine("\n--- 5. 批量处理服务 ---");
        
        var batchService = sp.GetRequiredService<BatchProcessingService>();
        var chatClient = aiFactory.GetDefaultChatClient();

        var prompts = new[]
        {
            "1+1等于几？",
            "中国的首都是哪里？",
            "今年是哪一年？"
        };

        Console.WriteLine($"批量处理 {prompts.Length} 个请求 (并发数: 3)...\n");

        var sw = Stopwatch.StartNew();
        var results = await batchService.ProcessBatchAsync<string>(
            chatClient,
            prompts,
            async prompt =>
            {
                var response = await chatClient.GetResponseAsync(prompt);
                return response.Messages.LastOrDefault()?.Text ?? "";
            });
        sw.Stop();

        foreach (var result in results)
        {
            var status = result.Success ? "✓" : "✗";
            Console.WriteLine($"[{status}] {result.Input}");
            if (result.Success)
                Console.WriteLine($"    → {result.Result?.Substring(0, Math.Min(50, result.Result?.Length ?? 0))}...");
        }

        Console.WriteLine($"\n总耗时: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"成功率: {results.Count(r => r.Success)}/{results.Count}");

        // ===== 6. 弹性执行器 =====
        Console.WriteLine("\n--- 6. 弹性执行器 (ResilientAgentExecutor) ---");
        Console.WriteLine(@"
ResilientAgentExecutor 提供企业级容错能力:

var executor = sp.GetRequiredService<ResilientAgentExecutor>();

// 配置重试策略
var options = new ResilienceOptions
{
    MaxRetries = 3,
    CircuitBreakerThreshold = 5,
    Timeout = TimeSpan.FromSeconds(30)
};

// 执行带弹性保护的操作
var result = await executor.ExecuteAsync(async () =>
{
    return await agent.RunAsync(prompt);
}, options);
");

        Console.WriteLine("\n========== 监控与可观测性演示结束 ==========");
    }
}
