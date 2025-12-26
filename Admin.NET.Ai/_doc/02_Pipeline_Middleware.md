# 02. 管道化中间件体系 (Pipeline & Middleware)

## 🎯 设计思维 (Mental Model)
AI 应用的生产化面临最大的挑战是**可观测性**。当模型给出一个错误答案时，我们需要知道：
1.  **输入是什么?** (Prompt Payload)
2.  **上下文里带了什么?** (History & Context)
3.  **模型用了多少 Token?** (Usage & Cost)
4.  **接口响应了多久?** (Latency)

`Admin.NET.Ai` 模拟了 ASP.NET Core 的请求管道，引入了 `AiPipelineBuilder`。每一个 AI 请求不再是简单的 SDK 调用，而是一次**有生命周期的生命流转**。

---

## 🏗️ 架构设计
### 核心概念
- **`AiPipelineBuilder`**: 负责组装中间件，并生成最终的执行逻辑。
- **`RunMiddlewareContext`**: 管道的“上下文”，携带了请求数据、元数据、TraceId 等。
- **`IRunMiddleware`**: 中间件接口，包含 `InvokeAsync` 方法。
- **洋葱模型**: 中间件按照注册顺序层层包裹。最内层是真正的模型调用（`IChatClient`）。

---

## 🛠️ 技术实现 (Implementation)

### 1. 核心接口 (`Abstractions/IRunMiddleware.cs`)
```csharp
public interface IRunMiddleware
{
    Task<ChatResponse> InvokeAsync(RunMiddlewareContext context, NextRunMiddleware next);
}
```

### 2. 审计中间件实现 (`Middleware/AuditMiddleware.cs`)
审计中间件是可观测性的核心。它不关心业务，只负责“录像”。

```csharp
public async Task<ChatResponse> InvokeAsync(RunMiddlewareContext context, NextRunMiddleware next)
{
    // 1. 前置：准备记录
    var requestId = Guid.NewGuid();
    var startTime = DateTime.UtcNow;

    try 
    {
        // 2. 传递给下一个中间件 (或最终执行器)
        var response = await next(context);

        // 3. 后置：记录结果
        await _auditStore.SaveAuditLogAsync(requestId.ToString(), 
            JsonSerializer.Serialize(context.Request), 
            JsonSerializer.Serialize(response),
            ...);
        
        return response;
    }
    catch (Exception ex)
    {
        // 记录失败日志并重新抛出
        await _auditStore.SaveAuditLogAsync(requestId.ToString(), ..., "Failed");
        throw;
    }
}
```

### 3. Token 成本控制 (`Middleware/TokenMonitoringMiddleware.cs`)
该中间件解析模型返回的 `Usage` 对象，匹配预定义的费率表（Pricing Table），计算出本次调用的真实消耗币种及金额。

---

## 🚀 代码示例 (Usage Example)

### 中间件注册
在 `ServiceCollectionInit.cs` 中：
```csharp
services.TryAddScoped<CachingMiddleware>();
services.TryAddScoped<RateLimitingMiddleware>();
services.TryAddScoped<TokenMonitoringMiddleware>();
services.TryAddScoped<AuditMiddleware>();
```

### 管道构建与调用
```csharp
// 通过注入的 AiPipelineBuilder 构建执行委托
var pipeline = builder
    .UseMiddleware<AuditMiddleware>()
    .UseMiddleware<TokenMonitoringMiddleware>()
    .Build();

// 调用
var response = await pipeline.ExecuteAsync(context);
```

---

## 📊 监控界面集成
所有的中间件数据最终都会流向 `TraceService`。在前端 DevUI 中，你可以看到每一步中间件执行的耗时和产生的日志，形成一条清晰的 **Trace Timeline**。

---

## ⚙️ 成本控制配置
在 `LLMAgent.Features.json` 中配置：
```json
{
  "LLMFeatures": {
    "CostControl": {
      "Enabled": true,
      "Quotas": {
        "Default": 0.5, // 默认预算 0.5 元
        "UserGroup_VIP": 10.0
      },
      "Pricing": {
        "gpt-4o": { "InputPrice": 0.000005, "OutputPrice": 0.000015 }
      }
    }
  }
}
```
