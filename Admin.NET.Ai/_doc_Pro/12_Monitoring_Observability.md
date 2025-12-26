# 监控与指标 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `AgentTelemetry.cs` | `Services/Monitoring/` | OpenTelemetry 集成 |
| `WorkflowMonitor.cs` | `Services/Monitoring/` | 工作流监控 |
| `PerformanceMetrics.cs` | `Services/Monitoring/` | 性能指标 |
| `TraceService.cs` | `Services/` | 追踪服务 |
| `MonitoringDemo.cs` | `Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 监控层次

```
[Application]
    ↓
[AgentTelemetry] ← Metrics/Traces/Logs
    ↓
[OpenTelemetry Exporter]
    ↓
┌──────────────────────────────────┐
│  Prometheus  │  Jaeger  │  Loki  │
└──────────────────────────────────┘
```

---

## 🔧 核心实现

### 1. OpenTelemetry 集成

```csharp
public class AgentTelemetry
{
    private static readonly Meter Meter = new("Admin.NET.Ai", "1.0.0");
    private static readonly ActivitySource ActivitySource = new("Admin.NET.Ai.Agent");
    
    // 指标定义
    public static readonly Counter<long> RequestCount = 
        Meter.CreateCounter<long>("ai_requests_total", description: "Total AI requests");
    
    public static readonly Histogram<double> RequestDuration = 
        Meter.CreateHistogram<double>("ai_request_duration_seconds", description: "Request duration");
    
    public static readonly Counter<long> TokensConsumed = 
        Meter.CreateCounter<long>("ai_tokens_consumed", description: "Tokens consumed");
    
    public static readonly UpDownCounter<int> ActiveRequests = 
        Meter.CreateUpDownCounter<int>("ai_active_requests", description: "Active requests");
    
    // 创建追踪 Span
    public static Activity? StartActivity(string operationName, Dictionary<string, object?>? tags = null)
    {
        var activity = ActivitySource.StartActivity(operationName, ActivityKind.Client);
        
        if (tags != null && activity != null)
        {
            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value);
            }
        }
        
        return activity;
    }
}
```

### 2. 请求追踪中间件

```csharp
public class TelemetryMiddleware : DelegatingChatClient
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken ct = default)
    {
        using var activity = AgentTelemetry.StartActivity("chat_completion", new Dictionary<string, object?>
        {
            ["model"] = options?.ModelId,
            ["messages_count"] = messages.Count()
        });
        
        AgentTelemetry.ActiveRequests.Add(1);
        var sw = Stopwatch.StartNew();
        
        try
        {
            var response = await base.GetResponseAsync(messages, options, ct);
            
            // 记录指标
            AgentTelemetry.RequestCount.Add(1, new KeyValuePair<string, object?>("status", "success"));
            
            if (response.Usage != null)
            {
                AgentTelemetry.TokensConsumed.Add(
                    response.Usage.InputTokens + response.Usage.OutputTokens,
                    new KeyValuePair<string, object?>("model", options?.ModelId));
            }
            
            activity?.SetTag("status", "success");
            return response;
        }
        catch (Exception ex)
        {
            AgentTelemetry.RequestCount.Add(1, new KeyValuePair<string, object?>("status", "error"));
            activity?.SetTag("status", "error");
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
        finally
        {
            sw.Stop();
            AgentTelemetry.RequestDuration.Record(sw.Elapsed.TotalSeconds);
            AgentTelemetry.ActiveRequests.Add(-1);
        }
    }
}
```

### 3. 工作流监控

```csharp
public class WorkflowMonitor
{
    private readonly ILogger<WorkflowMonitor> _logger;
    
    public async Task<T> MonitorAsync<T>(
        string workflowName, 
        Func<Task<T>> action,
        Dictionary<string, object?>? tags = null)
    {
        using var activity = AgentTelemetry.StartActivity($"workflow_{workflowName}", tags);
        var sw = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting workflow: {Workflow}", workflowName);
            
            var result = await action();
            
            sw.Stop();
            _logger.LogInformation("Workflow {Workflow} completed in {Duration}ms", 
                workflowName, sw.ElapsedMilliseconds);
            
            activity?.SetTag("duration_ms", sw.ElapsedMilliseconds);
            activity?.SetTag("status", "completed");
            
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Workflow {Workflow} failed after {Duration}ms", 
                workflowName, sw.ElapsedMilliseconds);
            
            activity?.SetTag("status", "failed");
            activity?.SetTag("error", ex.Message);
            
            throw;
        }
    }
}
```

### 4. 执行追踪服务

```csharp
public class TraceService
{
    private readonly ConcurrentDictionary<string, TraceSession> _sessions = new();
    
    public TraceSession StartSession(string sessionId)
    {
        var session = new TraceSession { Id = sessionId, StartTime = DateTime.UtcNow };
        _sessions[sessionId] = session;
        return session;
    }
    
    public void AddStep(string sessionId, TraceStep step)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Steps.Add(step);
        }
    }
    
    public TraceSession? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }
}

public class TraceSession
{
    public string Id { get; set; } = "";
    public DateTime StartTime { get; set; }
    public List<TraceStep> Steps { get; set; } = new();
}

public class TraceStep
{
    public string Name { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public long DurationMs { get; set; }
    public string Type { get; set; } = "";  // llm_call, tool_call, etc.
    public Dictionary<string, object?> Data { get; set; } = new();
}
```

---

## 📊 可视化数据结构

```csharp
// 用于前端时间轴展示
public class TimelineData
{
    public string SessionId { get; set; } = "";
    public List<TimelineEvent> Events { get; set; } = new();
}

public class TimelineEvent
{
    public long StartMs { get; set; }       // 相对于会话开始
    public long EndMs { get; set; }
    public string Type { get; set; } = "";  // llm_call, tool_call, agent_switch
    public string Label { get; set; } = "";
    public string? Color { get; set; }
    public object? Details { get; set; }
}
```

---

## ⚙️ 配置

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Admin.NET.Ai.Agent")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://localhost:4317")))
    .WithMetrics(metrics => metrics
        .AddMeter("Admin.NET.Ai")
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());
```

---

## 🚀 使用示例

```csharp
// 在 Controller 或 Service 中
var monitor = sp.GetRequiredService<WorkflowMonitor>();

var result = await monitor.MonitorAsync("multi_agent_discussion", async () =>
{
    // 业务逻辑
    return await orchestrator.RunDiscussionAsync(topic, rounds: 2);
}, new Dictionary<string, object?>
{
    ["topic"] = topic,
    ["rounds"] = 2
});

// 查看追踪
var traceService = sp.GetRequiredService<TraceService>();
var session = traceService.GetSession(sessionId);
foreach (var step in session.Steps)
{
    Console.WriteLine($"[{step.Timestamp}] {step.Name} ({step.DurationMs}ms)");
}
```
