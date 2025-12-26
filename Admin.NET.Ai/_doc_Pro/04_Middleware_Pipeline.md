# 中间件管道 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `CachingMiddleware.cs` | `Middleware/` | 语义缓存 |
| `RateLimitingMiddleware.cs` | `Middleware/` | 限流控制 |
| `TokenMonitoringMiddleware.cs` | `Middleware/` | Token 监控计费 |
| `AuditMiddleware.cs` | `Middleware/` | 审计日志 |
| `RetryMiddleware.cs` | `Middleware/` | 重试机制 |
| `LoggingMiddleware.cs` | `Middleware/` | 结构化日志 |
| `ContextInjectionMiddleware.cs` | `Middleware/` | 上下文注入 |
| `AiPipelineBuilder.cs` | `Core/` | 管道构建器 |

---

## 🏗️ 架构设计

### MEAI DelegatingChatClient 模式

```
Request → [Caching] → [RateLimiting] → [TokenMonitoring] → [Audit] → [LLM]
                                                                        ↓
Response ← [Caching] ← [RateLimiting] ← [TokenMonitoring] ← [Audit] ← [LLM]
```

所有中间件继承自 `DelegatingChatClient`：

```csharp
public class MyMiddleware : DelegatingChatClient
{
    public MyMiddleware(IChatClient innerClient) : base(innerClient) { }
    
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null, 
        CancellationToken ct = default)
    {
        // 前置逻辑
        var response = await base.GetResponseAsync(messages, options, ct);
        // 后置逻辑
        return response;
    }
}
```

---

## 🔧 各中间件详解

### 1. CachingMiddleware (语义缓存)

**目的**: 相似问题命中缓存，减少 LLM 调用

```csharp
public class CachingMiddleware : DelegatingChatClient
{
    private readonly ISemanticCache _cache;
    
    public override async Task<ChatResponse> GetResponseAsync(...)
    {
        var cacheKey = GenerateCacheKey(messages);
        
        // 1. 查缓存
        var cached = await _cache.GetAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("Cache HIT: {Key}", cacheKey);
            return DeserializeResponse(cached);
        }
        
        // 2. 调用下游
        var response = await base.GetResponseAsync(messages, options, ct);
        
        // 3. 存缓存
        await _cache.SetAsync(cacheKey, SerializeResponse(response), _options.CacheDuration);
        
        return response;
    }
    
    private string GenerateCacheKey(IEnumerable<ChatMessage> messages)
    {
        // 基于消息内容生成 Hash
        var content = string.Join("|", messages.Select(m => m.Text));
        return ComputeHash(content);
    }
}
```

**配置**:
```json
{
  "Caching": {
    "Enabled": true,
    "CacheDurationMinutes": 60,
    "SemanticSimilarityThreshold": 0.85
  }
}
```

---

### 2. RateLimitingMiddleware (限流)

**目的**: 控制请求频率，防止超出 API 配额

```csharp
public class RateLimitingMiddleware : DelegatingChatClient
{
    private readonly IRateLimiter _rateLimiter;
    
    public override async Task<ChatResponse> GetResponseAsync(...)
    {
        var userId = GetUserId();
        
        // 1. 尝试获取令牌
        if (!await _rateLimiter.TryAcquireAsync(userId))
        {
            throw new RateLimitExceededException($"Rate limit exceeded for user {userId}");
        }
        
        // 2. 通过后调用下游
        return await base.GetResponseAsync(messages, options, ct);
    }
}
```

**令牌桶算法**:
```csharp
public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly int _tokensPerMinute;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
    
    public async Task<bool> TryAcquireAsync(string key)
    {
        var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(_tokensPerMinute));
        return bucket.TryConsume(1);
    }
}
```

---

### 3. TokenMonitoringMiddleware (Token 计费)

**目的**: 统计 Token 使用量，计算成本

```csharp
public class TokenMonitoringMiddleware : DelegatingChatClient
{
    private readonly ITokenUsageStore _store;
    private readonly ICostCalculator _costCalculator;
    
    public override async Task<ChatResponse> GetResponseAsync(...)
    {
        var response = await base.GetResponseAsync(messages, options, ct);
        
        // 提取 Token 使用量
        var usage = response.Usage;
        if (usage != null)
        {
            var cost = _costCalculator.Calculate(
                modelId: options?.ModelId ?? "default",
                inputTokens: usage.InputTokens,
                outputTokens: usage.OutputTokens
            );
            
            await _store.RecordUsageAsync(new TokenUsageRecord
            {
                UserId = GetUserId(),
                ModelId = options?.ModelId,
                InputTokens = usage.InputTokens,
                OutputTokens = usage.OutputTokens,
                Cost = cost,
                Timestamp = DateTime.UtcNow
            });
        }
        
        return response;
    }
}
```

