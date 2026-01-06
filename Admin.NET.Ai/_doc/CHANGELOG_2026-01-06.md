# Admin.NET.Ai 重构变更记录

## 2026-01-06 MAF/MCP 整合升级

### 概述

本次重构将项目的 AI 基础设施升级到使用 Microsoft Agent Framework (MAF) 和官方 ModelContextProtocol SDK，删除了大量自定义实现。

---

## 1. DevUI 集成

### 新增文件
- **DevUIConfiguration.cs** - MAF DevUI 配置

### 使用方式
```csharp
// 服务注册
services.AddMafDevUI();

// 可选：注册演示 Agents
builder.AddDemoAgents();

// 端点映射
app.MapMafDevUI(app.Environment.IsDevelopment());
```

### 访问端点
| 端点 | 功能 |
|------|------|
| `/devui` | 可视化调试界面 |
| `/v1/responses` | OpenAI Responses API |

---

## 2. Workflow 重构 (MAF 原生)

### 删除文件
- `IWorkflowBuilder.cs`
- `GenericWorkflowBuilder.cs`
- `SequentialAgentWorkflow.cs`
- `WorkflowEvent.cs` (使用 MAF 原生事件)

### 重写文件
| 文件 | 变更 |
|------|------|
| `IWorkflowService.cs` | 使用 MAF `Workflow` 类型 |
| `WorkflowService.cs` | 使用 `WorkflowBuilder`, `InProcessExecution` |
| `WorkflowMonitor.cs` | 使用 MAF 原生事件 |
| `WorkflowDemo.cs` | 5 种演示模式 |

---

## 3. MCP 整合 (官方 SDK)

### 删除文件
- `IMcpClient.cs` - 自定义接口
- `IMcpResource.cs` - 包含 `IMcpPrompt`, `IMcpConnectionPool`, `IMcpConnection`
- `McpClientService.cs` - 自定义 HTTP/SSE 实现
- `McpConnectionPool.cs` - 自定义连接池
- `OfficialMcpClientService.cs` - 中间版本

### 重写文件
| 文件 | 变更 |
|------|------|
| `McpToolFactory.cs` | 使用官方 `McpClient` |
| `McpHealthCheck.cs` | 使用新 `McpToolFactory` |
| `LLMMcpOptions.cs` | 添加 `TransportType`, `Command`, `Arguments` |

### 新增包
```xml
<PackageReference Include="ModelContextProtocol" Version="0.5.0-preview.1" />
```

### 新 API
```csharp
// 加载所有 MCP 工具
var tools = await mcpFactory.LoadAllToolsAsync();

// 调用工具
var result = await mcpFactory.CallToolAsync("serverName", "toolName", args);

// 获取原生客户端
var client = await mcpFactory.GetClientAsync("serverName");
```

---

## 4. 配置更新

### McpServerConfig 新字段
```csharp
public string TransportType { get; set; } = "stdio";  // stdio 或 http
public string? Command { get; set; }                   // 启动命令
public string[] Arguments { get; set; } = [];          // 命令参数
```

### ServiceCollectionInit 更新
```csharp
// 旧
services.TryAddSingleton<IMcpClient, McpClientService>();
services.TryAddSingleton<IMcpConnectionPool, McpConnectionPool>();

// 新
services.TryAddSingleton<McpToolFactory>();
```

---

## 5. 构建状态

✅ Admin.NET.Ai 构建成功
