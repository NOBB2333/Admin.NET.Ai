# Admin.NET.Ai

<div align="center">

**English** | **[中文](./README.md)**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![MEAI](https://img.shields.io/badge/Microsoft.Extensions.AI-✓-blue)](https://www.nuget.org/packages/Microsoft.Extensions.AI)

</div>

---

Admin.NET.Ai is an enterprise-grade AI capability core library built on **.NET 10**. It uses a pipeline/middleware architecture and deeply integrates `Microsoft.Extensions.AI` (MEAI) and `Microsoft.Agents` frameworks.

> [!IMPORTANT]
> **Requirements**: This project requires .NET 10 features. Recommend using [mise](https://mise.jdx.dev/) to manage the environment with `mise use dotnet` to activate the SDK.

---

## ✨ Core Features

| Feature | Description |
| :--- | :--- |
| 🔌 **Multi-Provider Support** | Seamlessly switch between OpenAI, DeepSeek, Qwen, Gemini, Ollama, etc. |
| 🤖 **Multi-Agent Orchestration** | Sequential/Parallel/Orchestrator/Roundtable modes, LLM-driven Agent auto-discovery & dispatch |
| 🔧 **Enhanced Tool System** | FileSystem/Search/Shell tools + self-managed approval + MCP tool discovery |
| 🎨 **Media Generation** | TTS/ASR/Image/Video generation with multi-provider support |
| 📚 **Hybrid RAG** | Vector retrieval + Neo4j GraphRAG + Reranking |
| ⚡ **Three-Layer Middleware** | Chat pipeline (Token/cost/caching) + Tool pipeline (approval/monitoring/validation) + rate limiting/audit |
| 🗜️ **Three-Zone Compression** | First-turn preservation + LLM summarization of history + recent message retention |
| 🔥 **Hot-Reload Scripting** | Natasha C# script engine for dynamic Agent logic updates |
| 📊 **Full Observability** | Trace timeline + DevUI visual debugging |

### 📋 Feature Demos Overview (Console)

| # | Category | Module | Description |
| :---: | :--- | :--- | :--- |
| **★1** | **Universal** | **Universal Agent** | **All-in-One Agent, auto-loads all tools & agents** |
| 2 | Chat Basics | Basic Chat & Middleware | Chat, Audit, Tokens |
| 3 | Chat Basics | Prompt Engineering | Prompt Templates |
| 4 | Chat Basics | Structured Extraction | JSON Schema, TOON |
| 5 | Chat Basics | Code Generator | Structured Output |
| 6 | Chat Basics | Multimodal | Vision & Audio |
| 7 | Tools | Smart Tools & Approval | Discover, Approval |
| 8 | Tools | Enhanced Tool System | FileSystem/Search/Shell |
| 9 | Tools | MCP Protocol | External Tool Integration |
| 10 | Tools | MCP Calendar Assistant | Official SDK Tool Calls |
| 11 | Tools | MCP MiniApi Server | External Tool Integration |
| 12 | Agents | Built-in Agents | Sentiment/Knowledge Graph/Quality |
| 13 | Agents | LLM Agent Dispatch | Auto-Discovery |
| 14 | Agents | Multi-Agent Workflow | MAF Sequential & Autonomous |
| 15 | Agents | Multi-Agent Review | Writer→Reviewer→Editor |
| 16 | Agents | Customer Service Routing | Intent Recognition + Routing |
| 17 | Data | RAG Knowledge Retrieval | GraphRAG & Vector |
| 18 | Data | RAG + Agent QA | Knowledge Base + Reasoning |
| 19 | Data | Context Compression | ThreeZone/Summarizing/Counting |
| 20 | Data | Conversation Persistence | Thread & Database |
| 21 | Infra | Middleware Deep-dive | Middleware Stack |
| 22 | Infra | Content Safety | Sensitive Word Replace + PII Masking |
| 23 | Infra | Monitoring & Metrics | OpenTelemetry |
| 24 | Infra | Storage Strategies | Hot/Cold/Vector |
| 25 | Infra | Dynamic Script Hot-Reload | Natasha Scripting |
| 26 | Scenarios | Real-world Scenarios | Comprehensive Applications |
| 27 | Scenarios | Media Generation | TTS/ASR/Image/Video |

---

## 🚀 Quick Start

### 1. Install
```bash
dotnet add package Admin.NET.Ai  # Not uploaded yet, add project reference manually
```

### 2. Register Services
```csharp
services.AddAdminNetAi(configuration);
```

### 3. Usage Examples

#### Basic Chat
```csharp
var aiFactory = sp.GetRequiredService<IAiFactory>();
var client = aiFactory.GetDefaultChatClient();
var response = await client.GetResponseAsync("Hello, I'm Admin.NET");
```

#### Enhanced Tool System (Auto-Discovery + Context Injection)
```csharp
var toolManager = sp.GetRequiredService<ToolManager>();
var context = new ToolExecutionContext
{
    WorkingDirectory = Directory.GetCurrentDirectory(),
    UserId = "user-001"
};
var functions = toolManager.GetAllAiFunctions(context); // Auto-scan all tools with context
```

#### Multi-Agent Collaboration
```csharp
var orchestrator = new EnhancedMultiAgentOrchestrator(aiFactory);
orchestrator
    .AddAgent("Tech Expert", "Analyze from technical perspective", provider: "qwen")
    .AddAgent("Product Manager", "Analyze from product perspective", provider: "deepseek");
    
await foreach (var evt in orchestrator.RunDiscussionAsync("Impact of AI on development", rounds: 2))
{
    Console.Write(evt.Content);
}
```

#### MCP Tools
```csharp
[McpTool("Get weather information")]
public WeatherInfo GetWeather([McpParameter("City name")] string city)
{
    return new WeatherInfo { City = city, Temperature = 20 };
}
```

---

## 🏗️ Architecture

```
Admin.NET.Ai/
├── Abstractions/        # Interfaces: IAiFactory, IAiAgent, IAiCallableFunction, IChatReducer
├── Core/                # AiFactory, PipelineBuilder
├── Middleware/
│   ├── TokenMonitoringMiddleware   # Chat pipeline: Token/cost/newline fix
│   ├── ToolValidationMiddleware    # Tool pipeline: permission/approval/params/sandbox/sanitize
│   └── ToolMonitoringMiddleware    # Tool pipeline: classification logging/duration
├── Services/
│   ├── Tools/           # ToolManager + FileSystem/Search/Shell/AgentDispatch
│   ├── Context/         # ChatReducerFactory (ThreeZone/Summarizing/MessageCounting)
│   ├── MCP/             # MCP Protocol + Tool Discovery
│   ├── Media/           # TTS/ASR/ImageGen/VideoGen
│   ├── Rag/             # Vector + GraphRAG
│   └── Workflow/        # Multi-Agent Orchestration Engine
├── Configuration/       # JSON config files
├── _doc/                # User documentation
└── _doc_Pro/            # Technical deep-dive
```

### Middleware Responsibility Split

```
Request → TokenMonitoringMiddleware (Token/Cost)
            ↓
         LLM decides to call tool
            ↓
         ToolValidationMiddleware (Permission → Approval → Params → Sandbox → Sanitize)
            ↓
         ToolMonitoringMiddleware (🔧Tool / 🤖Agent / ⚡Skill classification)
            ↓
         Actual Tool Execution
```

---

## ⚙️ Configuration

### LLMAgent.Clients.json (Providers)
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

### LLMAgent.Mcp.json (MCP Servers)
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

## 📖 Documentation

- **User Docs**: `_doc/` - Feature introductions and usage examples
- **Technical Deep-Dive**: `_doc_Pro/` - Implementation details and source code analysis

---

## 🎯 Demos

Run the console demo:
```bash
dotnet run --project HeMaCupAICheck
```

Select **1** to enter the Universal Agent — all tools and agents auto-loaded, LLM decides autonomously what to invoke.
27 feature demos total, grouped by category: Chat Basics · Tool System · Agent/Workflow · Data & Knowledge · Infrastructure · Scenarios.

---

## ⚖️ License

Admin.NET.Ai is released under the [MIT License](LICENSE).