**成本计算**:
```csharp
public class ModelCostCalculator : ICostCalculator
{
    private readonly Dictionary<string, (decimal Input, decimal Output)> _prices = new()
    {
        ["gpt-4o"] = (0.005m, 0.015m),      // per 1K tokens
        ["gpt-4o-mini"] = (0.00015m, 0.0006m),
        ["deepseek-chat"] = (0.0001m, 0.0002m),
        ["qwen-plus"] = (0.0005m, 0.0015m),
    };
    
    public decimal Calculate(string modelId, int inputTokens, int outputTokens)
    {
        if (_prices.TryGetValue(modelId, out var price))
        {
            return (inputTokens / 1000m) * price.Input 
                 + (outputTokens / 1000m) * price.Output;
        }
        return 0;
    }
}
```

---

### 4. AuditMiddleware (审计日志)

**目的**: 记录所有请求/响应用于合规审计

```csharp
public class AuditMiddleware : DelegatingChatClient
{
    private readonly IAuditStore _auditStore;
    
    public override async Task<ChatResponse> GetResponseAsync(...)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = GetUserId(),
            RequestTime = DateTime.UtcNow,
            RequestMessages = messages.Select(m => new { m.Role, m.Text }).ToList()
        };
        
        try
        {
            var response = await base.GetResponseAsync(messages, options, ct);
            
            auditLog.ResponseTime = DateTime.UtcNow;
            auditLog.ResponseText = response.Messages.LastOrDefault()?.Text;
            auditLog.Success = true;
            
            return response;
        }
        catch (Exception ex)
        {
            auditLog.Success = false;
            auditLog.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            await _auditStore.SaveAsync(auditLog);
        }
    }
}
```

---

### 5. RetryMiddleware (重试)

**目的**: 处理瞬态错误，自动重试

```csharp
public class RetryMiddleware : DelegatingChatClient
{
    private readonly RetryOptions _options;
    
    public override async Task<ChatResponse> GetResponseAsync(...)
    {
        int attempt = 0;
        
        while (true)
        {
            try
            {
                return await base.GetResponseAsync(messages, options, ct);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < _options.MaxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(
                    _options.BaseDelayMs * Math.Pow(2, attempt)); // 指数退避
                
                _logger.LogWarning("Retry {Attempt}/{Max} after {Delay}ms: {Error}",
                    attempt, _options.MaxRetries, delay.TotalMilliseconds, ex.Message);
                    
                await Task.Delay(delay, ct);
            }
        }
    }
    
    private bool IsTransient(Exception ex)
    {
        return ex is HttpRequestException 
            || ex is TimeoutException
            || (ex is ApiException api && api.StatusCode >= 500);
    }
}
```

---

## 🔌 管道构建

### AiPipelineBuilder

```csharp
public class AiPipelineBuilder
{
    public IChatClient Build(IChatClient innerClient)
    {
        // 从内到外包装
        var client = innerClient;
        
        client = new RetryMiddleware(client, _retryOptions);
        client = new AuditMiddleware(client, _auditStore);
        client = new TokenMonitoringMiddleware(client, _tokenStore, _costCalculator);
        client = new RateLimitingMiddleware(client, _rateLimiter);
        client = new CachingMiddleware(client, _cache);
        
        return client;
    }
}
```

### 在 AiFactory 中使用

```csharp
public IChatClient? GetChatClient(string name)
{
    var innerClient = CreateInnerClient(name);
    return _pipelineBuilder.Build(innerClient);
}
```

---

## 📊 流式响应处理

```csharp
public override async IAsyncEnumerable<StreamingChatResponse> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // 流式响应需要特殊处理
    var totalTokens = 0;
    
    await foreach (var chunk in base.GetStreamingResponseAsync(messages, options, ct))
    {
        totalTokens += chunk.Contents.OfType<TextContent>().Sum(t => EstimateTokens(t.Text));
        yield return chunk;
    }
    
    // 流结束后记录使用量
    await RecordUsageAsync(totalTokens);
}
```

---

## ⚠️ 注意事项

1. **顺序重要**: 中间件顺序影响行为 (缓存应在最外层)
2. **流式兼容**: 必须同时覆盖 `GetResponseAsync` 和 `GetStreamingResponseAsync`
3. **异常传播**: 异常应正确向上传播，除非是重试场景
4. **线程安全**: 中间件应是无状态的或使用线程安全的状态管理
5. **性能**: 避免在热路径上做耗时操作
