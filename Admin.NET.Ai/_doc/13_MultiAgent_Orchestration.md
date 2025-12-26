# 13. 多 Agent 协作 (Multi-Agent Orchestration)

## 🎯 设计思维 (Mental Model)
单个 Agent 的能力有限。通过 **多 Agent 协作**，可以：
1. **分工合作**: 研究员→作家→编辑，各司其职
2. **多视角分析**: 技术、经济、伦理多维度
3. **避免同质化**: 使用不同 LLM 供应商，观点更多元
4. **工具增强**: Agent 可调用搜索、RAG、MCP 工具

---

## 🏗️ 两个协作引擎

| 特性 | MultiAgentOrchestrator | EnhancedMultiAgentOrchestrator |
|------|------------------------|--------------------------------|
| 供应商 | 单一 | 多供应商 (Qwen/DeepSeek/Gemini) |
| 工具调用 | ❌ | ✅ Search/RAG/MCP |
| Agent 隔离 | ✅ | ✅ |
| Token 优化 | ✅ | ✅ |

---

## ✨ 核心特性

### 1. 线程隔离
每个 Agent 有独立的 `ConversationHistory`，互不干扰。

### 2. Token 优化
只共享观点摘要 (`MaxSummaryLength=100`)，而非完整对话。

### 3. 多供应商
```csharp
orchestrator
    .AddAgent("保守派", "你倾向于稳定方案", provider: "qwen")
    .AddAgent("创新派", "你支持新技术", provider: "deepseek")
    .AddAgent("务实派", "你追求平衡", provider: "gemini");
```

### 4. 工具调用
```csharp
orchestrator
    .AddAgent("数据分析师", "...", provider: "qwen")
    .WithSearchTool("数据分析师", searchFunc)
    .WithRagTool("数据分析师", ragFunc)
    .WithMcpTool("数据分析师", "market_data", mcpFunc);
```

---

## 🚀 工作流模式

### 1. 顺序执行
```
研究员 → 作家 → 编辑
```

### 2. 并发执行
```
技术专家 ┐
经济学家 ├→ 汇总
伦理学者 ┘
```

### 3. 编排者模式
```
需求 → AI 拆分任务 → 并行分配 → 汇总
```

### 4. 圆桌讨论
```
Round 1: A发言 → B发言 → C发言
Round 2: 参考其他人继续讨论
...
最终: 主持人总结
```

---

## 🚀 代码示例

```csharp
var orchestrator = new EnhancedMultiAgentOrchestrator(aiFactory);

orchestrator
    .AddAgent("技术专家", "从技术角度分析", provider: "qwen")
    .AddAgent("产品经理", "从产品角度分析", provider: "deepseek");

await foreach (var evt in orchestrator.RunDiscussionAsync("AI 对软件开发的影响", rounds: 2))
{
    Console.Write(evt.Content);
}
```

---

## 📖 更多技术细节

详见 `_doc_Pro/02_MultiAgent_Orchestrator.md`
