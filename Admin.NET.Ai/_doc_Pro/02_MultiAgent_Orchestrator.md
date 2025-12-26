# 多 Agent 协作引擎 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `MultiAgentOrchestrator.cs` | `Services/Workflow/` | 基础协作引擎 (单供应商) |
| `EnhancedMultiAgentOrchestrator.cs` | `Services/Workflow/` | 增强版 (多供应商+工具) |
| `WorkflowDemo.cs` | `HeMaCupAICheck/Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 两个协作引擎对比

| 特性 | MultiAgentOrchestrator | EnhancedMultiAgentOrchestrator |
|------|------------------------|--------------------------------|
| 供应商 | 单一 `IChatClient` | 多供应商 `IAiFactory` |
| 工具调用 | ❌ | ✅ Search/RAG/MCP |
| Agent 隔离 | ✅ 独立历史 | ✅ 独立历史 |
| Token 优化 | ✅ 摘要共享 | ✅ 摘要共享 |

---

## 🧠 MultiAgentOrchestrator (基础版)

### 核心数据结构

```csharp
public class AgentParticipant
{
    public string Name { get; set; }                     // Agent 名称
    public string SystemPrompt { get; set; }             // 系统提示词
    public string Personality { get; set; }              // 个性描述
    public List<ChatMessage> ConversationHistory { get; set; }  // ★ 独立历史
}

public class SharedContext
{
    public string Topic { get; set; }                    // 讨论议题
    public List<ContextPoint> Points { get; set; }       // 共享观点 (摘要)
}

public class ContextPoint
{
    public string AgentName { get; set; }
    public int Round { get; set; }
    public string Summary { get; set; }                  // ★ 只共享摘要
}
```

### 线程隔离实现

```csharp
// 每个 Agent 有独立的对话历史
foreach (var agent in _participants)
{
    // 构建该 Agent 专属的上下文
    var messages = BuildAgentContext(agent, sharedContext, currentRound);
    
    // 获取响应
    var response = await _chatClient.GetStreamingResponseAsync(messages);
    
    // ★ 保存到该 Agent 独立的历史
    agent.ConversationHistory.Add(new ChatMessage(ChatRole.Assistant, fullResponse));
    
    // ★ 只把摘要添加到共享上下文
    var summary = ExtractKeyPoints(fullResponse, _options.MaxSummaryLength);
    sharedContext.AddPoint(agent.Name, round, summary);
}
```

### Token 优化策略

```csharp
public class MultiAgentOptions
{
    public int MaxSummaryLength { get; set; } = 100;   // 摘要最大长度
    public int MaxContextPoints { get; set; } = 6;     // 保留最近 N 个观点
    public int MaxResponseLength { get; set; } = 150;  // 限制回复长度
}
```

---

## 🚀 EnhancedMultiAgentOrchestrator (增强版)

### 多供应商支持

```csharp
public class EnhancedMultiAgentOrchestrator
{
    private readonly IAiFactory _aiFactory;  // ★ 使用工厂而非单一客户端

    public EnhancedMultiAgentOrchestrator AddAgent(
        string name, 
        string systemPrompt, 
        string? provider = null,    // ★ 指定供应商
        string? personality = null,
        IEnumerable<AgentTool>? tools = null)
    {
        // 根据供应商获取对应的 ChatClient
        var chatClient = provider != null 
            ? _aiFactory.GetChatClient(provider) 
            : _aiFactory.GetDefaultChatClient();

        _participants.Add(new EnhancedAgentParticipant
        {
            Name = name,
            Provider = provider ?? _aiFactory.DefaultProvider,
            ChatClient = chatClient!,
            Tools = tools?.ToList() ?? new List<AgentTool>()
        });
        return this;
    }
}
```

### 工具调用架构

```csharp
public class AgentTool
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Func<string, Task<string>> ExecuteAsync { get; set; }
}

