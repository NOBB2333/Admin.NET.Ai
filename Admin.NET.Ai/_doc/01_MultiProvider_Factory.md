# 01. 多模型 Provider 抽象引擎 (Multi-Provider Factory)

## 🎯 设计思维 (Mental Model)
在企业级 AI 应用中，**解耦**是第一优先级。开发者不应该直接依赖于某一个特定的 SDK（如 OpenAI SDK 或 Azure SDK），因为模型市场变化极快。

`AiFactory` 的五星级设计目标是：
1.  **统一接口**: 无论底层是哪个厂家，通过 `IAiFactory` 获取的始终是 `Microsoft.Extensions.AI.IChatClient` 标准接口。
2.  **配置驱动与热重载**: 通过 JSON 配置文件动态增删模型，支持 **Runtime Hot Reload**，零停机更新配置。
3.  **高可用性**: 内置 **健康检查 (Health Check)** 和 **降级重试 (Fallback)** 机制，确保服务稳定性。
4.  **按需实例化 (Lazy Loading)**: 延迟初始化，只在真正使用时创建连接。
5.  **完整生命周期**: 实现 `IDisposable/IAsyncDisposable`，杜绝资源泄漏。

---

## 🏗️ 架构设计
### 核心组件
- **`IAiFactory`**: 增强型接口，提供获取 Client、Agent、健康检查、降级重试等能力。
- **`AiFactory`**: 具体实现类，管理所有已注册的客户端，监听 `IOptionsMonitor` 配置变更。
- **`ClientHealthStatus`**: 用于描述模型服务的即时健康状态。

---

## 🛠️ 技术实现 (Implementation)

### 1. 核心代码解析 (`Core/AiFactory.cs`)
利用 `IOptionsMonitor` 实现配置热重载，并结合 `ConcurrentDictionary` 进行管理：

```csharp
public class AiFactory : IAiFactory
{
    // ...
    public AiFactory(IOptionsMonitor<LLMClientsConfig> optionsMonitor, ...)
    {
        // 监听配置变更，自动刷新客户端
        _optionsChangeToken = _optionsMonitor.OnChange(OnConfigurationChanged);
    }
    
    private void OnConfigurationChanged(LLMClientsConfig newConfig)
    {
        _logger.LogInformation("LLM configuration changed, refreshing clients...");
        RefreshClient(null); // 刷新所有客户端
    }
}
```

### 2. 企业级特性

#### ✅ 健康检查 (Health Checks)
在调用模型前，可预先检测服务可用性：
```csharp
var health = await aiFactory.CheckHealthAsync("gpt-4o");
if (!health.IsHealthy) 
{
    // 告警或切换
}
```

#### ✅ 自动降级 (Fallback)
提供主备方案，当主模型不可用时自动切换：
```csharp
// 尝试获取 "gpt-4o"，如果失败则自动尝试 "gpt-4o-mini" 或 "deepseek"
var client = await aiFactory.GetChatClientWithFallbackAsync("gpt-4o", new[] { "gpt-4o-mini", "deepseek" });
```

---

## 🚀 代码示例 (Usage Example)

### 基础调用
```csharp
// 注入 IAiFactory
var aiFactory = serviceProvider.GetRequiredService<IAiFactory>();

// 获取默认模型
var client = aiFactory.GetDefaultChatClient();

// 获取可用客户端列表
var availableClients = aiFactory.GetAvailableClients();

// 发起请求
var response = await client.GetResponseAsync("你好，我是 Admin.NET");
```

### 跨框架集成 (获取 Semantic Kernel)
```csharp
// 获取预配置好的 Kernel
var kernel = aiFactory.GetClient<Kernel>("gpt-4o");

// 使用 Kernel 执行插件
var result = await kernel.InvokeAsync("MyPlugin", "MyFunction", new() { ["input"] = "..." });
```

---

## ⚙️ 相关配置
在 `LLMAgent.Clients.json` 中定义。修改此文件时，`AiFactory` 会自动检测并应用变更。
```json
{
  "LLMClients": {
    "DefaultProvider": "OpenAI",
    "Clients": {
      "OpenAI": {
        "Provider": "OpenAI",
        "ModelId": "gpt-4o",
        "ApiKey": "sk-...",
        "BaseUrl": "https://api.openai.com/v1"
      }
    }
  }
}
```
