# 05. MCP (Model Context Protocol) 集成

## 🎯 设计思维 (Mental Model)
Agent 必须具备与外部世界交互的能力。但传统的 Tool 调用需要开发者为每一个 API 写 C# 函数。
**MCP (模型上下文协议)** 是由 Anthropic 提出，微软深度跟进的一套标准。它的核心逻辑是：**"工具即服务"**。

通过 MCP，Agent 可以：
1.  **动态发现工具**: 只要配置一个 MCP Server 的 URL 或进程命令，Agent 就能自动知道它有哪些 Function。
2.  **安全隔离**: 工具执行在另一个独立的进程（MCP Server）中，主程序更安全。
3.  **标准化**: 无论是查数据库、读 GitHub 还是搜 Google，都遵循同一套 JSON-RPC 规范。

---

## 🏗️ 架构设计 (2026-01 更新)

### 核心组件

| 组件 | 位置 | 说明 |
|------|------|------|
| `McpToolFactory` | `Services/MCP/` | **核心** - 使用官方 SDK 的工具工厂 |
| `McpToolAttribute` | `Services/MCP/Attributes/` | 标记方法为 MCP 工具 |
| `McpToolDiscoveryService` | `Services/MCP/` | 自动发现并注册本地工具 |
| `McpEndpoints` | `Services/MCP/` | HTTP/SSE 端点 |
| `McpHealthCheck` | `Services/MCP/` | 健康检查 |

### 依赖包
```xml
<PackageReference Include="ModelContextProtocol" Version="0.5.0-preview.1" />
```

---

## ✨ 新 API: McpToolFactory

### 加载所有服务器工具
```csharp
// 注入工厂
var factory = sp.GetRequiredService<McpToolFactory>();

// 加载配置中所有启用服务器的工具
var tools = await factory.LoadAllToolsAsync();

// 工具直接实现 AITool，可用于 ChatOptions
var options = new ChatOptions { Tools = tools };
```

### 调用指定工具
```csharp
var result = await factory.CallToolAsync(
    "serverName", 
    "toolName", 
    new Dictionary<string, object?> { ["param"] = "value" }
);
```

### 获取原生 SDK 客户端
```csharp
var client = await factory.GetClientAsync("serverName");
// 使用 SDK 原生 API
var resources = await client.ListResourcesAsync();
var prompts = await client.ListPromptsAsync();
```

---

## ⚙️ 配置

### 支持两种传输方式

#### 1. Stdio (默认) - 启动本地进程
```json
{
  "LLM-Mcp": {
    "Servers": [
      {
        "Name": "Calendar",
        "Enabled": true,
        "TransportType": "stdio",
        "Command": "dnx",
        "Arguments": ["Mcp.CN.Calendar@", "--yes"]
      }
    ]
  }
}
```

#### 2. HTTP/SSE - 连接远程服务
```json
{
  "LLM-Mcp": {
    "Servers": [
      {
        "Name": "GitHub",
        "Enabled": true,
        "TransportType": "http",
        "Url": "http://localhost:3000/sse"
      }
    ]
  }
}
```

### McpServerConfig 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `Name` | string | 服务器名称 |
| `Enabled` | bool | 是否启用 |
| `TransportType` | string | `"stdio"` 或 `"http"` |
| `Command` | string? | Stdio 启动命令 |
| `Arguments` | string[] | Stdio 命令参数 |
| `Url` | string | HTTP 服务地址 |

---

## ✨ 本地工具: [McpTool] 属性

### 使用方式

```csharp
// ✅ 方式1: 只传描述，名称自动取方法名 (get_current_time)
[McpTool("获取当前服务器时间")]
public DateTime GetCurrentTime()
{
    return DateTime.Now;
}

// ✅ 方式2: 显式指定名称和描述
[McpTool("get_weather", "根据城市名称获取天气信息")]
public WeatherInfo GetWeather(
    [McpParameter("城市名称")] string city,
    [McpParameter("温度单位")] string unit = "celsius")
{
    // 实现...
}
```

---

## 🚀 完整示例

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAdminNetAi(builder.Configuration);

var app = builder.Build();

// 发现本地 [McpTool] 方法
var discovery = app.Services.GetRequiredService<McpToolDiscoveryService>();
discovery.DiscoverFromAssembly(typeof(Program).Assembly);

// 映射 MCP 端点
app.MapMcpEndpoints();

app.Run();
```

```csharp
// 在 Agent 中使用
var factory = sp.GetRequiredService<McpToolFactory>();
var mcpTools = await factory.LoadAllToolsAsync();

var response = await chatClient.GetResponseAsync(
    "今天是农历几月几日？",
    new ChatOptions { Tools = mcpTools }
);
```

---

## 🔄 双向能力

| 角色 | 说明 |
|------|------|
| **MCP Client** | 连接外部 MCP Server (使用 `McpToolFactory`) |
| **MCP Server** | 暴露本地方法 (使用 `[McpTool]` 属性) |
