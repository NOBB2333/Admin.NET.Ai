# Admin.NET.Ai

<div align="center">

**[English](./README_EN.md)** | **中文**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![MEAI](https://img.shields.io/badge/Microsoft.Extensions.AI-✓-blue)](https://www.nuget.org/packages/Microsoft.Extensions.AI)

</div>

---

Admin.NET.Ai 是基于 **.NET 10** 构建的企业级 AI 能力核心类库。采用管道中间件模型（Pipeline/Middleware），深度集成 `Microsoft.Extensions.AI` (MEAI) 与 `Microsoft.Agents` 框架。

> [!IMPORTANT]
> **环境要求**：项目依赖 .NET 10 特性。推荐使用 [mise](https://mise.jdx.dev/) 管理环境，执行 `mise use dotnet` 激活 SDK。

---

## ✨ 核心特性

| 特性 | 描述 |
| :--- | :--- |
| 🔌 **多模型统一接入** | 无缝切换 OpenAI, DeepSeek, Qwen, Gemini, Ollama 等 |
| 🤖 **多 Agent 协作** | 顺序/并发/编排者/圆桌讨论模式，LLM 自主 Agent 发现与调度 |
| 🔧 **增强工具系统** | 文件系统/搜索/Shell 工具 + 自管理审批 + MCP 协议工具发现 |
| 🔧 **MCP 工具发现** | `[McpTool]` 属性一键暴露方法为 MCP 工具 |
| 🎨 **媒体生成** | TTS/ASR/图像生成/视频生成，多供应商支持 |
| 📚 **混合策略 RAG** | 向量检索 + Neo4j GraphRAG + 重排 (Rerank) |
| ⚡ **三层中间件管道** | Chat 管道（Token/成本/缓存）+ Tool 管道（审批/监控/验证）+ 限流/审计 |
| 🗜️ **三区上下文压缩** | 首轮保留 + LLM 摘要中间历史 + 近期消息保留 |
| 🔥 **热重载脚本** | Natasha C# 脚本引擎，动态更新 Agent 逻辑 |
| 📊 **全链路可观测** | Trace 时间轴 + DevUI 可视化调试 |


### 📋 功能演示全景 (Console)

| # | 分类 | 功能模块 | 说明 |
| :---: | :--- | :--- | :--- |
| **★1** | **综合** | **综合性对话智能体** | **All-in-One Agent，全部工具/Agent 自动加载** |
| 2 | 对话基础 | 基础对话与中间件 | Chat, Audit, Tokens |
| 3 | 对话基础 | 提示词工程 | Prompt Templates |
| 4 | 对话基础 | 结构化数据提取 | JSON Schema, TOON |
| 5 | 对话基础 | 代码生成助手 | Structured Output |
| 6 | 对话基础 | 多模态能力 | Vision & Audio |
| 7 | 工具系统 | 智能工具与审批流 | Discover, Approval |
| 8 | 工具系统 | 增强工具系统 | FileSystem/Search/Shell |
| 9 | 工具系统 | MCP 协议 | 外部工具集成 |
| 10 | 工具系统 | MCP 日历助手 | 官方 SDK 工具调用 |
| 11 | 工具系统 | MCP MiniApi 服务 | 外部工具集成 |
| 12 | Agent | 内置 Agent | 情感/知识图谱/质量评估 |
| 13 | Agent | LLM Agent 自主调度 | Auto-Discovery |
| 14 | Agent | 多 Agent 工作流 | MAF Sequential & Autonomous |
| 15 | Agent | 多 Agent 文档审核 | Writer→Reviewer→Editor |
| 16 | Agent | 客服智能分流 | 意图识别+路由 |
| 17 | 数据 | RAG 知识检索 | GraphRAG & Vector |
| 18 | 数据 | RAG + Agent 智能问答 | 知识库+推理 |
| 19 | 数据 | 上下文压缩策略 | 三区压缩/摘要/计数 |
| 20 | 数据 | 对话持久化 | Thread & Database |
| 21 | 基础设施 | 中间件详解 | Middleware Stack |
| 22 | 基础设施 | 内容安全过滤 | 敏感词替换+PII脱敏 |
| 23 | 基础设施 | 监控与指标 | OpenTelemetry |
| 24 | 基础设施 | 存储策略 | Hot/Cold/Vector |
| 25 | 基础设施 | 动态脚本热重载 | Natasha Scripting |
| 26 | 综合场景 | 综合场景应用 | Real-world Scenario |
| 27 | 综合场景 | 媒体生成 | TTS/ASR/图像/视频 |

---

## 🚀 快速开始

### 1. 安装依赖
```bash
dotnet add package Admin.NET.Ai  # 当前没上传，要手动添加引用项目
```

### 2. 注册服务
```csharp
services.AddAdminNetAi(configuration);
```

### 3. 使用示例

#### 基础对话
```csharp
var aiFactory = sp.GetRequiredService<IAiFactory>();
var client = aiFactory.GetDefaultChatClient();
var response = await client.GetResponseAsync("你好，我是 Admin.NET");
```

#### 增强工具系统（自动发现 + 上下文注入）
```csharp
var toolManager = sp.GetRequiredService<ToolManager>();
var context = new ToolExecutionContext
{
    WorkingDirectory = Directory.GetCurrentDirectory(),
    UserId = "user-001"
};
var functions = toolManager.GetAllAiFunctions(context); // 自动扫描全部工具并注入上下文
```

#### 多 Agent 协作
```csharp
var orchestrator = new EnhancedMultiAgentOrchestrator(aiFactory);
orchestrator
    .AddAgent("技术专家", "从技术角度分析", provider: "qwen")
    .AddAgent("产品经理", "从产品角度分析", provider: "deepseek");
    
await foreach (var evt in orchestrator.RunDiscussionAsync("AI 对开发的影响", rounds: 2))
{
    Console.Write(evt.Content);
}
```

#### MCP 工具
```csharp
[McpTool("获取天气信息")]
public WeatherInfo GetWeather([McpParameter("城市")] string city)
{
    return new WeatherInfo { City = city, Temperature = 20 };
}
```

---

## 🏗️ 架构

```
Admin.NET.Ai/
├── Abstractions/        # 接口: IAiFactory, IAiAgent, IAiCallableFunction, IChatReducer
├── Core/                # AiFactory, PipelineBuilder
├── Middleware/
│   ├── TokenMonitoringMiddleware   # Chat 管道: Token/成本/换行
│   ├── ToolValidationMiddleware    # Tool 管道: 权限/审批/参数/沙箱/脱敏
│   └── ToolMonitoringMiddleware    # Tool 管道: 分类日志/耗时
├── Services/
│   ├── Tools/           # ToolManager + FileSystem/Search/Shell/AgentDispatch
│   ├── Context/         # ChatReducerFactory (ThreeZone/Summarizing/MessageCounting)
│   ├── MCP/             # MCP 协议 + 工具发现
│   ├── Media/           # TTS/ASR/ImageGen/VideoGen
│   ├── Rag/             # Vector + GraphRAG
│   └── Workflow/        # 多 Agent 协作引擎
├── Configuration/       # JSON 配置文件
├── _doc/                # 用户文档
└── _doc_Pro/            # 技术详解
```

### 中间件职责划分

```
请求 → TokenMonitoringMiddleware (Token/成本)
         ↓
      LLM 决定调用工具
         ↓
      ToolValidationMiddleware (权限 → 审批 → 参数 → 沙箱 → 脱敏)
         ↓
      ToolMonitoringMiddleware (🔧Tool / 🤖Agent / ⚡Skill 分类日志)
         ↓
      实际工具执行
```

---

## ⚙️ 配置

### LLMAgent.Clients.json (供应商)
```json
{
  "LLM-Clients": {
    "DefaultProvider": "qwen-plus",
    "Clients": {
      "qwen-plus": { "Provider": "Qwen", "ModelId": "qwen-plus", "ApiKey": "sk-xxx" },
      "deepseek": { "Provider": "DeepSeek", "ModelId": "deepseek-chat", "ApiKey": "sk-xxx" }
    }
  }
}
```

### LLMAgent.Mcp.json (MCP 服务器)
```json
{
  "LLM-Mcp": {
    "Servers": [
      { "Name": "Filesystem", "Url": "http://localhost:3001/sse" }
    ]
  }
}
```

---

## 📖 文档

- **用户文档**: `_doc/` - 功能介绍与使用示例
- **技术详解**: `_doc_Pro/` - 实现细节与源码解析

---

## 🎯 演示

运行控制台演示:
```bash
dotnet run --project HeMaCupAICheck
```

选择 **1** 即可进入综合性对话智能体，全部工具和 Agent 自动加载，AI 自主决策调用。
共 27 个功能演示，按类别分组：对话基础 · 工具系统 · Agent/工作流 · 数据与知识 · 基础设施 · 综合场景。

---

## ⚖️ 许可证

Admin.NET.Ai 遵循 [MIT 许可证](LICENSE) 发布。