# 05. MCP (Model Context Protocol) 集成

## 🎯 设计思维 (Mental Model)
Agent 必须具备与外部世界交互的能力。但传统的 Tool 调用需要开发者为每一个 API 写 C# 函数。
**MCP (模型上下文协议)** 是由 Anthropic 提出，微软深度跟进的一套标准。它的核心逻辑是：**"工具即服务"**。

通过 MCP，Agent 可以：
1.  **动态发现工具**: 只要配置一个 MCP Server 的 URL，Agent 就能自动知道它有哪些 Function。
2.  **安全隔离**: 工具执行在另一个独立的进程（MCP Server）中，主程序更安全。
3.  **标准化**: 无论是查数据库、读 GitHub 还是搜 Google，都遵循同一套 JSON-RPC 规范。

---

## 🏗️ 架构设计

### 核心组件

| 组件 | 位置 | 说明 |
|------|------|------|
| `McpToolAttribute` | `Services/MCP/Attributes/` | 标记方法为 MCP 工具 |
| `McpToolDiscoveryService` | `Services/MCP/` | 自动发现并注册工具 |
| `McpEndpoints` | `Services/MCP/` | HTTP/SSE 端点 |
| `McpClientService` | `Services/Tools/` | MCP 客户端 (调用外部 MCP) |
| `McpConnectionPool` | `Services/MCP/` | 连接池管理 |

---

## ✨ 核心特性: [McpTool] 属性自动发现

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

### 属性选项

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | string? | 工具名称 (null=使用方法名转 snake_case) |
| `Description` | string | 工具描述 (必填) |
| `Category` | string? | 分类标签 |
| `RequiresApproval` | bool | 是否需要审批 |
| `TimeoutSeconds` | int | 超时时间 (默认30秒) |

---

## 🛠️ 技术实现

### 1. 自动发现流程

```
ASP.NET 启动
    ↓
McpToolDiscoveryService.DiscoverFromAssembly()
    ↓
扫描所有 [McpTool] 标记的方法
    ↓
构建 JSON Schema (参数类型/描述)
    ↓
注册到内部字典
    ↓
通过 /mcp/tools 或 SSE 暴露给客户端
```

### 2. MCP 端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/mcp/sse` | GET | SSE 长连接，推送工具列表 |
| `/mcp/tools` | GET | REST 获取工具列表 |
| `/mcp/call` | POST | 调用工具 `{tool: "name", arguments: {...}}` |
| `/mcp/messages` | POST | 标准 MCP 协议消息 |

### 3. 工具调用流程

```csharp
// POST /mcp/call
{
    "tool": "get_weather",
    "arguments": { "city": "北京", "unit": "celsius" }
}

// Response
{
    "success": true,
    "result": { "city": "北京", "temperature": 15, ... }
}
```

---

## 🚀 代码示例

### 在 ASP.NET Core 中启用

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAdminNetAi(builder.Configuration);

var app = builder.Build();

// 发现当前程序集的 [McpTool]
var discovery = app.Services.GetRequiredService<McpToolDiscoveryService>();
discovery.DiscoverFromAssembly(typeof(Program).Assembly);

// 映射 MCP 端点
app.MapMcpEndpoints();

app.Run();
```

### 作为 MCP Client 调用外部服务

```csharp
// 连接到外部 MCP Server
await mcpClient.ConnectAsync("Filesystem");

// 获取工具列表
var tools = await mcpClient.GetToolsAsync("Filesystem");

// 调用工具
var result = await mcpClient.CallToolAsync("Filesystem", "read_file", 
    new Dictionary<string, object> { ["path"] = "/etc/hosts" });
```

---

## ⚙️ 配置

### MCP Server 配置 (`LLMAgent.Mcp.json`)
```json
{
  "LLM-Mcp": {
    "Servers": [
      {
        "Name": "Filesystem",
        "Url": "http://localhost:3001/sse",
        "Enabled": true
      },
      {
        "Name": "GitHub",
        "Url": "http://localhost:3002/sse"
      }
    ]
  }
}
```

---

## 🔄 双向能力

| 角色 | 说明 |
|------|------|
| **MCP Client** | 调用外部 MCP Server (如 Claude Desktop 提供的工具) |
| **MCP Server** | 暴露系统 API，让外部 AI 调用 (如让 Claude 调用业务接口) |

通过 `[McpTool]` 属性，任何业务方法都可以一键暴露为 MCP 工具！
