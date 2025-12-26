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
| 🤖 **Multi-Agent Orchestration** | Sequential/Parallel/Orchestrator/Roundtable modes, multi-provider for diversity |
| 🔧 **MCP Tool Discovery** | `[McpTool]` attribute to expose methods as MCP tools |
| 🎨 **Media Generation** | TTS/ASR/Image/Video generation with multi-provider support |
| 📚 **Hybrid RAG** | Vector retrieval + Neo4j GraphRAG + Reranking |
| ⚡ **Middleware Pipeline** | Caching/Rate-limiting/Token billing/Audit/Retry |
| 🔥 **Hot-Reload Scripting** | Natasha C# script engine for dynamic Agent logic updates |
| 📊 **Full Observability** | Trace timeline + DevUI visual debugging |

---

## 🚀 Quick Start

### 1. Install
```bash
dotnet add package Admin.NET.Ai
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
[McpTool("Get weather information")]  // Name defaults to method name
public WeatherInfo GetWeather([McpParameter("City name")] string city)
{
    return new WeatherInfo { City = city, Temperature = 20 };
}
```

#### Image Generation
```csharp
var mediaService = sp.GetRequiredService<IMediaGenerationService>();
var result = await mediaService.GenerateImageAsync(new ImageGenRequest
{
    Prompt = "A cute robot cat",
    Provider = "OpenAI"
});
```

---

## 🏗️ Architecture

```
Admin.NET.Ai/
├── Abstractions/        # Interface definitions
├── Core/                # AiFactory, PipelineBuilder
├── Middleware/          # Caching/RateLimiting/Audit/TokenBilling
├── Services/
│   ├── MCP/             # MCP Protocol + Tool Discovery
│   ├── Media/           # TTS/ASR/ImageGen/VideoGen
│   ├── Rag/             # Vector + GraphRAG
│   └── Workflow/        # Multi-Agent Orchestration Engine
├── Configuration/       # JSON config files
├── _doc/                # User documentation
└── _doc_Pro/            # Technical deep-dive
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

Choose from 17 feature demos:
1. Basic Chat | 2. Multi-Agent Workflow | 3. Structured Output | 4. Tool Calling | 5. Hot-Reload Script
6. Context Compression | 7. Prompts | 8. RAG | 9. Multimodal | 10. Persistence
12. Built-in Agents | 13. Middleware | 14. MCP | 15. Monitoring | 16. Storage | **17. Media Generation**

---

## ⚖️ License

Admin.NET.Ai is released under the [MIT License](LICENSE).
