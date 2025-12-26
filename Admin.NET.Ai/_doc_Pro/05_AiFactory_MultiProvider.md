# AiFactory 多供应商工厂 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `IAiFactory.cs` | `Abstractions/` | 接口定义 |
| `AiFactory.cs` | `Core/` | 具体实现 |
| `LLMClientsConfig.cs` | `Options/` | 配置类 |
| `LLMAgent.Clients.json` | `Configuration/` | 供应商配置 |
| `AiPipelineBuilder.cs` | `Core/` | 管道构建 |

---

## 🏗️ 接口设计

```csharp
public interface IAiFactory : IDisposable, IAsyncDisposable
{
    // 核心获取
    IChatClient? GetChatClient(string name);
    IChatClient? GetDefaultChatClient();
    
    // 降级重试
    Task<IChatClient> GetChatClientWithFallbackAsync(
        string name, 
        IEnumerable<string>? fallbackNames = null, 
        CancellationToken ct = default);
    
    // 发现与管理
    IReadOnlyList<string> GetAvailableClients();
    string? DefaultProvider { get; }
    void RefreshClient(string? name = null);
    
    // 健康检查
    Task<ClientHealthStatus> CheckHealthAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<ClientHealthStatus>> CheckAllHealthAsync(CancellationToken ct = default);
    
    // Agent 管理
    TAgent? GetAgent<TAgent>(string name, string? instructions = null) where TAgent : class;
    TAgent? CreateAgent<TAgent>(string clientName, string agentName, string? instructions) where TAgent : class;
}
```

---

## 🔧 核心实现

### 懒加载与缓存

```csharp
public class AiFactory : IAiFactory
{
    private readonly ConcurrentDictionary<string, Lazy<IChatClient>> _clientCache = new();
    private readonly IOptionsMonitor<LLMClientsConfig> _optionsMonitor;
    private readonly AiPipelineBuilder _pipelineBuilder;
    
    public IChatClient? GetChatClient(string name)
    {
        if (!_config.Clients.ContainsKey(name))
        {
            _logger.LogWarning("Client '{Name}' not found in configuration", name);
            return null;
        }
        
        // 懒加载: 只在首次访问时创建
        var lazyClient = _clientCache.GetOrAdd(name, key => 
            new Lazy<IChatClient>(() => CreateClient(key)));
            
        return lazyClient.Value;
    }
    
    private IChatClient CreateClient(string name)
    {
        var config = _config.Clients[name];
        
        // 1. 创建原始客户端
        IChatClient innerClient = config.Provider switch
        {
            "OpenAI" => CreateOpenAiClient(config),
            "AzureOpenAI" => CreateAzureClient(config),
            "Ollama" => CreateOllamaClient(config),
            _ => CreateGenericOpenAiClient(config)  // 兼容 OpenAI 格式的供应商
        };
        
        // 2. 包装中间件管道
        return _pipelineBuilder.Build(innerClient);
    }
}
```

### 配置热重载

```csharp
public AiFactory(IOptionsMonitor<LLMClientsConfig> optionsMonitor, ...)
{
    _optionsMonitor = optionsMonitor;
    _config = optionsMonitor.CurrentValue;
    
    // 监听配置变更
    _optionsChangeToken = optionsMonitor.OnChange(OnConfigurationChanged);
}

private void OnConfigurationChanged(LLMClientsConfig newConfig)
{
    _logger.LogInformation("LLM configuration changed, refreshing all clients...");
    _config = newConfig;
    RefreshClient(null);  // 清除所有缓存
}

public void RefreshClient(string? name = null)
{
    if (name == null)
    {
        _clientCache.Clear();
    }
    else
    {
        _clientCache.TryRemove(name, out _);
    }
}
```

### 健康检查

```csharp
public async Task<ClientHealthStatus> CheckHealthAsync(string name, CancellationToken ct = default)
{
    var sw = Stopwatch.StartNew();
    
    try
    {
        var client = GetChatClient(name);
        if (client == null)
        {
            return new ClientHealthStatus(name, false, ErrorMessage: "Client not found");
        }
        
        // 发送简单测试请求
        var response = await client.GetResponseAsync("ping", cancellationToken: ct);
        
        sw.Stop();
        return new ClientHealthStatus(name, true, ResponseTime: sw.Elapsed);
    }
    catch (Exception ex)
    {
        sw.Stop();
        return new ClientHealthStatus(name, false, ResponseTime: sw.Elapsed, ErrorMessage: ex.Message);
    }
}
```