// 使用 Fluent API 配置工具
orchestrator
    .AddAgent("数据分析师", "你是数据分析师", provider: "qwen")
    .WithSearchTool("数据分析师", async query => 
        await webSearchService.SearchAsync(query))
    .WithRagTool("数据分析师", async query => 
        await ragService.RetrieveAsync(query))
    .WithMcpTool("数据分析师", "market_data", async query => 
        await mcpClient.CallToolAsync("market_data", query));
```

### 工具调用流程

```csharp
// 在讨论过程中
if (agent.Tools.Any() && round == 1)  // 第一轮调用工具
{
    foreach (var tool in agent.Tools)
    {
        yield return new EnhancedDiscussionEvent
        {
            Type = DiscussionEventType.ToolCalling,
            Content = $"[{agent.Name}] 调用工具: {tool.Name}"
        };
        
        string toolResultContent;
        try
        {
            var result = await tool.ExecuteAsync(topic);
            toolResults += $"\n[{tool.Name}结果]: {result}";
            toolResultContent = $"返回: {result}";
        }
        catch (Exception ex)
        {
            toolResultContent = $"错误: {ex.Message}";
        }
        
        yield return new EnhancedDiscussionEvent
        {
            Type = DiscussionEventType.ToolResult,
            Content = toolResultContent
        };
    }
}

// 将工具结果注入到 Agent 上下文
var systemContent = agent.SystemPrompt;
if (!string.IsNullOrEmpty(toolResults))
{
    systemContent += $"\n\n你可以参考以下工具调用的结果:\n{toolResults}";
}
```

---

## 📊 讨论事件类型

```csharp
public enum DiscussionEventType
{
    Started,          // 讨论开始
    RoundStarted,     // 轮次开始
    AgentSpeaking,    // Agent 开始发言
    ToolCalling,      // 调用工具
    ToolResult,       // 工具返回
    StreamingContent, // 流式内容
    AgentCompleted,   // Agent 发言完成
    Summarizing,      // 生成总结
    Completed         // 讨论结束
}
```

---

## 🎯 工作流模式

### 1. 顺序执行

```
Agent A → Agent B → Agent C
    ↓         ↓         ↓
  研究      写作      编辑
```

```csharp
string currentContent = topic;
foreach (var agent in agents)
{
    var response = await GetAgentResponse(agent, currentContent);
    currentContent = response;  // 输出传递给下一个
}
```

### 2. 并发执行

```
        ┌─ Agent A (技术) ─┐
Topic ──┼─ Agent B (经济) ─┼── 汇总
        └─ Agent C (伦理) ─┘
```

```csharp
var tasks = analysts.Select(async analyst => {
    var client = _aiFactory.GetChatClient(provider);
    return await client.GetResponseAsync(prompt);
});
var results = await Task.WhenAll(tasks);
// 然后汇总
```

### 3. 圆桌讨论

```
Round 1: A 发言 → B 发言 → C 发言
Round 2: A 发言(参考B,C) → B 发言(参考A,C) → C 发言(参考A,B)
Round 3: ...
最终: 主持人总结
```

### 4. 编排者模式

```
需求 → 编排者分析 → [子任务1, 子任务2, 子任务3]
                        ↓
              并行分配给不同 Agent
                        ↓
                    汇总结果
```

---

## 💡 多供应商避免同质化

```csharp
// 使用不同供应商创建不同视角的 Agent
orchestrator
    .AddAgent("保守派", "你倾向于稳定方案", provider: "qwen")
    .AddAgent("创新派", "你支持新技术", provider: "deepseek")
    .AddAgent("务实派", "你追求平衡", provider: "gemini");
```

不同 LLM 的训练数据和倾向不同，能带来更多元的观点。

---

## ⚠️ 注意事项

1. **线程安全**: `SharedContext` 需要考虑并发访问
2. **Token 控制**: 使用 `MaxSummaryLength` 和 `MaxContextPoints` 控制上下文大小
3. **流式输出**: 所有 LLM 调用使用 `GetStreamingResponseAsync`
4. **错误处理**: 工具调用失败不应阻断整个讨论
5. **供应商可用性**: 使用 `IAiFactory.GetChatClientWithFallbackAsync` 处理降级
