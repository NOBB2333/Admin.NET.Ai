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
| 🤖 **多 Agent 协作** | 顺序/并发/编排者/圆桌讨论模式，多供应商避免同质化 |
| 🔧 **MCP 工具发现** | `[McpTool]` 属性一键暴露方法为 MCP 工具 |
| 🎨 **媒体生成** | TTS/ASR/图像生成/视频生成，多供应商支持 |
| 📚 **混合策略 RAG** | 向量检索 + Neo4j GraphRAG + 重排 (Rerank) |
| ⚡ **中间件管道** | 缓存/限流/Token计费/审计/重试 |
| 🔥 **热重载脚本** | Natasha C# 脚本引擎，动态更新 Agent 逻辑 |
| 📊 **全链路可观测** | Trace 时间轴 + DevUI 可视化调试 |

---

## 🚀 快速开始

### 1. 安装依赖
```bash
dotnet add package Admin.NET.Ai
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
[McpTool("获取天气信息")]  // 名称自动取方法名
public WeatherInfo GetWeather([McpParameter("城市")] string city)
{
    return new WeatherInfo { City = city, Temperature = 20 };
}
```

#### 图像生成
```csharp
var mediaService = sp.GetRequiredService<IMediaGenerationService>();
var result = await mediaService.GenerateImageAsync(new ImageGenRequest
{
    Prompt = "一只可爱的机器猫",
    Provider = "AliyunBailian"
});
```

---

## 🏗️ 架构

```
Admin.NET.Ai/
├── Abstractions/        # 接口定义
├── Core/                # AiFactory, PipelineBuilder
├── Middleware/          # 缓存/限流/审计/Token计费
├── Services/
│   ├── MCP/             # MCP 协议 + 工具发现
│   ├── Media/           # TTS/ASR/ImageGen/VideoGen
│   ├── Rag/             # Vector + GraphRAG
│   └── Workflow/        # 多 Agent 协作引擎
├── Configuration/       # JSON 配置文件
├── _doc/                # 用户文档
└── _doc_Pro/            # 技术详解
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

可选择 17 个功能演示:
1. 基础对话 | 2. 多 Agent 工作流 | 3. 结构化输出 | 4. 工具调用 | 5. 热重载脚本
6. 上下文压缩 | 7. 提示词 | 8. RAG | 9. 多模态 | 10. 持久化
12. 内置 Agent | 13. 中间件 | 14. MCP | 15. 监控 | 16. 存储 | **17. 媒体生成**

---

## ⚖️ 许可证

Admin.NET.Ai 遵循 [MIT 许可证](LICENSE) 发布。