### 降级重试

```csharp
public async Task<IChatClient> GetChatClientWithFallbackAsync(
    string name, 
    IEnumerable<string>? fallbackNames = null, 
    CancellationToken ct = default)
{
    // 尝试主供应商
    var health = await CheckHealthAsync(name, ct);
    if (health.IsHealthy)
    {
        return GetChatClient(name)!;
    }
    
    _logger.LogWarning("Primary client '{Name}' unhealthy, trying fallbacks...", name);
    
    // 尝试备用供应商
    foreach (var fallback in fallbackNames ?? Enumerable.Empty<string>())
    {
        health = await CheckHealthAsync(fallback, ct);
        if (health.IsHealthy)
        {
            _logger.LogInformation("Fallback to '{Fallback}'", fallback);
            return GetChatClient(fallback)!;
        }
    }
    
    throw new InvalidOperationException($"All clients unavailable: {name}, {string.Join(", ", fallbackNames ?? [])}");
}
```

---

## 📊 供应商适配

### OpenAI 兼容供应商

```csharp
private IChatClient CreateGenericOpenAiClient(ClientConfig config)
{
    // 大多数供应商兼容 OpenAI API 格式
    return new OpenAIClient(new ApiKeyCredential(config.ApiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri(config.BaseUrl ?? "https://api.openai.com/v1")
    }).GetChatClient(config.ModelId);
}
```

### 支持的供应商

| 供应商 | Provider 值 | BaseUrl |
|--------|-------------|---------|
| OpenAI | `OpenAI` | https://api.openai.com/v1 |
| Azure OpenAI | `AzureOpenAI` | https://{resource}.openai.azure.com/ |
| DeepSeek | `DeepSeek` | https://api.deepseek.com |
| 通义千问 | `Qwen` | https://dashscope.aliyuncs.com/compatible-mode/v1 |
| Ollama | `Ollama` | http://localhost:11434 |

---

## ⚙️ 配置示例

### LLMAgent.Clients.json

```json
{
  "LLM-Clients": {
    "DefaultProvider": "qwen-plus",
    "Clients": {
      "gpt-4o": {
        "Provider": "OpenAI",
        "ModelId": "gpt-4o",
        "ApiKey": "sk-xxx",
        "BaseUrl": "https://api.openai.com/v1"
      },
      "deepseek-chat": {
        "Provider": "DeepSeek",
        "ModelId": "deepseek-chat",
        "ApiKey": "sk-xxx",
        "BaseUrl": "https://api.deepseek.com"
      },
      "qwen-plus": {
        "Provider": "Qwen",
        "ModelId": "qwen-plus",
        "ApiKey": "sk-xxx",
        "BaseUrl": "https://dashscope.aliyuncs.com/compatible-mode/v1"
      },
      "local-llama": {
        "Provider": "Ollama",
        "ModelId": "llama3.2",
        "BaseUrl": "http://localhost:11434"
      }
    }
  }
}
```

---

## 🚀 使用示例

```csharp
// 注入工厂
var aiFactory = sp.GetRequiredService<IAiFactory>();

// 获取默认客户端
var client = aiFactory.GetDefaultChatClient();

// 获取指定供应商
var deepseek = aiFactory.GetChatClient("deepseek-chat");

// 获取可用列表
var available = aiFactory.GetAvailableClients();
// ["gpt-4o", "deepseek-chat", "qwen-plus", "local-llama"]

// 带降级的获取
var reliable = await aiFactory.GetChatClientWithFallbackAsync(
    "gpt-4o", 
    fallbackNames: ["deepseek-chat", "qwen-plus"]);

// 健康检查
var health = await aiFactory.CheckHealthAsync("gpt-4o");
if (!health.IsHealthy)
{
    Console.WriteLine($"Unhealthy: {health.ErrorMessage}");
}
```

---

## ⚠️ 注意事项

1. **生命周期**: `AiFactory` 应注册为 Singleton
2. **资源释放**: 实现 `IDisposable`/`IAsyncDisposable`
3. **线程安全**: 使用 `ConcurrentDictionary` 和 `Lazy<T>`
4. **配置敏感**: API Key 不应硬编码，使用环境变量或密钥管理
5. **超时设置**: 健康检查应有合理的超时时间
