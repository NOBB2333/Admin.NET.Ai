# Admin.NET.Ai 项目架构总览

## 📁 目录结构

```
Admin.NET.Ai/
├── Abstractions/           # 接口定义
│   ├── IAiFactory.cs       # AI 工厂接口
│   ├── IChatClient.cs      # 对话客户端 (MEAI)
│   ├── IMediaGenerationService.cs  # 媒体生成
│   └── ...
├── Agents/                 # 内置 Agent
│   └── BuiltIn/
│       ├── SentimentAnalysisAgent.cs
│       ├── KnowledgeGraphAgent.cs
│       └── QualityAssessmentAgent.cs
├── Configuration/          # JSON 配置文件
│   ├── LLMAgent.Clients.json
│   ├── LLMAgent.Media.json
│   ├── LLMAgent.Mcp.json
│   └── ...
├── Core/                   # 核心实现
│   ├── Adapters/           # 适配器
│   │   └── UriImageAdapter.cs
│   ├── AiFactory.cs
│   └── AiPipelineBuilder.cs
├── Middleware/             # 中间件
│   ├── CachingMiddleware.cs
│   ├── RateLimitingMiddleware.cs
│   ├── TokenMonitoringMiddleware.cs
│   ├── AuditMiddleware.cs
│   └── ...
├── Options/               # 配置类
│   ├── LLMClientsConfig.cs
│   ├── LLMMediaOptions.cs
│   └── ...
├── Services/              # 服务实现
│   ├── MCP/               # MCP 协议
│   │   ├── Attributes/
│   │   │   └── McpToolAttribute.cs
│   │   ├── McpToolDiscoveryService.cs
│   │   ├── McpEndpoints.cs
│   │   └── ...
│   ├── Media/             # 媒体生成
│   │   └── MediaGenerationService.cs
│   ├── Rag/               # RAG 服务
│   │   ├── RagService.cs
│   │   └── GraphRagService.cs
│   ├── Tools/             # 工具管理
│   ├── Workflow/          # 工作流
│   │   ├── MultiAgentOrchestrator.cs
│   │   ├── EnhancedMultiAgentOrchestrator.cs
│   │   └── NatashaScriptEngine.cs
│   └── ...
├── _doc/                  # 用户文档
└── _doc_Pro/              # 技术详解
```

---

## 🏗️ 核心模块

### 1. AiFactory (多供应商工厂)

- **文件**: `Core/AiFactory.cs`
- **接口**: `IAiFactory`
- **功能**: 统一管理多个 LLM 供应商

```csharp
var client = aiFactory.GetChatClient("deepseek");
var defaultClient = aiFactory.GetDefaultChatClient();
var withFallback = await aiFactory.GetChatClientWithFallbackAsync("gpt-4", ["deepseek"]);
```

### 2. Middleware (中间件管道)

- **位置**: `Middleware/`
- **模式**: `DelegatingChatClient` 链式调用

| 中间件 | 功能 |
|--------|------|
| CachingMiddleware | 语义缓存 |
| RateLimitingMiddleware | 限流 |
| TokenMonitoringMiddleware | Token 计费 |
| AuditMiddleware | 审计日志 |
| RetryMiddleware | 重试 |

### 3. MCP Tool Discovery

- **位置**: `Services/MCP/`
- **核心**: `[McpTool]` 属性自动发现

```csharp
[McpTool("获取天气信息")]
public WeatherInfo GetWeather(string city) { ... }
```

### 4. Multi-Agent Orchestrator

- **位置**: `Services/Workflow/`
- **两个版本**:
  - `MultiAgentOrchestrator`: 单供应商
  - `EnhancedMultiAgentOrchestrator`: 多供应商+工具

### 5. Media Generation

- **位置**: `Services/Media/`
- **功能**: TTS, ASR, ImageGen, VideoGen

### 6. RAG

- **位置**: `Services/Rag/`
- **实现**: Vector RAG, Graph RAG

### 7. Scripting

- **位置**: `Services/Workflow/NatashaScriptEngine.cs`
- **功能**: 热重载 C# 脚本

---

## 🔌 依赖注入

### ServiceCollectionInit.cs

```csharp
services.AddAdminNetAi(configuration);

// 自动注册:
// - IAiFactory -> AiFactory
// - IMediaGenerationService -> MediaGenerationService
// - McpToolDiscoveryService
// - 所有中间件
// - RAG 服务
// - 工具管理器
```

---

## 📊 配置文件

| 文件 | 用途 |
|------|------|
| `LLMAgent.Clients.json` | LLM 供应商配置 |
| `LLMAgent.Media.json` | 媒体生成配置 |
| `LLMAgent.Mcp.json` | MCP 服务器配置 |
| `LLMAgent.Features.json` | 功能开关 |
| `LLMAgent.Rag.json` | RAG 配置 |

---

## 🧪 Demo 演示

| 编号 | Demo | 说明 |
|------|------|------|
| 1 | ChatDemo | 基础对话 |
| 2 | WorkflowDemo | 多 Agent 工作流 |
| 3 | StructuredOutputDemo | 结构化输出 |
| 4 | ToolDemo | 工具调用 |
| 5 | ScriptingDemo | 热重载脚本 |
| 6 | CompressionDemo | 上下文压缩 |
| 7 | PromptDemo | 提示词模板 |
| 8 | RagDemo | RAG 检索 |
| 9 | MultimodalDemo | 多模态 |
| 10 | PersistenceDemo | 对话持久化 |
| 12 | BuiltInAgentDemo | 内置 Agent |
| 13 | MiddlewareDemo | 中间件 |
| 14 | McpDemo | MCP 协议 |
| 15 | MonitoringDemo | 监控指标 |
| 16 | StorageDemo | 存储策略 |
| 17 | MediaDemo | 媒体生成 |

---

## 🔗 关键接口

```csharp
// AI 工厂
IAiFactory.GetChatClient(string name)
IAiFactory.GetDefaultChatClient()
IAiFactory.GetAvailableClients()

// 媒体生成
IMediaGenerationService.GenerateImageAsync(ImageGenRequest)
IMediaGenerationService.TextToSpeechAsync(TtsRequest)

// MCP 工具发现
McpToolDiscoveryService.DiscoverFromAssembly(Assembly)
McpToolDiscoveryService.ExecuteToolAsync(string tool, Dictionary<string, object?> args)

// 多 Agent
EnhancedMultiAgentOrchestrator.AddAgent(name, prompt, provider, tools)
EnhancedMultiAgentOrchestrator.RunDiscussionAsync(topic, rounds)
```
