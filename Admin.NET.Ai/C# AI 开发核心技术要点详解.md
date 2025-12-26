[toc]

# C# AI 开发核心技术要点详解
> 基于对54篇Microsoft Agent Framework及相关技术文档的全面分析
>

## 📋 C# AI开发完整技术图谱


—、技术栈与核心框架

1.1  主要技术框架

1.2 核心依赖库

二、工作流编排系统

2.1  工作流模式类型

2.2 工作流执行与管理

2.2 热重载工作流

三、对话管理与持久化

3.1 AgentThread对话线程隔离

3.2 对话持久化实现

3.3 自定义存储实现

3.3.1  InMemoryChatMessageStore - 内存存储（ 默认）

3.3.2 文件存储实现- JSON文件持久化

3.3.3 数据库存储- Entity Framework集成/sqlsugar

3.3.4 Redis分布式存储- 多实例共享

3.3.S 向量数据库存储- 语义搜索支持

3.3.6 SQLite轻量存储- 本地应用场景

3.3.7 云存储集成- Azure Cosmos DB等

3.3.8 使用示例：集成到Agent

3.3.9 其他实现- 待定 没想好

四、结构化数据处理

4.1  强类型结构化输出

4.2 JSON Schema约束输出

4.3 嵌套对象支持- 复杂数据结构

4.4 枚举类型处理- JsonStringEnumConverter

4.S 国产模型适配- DeepSeek/Qwen兼容策略

4.6 TOON协议支持- 比JSON更高效的序列化

五、上下文管理与压缩

S.1  消息计数压缩器

S.2 智能摘要压缩器

S.3 自定义压缩策略- IChatReducer接口实现

S.4 关键词优先保留- 业务关键消息保护

S.S 系统消息保护- 指令消息不被压缩

S.6 函数调用消息保护- 工具调用上下文完整

S.7 压缩触发策略- 阈值配置与触发时机

S.8 分层压缩策略

S.9 性能优化配置

S.1O 监控与调优

六、工具调用与函数集成

6.1  基础函数调用

6.2 AIFunctionFactory - 普通函数转AI工具

6.3 工具描述生成- Description特性自动提取

6.4 人工审批机制- ApprovalRequiredAIFunction

6.S 敏感工具控制- 高风险操作审批流程

6.6 MCP服务器暴露- Agent作为MCP服务提供

6.7 工具发现机制- 运行时动态加载

6.8 完整工具调用示例

6.9 工具调用监控和日志

七、中间件与拦截器

7.1  Run Middleware - 对话执行拦截

7.2 Function Calling Middleware - 工具调用拦截

7.3 日志记录中间件- 执行过程追踪

7.4 缓存中间件- 响应结果缓存

7.S 限流中间件- API调用频率控制

7.6 审计中间件- 操作记录追踪

7.7 费用监控中间件- Token使用监控与成本控制

7.7.1 Token监控中间件实现

7.7.2 费用计算器实现

7.7.3 预算管理器实现

7.7.4 数据模型定义

7.7.S 中间件注册和使用

7.7.6 监控仪表板集成- (待定)

7.9. NETCore.Encrypt加密库集成- （待定）

7.9 自定义中间件工厂

7.1O AIContextProvider

7.11  MCP Gateway

7.1O 中间件配置最佳实践

八、提示工程优化

1. YPrompt提示词管理系统

8.1 角色指令定义- Instructions系统提示

8.2 思维链提示- 分步骤推理引导

8.3 输出格式约束- 明确JSON结构要求

8.4 上下文丰富- 时间、用户信息等上下文注入

8.S 示例驱动- Few-shot learning示例

8.6 边界明确- 拒答策略和范围限定

8.7 提示工程最佳实践总结

1.分层提示设计

2.动态提示调整

3.提示词版本管理



九、性能优化技术

9.1 会话缓存- 响应提速1O-1OO倍

9.2 智能工具筛选- Tool Reduction技术

9.3 Token优化- 减少不必要的token消耗

9.4 流式响应处理- 实时显示逐步结果

9.S 批量处理优化- 大批量数据高效处理

9.6 模型选择策略- 不同场景选用合适模型（ 应该用不上）

9.7 性能优化配置示例



 十、RAG集成- 检索增强生成（ 重写-太简单了）

1O.1. TextSearchProvider的RAG实现细节

1O.2. Agentic RAG 与传统RAG 的对比实现 (控制层)

1O.2.1. 动态检索策略规划 (RAGPlan 扩展)

1O.2.2. 迭代优化与自我评估 (Iterative Refinement)

1O.3. RAG 监控与可观测性 (工程层- 可以查看切片)

1O.3.1. RAG 追踪中间件

1O.3.2. 评估指标（ Evaluation Metrics）



⼗—、高级AI能力

11.1  多模态处理- 文本、图像综合

11.2 情感分析集成- 情绪识别处理

11.3 知识图谱集成- 结构化知识查询

11.4 自动优化循环- 提示词自改进

十二、架构与能力整合模式

12.1 架构演进路径

12.2 能力叠加策略

13.3 DevUI在架构中的角色  


十三、监控与可观测性架构设计

13.1 OpenTelemetry集成- 分布式追踪

13.2 执行事件流- WorkflowEvent实时监控

13.3 性能指标收集- 响应时间、成功率等

13.4 错误处理与重试- 容错机制

13.S 对话质量评估- 输出结果验证

13.6 完整的监控配置类

13.7 使用示例

   

十四、DevUI调试界面 - 可视化测试调试

14.1 设计要点（ 基于Microsoft Agent Framework DevUI）

14.2 执行代码

14.3 核心调试功能

14.4 实战调试场景十五、其他-架构模式

1S.1 插件系统架构- 模块化扩展

1S.2 微服务集成- 分布式系统协作

1S.3 事件驱动架构- 异步消息处理

1S.4 CQRS模式- 命令查询职责分离

1S.S 领域驱动设计- 业务逻辑封装

十六、部署与运维

16.1  Docker容器化部署

16.2 健康检查与监控应用场景

—、实际应用场景示例

1.1 智能客服系统

1.2 内容生成流水线           

二、审批工作流- 人工介入流程

2.1 设计要点

2.2 执行代码

2.3 业务场景

三、电商客服场景- 订单查询处理

3.1 设计要点（ 基于多Agent协作）

3.2 执行架构

3.3 核心能力

四、技术支持场景- 问题诊断解决

4.1 设计要点

4.2 执行方案

4.3 工具集成示例

五、内容生成场景- 博客文章创作

S.1 设计要点（ 基于BlogAgent案例）

S.2 执行代码

S.3 生成流程

六、数据分析场景- 数据提取洞察

6.1 设计要点

6.2 执行方案

七、场景化最佳实践总结

7.1 模式选择指南

7.2 性能优化策略

7.3 质量保证机制    

 八、企业级特性完整实现

8.1 依赖注入集成- .NET IoC容器支持

8.2 配置化管理- appsettings.json配置

8.3 多环境支持- 开发/测试/生产环境

8.4 安全合规- 数据加密和访问控制

8.S 审计日志 - 操作记录追踪

8.6 版本管理- 提示词和配置版本控制

8.7 完整的启动配置示例

1. 引用文档索引



---

# 技术要点正文
## 一、技术栈与核心框架
### 1.1 主要技术框架
**Microsoft Agent Framework (MAF)** - 企业级AI代理开发框架

+ **定位**: 微软官方的生产级AI代理框架，集成Semantic Kernel和AutoGen优势
+ **核心能力**: 多代理协作、工作流编排、状态管理
+ **支持语言**: .NET + Python双语言支持
+ **GitHub**: [https://github.com/microsoft/agents](https://github.com/microsoft/agents)

**Microsoft.Extensions.AI (MEAI)** - AI能力基础抽象层

+ **定位**: .NET平台AI功能的标准化接口
+ **核心价值**: 依赖注入、配置化、中间件管道
+ **NuGet包**: `Microsoft.Extensions.AI`

**MCP (Model Context Protocol)** - 模型上下文协议

+ **定位**: 一个标准化的协议，用于LLM与外部工具（如数据库、API、知识库）的通信。MAF提供了原生支持。
+ **核心价值**:
    - **标准化接口**: 不同工具遵循统一协议，易于集成。
    - **动态工具发现**: 工具可以在运行时被Agent发现和调用。
    - **安全隔离**: 工具通常在独立进程中运行。
+ **在MAF中的应用**: 文档中展示了如何将Agent自身暴露为MCP Server的工具，以及如何让Agent连接外部的MCP Server（如连接Microsoft Learn文档库）来扩展其能力。

**Semantic Kernel (SK)** - 语义内核

+ **定位**: MAF的前身之一，一个面向生产环境的AI应用开发框架。MAF被描述为“集成Semantic Kernel和AutoGen精华的生产级智能体开发方案”和“Semantic Kernel和AutoGen的下一代演进版本”。
+ **与MAF的关系**: MAF整合了SK的优秀特性（如企业级功能、插件系统），并在此基础上发展出更专注于智能体协作和编排的新一代框架。文档指出，MAF在状态管理、多智能体协作等方面提供了更强大的解决方案。





注：

可明确 **MEAI 与 MAF 的选择策略**（文档中“MAF vs MEAI：如何选”表格）：  
• **MEAI**：适用于一次性、无状态的直接模型调用（`GetResponseAsync<T>()`）。  
• **MAF**：适用于需要长期上下文、多轮对话的智能体（`RunAsync<T>()`，`AgentThread`自动管理状态）。 





### 1.2 核心依赖库
```xml
<!-- 基础AI能力抽象 -->
<PackageReference Include="Microsoft.Extensions.AI" Version="1.0.0" />

<!-- OpenAI/Azure OpenAI集成 -->
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="8.0.0" />

<!-- Agent Framework核心 -->
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251105.1" />

<!-- Agent Framework 工作流 -->
<PackageReference Include="Microsoft.Agents.AI.WorkFlows" Version="1.0.0-preview.251105.1" />

<!-- Agent Framework 工作流 -->
<PackageReference Include="Microsoft.Agents.AI.hosting" Version="1.0.0-preview.251105.1" />

<!-- 向量存储等连接器（常用于实现RAG等能力） -->
<PackageReference Include="Microsoft.SemanticKernel.Connectors.InMemory" Version="1.67.0-preview" />
```

## 二、工作流编排系统
### 2.1 工作流模式类型
**工作流的具体模式**（文档中提到四种）：Sequential（顺序）、Concurrent（并发）、Handoffs（移交）、Groupchat（群聊）。 

```csharp
using Microsoft.Agents;

// 1. 顺序工作流 - 线性执行
// 应用场景：家具报价流水线、内容创作流程等需要严格顺序执行的业务
var sequentialWorkflow = AgentWorkflowBuilder.BuildSequential(
    "BlogGeneration",
    researcherAgent, 
    writerAgent, 
    reviewerAgent
);
var workflow = WorkflowBuilder.CreateSequentialBuilder()
    .AddExecutor("Agent1")
    .AddExecutor("Agent2")
    .AddExecutor("Agent3")
    .Build();


// 2. 并发工作流 - 并行执行  
// 应用场景：多源资料并行收集、多维度并行审查，可显著提升执行效率
var concurrentWorkflow = AgentWorkflowBuilder.BuildConcurrent(
    "ParallelResearch",
    new[] { githubResearcher, stackoverflowResearcher, docsResearcher },
    aggregator: results => MergeResults(results)
);

// Conditional Pattern（条件模式）: 根据条件选择不同的执行路径，适用于需要根据不同情况采取不同措施的流程。
var workflow = WorkflowBuilder.CreateConditionalBuilder()
    .AddExecutor("Agent1")
    .AddConditionalEdge("Agent1", "Agent2", condition: result => result == "Yes")
    .AddConditionalEdge("Agent1", "Agent3", condition: result => result == "No")
    .Build();


// 3. 交接工作流 - 动态路由  
// 特点：动态路由模式，根据条件将任务交接给特定专家Agent
// 应用场景：客服系统路由、内容审核发布系统等需要智能调度的场景
var handoffWorkflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
    .WithHandoffs(triageAgent, specialists)
    .WithHandoffs(specialists, triageAgent)
    .Build();
    

// 4. 群聊工作流 - 多代理协作
// 特点：多Agent协作讨论模式，支持轮询等管理策略
// 应用场景：辩论场景、多方协作决策、创意头脑风暴等
var groupChatWorkflow = AgentWorkflowBuilder.BuildGroupChat(
    participants: new[] { agent1, agent2, agent3 },
    moderator: moderatorAgent
);
// 或使用RoundRobin群聊管理器
var workflow = AgentWorkflowBuilder.CreateGroupChatBuilderWith(
    agents => new RoundRobinGroupChatManager(agents) { 
        MaximumIterationCount = 5 
    })
    .AddParticipants([chatClientAgent1, chatClientAgent2])
    .Build();
    

// 5. 嵌套工作流 
// 嵌套工作流允许将整个工作流作为另一个工作流的执行单元，实现工作流的模块化和复用。
// 1. 创建子工作流
var researchSubWorkflow = AgentWorkflowBuilder.BuildSequential(
    "ResearchSubflow",
    queryAnalyzerAgent,
    retrievalAgent,
    rerankerAgent
);

var writingSubWorkflow = AgentWorkflowBuilder.BuildSequential(
    "WritingSubflow", 
    outlineAgent,
    draftAgent,
    polishAgent
);

// 2. 将子工作流包装为Executor
var researchExecutor = new WorkflowExecutor(researchSubWorkflow);
var writingExecutor = new WorkflowExecutor(writingSubWorkflow);

// 3. 构建主工作流（嵌套子工作流）
var mainWorkflow = AgentWorkflowBuilder.BuildSequential(
    "MainBlogWorkflow",
    researchExecutor,      // 嵌套的研究子工作流
    writingExecutor,       // 嵌套的写作子工作流
    reviewAgent
);

// 实际应用示例
// 复杂的内容生成流水线
var contentGenerationWorkflow = (
    WorkflowBuilder()
    .SetStartExecutor(researchCoordinator)
    .AddEdge(researchCoordinator, researchSubWorkflow)  // 嵌套研究流程
    .AddEdge(researchSubWorkflow, writingOrchestrator)
    .AddEdge(writingOrchestrator, writingSubWorkflow)   // 嵌套写作流程
    .AddEdge(writingSubWorkflow, qualityGate)
    .AddConditionalEdge(qualityGate, 
        condition: output => output.QualityScore >= 80 ? "publish" : "rewrite",
        destinations: ["publishAgent", writingSubWorkflow]) // 可重新进入嵌套流程
    .Build()
);


// 6. 检查点机制 (Checkpoint Mechanism)
// 1. 定义可序列化的状态类
public class BlogGenerationState
{
    public string OriginalQuery { get; set; }
    public ResearchResult ResearchData { get; set; }
    public DraftContent CurrentDraft { get; set; }
    public ReviewFeedback Feedback { get; set; }
    public int CurrentStep { get; set; }
    public bool IsComplete { get; set; }
}

// 2. 配置检查点存储
var workflow = AgentWorkflowBuilder.BuildSequential(
    "CheckpointWorkflow",
    researcherAgent,
    writerAgent, 
    reviewerAgent
).WithCheckpointing(new CheckpointOptions
{
    StorageProvider = new FileSystemCheckpointStorage("checkpoints/"),
    CheckpointInterval = TimeSpan.FromMinutes(5),
    MaxCheckpoints = 10
});

// 3. 执行带检查点的工作流
var initialState = new BlogGenerationState 
{ 
    OriginalQuery = userInput,
    CurrentStep = 0 
};

await using var run = await InProcessExecution.StreamAsync(
    workflow, 
    messages, 
    initialState);

// 手动创建检查点
await run.CreateCheckpointAsync("manual_checkpoint_1");

// 从检查点恢复
var recoveredRun = await InProcessExecution.RestoreFromCheckpointAsync(
    workflow, 
    "checkpoint_id");

// 检查点事件处理
// 监听检查点相关事件
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    switch (evt)
    {
        case CheckpointCreatedEvent checkpoint:
            Console.WriteLine($"检查点已创建: {checkpoint.CheckpointId}");
            // 可保存检查点ID到数据库，用于后续恢复
            await _db.SaveCheckpointReferenceAsync(
                sessionId, 
                checkpoint.CheckpointId);
            break;
            
        case WorkflowResumedEvent resumed:
            Console.WriteLine($"工作流从检查点恢复: {resumed.FromCheckpointId}");
            break;
    }
}

// 企业级检查点策略
public class ResilientWorkflowService
{
    private readonly ICheckpointStorage _storage;
    
    public async Task<string> ExecuteWithResilienceAsync(string workflowId, string input)
    {
        // 尝试从最近检查点恢复
        var lastCheckpoint = await _storage.GetLatestCheckpointAsync(workflowId);
        
        if (lastCheckpoint != null)
        {
            // 从检查点恢复执行
            var recoveredResult = await workflow.RestoreFromCheckpointAsync(
                lastCheckpoint.Id);
            return recoveredResult.GetFinalAnswer();
        }
        else
        {
            // 全新执行，并设置自动检查点
            var run = await workflow.RunAsync(input);
            
            // 关键步骤后创建检查点
            if (run.CurrentStep == "research_complete")
            {
                await run.CreateCheckpointAsync("after_research");
            }
            
            return run.GetFinalAnswer();
        }
    }
}
```

**应用场景价值**:

+ **长时间运行流程**: 支持数小时甚至数天的复杂工作流
+ **容错恢复**: 进程崩溃或网络中断后可从最近检查点恢复
+ **分布式部署**: 检查点状态可在不同节点间迁移
+ **调试分析**: 检查点提供执行快照，便于问题诊断
+ **成本优化**: 避免因中断而重新执行昂贵操作（如大量API调用）

这两种机制共同构成了企业级工作流系统的核心基础设施，确保复杂AI工作流的可靠性、可维护性和生产就绪性。

### 2.2 工作流执行与管理
```csharp
// 执行工作流
await using StreamingRun run = await InProcessExecution.StreamAsync(
    workflow, 
    initialMessages
);

// 监听工作流事件
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is AgentRunUpdateEvent agentUpdate)
    {
        Console.WriteLine($"Agent {agentUpdate.ExecutorId} 正在执行");
    }
}
```

### 2.2 热重载工作流
<font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">dotnet run file特殊配置文件  需要研究下 是不是能传递参数 直接热重载文件，替代了工作流，code效率比拖拉拽高</font>

```csharp
// launchSettings.json风格配置支持
public class DotNetRunSettings
{
    public string ProfileName { get; set; }
    public string CommandName { get; set; } = "Project";
    public bool LaunchBrowser { get; set; }
    public string LaunchUrl { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

// 多环境配置支持
public class MultiEnvironmentConfig
{
    public static IConfiguration BuildConfiguration(string environment)
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile("launchSettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    // 静态文件服务集成
    public static void ConfigureStaticFiles(IApplicationBuilder app, string environment)
    {
        app.UseStaticFiles(new StaticFileOptions
                           {
                               ServeUnknownFileTypes = true,
                               DefaultContentType = "application/octet-stream"
                               });

        if (environment == "Development")
        {
            app.UseDirectoryBrowser();
        }
    }
}

// run.json配置文件示例
{
    "profiles": {
        "AIAgent.Dev": {
            "commandName": "Project",
            "environmentVariables": {
                "ASPNETCORE_ENVIRONMENT": "Development",
                "AZURE_OPENAI_ENDPOINT": " https://dev-openai.azure.com "
                }
        },
        "AIAgent.Prod": {
            "commandName": "Project", 
            "environmentVariables": {
                "ASPNETCORE_ENVIRONMENT": "Production",
                "AZURE_OPENAI_ENDPOINT": " https://prod-openai.azure.com "
                }
        }
    }
}
```



## 三、对话管理与持久化
### 3.1 AgentThread对话线程隔离
```csharp
// 创建独立对话线程
AgentThread thread = agent.GetNewThread();

// 多轮对话保持上下文
var response1 = await agent.RunAsync("第一轮问题", thread);
var response2 = await agent.RunAsync("基于上文的后续问题", thread); // Agent记得之前对话

// 多用户对话隔离
AgentThread user1Thread = agent.GetNewThread();
AgentThread user2Thread = agent.GetNewThread(); // 两个对话完全独立
```

### 3.2 对话持久化实现
```csharp
// 序列化对话状态
JsonElement serializedThread = thread.Serialize();
string jsonString = JsonSerializer.Serialize(serializedThread);

// 保存到数据库
var conversation = new Conversation 
{
    Id = Guid.NewGuid().ToString(),
    Context = jsonString
};
await dbContext.Conversations.AddAsync(conversation);
await dbContext.SaveChangesAsync();

// 从数据库恢复对话
var savedConversation = await dbContext.Conversations.FindAsync(conversationId);
JsonElement reloaded = JsonSerializer.Deserialize<JsonElement>(savedConversation.Context);
AgentThread resumedThread = agent.DeserializeThread(reloaded);

// 继续之前对话
var continuedResponse = await agent.RunAsync("继续之前的话题", resumedThread);
```

### 3.3 自定义存储实现
#### 3.3.1 InMemoryChatMessageStore - 内存存储（默认）
```csharp
public sealed class InMemoryChatMessageStore : ChatMessageStore, IList<ChatMessage>
{
    private List<ChatMessage> _messages = new List<ChatMessage>();

    // 默认构造函数（最常用）
    public InMemoryChatMessageStore() { }

    // 带缩减器的构造函数
    public InMemoryChatMessageStore(IChatReducer? chatReducer)
    {
        ChatReducer = chatReducer;
    }

    // 从序列化状态恢复
    public InMemoryChatMessageStore(JsonElement serializedState)
    {
        if (serializedState.ValueKind == JsonValueKind.Array)
        {
            _messages = serializedState.Deserialize<List<ChatMessage>>() ?? new();
        }
    }

    public override async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        _messages.AddRange(messages);
        
        // 应用缩减器（如果配置）
        if (ChatReducer != null)
        {
            _messages = (await ChatReducer.ReduceAsync(_messages, ct)).ToList();
        }
    }

    public override Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_messages.AsEnumerable());
    }

    public override JsonElement Serialize(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.SerializeToElement(_messages, options);
    }
}
```

#### 3.3.2 文件存储实现 - JSON文件持久化
```csharp
public class FileChatMessageStore : ChatMessageStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1); // 线程安全锁
    
    public FileChatMessageStore(string filePath, IChatReducer? reducer = null)
    {
        _filePath = filePath;
        ChatReducer = reducer;
    }

    public override async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            // 1. 加载现有消息
            var allMessages = (await LoadFromFileAsync(ct)).ToList();
            allMessages.AddRange(messages);
            
            // 2. 应用缩减器（自动裁剪）
            if (ChatReducer != null)
            {
                var reduced = await ChatReducer.ReduceAsync(allMessages, ct);
                allMessages = reduced.ToList();
            }
            
            // 3. 持久化保存
            await SaveToFileAsync(allMessages, ct);
        }
        finally { _lock.Release(); }
    }
    
    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try { return await LoadFromFileAsync(ct); }
        finally { _lock.Release(); }
    }

    private async Task<List<ChatMessage>> LoadFromFileAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return new List<ChatMessage>();

        var json = await File.ReadAllTextAsync(_filePath, ct);
        return JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
    }

    private async Task SaveToFileAsync(List<ChatMessage> messages, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    public override JsonElement Serialize(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.SerializeToElement(new { FilePath = _filePath }, options);
    }
}
```

#### 3.3.3 数据库存储 - Entity Framework集成/sqlsugar
```csharp
public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ThreadId { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }
}

public class ConversationDbContext : DbContext
{
    public DbSet<Conversation> Conversations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=ConversationDb.db");
        // 或使用SQL Server: optionsBuilder.UseSqlServer("YourConnectionString");
    }
}

public class DatabaseChatMessageStore : ChatMessageStore
{
    private readonly ConversationDbContext _dbContext;
    private readonly string _threadId;

    public DatabaseChatMessageStore(ConversationDbContext dbContext, string threadId, IChatReducer? reducer = null)
    {
        _dbContext = dbContext;
        _threadId = threadId;
        ChatReducer = reducer;
    }

    public override async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var dbMessages = messages.Select(m => new Conversation
        {
            ThreadId = _threadId,
            Role = m.Role.ToString(),
            Content = m.Text,
            Timestamp = DateTime.UtcNow,
            Metadata = JsonSerializer.Serialize(new { m.MessageId })
        });

        await _dbContext.Conversations.AddRangeAsync(dbMessages, ct);
        await _dbContext.SaveChangesAsync(ct);

        // 应用缩减器逻辑（可选）
        if (ChatReducer != null)
        {
            await ApplyReducerAsync(ct);
        }
    }

    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken ct = default)
    {
        var conversations = await _dbContext.Conversations
            .Where(c => c.ThreadId == _threadId)
            .OrderBy(c => c.Timestamp)
            .ToListAsync(ct);

        return conversations.Select(c => new ChatMessage
        {
            Role = Enum.Parse<ChatRole>(c.Role),
            Text = c.Content
        });
    }

    private async Task ApplyReducerAsync(CancellationToken ct)
    {
        var messages = await GetMessagesAsync(ct);
        var reduced = await ChatReducer!.ReduceAsync(messages, ct);
        
        // 删除被缩减的消息
        var toKeep = reduced.Select(m => m.Text).ToHashSet();
        var toDelete = await _dbContext.Conversations
            .Where(c => c.ThreadId == _threadId && !toKeep.Contains(c.Content))
            .ToListAsync(ct);

        _dbContext.Conversations.RemoveRange(toDelete);
        await _dbContext.SaveChangesAsync(ct);
    }

    public override JsonElement Serialize(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.SerializeToElement(new { ThreadId = _threadId }, options);
    }
}
```

#### 3.3.4 Redis分布式存储 - 多实例共享
```csharp
public class RedisChatMessageStore : ChatMessageStore
{
    private readonly IDistributedCache _redisCache;
    private readonly string _threadKey;
    private readonly TimeSpan _expiration;

    public RedisChatMessageStore(IDistributedCache redisCache, string threadId, 
        TimeSpan? expiration = null, IChatReducer? reducer = null)
    {
        _redisCache = redisCache;
        _threadKey = $"chat:{threadId}";
        _expiration = expiration ?? TimeSpan.FromHours(24);
        ChatReducer = reducer;
    }

    public override async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var existing = await GetMessagesAsync(ct);
        var allMessages = existing.Concat(messages).ToList();

        // 应用缩减器
        if (ChatReducer != null)
        {
            allMessages = (await ChatReducer.ReduceAsync(allMessages, ct)).ToList();
        }

        var serialized = JsonSerializer.Serialize(allMessages);
        await _redisCache.SetStringAsync(_threadKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        }, ct);
    }

    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken ct = default)
    {
        var cached = await _redisCache.GetStringAsync(_threadKey, ct);
        if (string.IsNullOrEmpty(cached))
            return Enumerable.Empty<ChatMessage>();

        return JsonSerializer.Deserialize<List<ChatMessage>>(cached) ?? new List<ChatMessage>();
    }

    public override async Task ClearAsync(CancellationToken ct = default)
    {
        await _redisCache.RemoveAsync(_threadKey, ct);
    }

    public override JsonElement Serialize(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.SerializeToElement(new { ThreadKey = _threadKey }, options);
    }
}
```

#### 3.3.5 向量数据库存储 - 语义搜索支持
```csharp
public sealed class VectorChatMessageStore : ChatMessageStore
{
    private readonly VectorStore _vectorStore;
    public string? ThreadDbKey { get; private set; }

    public VectorChatMessageStore(VectorStore vectorStore, JsonElement serializedStoreState, 
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        
        if (serializedStoreState.ValueKind is JsonValueKind.String)
        {
            ThreadDbKey = serializedStoreState.Deserialize<string>();
        }
    }

    public override async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        ThreadDbKey ??= Guid.NewGuid().ToString("N");
        
        var collection = _vectorStore.GetCollection<string, ChatHistoryItem>("ChatHistory");
        await collection.EnsureCollectionExistsAsync(ct);

        await collection.UpsertAsync(messages.Select(x => new ChatHistoryItem
        {
            Key = ThreadDbKey + x.MessageId,
            Timestamp = DateTimeOffset.UtcNow,
            ThreadId = ThreadDbKey,
            SerializedMessage = JsonSerializer.Serialize(x),
            MessageText = x.Text
        }), ct);
    }

    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken ct = default)
    {
        var collection = _vectorStore.GetCollection<string, ChatHistoryItem>("ChatHistory");
        await collection.EnsureCollectionExistsAsync(ct);

        var records = collection.GetAsync(
            x => x.ThreadId == ThreadDbKey, 
            10, 
            new() { OrderBy = x => x.Descending(y => y.Timestamp) }, 
            ct);

        var messages = new List<ChatMessage>();
        await foreach (var record in records)
        {
            messages.Add(JsonSerializer.Deserialize<ChatMessage>(record.SerializedMessage!)!);
        }

        messages.Reverse();
        return messages;
    }

    // 语义搜索扩展：根据内容相似度检索历史消息
    public async Task<IEnumerable<ChatMessage>> SearchSimilarMessagesAsync(string query, double threshold = 0.8, CancellationToken ct = default)
    {
        var collection = _vectorStore.GetCollection<string, ChatHistoryItem>("ChatHistory");
        var similar = await collection.FindNearestMatchesAsync(query, threshold, 5, ct);
        
        return similar.Select(x => JsonSerializer.Deserialize<ChatMessage>(x.SerializedMessage!)!);
    }

    public override JsonElement Serialize(JsonSerializerOptions? options = null)
    {
        return JsonSerializer.SerializeToElement(ThreadDbKey);
    }

    private sealed class ChatHistoryItem
    {
        [VectorStoreKey] public string? Key { get; set; }
        [VectorStoreData] public string? ThreadId { get; set; }
        [VectorStoreData] public DateTimeOffset? Timestamp { get; set; }
        [VectorStoreData] public string? SerializedMessage { get; set; }
        [VectorStoreData] public string? MessageText { get; set; }
    }
}
```

#### 3.3.6 SQLite轻量存储 - 本地应用场景
```csharp
public class SQLiteChatMessageStore : ChatMessageStore
{
    private readonly string _connectionString;
    private readonly string _threadId;

    public SQLiteChatMessageStore(string databasePath, string threadId, IChatReducer? reducer = null)
    {
        _connectionString = $"Data Source={databasePath};";
        _threadId = threadId;
        ChatReducer = reducer;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ChatMessages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ThreadId TEXT NOT NULL,
                Role TEXT NOT NULL,
                Content TEXT NOT NULL,
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                Metadata TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_ChatMessages_ThreadId ON ChatMessages(ThreadId);
        ";
        command.ExecuteNonQuery();
    }

    public override async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync(ct);

        foreach (var message in messages)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ChatMessages (ThreadId, Role, Content, Metadata)
                VALUES (@threadId, @role, @content, @metadata)
            ";
            command.Parameters.AddWithValue("@threadId", _threadId);
            command.Parameters.AddWithValue("@role", message.Role.ToString());
            command.Parameters.AddWithValue("@content", message.Text);
            command.Parameters.AddWithValue("@metadata", JsonSerializer.Serialize(new { message.MessageId }));
            
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken ct = default)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync(ct);

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Role, Content, Timestamp 
            FROM ChatMessages 
            WHERE ThreadId = @threadId 
            ORDER BY Timestamp ASC
        ";
        command.Parameters.AddWithValue("@threadId", _threadId);

        using var reader = await command.ExecuteReaderAsync(ct);
        var messages = new List<ChatMessage>();

        while (await reader.ReadAsync(ct))
        {
            messages.Add(new ChatMessage
            {
                Role = Enum.Parse<ChatRole>(reader.GetString(0)),
                Text = reader.GetString(1)
            });
        }

        return messages;
    }
}
```

#### 3.3.7 云存储集成 - Azure Cosmos DB等
```csharp
public class CosmosDBChatMessageStore : ChatMessageStore
{
    private readonly Container _container;
    private readonly string _threadId;

    public CosmosDBChatMessageStore(Container container, string threadId, IChatReducer? reducer = null)
    {
        _container = container;
        _threadId = threadId;
        ChatReducer = reducer;
    }

    public override async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var tasks = messages.Select(async message =>
        {
            var document = new
            {
                id = Guid.NewGuid().ToString(),
                threadId = _threadId,
                partitionKey = _threadId, // 使用threadId作为分区键
                role = message.Role.ToString(),
                content = message.Text,
                timestamp = DateTime.UtcNow,
                messageId = message.MessageId
            };

            await _container.CreateItemAsync(document, new PartitionKey(_threadId), cancellationToken: ct);
        });

        await Task.WhenAll(tasks);
    }

    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.threadId = @threadId ORDER BY c.timestamp")
            .WithParameter("@threadId", _threadId);

        var iterator = _container.GetItemQueryIterator<dynamic>(query);
        var messages = new List<ChatMessage>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            foreach (var item in response)
            {
                messages.Add(new ChatMessage
                {
                    Role = Enum.Parse<ChatRole>(item.role),
                    Text = item.content
                });
            }
        }

        return messages;
    }
}
```

#### 3.3.8 使用示例：集成到Agent
```csharp
// 使用文件存储的Agent配置
var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "文件存储示例",
    Instructions = "你是一个专业的助手",
    ChatMessageStoreFactory = ctx => new FileChatMessageStore(
        filePath: "chat-history.json",
        reducer: new MessageCountingChatReducer(10)
    )
});

// 使用Redis存储的Agent配置
var redisAgent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "Redis存储示例", 
    ChatMessageStoreFactory = ctx => new RedisChatMessageStore(
        redisCache: new RedisCache(/*配置*/),
        threadId: ctx.ThreadId,
        expiration: TimeSpan.FromDays(7),
        reducer: new MessageCountingChatReducer(20)
    )
});
```

#### 3.3.9 其他实现 - 待定 没想好
```csharp
// 实现自定义ChatMessageStore
internal sealed class SqlChatMessageStore : ChatMessageStore
{
    private readonly AppDbContext _dbContext;
    
    public override async Task AddMessagesAsync(
        IEnumerable<ChatMessage> messages, 
        CancellationToken ct = default)
    {
        // 保存消息到数据库
        await _dbContext.ChatMessages.AddRangeAsync(messages.Select(m => new ChatMessageEntity(m)));
        await _dbContext.SaveChangesAsync(ct);
    }
    
    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        CancellationToken ct = default)
    {
        // 从数据库加载消息
        return await _dbContext.ChatMessages
            .OrderBy(m => m.Timestamp)
            .Select(m => m.ToChatMessage())
            .ToListAsync(ct);
    }
}

// 使用自定义存储
var options = new ChatClientAgentOptions
{
    ChatMessageStoreFactory = ctx => new SqlChatMessageStore(dbContext)
};
```

## 四、结构化数据处理
### 4.1 强类型结构化输出
```csharp
// 定义输出数据结构
public class PersonInfo
{
    [JsonPropertyName("name")]
    [Description("人员姓名")]
    public string? Name { get; set; }
    
    [JsonPropertyName("age")] 
    [Description("年龄")]
    public int? Age { get; set; }
    
    [JsonPropertyName("occupation")]
    [Description("职业")]
    public string? Occupation { get; set; }
}

// 方式一：RunAsync泛型方法（推荐） 直接获取强类型对象
AgentRunResponse<PersonInfo> response = await agent.RunAsync<PersonInfo>(
    "请提供关于张三的信息，他是一名30岁的软件工程师。"
);

// 方式二：显式配置ResponseFormat
var options = new ChatOptions
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema<PersonInfo>()
};

Console.WriteLine($"姓名: {response.Result.Name}"); // 直接访问属性
Console.WriteLine($"年龄: {response.Result.Age}");
Console.WriteLine($"职业: {response.Result.Occupation}");
```

### 4.2 JSON Schema约束输出
实现方式：显式配置ResponseFormat为JSON Schema

```csharp
// 配置JSON Schema响应格式
var agentWithSchema = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "结构化输出Agent",
    Instructions = "你是一个信息提取助手",
    ChatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.ForJsonSchema<PersonInfo>()
    }
});

// 流式结构化输出
var updates = agentWithSchema.RunStreamingAsync("提取用户信息");
var finalResponse = await updates.ToAgentRunResponseAsync();
var personInfo = finalResponse.Deserialize<PersonInfo>();

// 生成JSON Schema并配置Agent
var schema = AIJsonUtilities.CreateJsonSchema(typeof(PersonInfo));
var options = new ChatOptions
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema(
        schema: schema,
        schemaName: "PersonInfo",
        schemaDescription: "个人信息描述")
};

// 创建配置了JSON Schema的Agent
var agentWithSchema = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "HelpfulAssistant",
    Instructions = "你是一个乐于助人的助手。",
    ChatOptions = options
});
```

**适用场景**：

+ 需要更精确的格式控制
+ 流式输出场景
+ 企业级应用要求

### 4.3 嵌套对象支持 - 复杂数据结构
```csharp
using System.ComponentModel;
using System.Text.Json.Serialization;

// 评论情感分析（嵌套对象）
public class SentimentAnalysis
{
    [JsonPropertyName("sentiment")]
    [Description("情感极性：正面、负面、中性")]
    public string? Sentiment { get; set; }
    
    [JsonPropertyName("confidence")]
    [Description("情感置信度，0-1之间")]
    public double Confidence { get; set; }
    
    [JsonPropertyName("reasons")]
    [Description("情感判断依据")]
    public List<string>? Reasons { get; set; }
}

// 产品评论分析（主对象，包含嵌套）
public class ProductReviewAnalysis
{
    [JsonPropertyName("product_name")]
    [Description("产品名称")]
    public string? ProductName { get; set; }
    
    [JsonPropertyName("rating")]
    [Description("评分，1-5分")]
    public int Rating { get; set; }
    
    [JsonPropertyName("sentiment_analysis")]
    [Description("情感分析结果")]
    public SentimentAnalysis? Sentiment { get; set; }
    
    [JsonPropertyName("key_points")]
    [Description("评论要点总结")]
    public List<string>? KeyPoints { get; set; }
    
    [JsonPropertyName("is_recommended")]
    [Description("是否推荐购买")]
    public bool IsRecommended { get; set; }
    
    [JsonPropertyName("tags")]
    [Description("评论标签分类")]
    public List<string>? Tags { get; set; }
}
```

```csharp
// 完整运行代码
using Microsoft.Extensions.AI;
using System.Text.Json;

// 1. 生成JSON Schema
var schema = AIJsonUtilities.CreateJsonSchema(typeof(ProductReviewAnalysis));

// 2. 配置结构化输出选项
var reviewOptions = new ChatOptions
{
    ResponseFormat = ChatResponseFormatJson.ForJsonSchema(
        schema: schema,
        schemaName: "ProductReviewAnalysis",
        schemaDescription: "产品评论分析结果，包含情感分析、关键要点等")
};

// 3. 准备系统提示词
var systemPrompt = @"你是一个专业的产品评论分析助手。请仔细分析用户提供的产品评论，严格按照JSON格式返回分析结果。
重点关注：产品名称识别、评分推断、情感判断、关键要点提取、推荐意向分析。";

// 4. 执行分析请求
var messages = new[]
{
    new ChatMessage(ChatRole.System, systemPrompt),
    new ChatMessage(ChatRole.User, "iPhone 15 Pro 屏幕非常清晰、运行速度超快、夜景拍照效果惊艳；就是价格有点高，但整体来说非常值得购买。")
};

var client = AIClientHelper.GetDefaultChatClient();
var result = await client.CompleteAsync(messages, reviewOptions);

// 5. 反序列化结果
try
{
    var analysis = JsonSerializer.Deserialize<ProductReviewAnalysis>(
        result.Message.Text!, 
        JsonSerializerOptions.Web);
    
    // 6. 使用分析结果
    Console.WriteLine($"产品: {analysis.ProductName}");
    Console.WriteLine($"评分: {analysis.Rating}/5");
    Console.WriteLine($"情感: {analysis.Sentiment?.Sentiment} (置信度: {analysis.Sentiment?.Confidence:P0})");
    Console.WriteLine($"推荐: {(analysis.IsRecommended ? "是" : "否")}");
    Console.WriteLine("关键要点:");
    foreach (var point in analysis.KeyPoints ?? new List<string>())
    {
        Console.WriteLine($"- {point}");
    }
}
catch (JsonException ex)
{
    // 错误处理：尝试提取JSON片段或使用默认值
    Console.WriteLine($"反序列化失败: {ex.Message}");
    // 兜底逻辑...
}

// Agent框架中的使用（MAF）
using Microsoft.AI.Agents;

// 使用Agent框架直接返回强类型结果
var agent = new ChatClientAgent(client, "你是一个产品评论分析专家");

// 直接获取结构化结果
var analysisResult = await agent.RunAsync<ProductReviewAnalysis>(
    "分析这个评论：三星Galaxy S24续航很棒，屏幕色彩鲜艳，但系统流畅度一般。价格合理，适合日常使用。");

// 使用结果驱动业务流程
if (analysisResult.Result.Rating >= 4)
{
    // 高评分评论，自动标记为优质内容
    await MarkAsFeaturedReview(analysisResult.Result);
}

if (analysisResult.Result.Sentiment?.Sentiment == "负面")
{
    // 负面评论，触发客服跟进流程
    await CreateCustomerServiceTicket(analysisResult.Result);
}
```

### 4.4 枚举类型处理 - JsonStringEnumConverter
```csharp
using System.ComponentModel;
using System.Text.Json.Serialization;

// 审批状态枚举
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApprovalStatus
{
    [Description("已批准")]
    Approved,
    
    [Description("已拒绝")]
    Rejected,
    
    [Description("待审批")]
    Pending,
    
    [Description("需要更多信息")]
    NeedMoreInfo,
    
    [Description("自动批准")]
    AutoApproved
}

// 风险等级枚举
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RiskLevel
{
    [Description("低风险")]
    Low,
    
    [Description("中风险")]
    Medium,
    
    [Description("高风险")]
    High,
    
    [Description("极高风险")]
    Critical
}

// 审批决策模型
public class ApprovalDecision
{
    [JsonPropertyName("application_id")]
    [Description("申请ID")]
    public string? ApplicationId { get; set; }
    
    [JsonPropertyName("status")]
    [Description("审批状态")]
    public ApprovalStatus Status { get; set; }
    
    [JsonPropertyName("risk_level")]
    [Description("风险等级评估")]
    public RiskLevel RiskLevel { get; set; }
    
    [JsonPropertyName("approver_comment")]
    [Description("审批意见")]
    public string? ApproverComment { get; set; }
    
    [JsonPropertyName("required_actions")]
    [Description("需要执行的操作")]
    public List<string>? RequiredActions { get; set; }
    
    [JsonPropertyName("next_review_date")]
    [Description("下次复核日期")]
    public DateTimeOffset? NextReviewDate { get; set; }
}

```

```csharp

// 完整运行代码
using Microsoft.Extensions.AI;
using System.Text.Json;

// 1. 生成包含枚举的Schema
var schema = AIJsonUtilities.CreateJsonSchema(typeof(ApprovalDecision));

// 2. 配置选项（确保枚举转换器生效）
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOptions.Converters.Add(new JsonStringEnumConverter());

var approvalOptions = new ChatOptions
{
    ResponseFormat = ChatResponseFormatJson.ForJsonSchema(
        schema: schema,
        schemaName: "ApprovalDecision",
        schemaDescription: "审批决策结果")
};

// 3. 准备包含枚举值的提示词
var systemPrompt = @"你是一个审批专家。请分析申请内容，返回审批决策。
可用状态值：Approved（批准）、Rejected（拒绝）、Pending（待审批）、NeedMoreInfo（需要更多信息）、AutoApproved（自动批准）
可用风险等级：Low（低）、Medium（中）、High（高）、Critical（极高）
请严格使用上述枚举值，不要自行创造新值。";

// 4. 执行审批分析
var messages = new[]
{
    new ChatMessage(ChatRole.System, systemPrompt),
    new ChatMessage(ChatRole.User, @"申请内容：张伟申请采购10台MacBook Pro用于新员工入职，预算25万元。
申请理由：新团队扩建需要，现有设备不足。供应商报价合理，符合采购流程。")
};

var client = AIClientHelper.GetDefaultChatClient();
var result = await client.CompleteAsync(messages, approvalOptions);

// 5. 反序列化并使用枚举结果
try
{
    var decision = JsonSerializer.Deserialize<ApprovalDecision>(
        result.Message.Text!, 
        jsonOptions); // 使用包含枚举转换器的选项
    
    // 基于枚举值的业务逻辑
    switch (decision.Status)
    {
        case ApprovalStatus.Approved:
            await ProcessApprovedApplication(decision);
            Console.WriteLine($"申请已批准，风险等级: {decision.RiskLevel}");
            break;
            
        case ApprovalStatus.Rejected:
            await ProcessRejectedApplication(decision);
            Console.WriteLine($"申请已拒绝，原因: {decision.ApproverComment}");
            break;
            
        case ApprovalStatus.NeedMoreInfo:
            await RequestMoreInformation(decision);
            Console.WriteLine("需要更多信息才能审批");
            break;
            
        default:
            await QueueForManualReview(decision);
            break;
    }
    
    // 输出枚举的描述信息
    var statusDescription = GetEnumDescription(decision.Status);
    var riskDescription = GetEnumDescription(decision.RiskLevel);
    Console.WriteLine($"审批状态: {statusDescription}, 风险等级: {riskDescription}");
}
catch (JsonException ex)
{
    Console.WriteLine($"反序列化失败: {ex.Message}");
}

// 辅助方法：获取枚举描述
private static string GetEnumDescription(Enum value)
{
    var field = value.GetType().GetField(value.ToString());
    var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
        .FirstOrDefault() as DescriptionAttribute;
    return attribute?.Description ?? value.ToString();
}
```

**流式输出中的枚举处理**

```csharp
// 流式场景下的枚举处理
var streamingUpdates = client.GetStreamingResponseAsync(messages, approvalOptions);

StringBuilder jsonBuilder = new StringBuilder();
await foreach (var chunk in streamingUpdates)
{
    jsonBuilder.Append(chunk);
    Console.Write(chunk); // 实时显示流式输出
}

// 流式完成后反序列化
var finalJson = jsonBuilder.ToString();
var streamingDecision = JsonSerializer.Deserialize<ApprovalDecision>(finalJson, jsonOptions);

// 验证枚举值合法性
if (!Enum.IsDefined(typeof(ApprovalStatus), streamingDecision.Status))
{
    // 处理模型返回了非法枚举值的情况
    streamingDecision.Status = ApprovalStatus.Pending;
    streamingDecision.ApproverComment += " (状态值无效，已重置为待审批)";
}
```

**错误处理和兜底策略**

```csharp
// 枚举值验证和修正
public static TEnum ValidateEnum<TEnum>(string value, TEnum defaultValue) where TEnum : struct
{
    if (Enum.TryParse<TEnum>(value, out var result) && Enum.IsDefined(typeof(TEnum), result))
    {
        return result;
    }
    
    // 尝试模糊匹配
    var normalizedValue = value.Trim().ToLower();
    foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
    {
        if (enumValue.ToString().ToLower() == normalizedValue)
        {
            return enumValue;
        }
    }
    
    return defaultValue;
}

// 在反序列化失败时使用
try
{
    var decision = JsonSerializer.Deserialize<ApprovalDecision>(jsonText, jsonOptions);
}
catch (JsonException)
{
    // 手动解析并验证枚举值
    using var doc = JsonDocument.Parse(jsonText);
    var decision = new ApprovalDecision
    {
        Status = ValidateEnum(doc.RootElement.GetProperty("status").GetString(), ApprovalStatus.Pending),
        RiskLevel = ValidateEnum(doc.RootElement.GetProperty("risk_level").GetString(), RiskLevel.Medium)
    };
}
```

### 4.5 国产模型适配 - DeepSeek/Qwen兼容策略
| **模型系列** | **开发者/组织** | **JSON Schema 支持** | **备注（Fallback 适用性）** |
| --- | --- | --- | --- |
| GPT-4o / o1 | OpenAI | 原生支持（Structured Outputs） | 行业标杆，零 fallback。 |
| Claude 3.5 Sonnet | Anthropic | 原生支持（Tool Use） | 强于复杂 schema，无需提示。 |
| Llama 3.1 / 3.2 | Meta | 原生支持（vLLM/框架） | 开源首选，但本地需 vLLM 启用 schema。 |
| Gemini 1.5 Pro | Google | 原生支持（Function Calling） | 集成 Vertex AI，schema 可靠。 |
| Qwen2.5 / Qwen3 | 阿里 | 部分支持（JSON Mode，无 schema） | 需提示约束（如您的 systemPrompt）；Qwen-Agent 辅助解析。 |
| DeepSeek-V3 / R1 | DeepSeek AI | 原生支持（JSON Mode + Strict） | 2025 升级后全兼容 OpenAI；早期 R1 偶需提示。 |
| GLM-4 | 智谱 AI | 原生支持 | 代码/工具调用强，schema 稳定。 |
| Mistral Large 2 | Mistral AI | 原生支持（Tool Calling） | 开源友好，vLLM 优化好。 |
| Phi-3.5 | Microsoft | 框架适配（需提示） | Semantic Kernel 中 fallback 常见；无原生 schema。 |
| Gemma 2 27B | Google | 需提示/框架 | 不支持原生；本地（如 LM Studio）易失败，适合简单 JSON。 |


```csharp
// 当模型不支持JSON Schema时
var deepseekResponse = await deepseekAgent.RunAsync<PersonInfo>(
    "用户输入",
    useJsonSchemaResponseFormat: false);

// 使用严格提示词约束
var systemPrompt = @"严格按下列JSON返回，不要输出任何其他文本：
{
    \"name\": \"字符串\",
    \"age\": 0,
    \"occupation\": \"字符串\"
}";
```

**配置对比**：

| 模型类型 | Schema支持 | 配置方式 | 提示词要求 |
| :--- | :--- | :--- | :--- |
| OpenAI/Azure | 自动 | Schema默认即可 | 简要描述 |
| DeepSeek/Qwen | ChatResponseFormat.Json | `useJsonSchemaResponseFormat: false` | 完整JSON模板 |


### 4.6 TOON协议支持 - 比JSON更高效的序列化
支持者的序列化，应该是有包的

**TOON协议优势**：

+ Token高效：比JSON节省30-60%的tokens
+ LLM友好：显式长度与字段，便于验证
+ 最小化语法：移除冗余标点

```csharp
// 安装NuGet包
dotnet add package AIDotNet.Toon

// 序列化示例
var data = new { users = new[] { new { id = 1, name = "Alice" } } };
var toonText = ToonSerializer.Serialize(data, options);

// 输出格式：
// users[1]{id,name}:
//   1,Alice
```

## 五、上下文管理与压缩
### 5.1 消息计数压缩器
```csharp
using Microsoft.Extensions.AI;

// 创建计数压缩器（保留最近5条消息）
var countingReducer = new MessageCountingChatReducer(targetCount: 5);

// 集成到Chat Client
var client = baseChatClient.AsBuilder()
    .UseChatReducer(reducer: countingReducer)
    .Build();

// 自动压缩长对话
var response = await client.GetResponseAsync(messages);
// 原始消息: [系统指令, 用户消息1, AI回复1, ..., 用户消息10, AI回复10]
// 压缩后: [系统指令, 用户消息6, AI回复6, ..., 用户消息10, AI回复10] (保留最近5轮)
```

### 5.2 智能摘要压缩器
```csharp
// 创建摘要压缩器
var summarizingReducer = new SummarizingChatReducer(
    chatClient: baseChatClient,  // 用于生成摘要的ChatClient
    targetCount: 2,              // 保留最近2条原始消息
    threshold: 1                 // 超过3条时触发摘要
);

// 配置摘要提示词
summarizingReducer.SummaryPrompt = "请将以下对话历史总结为简洁的摘要，保留关键信息：";

var client = baseChatClient.AsBuilder()
    .UseChatReducer(reducer: summarizingReducer)
    .Build();
```

### 5.3 自定义压缩策略 - IChatReducer接口实现
```csharp
public class BusinessContextReducer : IChatReducer
{
    private readonly int _maxMessages;
    private readonly HashSet<string> _protectedKeywords;
    
    public BusinessContextReducer(int maxMessages = 10, IEnumerable<string>? protectedKeywords = null)
    {
        _maxMessages = maxMessages;
        _protectedKeywords = protectedKeywords?.ToHashSet() ?? new HashSet<string>();
    }
    
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, 
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        
        if (messageList.Count <= _maxMessages)
            return messageList;
        
        // 实施压缩策略
        return await ApplyCompressionStrategy(messageList, cancellationToken);
    }
    
    private async Task<List<ChatMessage>> ApplyCompressionStrategy(
        List<ChatMessage> messages, 
        CancellationToken ct)
    {
        var compressed = new List<ChatMessage>();
        var preservedMessages = new List<ChatMessage>();
        
        // 策略1: 系统消息保护
        var systemMessages = messages.Where(m => m.Role == ChatRole.System);
        compressed.AddRange(systemMessages);
        
        // 策略2: 关键词优先保留
        var remaining = messages.Except(systemMessages).ToList();
        var (protectedMsgs, normalMsgs) = SplitByKeywords(remaining);
        
        preservedMessages.AddRange(protectedMsgs);
        
        // 策略3: 函数调用消息保护
        var functionMessages = ExtractFunctionMessages(normalMsgs);
        preservedMessages.AddRange(functionMessages);
        
        // 策略4: 时间窗口压缩
        var recentMessages = GetRecentMessages(
            normalMsgs.Except(functionMessages), 
            _maxMessages - preservedMessages.Count - compressed.Count
        );
        
        compressed.AddRange(preservedMessages);
        compressed.AddRange(recentMessages);
        
        return compressed;
    }
}
```

### 5.4 关键词优先保留 - 业务关键消息保护
```csharp
public class KeywordAwareReducer : IChatReducer
{
    private readonly string[] _criticalKeywords = new[]
    {
        "审批", "支付", "合同", "协议", "订单", 
        "价格", "金额", "截止时间", "重要", "紧急"
    };
    
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken ct)
    {
        var messageList = messages.ToList();
        
        // 分离关键消息和普通消息
        var (criticalMessages, normalMessages) = ClassifyMessages(messageList);
        
        // 永远保留关键消息
        var result = new List<ChatMessage>();
        result.AddRange(criticalMessages);
        
        // 对普通消息应用压缩
        if (normalMessages.Count > 10) // 阈值配置
        {
            var compressedNormal = await CompressNormalMessages(normalMessages, ct);
            result.AddRange(compressedNormal);
        }
        else
        {
            result.AddRange(normalMessages);
        }
        
        return result.Take(20); // 最终数量控制
    }
    
    private (List<ChatMessage> critical, List<ChatMessage> normal) 
        ClassifyMessages(List<ChatMessage> messages)
    {
        var critical = new List<ChatMessage>();
        var normal = new List<ChatMessage>();
        
        foreach (var message in messages)
        {
            if (_criticalKeywords.Any(keyword => 
                message.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                critical.Add(message);
            }
            else
            {
                normal.Add(message);
            }
        }
        
        return (critical, normal);
    }
}
```

### 5.5 系统消息保护 - 指令消息不被压缩
```csharp
public class SystemMessageProtectionReducer : IChatReducer
{
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken ct)
    {
        var messageList = messages.ToList();
        
        // 永远保留系统指令消息
        var systemMessages = messageList
            .Where(m => m.Role == ChatRole.System)
            .ToList();
            
        // 对非系统消息应用压缩
        var nonSystemMessages = messageList
            .Where(m => m.Role != ChatRole.System)
            .ToList();
            
        var compressedNonSystem = await CompressNonSystemMessages(nonSystemMessages, ct);
        
        // 合并结果：系统消息 + 压缩后的非系统消息
        var result = new List<ChatMessage>();
        result.AddRange(systemMessages);
        result.AddRange(compressedNonSystem);
        
        return result;
    }
}
```

### 5.6 函数调用消息保护 - 工具调用上下文完整
```csharp
public class FunctionCallPreservationReducer : IChatReducer
{
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken ct)
    {
        var messageList = messages.ToList();
        
        // 识别函数调用相关消息
        var functionRelatedMessages = IdentifyFunctionMessages(messageList);
        
        // 分离函数消息和非函数消息
        var (functionMessages, regularMessages) = SplitMessages(messageList, functionRelatedMessages);
        
        // 压缩常规消息，保留函数消息完整
        var compressedRegular = await CompressRegularMessages(regularMessages, ct);
        
        // 按时间顺序合并
        return MergePreservingOrder(functionMessages, compressedRegular);
    }
    
    private HashSet<ChatMessage> IdentifyFunctionMessages(List<ChatMessage> messages)
    {
        var functionMessages = new HashSet<ChatMessage>();
        
        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            
            // 识别函数调用模式
            if (message.Text.Contains("tool_calls") || 
                message.Text.Contains("function_call") ||
                message.Metadata?.ContainsKey("is_function_call") == true)
            {
                // 包含函数调用本身和相邻的上下文消息
                functionMessages.Add(message);
                
                // 添加上下文消息（前2条和后1条）
                if (i > 0) functionMessages.Add(messages[i-1]);
                if (i > 1) functionMessages.Add(messages[i-2]);
                if (i < messages.Count - 1) functionMessages.Add(messages[i+1]);
            }
        }
        
        return functionMessages;
    }
}
```

### 5.7 压缩触发策略 - 阈值配置与触发时机
```csharp
public class AdaptiveCompressionReducer : IChatReducer
{
    private readonly CompressionConfig _config;
    
    public AdaptiveCompressionReducer(CompressionConfig config)
    {
        _config = config;
    }
    
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken ct)
    {
        var messageList = messages.ToList();
        var totalTokens = EstimateTokens(messageList);
        
        // 多维度触发条件检查
        bool shouldCompress = CheckCompressionConditions(messageList, totalTokens);
        
        if (!shouldCompress)
            return messageList;
        
        // 根据严重程度选择压缩策略
        var compressionLevel = DetermineCompressionLevel(messageList.Count, totalTokens);
        
        return compressionLevel switch
        {
            CompressionLevel.Light => await ApplyLightCompression(messageList, ct),
            CompressionLevel.Medium => await ApplyMediumCompression(messageList, ct),
            CompressionLevel.Heavy => await ApplyHeavyCompression(messageList, ct),
            _ => messageList
        };
    }
    
    private bool CheckCompressionConditions(List<ChatMessage> messages, int totalTokens)
    {
        // 消息数量阈值
        if (messages.Count > _config.MessageCountThreshold)
            return true;
            
        // Token数量阈值
        if (totalTokens > _config.TokenCountThreshold)
            return true;
            
        // 对话时长阈值（长时间对话）
        var firstMessageTime = GetFirstMessageTime(messages);
        var duration = DateTime.Now - firstMessageTime;
        if (duration > _config.TimeDurationThreshold)
            return true;
            
        return false;
    }
}

public class CompressionConfig
{
    public int MessageCountThreshold { get; set; } = 20;
    public int TokenCountThreshold { get; set; } = 4000;
    public TimeSpan TimeDurationThreshold { get; set; } = TimeSpan.FromMinutes(30);
    public double CompressionRatio { get; set; } = 0.3; // 压缩到30%
}
```

### 5.8 分层压缩策略
```csharp
// 智能分层压缩实现
public class LayeredCompressionReducer : IChatReducer
{
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken ct)
    {
        var messageList = messages.ToList();
        
        if (messageList.Count <= 15) // 第一层阈值
            return messageList;
            
        // 分层压缩策略
        var compressed = await ApplyLayeredCompression(messageList, ct);
        
        return compressed;
    }
    
    private async Task<List<ChatMessage>> ApplyLayeredCompression(
        List<ChatMessage> messages, CancellationToken ct)
    {
        var compressed = new List<ChatMessage>();
        
        // 第1层：保留系统消息和最近5条
        compressed.AddRange(messages.Where(m => m.Role == ChatRole.System));
        compressed.AddRange(messages.TakeLast(5));
        
        // 第2层：对中间消息进行智能摘要
        var middleMessages = messages
            .Skip(compressed.Count)
            .Take(messages.Count - compressed.Count - 5)
            .ToList();
            
        if (middleMessages.Count > 0)
        {
            var summary = await GenerateSummary(middleMessages, ct);
            compressed.Insert(compressed.Count - 5, 
                new ChatMessage(ChatRole.System, $"对话摘要: {summary}"));
        }
        
        return compressed;
    }
}
```

### 5.9 性能优化配置
```csharp
// 生产环境压缩配置
services.AddSingleton<IChatReducer>(provider => 
{
    var config = provider.GetRequiredService<IConfiguration>();
    
    return new CompositeChatReducer(new[]
    {
        new SystemMessageProtectionReducer(),
        new KeywordAwareReducer(
            criticalKeywords: config.GetSection("Compression:CriticalKeywords").Get<string[]>()
        ),
        new FunctionCallPreservationReducer(),
        new AdaptiveCompressionReducer(
            new CompressionConfig
            {
                MessageCountThreshold = config.GetValue<int>("Compression:MessageThreshold"),
                TokenCountThreshold = config.GetValue<int>("Compression:TokenThreshold"),
                CompressionRatio = config.GetValue<double>("Compression:Ratio")
            }
        )
    });
});

// appsettings.json配置
{
  "Compression": {
    "MessageThreshold": 25,
    "TokenThreshold": 6000,
    "Ratio": 0.4,
    "CriticalKeywords": ["审批", "支付", "合同", "订单", "重要"]
  }
}
```

### 5.10 监控与调优
```csharp
// 压缩效果监控
public class CompressionMonitor
{
    public void LogCompressionEffectiveness(
        List<ChatMessage> original, 
        List<ChatMessage> compressed,
        TimeSpan compressionTime)
    {
        var originalCount = original.Count;
        var compressedCount = compressed.Count;
        var compressionRatio = (double)compressedCount / originalCount;
        
        var originalTokens = EstimateTokens(original);
        var compressedTokens = EstimateTokens(compressed);
        var tokenSaving = 1.0 - (double)compressedTokens / originalTokens;
        
        // 记录压缩指标
        Logger.LogInformation(
            "压缩效果: 消息数 {Original} → {Compressed} ({Ratio:P1}), " +
            "Token数 {OriginalTokens} → {CompressedTokens} ({TokenSaving:P1}节省), " +
            "耗时: {CompressionTime}ms",
            originalCount, compressedCount, compressionRatio,
            originalTokens, compressedTokens, tokenSaving,
            compressionTime.TotalMilliseconds);
    }
}
```



## 六、工具调用与函数集成


| **模型系列** | **开发者/组织** | **Function Calling 支持** | **备注** |
| --- | --- | --- | --- |
| GPT-4o / GPT-4 | OpenAI | 原生支持 | 行业标准，广泛用于 Agent 系统。 |
| Claude 3.5 Sonnet | Anthropic | 原生支持 | 强于复杂工具链调用。 |
| Llama 3.1 | Meta | 原生支持 | 开源首选，支持多语言。 |
| Gemini 1.5 | Google | 原生支持 | 集成 Google 生态工具。 |
| 通义千问 (Qwen2.5) | 阿里 | 原生支持 | Qwen-Agent 框架增强，适合中文场景。 |
| DeepSeek-V3/R1 | DeepSeek AI | 原生支持（平台适配） | 2025 年升级后可用，早期需框架绕行。 |
| GLM-4 | 智谱 AI | 原生支持 | 强于代码生成工具调用。 |
| Kimi / Moonshot | 月之暗面 | 原生支持 | 专注长上下文工具集成。 |
| 文心一言 (ERNIE) | 百度 | 原生支持 | 集成百度搜索工具。 |
| Phi-3 | Microsoft | 框架适配 | 通过 Semantic Kernel 等实现。 |


### 6.1 基础函数调用
```csharp
// 定义可调用函数
[Description("获取指定城市的天气信息")]
public static string GetWeather(
    [Description("城市名称，例如：北京")] string city)
{
    // 实际天气API调用逻辑
    return $"{city}的天气是晴，25℃";
}

// 注册到Agent
var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Tools = [AIFunctionFactory.Create(GetWeather)]
});

// Agent会自动判断何时调用天气函数
```

```csharp
// 基础函数定义
[Description("计算两个数字的和")]
public static int AddNumbers(
    [Description("第一个数字")] int a,
    [Description("第二个数字")] int b)
{
    return a + b;
}

// 注册到Agent
var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Tools = [AIFunctionFactory.Create(AddNumbers)]
});

// 使用示例：Agent会自动识别何时调用函数
var response = await agent.RunAsync("请计算25加38等于多少？");
// Agent会自动调用AddNumbers(25, 38)并返回结果
```

### 6.2 AIFunctionFactory - 普通函数转AI工具
```csharp
// 1. 静态方法转换
public static class MathFunctions
{
    [Description("计算平方根")]
    public static double Sqrt(double number) => Math.Sqrt(number);
}

var mathTool = AIFunctionFactory.Create(MathFunctions.Sqrt);

// 2. 实例方法转换
public class WeatherService
{
    [Description("获取城市天气")]
    public async Task<string> GetWeatherAsync(string city)
    {
        // 调用天气API
        return await _httpClient.GetStringAsync($" https://api.weather.com/ {city}");
    }
}

var weatherService = new WeatherService();
var weatherTool = AIFunctionFactory.Create(weatherService.GetWeatherAsync);

// 3. 带复杂参数的函数
[Description("创建用户账户")]
public static User CreateUser(
    [Description("用户名")] string username,
    [Description("邮箱地址")] string email,
    [Description("用户角色")] UserRole role = UserRole.User)
{
    return new User { Username = username, Email = email, Role = role };
}

var createUserTool = AIFunctionFactory.Create(CreateUser);
```

### 6.3 工具描述生成 - Description特性自动提取
```csharp
// 自动提取工具元数据
public class ToolMetadataGenerator
{
    public static ToolDefinition GenerateToolDefinition(MethodInfo method)
    {
        var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();
        var parameters = method.GetParameters();
        
        var paramDescriptions = parameters.Select(p => 
        {
            var paramDesc = p.GetCustomAttribute<DescriptionAttribute>();
            return new ParameterDefinition
            {
                Name = p.Name!,
                Description = paramDesc?.Description ?? p.Name!,
                Type = GetParameterType(p.ParameterType),
                IsRequired = !p.HasDefaultValue
            };
        });
        
        return new ToolDefinition
        {
            Name = method.Name,
            Description = descriptionAttr?.Description ?? method.Name,
            Parameters = paramDescriptions.ToList()
        };
    }
}

// 使用示例
var toolDefinition = ToolMetadataGenerator.GenerateToolDefinition(
    typeof(MathFunctions).GetMethod(nameof(MathFunctions.Sqrt)));

Console.WriteLine($"工具名: {toolDefinition.Name}");
Console.WriteLine($"描述: {toolDefinition.Description}");
foreach (var param in toolDefinition.Parameters)
{
    Console.WriteLine($"参数: {param.Name} - {param.Description}");
}
```

### 6.4 人工审批机制 - ApprovalRequiredAIFunction
```csharp
// 创建需要审批的工具
var approvedWeatherFunction = new ApprovalRequiredAIFunction(
    underlyingFunction: AIFunctionFactory.Create(GetWeather),
    approvalPrompt: "是否允许查询天气信息？"
);

// 使用审批流程
var response = await agent.RunAsync("查询北京天气", thread);
if (response.Contains("[需要审批]"))
{
    Console.Write("是否批准查询天气？(Y/N): ");
    var approval = Console.ReadLine();
    if (approval?.ToUpper() == "Y")
    {
        // 继续执行已批准的工具调用
    }
}

// 创建需要审批的高风险工具
public class DatabaseOperations
{
    [Description("删除用户数据")]
    public static bool DeleteUserData(string userId)
    {
        // 高风险操作
        return Database.DeleteUser(userId);
    }
}

// 包装为需要审批的函数
var deleteFunction = new ApprovalRequiredAIFunction(
    underlyingFunction: AIFunctionFactory.Create(DatabaseOperations.DeleteUserData),
    approvalPrompt: "⚠️ 高风险操作：是否允许删除用户数据？此操作不可逆转。",
    requiredApprovalLevel: ApprovalLevel.Manager
);

// 审批流程实现
public class ApprovalWorkflow
{
    public async Task<ApprovalResult> RequestApprovalAsync(
        string operation, 
        string details, 
        ApprovalLevel level)
    {
        // 发送审批通知到相应审批人
        var approver = await GetApproverAsync(level);
        var approvalRequest = new ApprovalRequest
        {
            Operation = operation,
            Details = details,
            RequestedBy = CurrentUser,
            RequestedAt = DateTime.UtcNow
        };
        
        // 等待审批结果
        return await approver.ReviewAsync(approvalRequest);
    }
}

// 在Agent中使用
var agentWithApproval = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Tools = [deleteFunction]
});

// 当Agent尝试调用删除函数时，会触发审批流程
```

### 6.5 敏感工具控制 - 高风险操作审批流程
```csharp
// 分级审批控制
public enum ApprovalLevel
{
    Automatic,  // 自动批准
    User,       // 用户确认
    Manager,    // 经理审批
    Admin       // 管理员审批
}

public class SensitiveToolController
{
    private readonly Dictionary<string, ApprovalLevel> _toolApprovalLevels = new()
    {
        ["查询天气"] = ApprovalLevel.Automatic,
        ["修改用户信息"] = ApprovalLevel.User,
        ["删除数据"] = ApprovalLevel.Manager,
        ["系统配置"] = ApprovalLevel.Admin
    };
    
    public async Task<bool> CheckApprovalAsync(string toolName, object[] parameters)
    {
        var requiredLevel = _toolApprovalLevels.GetValueOrDefault(toolName, ApprovalLevel.Manager);
        var currentUserLevel = await GetCurrentUserApprovalLevelAsync();
        
        if (currentUserLevel >= requiredLevel)
            return true;
            
        // 触发审批流程
        return await RequestApprovalAsync(toolName, parameters, requiredLevel);
    }
}

// 安全工具包装器
public class SecureAIFunction : AIFunction
{
    private readonly AIFunction _innerFunction;
    private readonly SensitiveToolController _controller;
    
    public SecureAIFunction(AIFunction innerFunction, SensitiveToolController controller)
    {
        _innerFunction = innerFunction;
        _controller = controller;
    }
    
    public override async Task<object?> InvokeAsync(object?[] parameters)
    {
        if (!await _controller.CheckApprovalAsync(Name, parameters))
        {
            throw new UnauthorizedAccessException($"操作 {Name} 未获得批准");
        }
        
        return await _innerFunction.InvokeAsync(parameters);
    }
}
```

### 6.6 MCP服务器暴露 - Agent作为MCP服务提供
```csharp
// 将Agent工具暴露为MCP服务
public class AgentMcpServer
{
    private readonly IAgent _agent;
    private readonly McpServer _mcpServer;
    
    public AgentMcpServer(IAgent agent, int port = 8080)
    {
        _agent = agent;
        _mcpServer = new McpServerBuilder()
            .WithTools(ExportAgentTools())
            .WithPort(port)
            .Build();
    }
    
    private IEnumerable<McpTool> ExportAgentTools()
    {
        // 将Agent的所有工具转换为MCP工具
        foreach (var tool in _agent.GetAvailableTools())
        {
            yield return new McpTool
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = tool.Parameters.Select(p => new McpParameter
                {
                    Name = p.Name,
                    Type = MapToMcpType(p.Type),
                    Description = p.Description
                }).ToList(),
                Execute = async (parameters) => await tool.InvokeAsync(parameters)
            };
        }
    }
    
    public async Task StartAsync()
    {
        await _mcpServer.StartAsync();
        Console.WriteLine($"MCP服务已启动，端口: {_mcpServer.Port}");
    }
}

// 使用示例
var agent = CreateSmartAgent();
var mcpServer = new AgentMcpServer(agent, port: 3000);
await mcpServer.StartAsync();

// 现在其他MCP客户端可以连接并使用这个Agent的工具
```

### 6.7 工具发现机制 - 运行时动态加载
```csharp
// 动态工具加载器
public class DynamicToolLoader
{
    private readonly IServiceProvider _serviceProvider;
    
    public DynamicToolLoader(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IEnumerable<AIFunction> LoadToolsFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            // 查找标记为工具的类型
            if (type.GetCustomAttribute<ToolAttribute>() != null)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.GetCustomAttribute<ToolFunctionAttribute>() != null)
                    {
                        yield return CreateToolFromMethod(type, method);
                    }
                }
            }
        }
    }
    
    private AIFunction CreateToolFromMethod(Type type, MethodInfo method)
    {
        if (method.IsStatic)
        {
            return AIFunctionFactory.Create(method);
        }
        else
        {
            var instance = _serviceProvider.GetService(type) ?? 
                          ActivatorUtilities.CreateInstance(_serviceProvider, type);
            return AIFunctionFactory.Create(instance, method);
        }
    }
}

// 运行时工具热加载
public class HotSwapToolManager
{
    private readonly List<AIFunction> _loadedTools = new();
    private readonly FileSystemWatcher _watcher;
    
    public HotSwapToolManager(string pluginsDirectory)
    {
        _watcher = new FileSystemWatcher(pluginsDirectory, "*.dll");
        _watcher.Created += OnPluginAdded;
        _watcher.Changed += OnPluginChanged;
        _watcher.EnableRaisingEvents = true;
        
        // 加载现有插件
        LoadExistingPlugins(pluginsDirectory);
    }
    
    private void OnPluginAdded(object sender, FileSystemEventArgs e)
    {
        LoadPluginAssembly(e.FullPath);
    }
    
    private void LoadPluginAssembly(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var tools = _toolLoader.LoadToolsFromAssembly(assembly);
            _loadedTools.AddRange(tools);
            
            // 更新Agent工具列表
            UpdateAgentTools();
        }
        catch (Exception ex)
        {
            Logger.LogError($"加载插件失败: {ex.Message}");
        }
    }
    
    private void UpdateAgentTools()
    {
        // 动态更新Agent的工具配置
        foreach (var agent in _registeredAgents)
        {
            agent.UpdateTools(_loadedTools);
        }
    }
}

// 使用特性标记工具
[AttributeUsage(AttributeTargets.Class)]
public class ToolAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class ToolFunctionAttribute : Attribute { }

[Tool]
public class FinanceTools
{
    [ToolFunction]
    [Description("计算复利")]
    public static decimal CalculateCompoundInterest(
        [Description("本金")] decimal principal,
        [Description("年利率")] decimal rate,
        [Description("年数")] int years)
    {
        return principal * (decimal)Math.Pow(1 + (double)rate, years);
    }
}
```

### 6.8 完整工具调用示例
```csharp
// 综合示例：智能财务助手
public class FinancialAssistant
{
    private readonly IAgent _agent;
    
    public FinancialAssistant()
    {
        var tools = new[]
        {
            // 数学计算工具
            AIFunctionFactory.Create(CalculateCompoundInterest),
            AIFunctionFactory.Create(CalculateMonthlyPayment),
            
            // 数据查询工具
            AIFunctionFactory.Create(GetStockPrice),
            AIFunctionFactory.Create(GetExchangeRate),
            
            // 高风险工具（需要审批）
            new ApprovalRequiredAIFunction(
                AIFunctionFactory.Create(TransferFunds),
                "⚠️ 资金转账操作需要审批"
            )
        };
        
        _agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Name = "财务助手",
            Instructions = "你是一个专业的财务顾问，可以帮助用户进行各种财务计算和查询",
            Tools = tools
        });
    }
    
    [Description("计算复利")]
    public static decimal CalculateCompoundInterest(decimal principal, decimal rate, int years)
    {
        return principal * (decimal)Math.Pow(1 + (double)rate, years);
    }
    
    [Description("计算贷款月供")]
    public static decimal CalculateMonthlyPayment(decimal loanAmount, decimal annualRate, int months)
    {
        var monthlyRate = (double)annualRate / 12 / 100;
        return loanAmount * (decimal)(monthlyRate * Math.Pow(1 + monthlyRate, months) / 
                                    (Math.Pow(1 + monthlyRate, months) - 1));
    }
    
    [Description("获取股票价格")]
    public static async Task<decimal> GetStockPrice(string symbol)
    {
        // 调用股票API
```csharp
        return await StockApi.GetPriceAsync(symbol);
    }
    
    [Description("转账操作")]
    public static bool TransferFunds(string fromAccount, string toAccount, decimal amount)
    {
        // 实际转账逻辑
        return BankService.Transfer(fromAccount, toAccount, amount);
    }
    
    public async Task<string> HandleQueryAsync(string query)
    {
        return await _agent.RunAsync(query);
    }
}
```

```csharp
// 使用示例
var assistant = new FinancialAssistant();
var result = await assistant.HandleQueryAsync("帮我计算10万元，年化5%，5年后的复利是多少？");
// Agent会自动调用CalculateCompoundInterest(100000, 0.05m, 5)


var stockResult = await assistant.HandleQueryAsync("查询AAPL的当前股价");
// Agent会自动调用GetStockPrice("AAPL")

var transferResult = await assistant.HandleQueryAsync("从我的账户转账1000元到张三账户");
// 会触发审批流程，等待用户确认
```

### 6.9 工具调用监控和日志
```csharp
// 工具调用监控中间件
public class ToolMonitoringMiddleware : IToolCallingMiddleware
{
    public async Task<ToolResponse> InvokeAsync(
        ToolCallingContext context, 
        NextToolCallingMiddleware next)
    {
        var startTime = DateTime.UtcNow;
        var toolName = context.ToolCall.Name;
        var parameters = context.ToolCall.Arguments;
        
        try
        {
            Logger.LogInformation($"开始调用工具: {toolName}, 参数: {JsonSerializer.Serialize(parameters)}");
            
            var result = await next(context);
            
            var duration = DateTime.UtcNow - startTime;
            Logger.LogInformation($"工具调用完成: {toolName}, 耗时: {duration.TotalMilliseconds}ms");
            
            // 记录指标
            Metrics.RecordToolCall(toolName, duration, success: true);
            
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.LogError($"工具调用失败: {toolName}, 错误: {ex.Message}");
            Metrics.RecordToolCall(toolName, duration, success: false);
            throw;
        }
    }
}
```

```csharp
// 注册监控中间件
var agent = chatClient.CreateAIAgent(options)
    .UseToolCallingMiddleware<ToolMonitoringMiddleware>();
```

这些工具调用与扩展机制提供了强大的函数集成能力，使得Agent可以安全、可控地访问外部系统和执行复杂操作，同时保持了良好的可扩展性和可维护性。





## 七、中间件与拦截器


核心特性：  
横切关注点分离：将日志、缓存、安全等通用功能与业务逻辑分离  
非侵入式编程：不修改原有代码即可添加新功能  
可组合性：多个中间件可以组合使用

```csharp
// AOP中间件链示例
var agent = chatClient.CreateAIAgent(options)
    .UseMiddleware<AuthenticationMiddleware>()    // 认证
    .UseMiddleware<LoggingMiddleware>()          // 日志
    .UseMiddleware<CachingMiddleware>()          // 缓存
    .UseMiddleware<RateLimitingMiddleware>();    // 限流
```



### 7.1 Run Middleware - 对话执行拦截
作用：在Agent执行对话前后插入自定义逻辑，实现AOP编程

```csharp
// 自定义运行中间件
public class LoggingMiddleware : IRunMiddleware
{
    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        // 调用前逻辑
        Console.WriteLine($"开始处理请求: {context.Request.Messages.Last().Text}");
        var startTime = DateTime.Now;
        
        // 调用下一个中间件
        var response = await next(context);
        
        // 调用后逻辑  
        var duration = DateTime.Now - startTime;
        Console.WriteLine($"请求处理完成，耗时: {duration.TotalMilliseconds}ms");
        
        return response;
    }
}
```

```csharp
// 注册中间件
var agent = chatClient.CreateAIAgent(options)
    .UseMiddleware<LoggingMiddleware>();
```

### 7.2 Function Calling Middleware - 工具调用拦截
作用：在函数调用前后添加控制逻辑，如权限验证、参数校验等

```csharp
// 工具调用监控中间件
public class ToolMonitoringMiddleware : IToolCallingMiddleware
{
    public async Task<ToolResponse> InvokeAsync(
        ToolCallingContext context, 
        NextToolCallingMiddleware next)
    {
        var startTime = DateTime.UtcNow;
        var toolName = context.ToolCall.Name;
        var parameters = context.ToolCall.Arguments;
        
        try
        {
            Logger.LogInformation($"开始调用工具: {toolName}, 参数: {JsonSerializer.Serialize(parameters)}");
            
            var result = await next(context);
            
            var duration = DateTime.UtcNow - startTime;
            Logger.LogInformation($"工具调用完成: {toolName}, 耗时: {duration.TotalMilliseconds}ms");
            
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Logger.LogError($"工具调用失败: {toolName}, 错误: {ex.Message}");
            throw;
        }
    }
}
```

```csharp
// 注册工具调用中间件
var agent = chatClient.CreateAIAgent(options)
    .UseToolCallingMiddleware<ToolMonitoringMiddleware>();
```

### 


### 7.3 日志记录中间件 - 执行过程追踪
```csharp
public class ComprehensiveLoggingMiddleware : IRunMiddleware
{
    private readonly ILogger<ComprehensiveLoggingMiddleware> _logger;
    
    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        var requestId = Guid.NewGuid();
        var userMessage = context.Request.Messages.LastOrDefault(m => m.Role == ChatRole.User);
        
        _logger.LogInformation("🔍 [Request-{RequestId}] 开始处理用户请求: {Message}", 
            requestId, userMessage?.Text);
        
        try
        {
            var response = await next(context);
            
            _logger.LogInformation("✅ [Request-{RequestId}] 请求处理成功", requestId);
            _logger.LogDebug("📊 [Request-{RequestId}] 响应内容: {Response}", 
                requestId, response.Message.Text);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Request-{RequestId}] 请求处理失败", requestId);
            throw;
        }
    }
}
```

### 7.4 缓存中间件 - 响应结果缓存
```csharp
public class CachingMiddleware : IRunMiddleware
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingMiddleware> _logger;
    
    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        // 生成缓存键（基于消息内容和配置）
        var cacheKey = GenerateCacheKey(context.Request);
        
        // 尝试从缓存获取
        var cachedResponse = await _cache.GetStringAsync(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogInformation("🎯 缓存命中: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<ChatResponse>(cachedResponse);
        }
        
        // 执行实际调用
        var response = await next(context);
        
        // 缓存结果（配置缓存策略）
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), cacheOptions);
        _logger.LogInformation("💾 结果已缓存: {CacheKey}", cacheKey);
        
        return response;
    }
    
    private string GenerateCacheKey(ChatRequest request)
    {
        var lastUserMessage = request.Messages.Last(m => m.Role == ChatRole.User);
        return $"agent_response:{lastUserMessage.Text.GetHashCode():X}";
    }
}
```

### 7.5 限流中间件 - API调用频率控制
```csharp
public class RateLimitingMiddleware : IRunMiddleware
{
    private readonly IRateLimiter _rateLimiter;
    
    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        var userId = context.GetUserId(); // 从上下文中获取用户标识
        
        if (!await _rateLimiter.CheckLimitAsync(userId))
        {
            throw new RateLimitExceededException("API调用频率超限，请稍后重试");
        }
        
        // 记录调用
        await _rateLimiter.RecordRequestAsync(userId);
        
        return await next(context);
    }
}
```

```csharp
// 令牌桶限流器实现
public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly int _maxTokens;
    private readonly TimeSpan _refillInterval;
    
    public async Task<bool> CheckLimitAsync(string userId)
    {
        var bucket = _buckets.GetOrAdd(userId, _ => new TokenBucket(_maxTokens, _refillInterval));
        return await bucket.TryConsumeAsync();
    }
}
```

### 7.6 审计中间件 - 操作记录追踪
```csharp
public class AuditMiddleware : IRunMiddleware
{
    private readonly IAuditService _auditService;
    
    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        var auditRecord = new AuditRecord
        {
            Id = Guid.NewGuid(),
            UserId = context.GetUserId(),
            Action = "Agent_Execution",
            RequestData = JsonSerializer.Serialize(context.Request),
            Timestamp = DateTime.UtcNow,
            IpAddress = context.GetClientIp()
        };
        
        try
        {
            var response = await next(context);
            
            // 记录成功审计
            auditRecord.ResponseData = JsonSerializer.Serialize(response);
            auditRecord.Status = AuditStatus.Success;
            await _auditService.LogAsync(auditRecord);
            
            return response;
        }
        catch (Exception ex)
        {
            // 记录失败审计
            auditRecord.ErrorMessage = ex.Message;
            auditRecord.Status = AuditStatus.Failed;
            await _auditService.LogAsync(auditRecord);
            
            throw;
        }
    }
}
```

```csharp
// 审计记录模型
public class AuditRecord
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public string RequestData { get; set; }
    public string ResponseData { get; set; }
    public AuditStatus Status { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
}
```

### 7.7 费用监控中间件 - Token使用监控与成本控制
<font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">核心设计要点</font>

+ <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">实时Token计数</font><font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">：监控每次调用的输入/输出Token消耗</font>
+ <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">成本计算：基于不同模型定价计算实际费用</font>
+ <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">预算控制</font><font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">：设置用户/应用级别的使用限额</font>
+ <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">预警机制：接近限额时自动预警和限制</font>

<font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);"></font>

#### <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">7.7.1 Token监控中间件实现</font>
```csharp
/// <summary>
/// Token使用监控和费用控制中间件
/// </summary>
public class TokenMonitoringMiddleware : IRunMiddleware
{
    private readonly ITokenUsageStore _tokenStore;
    private readonly ILogger<TokenMonitoringMiddleware> _logger;
    private readonly ICostCalculator _costCalculator;
    private readonly IBudgetManager _budgetManager;

    public TokenMonitoringMiddleware(
        ITokenUsageStore tokenStore,
        ILogger<TokenMonitoringMiddleware> logger,
        ICostCalculator costCalculator,
        IBudgetManager budgetManager)
    {
        _tokenStore = tokenStore;
        _logger = logger;
        _costCalculator = costCalculator;
        _budgetManager = budgetManager;
    }

    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        var userId = context.GetUserId();
        var modelName = context.Request.Model ?? "gpt-4o";
        var requestId = Guid.NewGuid().ToString("N")[..8];

        // 1. 检查预算限制
        var budgetCheck = await _budgetManager.CheckBudgetAsync(userId, modelName);
        if (!budgetCheck.IsWithinBudget)
        {
            _logger.LogWarning("🚫 [Request-{RequestId}] 用户 {UserId} 超出预算限制", requestId, userId);
            throw new BudgetExceededException($"本月预算已用尽: {budgetCheck.UsedAmount:C} / {budgetCheck.BudgetAmount:C}");
        }

        // 2. 记录请求开始
        var tokenUsage = new TokenUsageRecord
        {
            RequestId = requestId,
            UserId = userId,
            Model = modelName,
            StartTime = DateTime.UtcNow,
            InputMessage = context.Request.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text,
            Status = TokenUsageStatus.Running
            };

        await _tokenStore.RecordStartAsync(tokenUsage);

        _logger.LogInformation("📊 [Request-{RequestId}] 开始Token监控 - 用户: {UserId}, 模型: {Model}", 
                               requestId, userId, modelName);

        try
        {
            // 3. 执行实际请求
            var response = await next(context);

            // 4. 计算Token使用量（实际实现需要从响应头或单独API获取）
            var usage = await CalculateTokenUsageAsync(context.Request, response, modelName);

            // 5. 计算费用
            var cost = _costCalculator.CalculateCost(usage, modelName);

            // 6. 更新使用记录
            tokenUsage.CompletionTime = DateTime.UtcNow;
            tokenUsage.PromptTokens = usage.PromptTokens;
            tokenUsage.CompletionTokens = usage.CompletionTokens;
            tokenUsage.TotalTokens = usage.TotalTokens;
            tokenUsage.Cost = cost;
            tokenUsage.Status = TokenUsageStatus.Completed;
            tokenUsage.ResponseMessage = response.Message.Text?[..Math.Min(500, response.Message.Text.Length)]; // 截取部分内容

            await _tokenStore.RecordCompletionAsync(tokenUsage);

            // 7. 检查预算预警
            var budgetStatus = await _budgetManager.GetBudgetStatusAsync(userId, modelName);
            if (budgetStatus.UsagePercentage >= 0.8m) // 80%预警
            {
                _logger.LogWarning("⚠️ [Request-{RequestId}] 用户 {UserId} 预算使用已达 {Percentage}%", 
                    requestId, userId, budgetStatus.UsagePercentage * 100);
            }

            _logger.LogInformation("✅ [Request-{RequestId}] Token使用: 输入{PromptTokens}, 输出{CompletionTokens}, 总计{TotalTokens}, 费用: {Cost:C}", 
                requestId, usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens, cost);

            return response;
        }
        catch (Exception ex)
        {
            // 8. 记录失败情况
            tokenUsage.CompletionTime = DateTime.UtcNow;
            tokenUsage.Status = TokenUsageStatus.Failed;
            tokenUsage.ErrorMessage = ex.Message;
            await _tokenStore.RecordCompletionAsync(tokenUsage);

            _logger.LogError(ex, "❌ [Request-{RequestId}] Token监控记录失败", requestId);
            throw;
        }
    }

    private async Task<TokenUsage> CalculateTokenUsageAsync(ChatRequest request, ChatResponse response, string modelName)
    {
        // 实际实现需要调用Token计数服务或使用本地Tokenizer
        // 这里使用简化版本，生产环境需要更精确的计算
        
        var promptText = string.Join(" ", request.Messages.Select(m => m.Text));
        var completionText = response.Message.Text ?? "";
        
        return new TokenUsage
        {
            PromptTokens = await EstimateTokensAsync(promptText, modelName),
            CompletionTokens = await EstimateTokensAsync(completionText, modelName),
            TotalTokens = 0 // 将在下面计算
        };
    }

    private async Task<int> EstimateTokensAsync(string text, string modelName)
    {
        // 简化版Token估算（实际应使用相应模型的Tokenizer）
        // 英文大致规则：1个Token ≈ 4个字符或0.75个单词
        if (string.IsNullOrEmpty(text)) return 0;
        
        // 中文Token估算（更复杂，需要分词）
        if (ContainsChinese(text))
        {
            return text.Length; // 中文大致1个字1个Token
        }
        
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private bool ContainsChinese(string text)
    {
        return text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
    }
}
```

#### <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">7.7.2 费用计算器实现</font>
```csharp
/// <summary>
/// 基于模型定价的费用计算器
/// </summary>
public class ModelCostCalculator : ICostCalculator
{
    private readonly Dictionary<string, ModelPricing> _pricing = new()
    {
        // Azure OpenAI 定价（示例，请参考实际定价）
        ["gpt-4o"] = new ModelPricing { InputPer1K = 0.01m, OutputPer1K = 0.03m },
        ["gpt-4o-mini"] = new ModelPricing { InputPer1K = 0.0025m, OutputPer1K = 0.01m },
        ["gpt-35-turbo"] = new ModelPricing { InputPer1K = 0.0015m, OutputPer1K = 0.002m }
    };

    public decimal CalculateCost(TokenUsage usage, string modelName)
    {
        if (!_pricing.TryGetValue(modelName, out var pricing))
        {
            throw new ArgumentException($"未知的模型定价: {modelName}");
        }

        var inputCost = (usage.PromptTokens / 1000m) * pricing.InputPer1K;
        var outputCost = (usage.CompletionTokens / 1000m) * pricing.OutputPer1K;

        return Math.Round(inputCost + outputCost, 4);
    }
}

public record ModelPricing
{
    public decimal InputPer1K { get; init; }  // 每1000个输入Token价格
    public decimal OutputPer1K { get; init; } // 每1000个输出Token价格
}
```

#### <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">7.7.3 </font><font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">预算管理器实现</font>
```csharp
/// <summary>
/// 用户预算管理和控制
/// </summary>
public class BudgetManager : IBudgetManager
{
    private readonly IBudgetStore _budgetStore;
    private readonly ILogger<BudgetManager> _logger;

    public BudgetManager(IBudgetStore budgetStore, ILogger<BudgetManager> logger)
    {
        _budgetStore = budgetStore;
        _logger = logger;
    }

    public async Task<BudgetCheckResult> CheckBudgetAsync(string userId, string modelName)
    {
        var budget = await _budgetStore.GetUserBudgetAsync(userId, modelName) 
            ?? CreateDefaultBudget(userId, modelName);

        var currentUsage = await _budgetStore.GetCurrentUsageAsync(userId, modelName);

        return new BudgetCheckResult
        {
            IsWithinBudget = currentUsage < budget.MonthlyLimit,
            UsedAmount = currentUsage,
            BudgetAmount = budget.MonthlyLimit,
            UsagePercentage = currentUsage / budget.MonthlyLimit
            };
    }

    public async Task<BudgetStatus> GetBudgetStatusAsync(string userId, string modelName)
    {
        var budget = await _budgetStore.GetUserBudgetAsync(userId, modelName) 
            ?? CreateDefaultBudget(userId, modelName);

        var usage = await _budgetStore.GetCurrentUsageAsync(userId, modelName);

        return new BudgetStatus
        {
            UserId = userId,
            Model = modelName,
            MonthlyLimit = budget.MonthlyLimit,
            CurrentUsage = usage,
            UsagePercentage = usage / budget.MonthlyLimit,
            Remaining = budget.MonthlyLimit - usage,
            ResetDate = GetNextResetDate()
            };
    }

    public async Task RecordUsageAsync(string userId, string modelName, decimal amount)
    {
        await _budgetStore.RecordUsageAsync(userId, modelName, amount);

        var status = await GetBudgetStatusAsync(userId, modelName);
        if (status.UsagePercentage >= 0.9m)
        {
            await TriggerBudgetAlertAsync(userId, modelName, status);
        }
    }

    private async Task TriggerBudgetAlertAsync(string userId, string modelName, BudgetStatus status)
    {
        _logger.LogWarning("🔔 用户 {UserId} {Model} 预算使用已达 {Percentage:P0}", 
                           userId, modelName, status.UsagePercentage);

        // 可以集成邮件、短信等通知系统
        // await _notificationService.SendBudgetAlertAsync(userId, status);
    }

    private UserBudget CreateDefaultBudget(string userId, string modelName)
    {
        return new UserBudget
        {
            UserId = userId,
            Model = modelName,
            MonthlyLimit = 100m, // 默认100元/月
            CreatedAt = DateTime.UtcNow
            };
    }

    private DateTime GetNextResetDate()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1).AddMonths(1);
    }
}
```

#### <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">7.7.4 数据模型定义</font>
```csharp
// Token使用记录
public class TokenUsageRecord
{
    public string RequestId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Model { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal Cost { get; set; }
    public string? InputMessage { get; set; }
    public string? ResponseMessage { get; set; }
    public TokenUsageStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TokenUsageStatus
{
    Running,
    Completed,
    Failed
    }

// Token使用量
public record TokenUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

// 预算相关模型
public class UserBudget
{
    public string UserId { get; set; } = null!;
    public string Model { get; set; } = null!;
    public decimal MonthlyLimit { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record BudgetCheckResult
{
    public bool IsWithinBudget { get; init; }
    public decimal UsedAmount { get; init; }
    public decimal BudgetAmount { get; init; }
    public decimal UsagePercentage { get; init; }
}

public record BudgetStatus
{
    public string UserId { get; init; } = null!;
    public string Model { get; init; } = null!;
    public decimal MonthlyLimit { get; init; }
    public decimal CurrentUsage { get; init; }
    public decimal UsagePercentage { get; init; }
    public decimal Remaining { get; init; }
    public DateTime ResetDate { get; init; }
}
```

#### <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">7.7.5 中间件注册和使用</font>
```csharp
// 依赖注入注册
builder.Services.AddSingleton<ITokenUsageStore, SqlTokenUsageStore>();
builder.Services.AddSingleton<ICostCalculator, ModelCostCalculator>();
builder.Services.AddSingleton<IBudgetManager, BudgetManager>();
builder.Services.AddSingleton<IBudgetStore, SqlBudgetStore>();

// Agent配置中使用Token监控中间件
var agent = chatClient.CreateAIAgent(options)
    .UseMiddleware<TokenMonitoringMiddleware>()  // Token监控
    .UseMiddleware<LoggingMiddleware>()          // 日志
    .UseMiddleware<CachingMiddleware>()          // 缓存
    .UseMiddleware<RateLimitingMiddleware>();    // 限流

// 或者使用MEAI的扩展方法
builder.Services.AddChatClient(sp => /* ... */)
    .UseTokenMonitoring()  // Token监控
    .UseLogging()          // 日志
    .UseDistributedCache() // 缓存
    .UseFunctionInvocation(); // 函数调用
```

#### <font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">7.7.6 监控仪表板集成 - (待定)</font>
```csharp
// 费用监控API端点（可集成到DevUI）
[ApiController]
[Route("api/monitoring")]
public class TokenMonitoringController : ControllerBase
{
    private readonly ITokenUsageStore _tokenStore;
    private readonly IBudgetManager _budgetManager;

    [HttpGet("usage/{userId}")]
    public async Task<IActionResult> GetUserUsage(string userId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var usage = await _tokenStore.GetUserUsageAsync(userId, startDate, endDate);
        return Ok(usage);
    }

    [HttpGet("budget/{userId}")]
    public async Task<IActionResult> GetUserBudgetStatus(string userId)
    {
        var status = await _budgetManager.GetBudgetStatusAsync(userId, "all");
        return Ok(status);
    }

    [HttpGet("cost-trend")]
    public async Task<IActionResult> GetCostTrend([FromQuery] string model, [FromQuery] int days = 30)
    {
        var trend = await _tokenStore.GetCostTrendAsync(model, days);
        return Ok(trend);
    }
}
```

核心特性总结

1. <font style="color:rgb(31, 35, 40);">实时监控</font><font style="color:rgb(31, 35, 40);">：每次调用都记录详细的Token使用和费用</font>
2. <font style="color:rgb(31, 35, 40);">预算控制</font><font style="color:rgb(31, 35, 40);">：支持用户级别的月度预算限制</font>
3. <font style="color:rgb(31, 35, 40);">预警机制</font><font style="color:rgb(31, 35, 40);">：接近限额时自动预警</font>
4. <font style="color:rgb(31, 35, 40);">多模型支持</font><font style="color:rgb(31, 35, 40);">：不同模型使用不同的定价策略</font>
5. <font style="color:rgb(31, 35, 40);">数据持久化</font><font style="color:rgb(31, 35, 40);">：所有使用记录都保存到数据库供分析</font>
6. <font style="color:rgb(31, 35, 40);">集成友好：可轻松集成到现有的中间件管道中</font>



### <font style="background-color:rgba(255, 255, 255, 0.9);">7.9. NETCore.Encrypt加密库集成 - （待定）</font>
```csharp
// AI应用安全加密集成
public class AISecurityService
{
    private readonly IEncryptProvider _encryptor;

    // 对称加密在AI应用中的安全集成
    public async Task<EncryptedMessage> EncryptChatMessageAsync(ChatMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var encrypted = _encryptor.Encrypt(json);
        return new EncryptedMessage
        {
            Data = encrypted,
            Algorithm = "AES-256-GCM",
            KeyId = _currentKeyId
            };
    }

    // 数据传输和存储加密方案
    public async Task<string> EncryptForStorageAsync(object data, string storageKey)
    {
        var serialized = JsonSerializer.Serialize(data);
        return _encryptor.Encrypt(serialized, storageKey);
    }

    // 身份验证加密支持
    public async Task<AuthToken> GenerateSecureTokenAsync(UserIdentity user)
    {
        var tokenData = new
        {
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddHours(24),
            Permissions = user.Permissions
            };

        var encryptedToken = _encryptor.Encrypt(JsonSerializer.Serialize(tokenData));
        return new AuthToken { Value = encryptedToken };
    }
}

// 加密配置
public class EncryptionSettings
{
    public string DefaultKey { get; set; }
    public string KeyRotationSchedule { get; set; } = "0 0 1 * *"; // 每月1号轮换
    public List<string> AllowedAlgorithms { get; set; } = new() { "AES-256-GCM", "RSA-OAEP" };
}
```

### 7.9 自定义中间件工厂
```csharp
// 条件中间件：根据配置动态启用/禁用
public class ConditionalMiddleware : IRunMiddleware
{
    private readonly IRunMiddleware _innerMiddleware;
    private readonly bool _isEnabled;
    
    public ConditionalMiddleware(IRunMiddleware innerMiddleware, IConfiguration config)
    {
        _innerMiddleware = innerMiddleware;
        _isEnabled = config.GetValue<bool>("Middleware:EnableConditional");
    }
    
    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        if (_isEnabled)
        {
            return await _innerMiddleware.InvokeAsync(context, next);
        }
        else
        {
            return await next(context);
        }
    }
}
```

中间件执行顺序说明  
在MAF框架中，中间件按照注册顺序执行，形成"洋葱模型"：  
请求阶段：从上到下执行中间件的前置逻辑  
核心处理：执行Agent的实际对话处理  
响应阶段：从下到上执行中间件的后置逻辑  
请求 → 中间件A前置 → 中间件B前置 → Agent处理 → 中间件B后置 → 中间件A后置 → 响应  
这种设计使得中间件可以灵活地处理各种横切关注点，同时保持代码的整洁和可维护性。

<font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);"></font>

<font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);"></font>

### <font style="background-color:rgba(255, 255, 255, 0.9);">7.10 AIContextProvider</font>
根据文档《Microsoft Agent Framework - AIContextProvider 上下文管理.md》和《从"死记硬背"到"主动思考"：用 Microsoft Agent Framework 重新定义 RAG.md》，`<font style="color:rgba(0, 0, 0, 0.86);background-color:rgba(255, 255, 255, 0.9);">AIContextProvider</font>` 是Microsoft Agent Framework中用于实现有状态、个性化对话的核心机制。它的完整生命周期主要包含两个关键钩子方法，用于在Agent执行对话的前后注入和管理上下文信息：

1. 调用前钩子 (`**<font style="background-color:rgba(255, 255, 255, 0.9);">InvokingAsync</font>**`)：
    - 作用：在Agent处理用户请求之前被调用。
    - 功能：在此阶段，您可以动态地为本次对话调用注入额外的上下文信息。例如，从一个外部知识库或数据库中检索与当前对话相关的信息，并将这些信息作为上下文提供给Agent，使其回答更准确、更具个性化。
2. 调用后钩子 (`**<font style="background-color:rgba(255, 255, 255, 0.9);">InvokedAsync</font>**`)：
    - 作用：在Agent生成响应之后被调用。
    - 功能：在此阶段，您可以对Agent的响应结果进行后处理。例如，从对话中提取关键信息并保存到数据库（实现记忆功能），或者进行日志记录、审计等操作。

`<font style="background-color:rgba(255, 255, 255, 0.9);">AIContextProvider</font>` 的生命周期管理允许开发者将自定义逻辑（如检索增强生成RAG、记忆持久化、可观测性埋点）切入到Agent的运行过程中，是实现智能体“主动思考”和状态管理的关键组件。它优雅地将业务逻辑与Agent的核心对话能力分离开来

```csharp
// 1. 实现 AIContextProvider 抽象类
public class BlogAIContextProvider : AIContextProvider
{
    private readonly IBlogStore _blogStore;

    // 通过构造函数注入依赖（如数据库访问层）
    public BlogAIContextProvider(IBlogStore blogStore)
    {
        _blogStore = blogStore;
    }

    // 2. 实现调用前钩子 - 在Agent处理请求前注入上下文
    protected override async Task<AIContext?> InvokingAsync(
        AIContextProviderContext context, 
        CancellationToken cancellationToken = default)
    {
        // 示例：从对话历史中提取博客ID
        var blogId = ExtractBlogIdFromContext(context);
        
        if (!string.IsNullOrEmpty(blogId))
        {
            // 从数据库获取该博客的完整信息
            var blog = await _blogStore.GetBlogAsync(blogId, cancellationToken);
            
            if (blog != null)
            {
                // 将博客内容作为额外上下文注入本次对话
                return new AIContext
                {
                    Role = "user",
                    Content = $"这是您要修改的博客内容：\n{blog.Content}\n\n请根据用户请求进行修改。"
                };
            }
        }
        
        return null; // 不注入额外上下文
    }

    // 3. 实现调用后钩子 - 在Agent生成响应后保存状态
    protected override async Task<AIContext?> InvokedAsync(
        AIContextProviderContext context, 
        CancellationToken cancellationToken = default)
    {
        // 示例：从Agent的响应中提取关键信息并保存
        var blogUpdate = ExtractBlogUpdateFromResponse(context);
        
        if (blogUpdate != null)
        {
            // 将更新保存到数据库，实现对话状态的持久化
            await _blogStore.SaveBlogUpdateAsync(blogUpdate, cancellationToken);
            
            // 可以返回一个确认信息作为上下文
            return new AIContext
            {
                Role = "system", 
                Content = "已成功保存您的修改。"
            };
        }
        
        return null;
    }

    // 辅助方法：从上下文中提取博客ID
    private string? ExtractBlogIdFromContext(AIContextProviderContext context)
    {
        // 实现具体的提取逻辑
        return null;
    }

    // 辅助方法：从响应中提取博客更新内容
    private BlogUpdate? ExtractBlogUpdateFromResponse(AIContextProviderContext context)
    {
        // 实现具体的提取逻辑
        return null;
    }
}

// 4. 注册到依赖注入容器
builder.Services.AddTransient<AIContextProvider, BlogAIContextProvider>();
```

**<font style="background-color:rgba(255, 255, 255, 0.9);">关键点说明：</font>**

+ `**<font style="background-color:rgba(255, 255, 255, 0.9);">InvokingAsync</font>**`<font style="background-color:rgba(255, 255, 255, 0.9);">：在Agent思考前运行，用于</font>**<font style="background-color:rgba(255, 255, 255, 0.9);">检索和注入上下文</font>**<font style="background-color:rgba(255, 255, 255, 0.9);">（如从数据库获取博客内容）。</font>
+ `**<font style="background-color:rgba(255, 255, 255, 0.9);">InvokedAsync</font>**`<font style="background-color:rgba(255, 255, 255, 0.9);">：在Agent回答后运行，用于</font>**<font style="background-color:rgba(255, 255, 255, 0.9);">保存状态和记忆</font>**<font style="background-color:rgba(255, 255, 255, 0.9);">（如将修改内容存回数据库）。</font>
+ <font style="background-color:rgba(255, 255, 255, 0.9);">这两个方法共同构成了 </font>`<font style="background-color:rgba(255, 255, 255, 0.9);">AIContextProvider</font>`<font style="background-color:rgba(255, 255, 255, 0.9);"> 的完整生命周期，使Agent能够实现有状态的、基于上下文的对话。</font>

### <font style="background-color:rgba(255, 255, 255, 0.9);">7.11 MCP Gateway</font>
<font style="background-color:rgba(255, 255, 255, 0.9);">MCP Gateway 的架构远比一个简单的中间件复杂。根据文档《MCP Gateway 综述与实战指南.md》，MCP Gateway 是一个功能完整的反向代理和管理层，其架构兼具</font>**<font style="background-color:rgba(255, 255, 255, 0.9);">数据平面</font>**<font style="background-color:rgba(255, 255, 255, 0.9);">和</font>**<font style="background-color:rgba(255, 255, 255, 0.9);">控制平面</font>**<font style="background-color:rgba(255, 255, 255, 0.9);">功能</font>

+ **数据平面功能**<font style="background-color:rgba(255, 255, 255, 0.9);">：负责将客户端流量（如SSE、HTTP消息）通过</font>**会话感知路由**<font style="background-color:rgba(255, 255, 255, 0.9);">转发到正确的MCP服务器实例。这确保了同一会话的请求始终由同一后端实例处理，保持对话上下文。</font>
+ **控制平面功能**<font style="background-color:rgba(255, 255, 255, 0.9);">：提供了一套完整的RESTful API（如 </font>`<font style="background-color:rgba(255, 255, 255, 0.9);">POST /adapters</font>`<font style="background-color:rgba(255, 255, 255, 0.9);">, </font>`<font style="background-color:rgba(255, 255, 255, 0.9);">GET /adapters/{name}/status</font>`<font style="background-color:rgba(255, 255, 255, 0.9);">），用于管理MCP服务器的</font>**全生命周期**<font style="background-color:rgba(255, 255, 255, 0.9);">，包括部署、更新、状态检查、日志查看和删除。</font>

<font style="background-color:rgba(255, 255, 255, 0.9);">它被设计为在Kubernetes环境中运行，基于StatefulSet和Headless Service实现高可用和弹性伸缩，并集成了企业级特性如Bearer Token认证、RBAC/ACL授权和可观测性（日志、指标、追踪）。</font>

<font style="background-color:rgba(255, 255, 255, 0.9);">因此，MCP Gateway是一个独立的、复杂的系统级组件，而非一个可以简单嵌入到应用中的中间件。</font>

<font style="background-color:rgba(255, 255, 255, 0.9);"></font>

文档中关于 MCP Gateway 的代码实例主要体现在其 **控制平面API** 和 **数据平面路由** 的定义上。这些定义清晰地展示了它远不止一个中间件。

**控制平面 API 示例（用于管理 MCP 服务器生命周期）：**

<font style="background-color:rgba(255, 255, 255, 0.9);"> </font>文档在“四、控制平面 API（示例）”部分提供了具体的 RESTful 接口定义：

```http
# 部署并注册一个新的 MCP 服务器实例
POST /adapters

# 获取所有已注册的 MCP 适配器（实例）列表
GET /adapters

# 获取某个特定适配器的元数据信息
GET /adapters/{name}

# 查询某个 MCP 服务器实例的部署和运行状态
GET /adapters/{name}/status

# 查看某个 MCP 服务器实例的运行日志
GET /adapters/{name}/logs

# 更新某个 MCP 服务器实例的配置
PUT /adapters/{name}

# 删除并清理某个 MCP 服务器实例
DELETE /adapters/{name}
```

**数据平面路由示例（用于转发客户端请求）：**<font style="background-color:rgba(255, 255, 255, 0.9);"> 文档在“五、数据平面路由（示例）”部分展示了客户端如何通过 Gateway 与后端 MCP 服务器交互：</font>

```http
# 通过 Server-Sent Events (SSE) 与 MCP 服务器建立流式连接
GET /adapters/{name}/sse

# 向指定 MCP 服务器实例发送基于会话的消息
POST /adapters/{name}/messages

# 使用流式 HTTP 接口与 MCP 服务器通信
POST /adapters/{name}/mcp
```

**项目结构代码示例：**<font style="background-color:rgba(255, 255, 255, 0.9);"> 文档在“六、项目结构概览”部分展示了其复杂的项目组成，这进一步说明它是一个完整的工程项目：</font>

```plain
mcp-gateway/
 ├─ dotnet/                     # 主网关服务 (.NET 8)
 │   ├─ Microsoft.McpGateway.Service/   # 核心服务
 │   └─ Microsoft.McpGateway.Management/ # 管理模块
 ├─ mcp-example-server/         # 示例 MCP 服务器
 ├─ deployment/
 │   ├─ infra/azure-deployment.bicep    # Azure 部署脚本
 │   └─ k8s/                    # Kubernetes 部署配置
 ├─ openapi/                    # OpenAPI 3.0 规范
 └─ workflows/                  # CI/CD 工作流
```

### 7.10 中间件配置最佳实践
```csharp
// 生产环境中间件配置
services.AddSingleton<IRunMiddleware, AuthenticationMiddleware>();
services.AddSingleton<IRunMiddleware, LoggingMiddleware>();
services.AddSingleton<IRunMiddleware, CachingMiddleware>();
services.AddSingleton<IRunMiddleware, RateLimitingMiddleware>();
services.AddSingleton<IRunMiddleware, AuditMiddleware>();
```

```csharp
// Agent构建时应用中间件
var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "生产环境Agent"
})
.UseMiddleware<AuthenticationMiddleware>()
.UseMiddleware<LoggingMiddleware>()
.UseMiddleware<CachingMiddleware>()
.UseMiddleware<RateLimitingMiddleware>()
.UseMiddleware<AuditMiddleware>();
```

## 


## 八、提示工程优化
### <font style="background-color:rgba(255, 255, 255, 0.9);">1. YPrompt提示词管理系统</font>
```csharp
// YPrompt核心管理器
public class YPromptManager
{
    private readonly IPromptRepository _repository;

    // 对话式意图挖掘自动生成提示词
    public async Task<PromptTemplate> GeneratePromptFromIntentAsync(string userInput)
    {
        var intent = await AnalyzeUserIntentAsync(userInput);
        return await _repository.GetBestMatchTemplateAsync(intent);
    }

    // 系统/用户提示词优化流程
    public async Task<OptimizedPrompt> OptimizePromptAsync(PromptTemplate template, OptimizationCriteria criteria)
    {
        var analyzer = new PromptEffectivenessAnalyzer();
        var analysis = await analyzer.AnalyzeAsync(template);
        return await _optimizer.OptimizeAsync(template, analysis, criteria);
    }

    // 提示词版本管理机制
    public async Task<PromptVersion> CreateVersionAsync(string promptId, string changes)
    {
        var version = new PromptVersion
        {
            PromptId = promptId,
            VersionNumber = await GetNextVersionAsync(promptId),
            Content = changes,
            CreatedAt = DateTime.UtcNow
            };
        return await _repository.SaveVersionAsync(version);
    }
}

// 提示词生成请求
public class PromptGenerationRequest
{
    public string UserQuery { get; set; }
    public string Domain { get; set; }
    public string Style { get; set; } // "technical", "conversational", etc.
    public int ComplexityLevel { get; set; } = 1;
}
```







### 8.1 角色指令定义 - Instructions系统提示
作用：通过系统提示词明确Agent的角色定位和能力边界

```csharp
// 基础角色定义
var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "技术专家",
    Instructions = @"你是一个资深软件工程师，专注于.NET技术和AI应用开发。
    
核心能力：
- 代码编写和调试
- 架构设计建议
- 技术问题解答
- 最佳实践指导

回答要求：
- 专业准确，提供可执行的代码示例
- 结合实际场景给出建议
- 标注技术风险和使用注意事项"
});
```

```csharp
// 多角色协作示例
var codeReviewer = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "代码审查员",
    Instructions = @"你是严格的代码审查专家，专注于代码质量、安全性和性能。

审查重点：
1. 代码规范符合性
2. 潜在安全漏洞
3. 性能优化建议
4. 可维护性问题

输出格式：
- 按严重程度分类问题（严重/警告/建议）
- 提供具体修改建议
- 给出改进后的代码示例"
});
```

### 8.2 思维链提示 - 分步骤推理引导
作用：引导模型进行逻辑推理，提高复杂问题解决能力

```csharp
// 复杂问题分步推理提示
var complexPrompt = @"请按以下步骤分析这个问题：

步骤1：理解问题核心
- 用户真正需要解决什么问题？
- 涉及哪些技术领域？

步骤2：分析约束条件
- 有哪些技术限制？
- 性能要求是什么？
- 安全性考虑有哪些？

步骤3：设计解决方案
- 提出2-3个可行方案
- 比较各方案优缺点

步骤4：给出具体实现
- 提供核心代码框架
- 说明关键实现细节

步骤5：验证和优化
- 如何测试解决方案？
- 可能的优化方向？

现在请分析：如何设计一个高性能的分布式缓存系统？";
```

```csharp
var response = await agent.RunAsync(complexPrompt);
```

### 8.3 输出格式约束 - 明确JSON结构要求
作用：确保模型输出符合预期的结构化格式

```csharp
// 严格JSON格式约束
var jsonFormatPrompt = """ 
请严格按照以下JSON格式返回结果，不要包含任何其他文本：

{
    "analysis": {
        "problem": "问题描述",
        "complexity": "高/中/低",
        "estimated_time": "预估解决时间"
    },
    "solutions": [
        {
            "name": "方案名称",
            "pros": ["优点1", "优点2"],
            "cons": ["缺点1", "缺点2"],
            "recommendation": true/false
        }
    ],
    "code_example": {
        "language": "编程语言",
        "snippet": "代码片段"
    }
}

用户问题：如何处理大数据量的实时分析？
""";
```

```csharp
// 使用结构化输出类型
public class SolutionAnalysis
{
    public AnalysisInfo Analysis { get; set; }
    public List<Solution> Solutions { get; set; }
    public CodeExample CodeExample { get; set; }
}
```

```csharp
var structuredResponse = await agent.RunAsync<SolutionAnalysis>(jsonFormatPrompt);
```

### 8.4 上下文丰富 - 时间、用户信息等上下文注入
作用：为模型提供更丰富的上下文信息，提升回答相关性

```csharp
// 动态上下文构建
public class ContextEnricher
{
    public string BuildContext(string userQuery, UserProfile user, DateTime currentTime)
    {
        return $@"
当前上下文信息：
- 用户身份：{user.Role} ({user.ExperienceLevel}级)
- 当前时间：{currentTime:yyyy-MM-dd HH:mm}
- 最近相关活动：{user.RecentActivities}
- 技术偏好：{string.Join(", ", user.TechPreferences)}

用户问题：{userQuery}

请根据以上上下文提供个性化回答。";
    }
}
```

```csharp
// 使用示例
var context = contextEnricher.BuildContext(
    "如何优化数据库查询性能？", 
    currentUser, 
    DateTime.Now
);
var response = await agent.RunAsync(context);
```

### 8.5 示例驱动 - Few-shot learning示例
作用：通过提供示例引导模型学习期望的回答模式

```csharp
// Few-shot学习提示词
var fewShotPrompt = """ 
请参考以下示例格式回答问题：

示例1：
输入：'如何实现用户认证？'
输出：
{
    "技术方案": "JWT令牌认证",
    "步骤": ["生成密钥", "签发令牌", "验证令牌"],
    "代码语言": "C#",
    "复杂度": "中等"
}

示例2：
输入：'如何处理高并发请求？'
输出：
{
    "技术方案": "Redis缓存 + 负载均衡",
    "步骤": ["配置缓存", "设置负载均衡器", "监控性能"],
    "代码语言": "多种",
    "复杂度": "高"
}

现在请回答：'如何设计微服务架构？'
""";
```

```csharp
var response = await agent.RunAsync(fewShotPrompt);
```

### 8.6 边界明确 - 拒答策略和范围限定
作用：明确Agent的能力边界，避免回答超出范围的问题

```csharp
// 明确的边界定义
var boundedAgent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "技术顾问",
    Instructions = @"你是一个专业的技术顾问，专注于软件开发技术问题。

能力范围：
- 编程语言（C#、Python、JavaScript等）
- 框架和库（.NET、React、TensorFlow等）
- 系统设计和架构
- 代码审查和优化

超出范围的问题（请明确拒绝）：
- 法律、医疗、金融投资建议
- 政治敏感话题
- 个人隐私相关问题
- 超出你知识截止时间的事件

拒绝回答模板：
'抱歉，这个问题超出了我的专业范围。我主要专注于技术开发领域的问题。'

请严格遵守以上边界。"
});
```

```csharp
// 边界检查中间件
public class BoundaryCheckMiddleware : IRunMiddleware
{
    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        var userMessage = context.Request.Messages.Last().Text;
        
        if (IsOutOfBoundary(userMessage))
        {
            return new ChatResponse
            {
                Message = new ChatMessage(ChatRole.Assistant, 
                    "抱歉，这个问题超出了我的专业范围。")
            };
        }
        
        return await next(context);
    }
    
    private bool IsOutOfBoundary(string message)
    {
        var outOfBoundaryKeywords = new[]
        {
            "投资建议", "法律意见", "医疗诊断", "政治观点"
        };
        
        return outOfBoundaryKeywords.Any(keyword => 
            message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
```

### 8.7 提示工程最佳实践总结
#### 1.分层提示设计
```csharp
// 三层提示结构
var layeredPrompt = @"
第一层：角色定义
- 你是什么专家
- 你的核心能力

第二层：任务要求
- 具体要完成什么
- 输出格式规范

第三层：约束条件
- 不能做什么
- 质量要求标准
";
```

#### 2.动态提示调整
```csharp
public class AdaptivePromptEngine
{
    public string AdaptPrompt(string basePrompt, ConversationContext context)
    {
        // 根据对话历史调整提示词
        if (context.ConversationLength > 10)
        {
            return basePrompt + "\n\n注意：这是多轮对话，请保持上下文连贯性。";
        }
        
        if (context.UserExpertise == "初级")
        {
            return basePrompt + "\n\n请用简单易懂的语言解释，避免技术术语。";
        }
        
        return basePrompt;
    }
}
```

#### 3.提示词版本管理
```csharp
// 提示词配置化管理
public class PromptConfiguration
{
    public string Version { get; set; }
    public Dictionary<string, string> Prompts { get; set; }
}
```

```json
// appsettings.json
{
  "PromptConfig": {
    "Version": "1.2.0",
    "Prompts": {
      "TechnicalAdvisor": "你是一个资深技术顾问...",
      "CodeReviewer": "你是严格的代码审查专家..."
    }
  }
}
```

这些提示工程技术共同构成了高效的AI对话系统基础，通过精心设计的提示词可以显著提升Agent的回答质量和专业性。

### 
## 九、性能优化技术
### 9.1 会话缓存 - 响应提速10-100倍
```csharp
// 智能会话缓存实现
public class IntelligentSessionCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

    public async Task<ChatResponse?> GetCachedResponseAsync(string cacheKey)
    {
        // 1. 检查内存缓存（最快）
        if (_memoryCache.TryGetValue(cacheKey, out ChatResponse? memoryResponse))
        {
            Metrics.RecordCacheHit("memory");
            return memoryResponse;
        }

        // 2. 检查分布式缓存
        var distributedData = await _distributedCache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(distributedData))
        {
            var response = JsonSerializer.Deserialize<ChatResponse>(distributedData);
            
            // 回填内存缓存
            _memoryCache.Set(cacheKey, response, _defaultExpiration);
            Metrics.RecordCacheHit("distributed");
            
            return response;
        }

        Metrics.RecordCacheMiss();
        return null;
    }

    public async Task SetCachedResponseAsync(string cacheKey, ChatResponse response)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _defaultExpiration,
            Size = 1 // 相对大小，用于内存管理
        };

        // 1. 设置内存缓存
        _memoryCache.Set(cacheKey, response, cacheOptions);

        // 2. 设置分布式缓存
        var serializedData = JsonSerializer.Serialize(response);
        await _distributedCache.SetStringAsync(
            cacheKey, 
            serializedData, 
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultExpiration
            });
    }

    // 智能缓存键生成
    public string GenerateCacheKey(ChatRequest request, string? userId = null)
    {
        var lastUserMessage = request.Messages.Last(m => m.Role == ChatRole.User);
        
        var keyComponents = new List<string>
        {
            $"agent:{request.AgentId}",
            $"query:{lastUserMessage.Text.GetHashCode():X8}",
            $"context:{request.Messages.Count}"
        };

        if (!string.IsNullOrEmpty(userId))
        {
            keyComponents.Add($"user:{userId}");
        }

        return string.Join("|", keyComponents);
    }
}
```

```csharp
// 缓存感知的Agent中间件
public class CachingMiddleware : IRunMiddleware
{
    private readonly IntelligentSessionCache _cache;

    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        var cacheKey = _cache.GenerateCacheKey(context.Request, context.GetUserId());
        
        // 尝试从缓存获取
        var cachedResponse = await _cache.GetCachedResponseAsync(cacheKey);
        if (cachedResponse != null)
        {
            context.Logger.LogInformation("缓存命中，跳过模型调用");
            return cachedResponse;
        }

        // 执行实际调用
        var response = await next(context);

        // 缓存结果（仅当成功时）
        if (response.IsSuccessful)
        {
            await _cache.SetCachedResponseAsync(cacheKey, response);
        }

        return response;
    }
}
```

```csharp
// 使用示例
services.AddSingleton<IntelligentSessionCache>();
var agent = chatClient.CreateAIAgent(options)
    .UseMiddleware<CachingMiddleware>();
```

### 9.2 智能工具筛选 - Tool Reduction技术
```csharp
// 基于上下文的工具筛选器
public class ContextAwareToolReducer
{
    public IEnumerable<AIFunction> ReduceTools(
        IEnumerable<AIFunction> availableTools, 
        ChatRequest request)
    {
        var userMessage = request.Messages.Last().Text;
        var relevantTools = new List<AIFunction>();

        foreach (var tool in availableTools)
        {
            if (IsToolRelevant(tool, userMessage, request.Context))
            {
                relevantTools.Add(tool);
            }
        }

        // 如果相关工具太多，进一步筛选
        if (relevantTools.Count > 5)
        {
            return relevantTools.OrderByDescending(t => GetRelevanceScore(t, userMessage))
                               .Take(5);
        }

        return relevantTools;
    }

    private bool IsToolRelevant(AIFunction tool, string userMessage, object? context)
    {
        var keywords = ExtractKeywords(userMessage);
        var toolKeywords = ExtractToolKeywords(tool);

        // 基于关键词匹配
        if (keywords.Any(k => toolKeywords.Contains(k)))
            return true;

        // 基于上下文匹配
        if (context != null && IsContextRelevant(tool, context))
            return true;

        // 基于工具使用频率（优先常用工具）
        return GetToolUsageFrequency(tool.Name) > 0.1;
    }

    // 动态工具加载策略
    public class DynamicToolLoader
    {
        private readonly Dictionary<string, AIFunction> _toolRegistry = new();
        private readonly ToolUsageTracker _usageTracker;

        public void RegisterTool(string category, AIFunction tool)
        {
            _toolRegistry[category] = tool;
        }

        public IEnumerable<AIFunction> LoadToolsForScenario(string scenario)
        {
            return scenario switch
            {
                "technical_support" => LoadTechnicalTools(),
                "content_creation" => LoadContentTools(),
                "data_analysis" => LoadDataTools(),
                _ => LoadDefaultTools()
            };
        }

        private IEnumerable<AIFunction> LoadTechnicalTools()
        {
            return _toolRegistry.Where(kv => kv.Key == "code" || kv.Key == "debug")
                               .Select(kv => kv.Value)
                               .Concat(LoadHighFrequencyTools());
        }
    }
}
```

```csharp
// 集成到Agent配置
var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Tools = toolReducer.LoadToolsForScenario("technical_support"),
    ToolReductionStrategy = new ContextAwareToolReducer()
});
```

### 9.3 Token优化 - 减少不必要的token消耗
```csharp
// Token优化管理器
public class TokenOptimizationManager
{
    private readonly IChatReducer _chatReducer;
    private readonly ITokenEstimator _tokenEstimator;

    public async Task<ChatRequest> OptimizeRequestAsync(ChatRequest request)
    {
        var optimizedMessages = await _chatReducer.ReduceAsync(request.Messages);
        var tokenCount = _tokenEstimator.Estimate(optimizedMessages);

        // 如果仍然超过限制，应用激进压缩
        if (tokenCount > GetTokenLimit())
        {
            optimizedMessages = await ApplyAggressiveCompression(optimizedMessages);
        }

        return request with { Messages = optimizedMessages.ToList() };
    }

    // 消息精简策略
    public class MessageMinimizer
    {
        public ChatMessage MinimizeMessage(ChatMessage message)
        {
            return message with 
            { 
                Text = message.Role switch
                {
                    ChatRole.System => CompressSystemPrompt(message.Text),
                    ChatRole.User => RemoveRedundantText(message.Text),
                    ChatRole.Assistant => KeepEssentialResponse(message.Text),
                    _ => message.Text
                }
            };
        }

        private string CompressSystemPrompt(string prompt)
        {
            // 移除注释和空行
            var lines = prompt.Split('\n')
                             .Where(line => !line.TrimStart().StartsWith("//"))
                             .Where(line => !string.IsNullOrWhiteSpace(line))
                             .Select(line => line.Trim());
            
            return string.Join(" ", lines);
        }

        private string RemoveRedundantText(string text)
        {
            // 移除问候语、冗余描述等
            var patterns = new[]
            {
                @"^(你好|您好|嗨)\s*[,，]?\s*",
                @"谢谢$|请问$|能不能$",
                @"\s+"
            };

            var result = text;
            foreach (var pattern in patterns)
            {
                result = Regex.Replace(result, pattern, " ");
            }

            return result.Trim();
        }
    }

    // Token使用监控
    public class TokenUsageMonitor
    {
        public void LogTokenUsage(ChatRequest request, ChatResponse response)
        {
            var inputTokens = _tokenEstimator.Estimate(request.Messages);
            var outputTokens = _tokenEstimator.Estimate(response.Message.Text);
            var totalTokens = inputTokens + outputTokens;

            Logger.LogInformation(
                "Token使用: 输入={InputTokens}, 输出={OutputTokens}, 总计={TotalTokens}, 成本=${Cost:F4}",
                inputTokens, outputTokens, totalTokens, CalculateCost(totalTokens));

            // 触发警告阈值
            if (totalTokens > GetWarningThreshold())
            {
                Logger.LogWarning("Token使用超过警告阈值");
            }
        }

        private decimal CalculateCost(int tokens)
        {
            // 根据模型定价计算成本
            return tokens * 0.000002m; // GPT-4定价示例
        }
    }
}
```

```csharp
// 集成到中间件管道
public class TokenOptimizationMiddleware : IRunMiddleware
{
    private readonly TokenOptimizationManager _tokenManager;

    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        // 优化请求
        var optimizedRequest = await _tokenManager.OptimizeRequestAsync(context.Request);
        var optimizedContext = context with { Request = optimizedRequest };

        var response = await next(optimizedContext);

        // 记录Token使用
        _tokenManager.Monitor.LogTokenUsage(optimizedRequest, response);

        return response;
    }
}
```

### 9.4 流式响应处理 - 实时显示逐步结果
```csharp
// 流式响应处理器
public class StreamingResponseHandler
{
    public async IAsyncEnumerable<string> ProcessStreamingResponseAsync(
        StreamingRun streamingRun)
    {
        var completeContent = new StringBuilder();
        
        await foreach (var update in streamingRun.WatchStreamAsync())
        {
            switch (update)
            {
                case ContentUpdateEvent contentUpdate:
                    yield return contentUpdate.Text;
                    completeContent.Append(contentUpdate.Text);
                    break;

                case FunctionCallUpdateEvent functionUpdate:
                    yield return $"\n[调用工具: {functionUpdate.FunctionName}]";
                    break;

                case CompletionEvent completion:
                    yield return $"\n\n[完成: {completion.FinishReason}]";
                    break;
            }
        }

        // 可选：保存完整响应
        await SaveCompleteResponseAsync(completeContent.ToString());
    }
}
```

```csharp
```csharp
// WebSocket流式传输（ASP.NET Core）
[ApiController]
public class StreamingChatController : ControllerBase
{
    [HttpGet("/chat/stream")]
    public async Task StreamChatAsync([FromQuery] string message)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        var streamingRun = await _agent.RunStreamingAsync(message);
        
        await foreach (var chunk in _streamingHandler.ProcessStreamingResponseAsync(streamingRun))
        {
            var eventData = $"data: {JsonSerializer.Serialize(chunk)}\n\n";
            await Response.WriteAsync(eventData);
            await Response.Body.FlushAsync();
            
            await Task.Delay(50); // 控制流式速度
        }

        await Response.WriteAsync("data: [DONE]\n\n");
    }
}
```

```javascript
// 前端流式显示示例
public class StreamingChatUI
{
    public async Task DisplayStreamingResponseAsync(string message)
    {
        var responseElement = document.getElementById("response");
        responseElement.innerHTML = "";

        using var response = await fetch(`/chat/stream?message=${encodeURIComponent(message)}`);
        const reader = response.body.getReader();
        const decoder = new TextDecoder();

        while (true) {
            const { value, done } = await reader.read();
            if (done) break;

            const chunk = decoder.decode(value);
            const lines = chunk.split('\n');
            
            for (const line of lines) {
                if (line.startsWith('data: ')) {
                    const data = line.slice(6);
                    if (data === '[DONE]') return;
                    
                    try {
                        const content = JSON.parse(data);
                        responseElement.innerHTML += content;
                        responseElement.scrollTop = responseElement.scrollHeight;
                    } catch (e) {
                        // 处理非JSON数据
                        responseElement.innerHTML += data;
                    }
                }
            }
        }
    }
}
```

### 9.5 批量处理优化 - 大批量数据高效处理
```csharp
// 批量请求处理器
public class BatchRequestProcessor
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxConcurrentBatches;

    public BatchRequestProcessor(int maxConcurrency = 5)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency);
        _maxConcurrentBatches = maxConcurrency;
    }

    public async Task<List<BatchResult>> ProcessBatchAsync(
        IEnumerable<ChatRequest> requests,
        CancellationToken ct = default)
    {
        var batches = requests.Batch(10); // 每批10个请求
        var results = new ConcurrentBag<BatchResult>();

        var batchTasks = batches.Select(async (batch, index) =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                Logger.LogInformation("处理批次 {BatchIndex}", index);
                var batchResults = await ProcessSingleBatchAsync(batch, ct);
                results.Add(new BatchResult(index, batchResults));
                return batchResults;
            }
            finally
            {
                _semaphore.Release();
            }
        });

        await Task.WhenAll(batchTasks);
        return results.OrderBy(r => r.BatchIndex).ToList();
    }

    private async Task<List<ChatResponse>> ProcessSingleBatchAsync(
        IEnumerable<ChatRequest> batch, 
        CancellationToken ct)
    {
        var tasks = batch.Select(request => _agent.RunAsync(request, ct));
        return (await Task.WhenAll(tasks)).ToList();
    }

    // 智能批处理策略
    public class IntelligentBatchingStrategy
    {
        public IEnumerable<ChatRequest>[] CreateOptimalBatches(
            IEnumerable<ChatRequest> requests)
        {
            var requestList = requests.ToList();
            
            // 按相似性分组（相似请求可以共享缓存）
            var similarityGroups = GroupBySimilarity(requestList);
            
            // 按优先级排序
            var prioritized = PrioritizeRequests(similarityGroups);
            
            // 创建均衡的批次
            return CreateBalancedBatches(prioritized, maxBatchSize: 10);
        }

        private List<List<ChatRequest>> GroupBySimilarity(List<ChatRequest> requests)
        {
            // 基于消息内容的简单相似性分组
            return requests.GroupBy(r => r.Messages.Last().Text.GetHashCode())
                         .Select(g => g.ToList())
                         .ToList();
        }
    }
}
```

```csharp
// 批量处理API端点
[ApiController]
public class BatchChatController : ControllerBase
{
    [HttpPost("chat/batch")]
    public async Task<ActionResult<BatchResponse>> ProcessBatchAsync(
        BatchRequest batchRequest)
    {
        if (batchRequest.Requests.Count > 100)
        {
            return BadRequest("单次批量处理不能超过100个请求");
        }

        var results = await _batchProcessor.ProcessBatchAsync(
            batchRequest.Requests, 
            HttpContext.RequestAborted);

        return new BatchResponse
        {
            TotalProcessed = results.Sum(r => r.Results.Count),
            BatchResults = results,
            ProcessingTime = DateTime.UtcNow - batchRequest.RequestTime
        };
    }
}
```



### 9.6 模型选择策略 - 不同场景选用合适模型 （应该用不上）
```csharp
// 智能模型路由器
public class ModelRouter
{
    private readonly Dictionary<string, IChatClient> _availableModels;
    private readonly ModelPerformanceTracker _performanceTracker;

    public IChatClient SelectOptimalModel(ChatRequest request, UserContext userContext)
    {
        var criteria = new ModelSelectionCriteria
        {
            Complexity = EstimateComplexity(request),
            Urgency = userContext.UrgencyLevel,
            CostSensitivity = userContext.CostPreference,
            QualityRequirement = userContext.QualityRequirement
        };

        return criteria switch
        {
            // 高复杂度+高质量要求 → GPT-4
            { Complexity: > 0.8, QualityRequirement: QualityRequirement.High } 
                => _availableModels["gpt-4"],
            
            // 一般任务+成本敏感 → GPT-3.5-Turbo
            { Complexity: <= 0.8, CostSensitivity: CostSensitivity.High }
                => _availableModels["gpt-3.5-turbo"],
            
            // 实时性要求高 → 更快的模型
            { Urgency: UrgencyLevel.High }
                => _availableModels["claude-instant"],
            
            // 代码相关任务 → 专用代码模型
            _ when IsCodeRelatedRequest(request) 
                => _availableModels["codex"],
            
            // 默认选择
            _ => _availableModels["gpt-3.5-turbo"]
        };
    }

    // 动态模型切换器
    public class DynamicModelSwitcher
    {
        public async Task<ChatResponse> ExecuteWithFallbackAsync(
            ChatRequest request, 
            params string[] modelPriority)
        {
            foreach (var modelName in modelPriority)
            {
                try
                {
                    var client = _availableModels[modelName];
                    return await client.CompleteAsync(request);
                }
                catch (RateLimitException)
                {
                    Logger.LogWarning("模型 {Model} 限流，尝试下一个", modelName);
                    continue;
                }
                catch (ModelOverloadException)
                {
                    Logger.LogWarning("模型 {Model} 过载，尝试下一个", modelName);
                    continue;
                }
            }
            
            throw new AllModelsUnavailableException("所有备用模型都不可用");
        }
    }

    // 模型性能监控
    public class ModelPerformanceTracker
    {
        private readonly ConcurrentDictionary<string, ModelMetrics> _metrics = new();

        public void RecordModelPerformance(string modelName, ModelPerformance metrics)
        {
            var modelMetrics = _metrics.GetOrAdd(modelName, _ => new ModelMetrics());
            modelMetrics.Record(metrics);
        }

        public ModelRanking GetModelRankings()
        {
            return new ModelRanking
            {
                BySpeed = _metrics.OrderBy(m => m.Value.AverageResponseTime).Select(m => m.Key),
                ByCost = _metrics.OrderBy(m => m.Value.AverageCost).Select(m => m.Key),
                ByReliability = _metrics.OrderByDescending(m => m.Value.SuccessRate).Select(m => m.Key)
            };
        }
    }
}
```

```csharp
// 使用智能模型路由的Agent
public class OptimizedAgent
{
    private readonly ModelRouter _modelRouter;

    public async Task<ChatResponse> RunOptimizedAsync(ChatRequest request, UserContext userContext)
    {
        var optimalModel = _modelRouter.SelectOptimalModel(request, userContext);
        
        // 如果有性能要求，使用带降级的执行
        if (userContext.UrgencyLevel == UrgencyLevel.High)
        {
            return await _modelRouter.DynamicSwitcher.ExecuteWithFallbackAsync(
                request, "gpt-4", "gpt-3.5-turbo", "claude-instant");
        }

        return await optimalModel.CompleteAsync(request);
    }
}
```

### 9.7 性能优化配置示例
```csharp
// 生产环境性能配置
services.AddSingleton<IntelligentSessionCache>();
services.AddSingleton<ModelRouter>();
services.AddSingleton<BatchRequestProcessor>();

var optimizedAgent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Name = "高性能优化Agent",
    Instructions = "你是一个高效的AI助手"
})
.UseMiddleware<CachingMiddleware>()
.UseMiddleware<TokenOptimizationMiddleware>()
.UseMiddleware<BatchProcessingMiddleware>();
```

```csharp
// 性能监控仪表板
app.MapGet("/performance/metrics", async () =>
{
    var cacheStats = await cache.GetStatisticsAsync();
    var modelRankings = modelRouter.PerformanceTracker.GetModelRankings();
    var tokenUsage = tokenOptimizer.Monitor.GetUsageReport();

    return new PerformanceDashboard
    {
        CacheHitRate = cacheStats.HitRate,
        TopPerformingModels = modelRankings.ByReliability.Take(3),
        TokenUsageTrends = tokenUsage,
        AverageResponseTime = CalculateAverageResponseTime()
    };
});
```

这些性能优化技术可以显著提升AI应用的响应速度、降低运营成本，并为用户提供更好的体验。





## 十、RAG集成 - 检索增强生成（重写-太简单了）
核心思想是通过 **非侵入式 Context Provider 机制**，将外部知识库的检索结果作为上下文注入到 LLM 的提示词中，增强其回答的准确性和时效性。

**设计要点（基于 TextSearchProvider）**

+ **零侵入集成：** 基于 `IContextProvider` 接口，通过 Agent 链式调用或配置注入，实现对核心 Agent 逻辑的无感增强。
+ **动态检索策略：** 不仅支持语义搜索和关键词匹配，还应支持 **混合检索（Hybrid Search）**、**重排序（Reranking）**、和 **多轮/子查询生成（Sub-Query Generation）**。
+ **多源知识库：** 可对接 Qdrant, Milvus, Redis, Elasticsearch 等向量数据库和全文搜索引擎，以及本地文件存储。
+ **上下文窗口优化：** 智能选择和截断检索文档，确保最终的上下文大小不超过模型的限制，并聚焦于最相关的信息。  




这里应该有点问题不对  

```csharp
// 1. 配置TextSearchProvider
AIAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    AIContextProviderFactory = ctx => new TextSearchProvider(
        searchFunc: QdrantSearchAsync, // 自定义检索函数
        serializedState: ctx.SerializedState,
        new TextSearchProviderOptions()
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
            RecentMessageMemoryLimit = 6
        })
});
```

```csharp
// 2. 真实检索函数实现
static async Task<IEnumerable<TextSearchProvider.TextSearchResult>> QdrantSearchAsync(
    string query, CancellationToken cancellationToken)
{
    var results = await vectorStore.SearchAsync(query, limit: 3);
    return results.Select(result => new TextSearchProvider.TextSearchResult
    {
        SourceName = result.Record.SourceName,
        SourceLink = result.Record.SourceLink,
        Text = result.Record.Content
    });
}
```

适用场景  
电商客服：退货政策、产品参数查询  
技术支持：故障排查手册、API文档检索  
企业内部：规章制度、员工手册问答



### <font style="background-color:rgba(255, 255, 255, 0.9);">10.1. </font>**<font style="background-color:rgba(255, 255, 255, 0.9);">TextSearchProvider的RAG实现细节</font>**


```csharp
// TextSearchProvider 的 ITextSearchProvider 接口扩展
public class AdvancedTextSearchProvider : ITextSearchProvider
{
    // ... 其他代码

    public async Task<IEnumerable<ChunkedDocument>> ChunkAndIndexAsync(IEnumerable<Document> documents)
    {
        // 智能分块策略
        var chunker = new AdaptiveTextChunker(
            maxChunkSize: 1024,
            overlap: 128,
            separatorType: SeparatorType.Markdown); // 支持Markdown, JSON, Code等结构化分块

        // 引入元数据富集
        var documentsWithMetadata = EnrichMetadata(documents);

        var chunks = await chunker.ChunkDocumentsAsync(documentsWithMetadata);

        // 索引写入：批量写入性能优化
        await _vectorStore.BulkIndexAsync(chunks);
        
        return chunks;
    }

    private IEnumerable<Document> EnrichMetadata(IEnumerable<Document> documents)
    {
        // 自动提取文档关键信息：作者、主题、创建时间、访问权限等
        // 这些元数据用于后续的 Pre-Filtering 或 Reranking
        foreach (var doc in documents)
        {
            doc.Metadata["security_level"] = "internal";
            doc.Metadata["last_modified"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
            yield return doc;
        }
    }
}
```



单纯的向量搜索可能错过关键词，单纯的关键词搜索可能错过语义。混合检索是最佳实践。

```csharp
// TextSearchProvider 的 SearchAsync 接口扩展
public class AdvancedTextSearchProvider : ITextSearchProvider
{
    public async Task<SearchResults> SearchAsync(string query, SearchOptions options)
    {
        // 1. **预过滤 (Pre-Filtering)**: 基于元数据进行筛选 (如：只搜索特定部门文档)
        var preFilter = BuildMetadataFilter(options.Filters);

        // 2. **语义搜索 (Vector Search)**: 检索相关性高但关键词不明确的文档
        var semanticResults = await _vectorStore.SemanticSearchAsync(query, options.MaxResults, preFilter);
        
        // 3. **关键词搜索 (Keyword/Sparse Search)**: 召回关键词准确的文档（如使用BM25）
        var keywordResults = await _vectorStore.KeywordSearchAsync(query, options.MaxResults, preFilter);

        // 4. **结果融合 (Fusion)**: 采用 Reciprocal Rank Fusion (RRF) 算法合并结果
        var fusedResults = RRF.Fuse(semanticResults, keywordResults);

        // 5. **重排序 (Re-ranking)**: 使用更小的、更强大的重排序模型 (e.g., MiniLM, BGE)
        // 重新评估 Top N 结果的精确相关性
        return await FusionAndRerankAsync(fusedResults, query);
    }
    
    // Reranking 示例函数
    private async Task<SearchResults> FusionAndRerankAsync(SearchResults fusedResults, string query)
    {
        var reranker = _rerankerModel.CreateReranker();
        var documents = fusedResults.Results.Select(r => r.Text).ToList();
        
        // 调用重排序模型 API
        var scores = await reranker.RerankAsync(query, documents);
        
        // 根据新分数排序并返回 Top K
        var finalResults = fusedResults.Results
            .Zip(scores, (result, score) => (Result: result, Score: score))
            .OrderByDescending(x => x.Score)
            .Take(5) // 取最终的 5 个结果
            .Select(x => x.Result)
            .ToList();
            
        return new SearchResults(finalResults);
    }
}
```





### 10.2. Agentic RAG 与传统 RAG 的对比实现 (控制层)
Agentic RAG 将 RAG 过程从一个固定的管道（Pipeline）提升为一个由 AI 驱动的 **动态规划和执行（Orchestration）** 过程。

#### 10.2.1. 动态检索策略规划 (`RAGPlan` 扩展)
根据查询的复杂度和类型，LLM 自身动态决定最佳的检索方式。

```csharp
public class AgenticRAGStrategy : IRAGStrategy
{
    public async Task<RAGPlan> CreateExecutionPlanAsync(QueryAnalysis analysis)
    {
        var plan = new RAGPlan();

        // 1. **查询类型识别 (Query Classification)**
        if (analysis.Type == QueryType.MultiHop) // 多跳问题 (e.g., "A公司的CEO的妻子是哪里毕业的?")
        {
            plan.Strategy = RetrievalStrategy.MultiStepReasoning;
            // 细化规划：分解成子问题
            plan.Steps = await DecomposeQueryAsync(analysis.Query);
        }
        else if (analysis.Type == QueryType.CodeSnippet) // 代码/API 问题
        {
            plan.Strategy = RetrievalStrategy.CodeRetrieval;
            // 指定检索源：优先搜索代码库索引，并开启关键词兜底
            plan.RetrievalOptions.Sources = new[] { "CodeBase", "API_Docs" };
            plan.RetrievalOptions.UseKeywordFallback = true;
        }
        else // 简单事实问答
        {
            plan.Strategy = RetrievalStrategy.DirectAnswering;
        }

        // 2. **查询重写 (Query Rewriting)**
        // 将用户的口语化、含糊不清的查询重写成更适合搜索的关键词和结构
        plan.RewrittenQuery = await _llmRewriter.RewriteAsync(analysis.Query, plan.Strategy);

        return plan;
    }
}

// RAGPlan 数据结构示例
public class RAGPlan
{
    public RetrievalStrategy Strategy { get; set; } // DirectAnswering, MultiStepReasoning, CodeRetrieval
    public string RewrittenQuery { get; set; }     // 优化后的检索查询
    public List<PlanStep> Steps { get; set; }      // 针对 Multi-step 的执行步骤
    public RetrievalOptions RetrievalOptions { get; set; }
}

public class RetrievalOptions
{
    public IEnumerable<string> Sources { get; set; } = new[] { "Default_KB" }; // 指定知识源
    public bool UseKeywordFallback { get; set; } = false; // 是否开启关键词兜底
    public int MaxChunkCount { get; set; } = 5;
}
```

#### 10.2.2. 迭代优化与自我评估 (`Iterative Refinement`)
引入评估机制，只有在首次检索结果不理想时，才进行第二次甚至第三次迭代，以节省 Token 和时间。

```csharp
public class AgenticRAGStrategy : IRAGStrategy
{
    // ... 其他代码

    // 迭代优化机制
    public async Task<RAGResult> ExecuteWithIterativeRefinementAsync(RAGPlan plan)
    {
        var currentResult = await ExecutePlanAsync(plan);
        int maxIterations = 2; // 最多迭代 2 次

        for (int i = 0; i < maxIterations; i++)
        {
            // 1. **质量评估 (Quality Evaluation)**
            // 使用另一个专门的 LLM (或小模型) 评估当前答案的准确性、完整性和对检索文档的忠实度
            var qualityScore = await EvaluateResultQualityAsync(currentResult, plan.RewrittenQuery);

            if (qualityScore >= 0.8) // 达到高质量阈值
            {
                currentResult.IsRefined = (i > 0);
                return currentResult;
            }

            // 2. **计划优化 (Refinement)**
            // 如果质量不佳，根据反馈生成新的检索查询或修改检索策略
            plan = await RefinePlanBasedOnFeedbackAsync(plan, currentResult, qualityScore);

            // 3. **执行下一轮检索**
            currentResult = await ExecutePlanAsync(plan);
        }

        return currentResult; // 返回最终结果
    }
}
```

---

### 10.3. RAG 监控与可观测性 (工程层 -  可以查看切片)
为了实现像中间件一样的追踪能力，需要将 RAG 的执行细节也纳入监控体系。  

#### 10.3.1. RAG 追踪中间件
可以设计一个专门的 `RAGTracingMiddleware`，位于 `TokenMonitoringMiddleware` 之前，负责记录 RAG 过程。

```csharp
public class RAGTracingMiddleware : IRunMiddleware
{
    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        var startTime = DateTime.Now;
        var query = context.Request.Messages.Last().Text;

        // 1. **记录检索前状态**
        Logger.LogInformation($"[RAG_TRACE] 开始处理查询: {query}");

        // 2. **执行检索 (TextSearchProvider 会在这里运行)**
        var response = await next(context);

        // 3. **提取并记录检索结果（需要 ContextProvider 机制暴露检索数据）**
        // 假设 TextSearchProvider 将检索数据写入 Context.Metadata
        var retrievalData = context.Metadata.Get<RetrievalData>("RetrievalResults");

        Logger.LogDebug($"[RAG_TRACE] 检索耗时: {(DateTime.Now - startTime).TotalMilliseconds}ms");
        Logger.LogDebug($"[RAG_TRACE] 检索到 {retrievalData.ChunkCount} 个 Chunk.");
        Logger.LogDebug($"[RAG_TRACE] 注入到 Prompt 的上下文长度: {retrievalData.ContextLength} chars.");

        // 4. **将检索源信息添加到最终响应中**
        response.Metadata["RAG_Sources"] = retrievalData.Sources;

        return response;
    }
}

// 注册
var agent = chatClient.CreateAIAgent(options)
    .UseMiddleware<RAGTracingMiddleware>()
    .UseMiddleware<TokenMonitoringMiddleware>(); // RAGTracing 位于 Token监控之前
```

#### 10.3.2. 评估指标（Evaluation Metrics）
```csharp
// Agentic RAG动态规划实现
public class AgenticRAGStrategy : IRAGStrategy
{
    public async Task<RAGPlan> CreateExecutionPlanAsync(QueryAnalysis analysis)
    {
        // 动态检索策略规划
        var plan = new RAGPlan();

        // 根据查询复杂度选择策略
        if (analysis.Complexity > 0.8)
        {
            plan.Strategy = RetrievalStrategy.MultiStepReasoning;
            plan.Steps = await PlanMultiStepRetrievalAsync(analysis);
        }
        else
        {
            plan.Strategy = RetrievalStrategy.DirectAnswering;
            plan.Steps = await PlanDirectRetrievalAsync(analysis);
        }

        return plan;
    }

    // 迭代优化机制
    public async Task<RAGResult> ExecuteWithIterativeRefinementAsync(RAGPlan plan)
    {
        var currentResult = await ExecutePlanAsync(plan);

        // 质量评估与迭代优化
        var qualityScore = await EvaluateResultQualityAsync(currentResult);

        if (qualityScore < 0.7) // 质量阈值
        {
            var refinedPlan = await RefinePlanBasedOnFeedbackAsync(plan, currentResult);
            currentResult = await ExecutePlanAsync(refinedPlan);
        }

        return currentResult;
    }
}

// 传统RAG vs Agentic RAG对比
public class RAGComparison
{
    public static void DemonstrateDifferences()
    {
        // 传统RAG：固定流程
        var traditionalRAG = new TraditionalRAGPipeline();

        // Agentic RAG：动态规划  
        var agenticRAG = new AgenticRAGOrchestrator();

        Console.WriteLine("传统RAG: 检索 -> 生成 -> 输出");
        Console.WriteLine("Agentic RAG: 分析 -> 规划 -> 执行 -> 评估 -> 优化");
    }
}
```







## 十一、Skill


**什么是 Skill？**

**Skill = 按需加载的提示词（上下文）管理系统**。它不是取代其他工具，而是组织和调用它们的容器/框架。

一个 Skill 必须包含：

| **组件** | **解释** |
| --- | --- |
| Skill 定义（skill manifest） | 作用、输入、输出 |
| 触发条件（trigger condition） | 什么时候该被调用 |
| Prompt 模块（prompt template） | 技能内部的推理逻辑 |
| 函数或工具（可选） | 是否需要 API、计算、检索 |
| 返回 schema | 输出必须结构化，否则无法组合 |


Skill 是行为模块化，不是代码模块化。



**核心价值：解决AI写作的三大痛点**

1. **告别重复劳动**：无需每次手动查找、复制粘贴冗长的提示词和检查清单。
2. **突破Token限制**：通过“按需加载，用多少读多少”的渐进式披露机制，将庞大的方法论拆解为小文件，显著节省Token。
3. **实现知识沉淀与迭代**：将个人或团队的最佳实践（方法论、检查清单、成功/失败案例）结构化地封装成Skill，使其可复用、可版本管理、可迁移。



**Skill 的工作原理（渐进式披露）**

Skill采用三层结构，像一本组织良好的手册，让Claude仅在需要时才加载信息，从而节省Token并提升效率。

| 层级 | 内容 | 加载时机 | 作用 |
| --- | --- | --- | --- |
| **第一层：元数据** | Skill的名称 (`name`) 和简介 (`description`) | **启动时自动加载**到系统提示词 | 让Claude知道有哪些Skill可用，但不知道具体内容。  |
| **第二层：指令** | `SKILL.md`文件的正文，告诉Claude“怎么做”和“读哪些文件”。  | 当Claude判断该Skill与当前任务**最相关**时加载。  | 提供执行任务的路线图。 |
| **第三层：资源** | 其他文件夹中的具体文件（模板、示例、方法论文档等）。  | 根据`SKILL.md`<br/>中的指令，**按需加载**特定文件。 | 提供完成任务所需的具体知识和方法。 |


****

****

**Skill 的三种主要类型**

1. **知识型Skill**：基于文档/知识库（如产品手册、法律条文），Agent在回答相关问题时优先检索并引用。
2. **工具型Skill**：封装了可执行的操作（如发送邮件、查询数据库、生成图表），通过Function Calling或代码执行实现。
3. **流程型Skill**：定义了完成复杂任务的标准步骤和决策逻辑（如故障排查流程、订单审核流程），可引导Agent按步骤推理。



① 纯语言技能（Language Skill）

比如：

+ 观点分析
+ 文本润色
+ 任务拆解
+ 内容生成
+ 样式转换（正式 → 口语）

这类 Skill 完全基于 Prompt。

② 工具型技能（Tool-enabled Skill）

依赖外部工具：

比如：

+ 搜索（SearchSkill）
+ 数据库查询（DBSkill）
+ PDF 生成（PDFSkill）
+ 图表绘制（ChartSkill）

Skill 内部要定义工具调用顺序。

③ 工作流级技能（Workflow Skill）

多个步骤组合成一个大 Skill，比如：

+ “写一篇文章”
+ “生成 PPT”
+ “制作视频脚本”
+ “从模糊需求到可执行计划”

本质上是“技能树（skill tree）”。





**Skill 需要满足的七条设计原则**

① 单一能力原则（出来就行了Single Responsibility）

一个 Skill 只能做一件事。

② 清晰输入输出（明确 schema）

Skill 不是黑箱。必须有：

```plain
{  "input_schema": {...},  "output_schema": {...}}
```

③ 可组合性（Composable）

所有 Skill 可像积木一样组合：

```plain
Skill A 输出 → Skill B 输入
```

④ 可解释性（Explainability）

Skill 内部要能 log 出推理链路。

⑤ 可替换性（Pluggable）

可以动态替换：

+ 不同模型
+ 不同工具
+ 不同 Prompt 版本

⑥ 可升级版本（Versioning）

Skill 必须有版本号： 

```plain
v1.0 → v1.1 → v2.0
```

`<font style="background-color:rgba(129, 139, 152, 0.12);">⑦ 可调度性（Invocable by Agent）</font>`

智能体能根据任务自动判断：

+ 调用哪个 Skill？
+ 顺序是什么？
+ 是否需要回滚？

## Skill 的标准结构
下面是一个标准 Skill Manifest：

```plain
name: SummarizeSkillversion: 1.0.0description: 提供针对任意文本的结构化总结能力input_schema:  type: object  properties:    text:      type: stringoutput_schema:  type: object  properties:    summary:      type: string    keywords:      type: arraytrigger:  detect_by:    - "帮我总结"    - "概括一下"  confidence_threshold: 0.5type: language_skillprompt_template: |  你是总结助手，请用结构化方式总结以下文本：  {{text}}
```

## 如何让智能体使用 Skill？（核心机制）
智能体必须有一个 “Skill Selector（技能选择器）”。

流程：

```plain
用户输入 → 意图识别 → 匹配技能 → 调用技能 → 返回结果
```

Selector 的实现有三类：

① 关键词匹配（最轻量）

用类似 router 的方式匹配触发句。

② embedding + 最近邻搜索（最常用）

用户需求向量化

Skill 描述向量化

做向量召回，取 top-1/top-3

③ 大模型推理然这个（最精准）

让 LLM 决定调用哪个 Skill：

```plain
用户需求是什么？
应该使用哪个 Skill？
为什么？
```

## Skill 调度流程
下面是 Skill 调用流程图：

```plain
┌────────────────────┐
│ User Input         │
└───────────┬────────┘
            ▼
┌────────────────────┐
│ Intent Recognition │
└───────────┬────────┘
 ▼┌────────────────────┐
 │ Skill Selector     │
 └───────────┬────────┘
▼┌────────────────────┐
 │ Skill Execution    │
 │ - prompt           │
 │ - tool             │
 │ - workflow         │
 └───────────┬────────┘
 ▼┌────────────────────┐
 │ Response Builder   │
 └────────────────────┘
 Skill System=可插拔、可组合、可调度、可扩展的 Agent 能力体系。
```

## Skill 示例
（1）任务拆解技能：对TaskDecomposeSkill

输入：

```plain
{ "task": "帮我写一个 AI 周报"}
```

输出：

```plain
{ "steps": [   {"step": "信息搜集"},   {"step": "内容总结"},   {"step": "结构设计"},   {"step": "文案生成"} ]}（2）搜索技能：SearchSkill（Tool-enabled）
```

定义：

```plain
type: tool_skilltool: bing_searchprompt: |  将用户需求转为搜索关键词：{{query}}（3）文章生成技能：ArticleSkill（workflow skill）
```

内部自动拆分流程：

1. 获取需求 →
2. 生成结构 →
3. 填充内容 →
4. 校对 →
5. 输出最终文章

## Skill System + Workflow + Memory → 智能体完整架构
一个成熟智能体必须包含：

| **模块** | **作用** |
| --- | --- |
| Persona System | 角色/风格 |
| Memory System | 用户长期记忆 |
| Tooling System | 调用外部工具 |
| Skill System | 行为模块化 |
| Workflow Engine | 任务级执行 |
| State Machine | 行为状态控制 |


Skill System 是承上启下的关键核心层









## 十一、高级AI能力
### 11.1 多模态处理 - 文本、图像综合
**设计要点（基于MAF多模态支持）**

统一接口：文本、图像、音频统一处理接口  
上下文融合：多模态信息在对话上下文中整合  
智能路由：根据输入类型自动选择处理路径  
执行代码

```csharp
// 多模态Agent配置
AIAgent multimodalAgent = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Instructions = "你能够处理文本、图像和多模态内容",
    Tools = [
        AIFunctionFactory.Create(AnalyzeImage),
        AIFunctionFactory.Create(ProcessAudio),
        AIFunctionFactory.Create(GenerateImage)
    ]
});

// 图像分析工具
[Description("分析图像内容并生成描述")]
static async Task<string> AnalyzeImage(string imageUrl, string analysisType)
{
    var visionClient = new ComputerVisionClient();
    var result = await visionClient.AnalyzeImageAsync(imageUrl);
    return $"图像分析结果: {result.Description}";
}
```

### 11.2 情感分析集成 - 情绪识别处理
设计要点  
实时情感检测：在对话过程中实时分析用户情绪  
响应策略调整：根据情感状态调整回复语气和策略  
异常情绪预警：检测到强烈负面情绪时触发人工介入  
执行方案

```csharp
// 情感感知Agent
public class EmotionAwareAgent
{
    private readonly ISentimentAnalyzer _sentimentAnalyzer;
    
    public async Task<ChatResponse> ProcessWithEmotion(string userInput)
    {
        // 1. 情感分析
        var sentiment = await _sentimentAnalyzer.Analyze(userInput);
        
        // 2. 根据情感调整指令
        var instructions = AdjustInstructionsBasedOnSentiment(
            BaseInstructions, sentiment);
            
        // 3. 执行Agent
        return await agent.RunAsync(userInput, instructions);
    }
    
    private string AdjustInstructionsBasedOnSentiment(string baseInstructions, Sentiment sentiment)
    {
        return sentiment.Score < -0.5 ? 
            baseInstructions + " 用户可能感到不满，请用安抚性语气回应。" :
            baseInstructions;
    }
}
```

### 11.3 知识图谱集成 - 结构化知识查询
设计要点  
图数据库集成：Neo4j、Azure Cosmos DB等  
语义关系查询：基于关系的智能检索

推理能力增强：利用图谱关系进行逻辑推理  
执行代码

```csharp
// 知识图谱查询工具
[Description("从知识图谱中查询实体关系和属性")]
static async Task<string> QueryKnowledgeGraph(string entity, string relationshipType)
{
    using var session = graphDatabase.Driver.Session();
    
    var query = @"
    MATCH (e:Entity {name: $entity})-[r:RELATIONSHIP]->(related)
    WHERE r.type = $relationshipType
    RETURN related.name as name, r.properties as properties";
    
    var results = await session.RunAsync(query, new { entity, relationshipType });
    return JsonSerializer.Serialize(results.ToList());
}

// 集成到Agent
builder.AddAIAgent("知识专家", "基于知识图谱回答复杂关系问题")
    .WithAITool(AIFunctionFactory.Create(QueryKnowledgeGraph));
```

### 11.4 自动优化循环 - 提示词自改进
设计要点  
A/B测试框架：并行测试不同提示词效果  
质量评估：自动评估回复质量（相关性、准确性、满意度）  
迭代优化：基于评估结果自动优化提示词  
执行架构

```csharp
// 自动优化管理器
public class PromptOptimizer
{
    public async Task<string> OptimizeInstructions(string baseInstructions, string domain)
    {
        // 1. 生成多个变体
        var variants = GenerateInstructionVariants(baseInstructions);
        
        // 2. 并行测试效果
        var testResults = await Task.WhenAll(
            variants.Select(v => TestInstructionVariant(v, domain)));
            
        // 3. 选择最优版本
        return SelectBestVariant(variants, testResults);
    }
    
    private async Task<TestResult> TestInstructionVariant(string instructions, string domain)
    {
        var testAgent = CreateTestAgent(instructions);
        var testCases = GetTestCases(domain);
        
        var scores = await Task.WhenAll(
            testCases.Select(tc => EvaluateResponse(testAgent, tc)));
            
        return new TestResult {
            Instructions = instructions,
            AverageScore = scores.Average(),
            Stability = CalculateStability(scores)
        };
    }
}
```



## 


## 十二、架构与能力整合模式
### 12.1 架构演进路径
单Agent → 多Agent协作 → 微服务架构 → 事件驱动系统

### 12.2 能力叠加策略
基础层：RAG + 工具调用  
增强层：多模态 + 情感分析  
智能层：知识图谱 + 自优化

### 13.3 DevUI在架构中的角色
开发阶段：可视化调试和性能分析  
测试阶段：集成测试和回归验证  
运维阶段：生产环境问题诊断（受限模式）  
这套架构模式和高级能力设计基于文档中的实际技术实现，可以支撑复杂的企业级AI应用开发。需要我详细解释某个特定的架构模式或AI能力吗？



总结  
这份文档涵盖了C# AI开发的完整技术栈，从基础框架到高级特性，为企业级AI应用开发提供了全面的技术参考。每个技术点都配有实际代码示例和文档引用，方便开发者快速上手和实践。

## 十三、监控与可观测性架构设计
### 13.1 OpenTelemetry集成 - 分布式追踪
基于《NET+AI _ MEAI _ .NET 平台的 AI 底座 （1）.md》的架构基础：

```csharp
// 在Program.cs中配置OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Microsoft.AgentFramework")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => 
            options.Endpoint = new Uri(" http://localhost:4317 ")))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

### 13.2 执行事件流 - WorkflowEvent实时监控
基于《Microsoft Agent Framework - 了解Workflows的几种不同模式.md》：

```csharp
public class WorkflowMonitor
{
    private readonly ILogger<WorkflowMonitor> _logger;
    
    public WorkflowMonitor(ILogger<WorkflowMonitor> logger)
    {
        _logger = logger;
    }
    
    // 监听工作流事件
    public async Task MonitorWorkflowEventsAsync(Workflow workflow, string workflowId)
    {
        await foreach (var evt in workflow.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentRunUpdateEvent agentUpdate:
                    LogAgentProgress(workflowId, agentUpdate);
                    break;
                    
                case FunctionCallingEvent functionCall:
                    LogFunctionCall(workflowId, functionCall);
                    break;
                    
                case WorkflowOutputEvent output:
                    LogWorkflowCompletion(workflowId, output);
                    break;
                    
                case WorkflowErrorEvent error:
                    LogWorkflowError(workflowId, error);
                    break;
            }
        }
    }
    
    private void LogAgentProgress(string workflowId, AgentRunUpdateEvent update)
    {
        using var activity = Diagnostics.ActivitySource.StartActivity("Agent.Progress");
        activity?.SetTag("workflow.id", workflowId);
        activity?.SetTag("agent.name", update.AgentName);
        activity?.SetTag("agent.step", update.Step);
        
        _logger.LogInformation(
            "工作流 {WorkflowId} - Agent {AgentName} 进度: {Step}",
            workflowId, update.AgentName, update.Step);
    }
}
```

### 13.3 性能指标收集 - 响应时间、成功率等
基于《NET+AI _ MEAI _ 会话缓存（5）.md》的性能优化：

```csharp
public class PerformanceMetricsCollector
{
    private readonly Counter<int> _requestCounter;
    private readonly Histogram<double> _responseTimeHistogram;
    private readonly Counter<int> _errorCounter;
    
    public PerformanceMetricsCollector(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Microsoft.AgentFramework");
        
        _requestCounter = meter.CreateCounter<int>("agent.requests.total", 
            description: "Total agent requests");
            
        _responseTimeHistogram = meter.CreateHistogram<double>("agent.response.time",
            unit: "ms", description: "Agent response time distribution");
            
        _errorCounter = meter.CreateCounter<int>("agent.errors.total",
            description: "Total agent errors");
    }
    
    public async Task<T> TrackAgentExecutionAsync<T>(
        string agentName, 
        Func<Task<T>> operation)
    {
        var startTime = DateTime.UtcNow;
        _requestCounter.Add(1, new KeyValuePair<string, object?>("agent", agentName));
        
        try
        {
            var result = await operation();
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _responseTimeHistogram.Record(duration, 
                new KeyValuePair<string, object?>("agent", agentName),
                new KeyValuePair<string, object?>("success", true));
                
            return result;
        }
        catch (Exception ex)
        {
            _errorCounter.Add(1, 
                new KeyValuePair<string, object?>("agent", agentName),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
            throw;
        }
    }
}
```

### 13.4 错误处理与重试 - 容错机制
基于《NET+AI _ Agent _ 会话保存与恢复（4）.md》的持久化能力：

```csharp
public class ResilientAgentExecutor
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ResilientAgentExecutor> _logger;
    
    public ResilientAgentExecutor(IChatClient chatClient, ILogger<ResilientAgentExecutor> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }
    
    public async Task<AgentRunResponse> ExecuteWithRetryAsync(
        AIAgent agent, 
        string input, 
        int maxRetries = 3)
    {
        var retryCount = 0;
        
        while (true)
        {
            try
            {
                using var activity = Diagnostics.ActivitySource.StartActivity("Agent.Execution");
                activity?.SetTag("agent.name", agent.Name);
                activity?.SetTag("retry.count", retryCount);
                
                return await agent.RunAsync(input);
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, 
                    "Agent {AgentName} 执行失败，正在进行第 {RetryCount} 次重试",
                    agent.Name, retryCount);
                
                // 指数退避
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
        }
    }
}
```

### 13.5 对话质量评估 - 输出结果验证
基于《Microsoft Agent Framework - 结构化输出.md》：

```csharp
public class ConversationQualityValidator
{
    private readonly JsonSerializerOptions _jsonOptions;
    
    public ConversationQualityValidator()
    {
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }
    
    public ValidationResult ValidateStructuredOutput<T>(
        string agentResponse, 
        JsonSchema expectedSchema) where T : class
    {
        var result = new ValidationResult();
        
        try
        {
            // 1. JSON格式验证
            var jsonDocument = JsonDocument.Parse(agentResponse);
            result.IsValidJson = true;
            
            // 2. Schema验证
            var schemaValidation = expectedSchema.Validate(jsonDocument);
            result.SchemaErrors = schemaValidation.ToList();
            result.IsSchemaValid = !schemaValidation.Any();
            
            // 3. 业务逻辑验证
            if (result.IsSchemaValid)
            {
                var businessObject = JsonSerializer.Deserialize<T>(agentResponse, _jsonOptions);
                result.BusinessValidation = ValidateBusinessRules(businessObject);
            }
        }
        catch (JsonException ex)
        {
            result.IsValidJson = false;
            result.ValidationErrors.Add($"JSON解析失败: {ex.Message}");
        }
        
        return result;
    }
    
    private List<string> ValidateBusinessRules<T>(T obj) where T : class
    {
        var errors = new List<string>();
        
        // 基于业务规则的验证逻辑
        if (obj is ApprovalDecision decision)
        {
            if (decision.Status == ApprovalStatus.Approved && 
                decision.RiskLevel == RiskLevel.Critical)
            {
                errors.Add("高风险申请不能自动批准");
            }
        }
        
        return errors;
    }
}
```

### 13.6 完整的监控配置类
```csharp
public class AgentMonitoringConfiguration
{
    public bool EnableDistributedTracing { get; set; } = true;
    public bool EnablePerformanceMetrics { get; set; } = true;
    public bool EnableQualityValidation { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    
    // 监控阈值配置
    public TimeSpan SlowResponseThreshold { get; set; } = TimeSpan.FromSeconds(30);
    public double ErrorRateThreshold { get; set; } = 0.05; // 5%
    
    // 告警配置
    public AlertConfiguration Alerts { get; set; } = new();
}
```

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentMonitoring(
        this IServiceCollection services,
        Action<AgentMonitoringConfiguration> configure)
    {
        var config = new AgentMonitoringConfiguration();
        configure(config);
        
        services.AddSingleton(config);
        services.AddSingleton<WorkflowMonitor>();
        services.AddSingleton<PerformanceMetricsCollector>();
        services.AddSingleton<ResilientAgentExecutor>();
        services.AddSingleton<ConversationQualityValidator>();
        
        if (config.EnableDistributedTracing)
        {
            services.AddOpenTelemetry();
        }
        
        return services;
    }
}
```

### 13.7 使用示例
```csharp
// 在Program.cs中配置
builder.Services.AddAgentMonitoring(config =>
{
    config.EnableDistributedTracing = true;
    config.MaxRetryAttempts = 3;
    config.SlowResponseThreshold = TimeSpan.FromSeconds(10);
});
```

```csharp
// 在Agent执行中使用
var agent = chatClient.CreateAIAgent(options);
var monitor = serviceProvider.GetRequiredService<WorkflowMonitor>();
var executor = serviceProvider.GetRequiredService<ResilientAgentExecutor>();

// 执行并监控
var result = await executor.ExecuteWithRetryAsync(agent, userInput);
await monitor.MonitorWorkflowEventsAsync(workflow, "workflow-123");
```

这个设计方案结合了您提供的54个文档中的核心概念，提供了企业级的监控与可观测性解决方案。





## 十四、DevUI调试界面 - 可视化测试调试
### 14.1 设计要点（基于Microsoft Agent Framework DevUI）
零编码启用：一行代码即可集成可视化调试界面  
全流程追踪：Agent思考过程、工具调用、工作流流转可视化  
实时反馈：开发阶段即时查看Agent执行状态

### 14.2 执行代码
```csharp
// 1. 启用DevUI（仅需1行代码）
if (builder.Environment.IsDevelopment())
{
    app.MapDevUI(); // 启用DevUI，访问地址：/dev-ui
}

// 2. 完整配置示例
var builder = WebApplication.CreateBuilder(args);

// 注册多角色Agent
builder.AddAIAgent("客服助手", "处理客户咨询和订单查询")
    .WithAITools(GetOrderStatus, GetProductInfo);

builder.AddAIAgent("技术专家", "解决技术问题和故障诊断")
    .WithAITools(CheckSystemLogs, RunDiagnostics);

// 启用DevUI可视化调试
var app = builder.Build();
app.MapDevUI();
app.Run();
```

### 14.3 核心调试功能
Agent列表查看：所有注册Agent一目了然  
交互式对话测试：实时测试Agent响应  
工具调用日志：可视化查看工具触发和参数  
工作流调试：完整展示多Agent协作流程

### 14.4 实战调试场景
```csharp
// 问题：工具调用失败排查
// DevUI显示：get_weather工具location参数为空
// 解决方案：优化Agent指令，明确参数提取规则

// 问题：工作流流转异常  
// DevUI显示：评审员Agent未触发
// 解决方案：检查Agent名称注册一致性
```

## 十五、其他-架构模式
### 15.1 插件系统架构 - 模块化扩展
**设计要点（基于MCP协议）**

标准化接口：所有插件实现统一协议  
热插拔机制：运行时动态加载/卸载插件  
依赖隔离：插件间相互独立，故障隔离  
执行架构

```csharp
// MCP插件管理器
public class McpPluginManager
{
    private readonly Dictionary<string, IMcpServer> _plugins = new();
    
    public void RegisterPlugin(string name, IMcpServer plugin)
    {
        _plugins[name] = plugin;
        // 自动注册工具到Agent框架
        foreach (var tool in plugin.GetTools())
        {
            agentFramework.RegisterTool(tool);
        }
    }
    
    public async Task<object> ExecuteTool(string pluginName, string toolName, object parameters)
    {
        return await _plugins[pluginName].ExecuteTool(toolName, parameters);
    }
}
```

### 15.2 微服务集成 - 分布式系统协作
**设计要点（基于.NET Aspire）**

服务发现：自动发现和注册微服务  
依赖管理：声明式服务依赖关系  
统一配置：集中化管理跨服务配置  
执行方案

```csharp
// Aspire服务编排
var builder = DistributedApplication.CreateBuilder(args);

var aiService = builder.AddProject<Projects.AIService>("ai-service");
var ragService = builder.AddProject<Projects.RAGService>("rag-service");
var workflowService = builder.AddProject<Projects.WorkflowService>("workflow-service");

// 前端服务依赖AI服务
builder.AddProject<Projects.WebFrontend>("webfrontend")
    .WithReference(aiService)
    .WithReference(ragService)
    .WaitFor(aiService, ragService);
```

### 15.3 事件驱动架构 - 异步消息处理
设计要点  
事件溯源：完整记录系统状态变化  
异步处理：提高系统吞吐量和响应性  
最终一致性：支持分布式事务  
执行代码

```csharp
// 事件驱动的Agent工作流
public class EventDrivenWorkflow
{
    private readonly IEventBus _eventBus;
    
    public async Task ProcessUserRequest(string request)
    {
        // 1. 发布分析事件
        await _eventBus.Publish(new AnalysisRequestedEvent(request));
        
        // 2. 事件处理器自动触发相应Agent
        // - AnalysisCompletedEvent → 触发执行Agent
        // - ExecutionCompletedEvent → 触发审核Agent
    }
}

// 事件处理器注册
builder.Services.AddEventHandler<AnalysisRequestedEvent, AnalysisAgentHandler>();
builder.Services.AddEventHandler<ExecutionCompletedEvent, ReviewAgentHandler>();
```

### 15.4 CQRS模式 - 命令查询职责分离
设计要点  
读写分离：命令端优化写入，查询端优化读取  
数据投影：为不同查询需求构建专用视图  
最终一致性：异步同步命令端和查询端数据  
执行架构

```csharp
// 命令端：处理Agent执行
public class AgentCommandHandler
{
    public async Task<Guid> Handle(CreateAgentExecutionCommand command)
    {
        var execution = new AgentExecution(command.Request);
        await _repository.Save(execution);
        await _eventBus.Publish(new ExecutionCreatedEvent(execution.Id));
        return execution.Id;
    }
}

// 查询端：提供执行状态查询
public class AgentQueryService
{
    public async Task<ExecutionView> GetExecutionStatus(Guid executionId)
    {
        return await _queryStore.GetExecutionView(executionId);
    }
}
```

### 15.5 领域驱动设计 - 业务逻辑封装
设计要点  
聚合根：封装业务规则和不变条件  
值对象：不可变的业务概念封装  
领域服务：跨聚合的业务逻辑

**执行示例**

```csharp
// Agent执行聚合根
public class AgentExecution : AggregateRoot
{
    private List<ExecutionStep> _steps = new();
    private ExecutionStatus _status = ExecutionStatus.Pending;
    
    public void AddStep(ExecutionStep step)
    {
        // 业务规则：只能向进行中的执行添加步骤
        if (_status != ExecutionStatus.Running)
            throw new InvalidOperationException("执行未运行");
            
        _steps.Add(step);
        AddDomainEvent(new StepAddedEvent(Id, step));
    }
    
    public void Complete()
    {
        _status = ExecutionStatus.Completed;
        AddDomainEvent(new ExecutionCompletedEvent(Id, _steps));
    }
}
```



## 十六、部署与运维
### 16.1 Docker容器化部署
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AgentApp/AgentApp.csproj", "AgentApp/"]
RUN dotnet restore "AgentApp/AgentApp.csproj"
COPY . .
RUN dotnet build "AgentApp/AgentApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgentApp/AgentApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AgentApp.dll"]
```

### 16.2 健康检查与监控
```csharp
// 健康检查端点
app.MapHealthChecks("/health");

// 自定义Agent健康检查
services.AddHealthChecks()
    .AddCheck<AgentHealthCheck>("agent_health")
    .AddAzureBlobStorageStorage("storage_health");
```

```csharp
public class AgentHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken ct = default)
    {
        try
        {
            // 测试Agent响应能力
            var testResponse = await agent.RunAsync("健康检查", ct);
            return HealthCheckResult.Healthy("Agent服务正常");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Agent服务异常", ex);
        }
    }
}
```

# 应用场景
## 一、实际应用场景示例
### 1.1 智能客服系统
```csharp
// 客服工作流定义
var customerServiceWorkflow = AgentWorkflowBuilder.BuildSequential(
    "CustomerService",
    triageAgent,      // 问题分类
    specialistAgent,  // 专业处理
    satisfactionAgent // 满意度调查
);
```

```csharp
// 持久化客服对话
public class CustomerServiceManager
{
    private readonly Dictionary<string, AgentThread> _userSessions = new();
    private readonly IChatMessageStore _messageStore;
    
    public async Task<string> HandleUserQuery(string userId, string query)
    {
        // 获取或创建用户会话
        if (!_userSessions.TryGetValue(userId, out var thread))
        {
            thread = agent.GetNewThread();
            _userSessions[userId] = thread;
        }
        
        // 处理查询并保存历史
        var response = await agent.RunAsync(query, thread);
        await _messageStore.AddMessagesAsync(new[] { 
            new ChatMessage(Role.User, query),
            new ChatMessage(Role.Assistant, response.Text) 
        });
        
        return response.Text;
    }
}
```

### 1.2 内容生成流水线
```csharp
// 博客生成工作流
var blogGenerationWorkflow = AgentWorkflowBuilder.BuildSequential(
    "BlogGeneration",
    researchAgent,    // 资料收集
    outlineAgent,     // 大纲生成  
    writingAgent,     // 内容撰写
    reviewAgent,      // 质量审查
    seoAgent          // SEO优化
);
```

```csharp
// 执行内容生成
var blogTopic = "Microsoft Agent Framework入门指南";
var result = await blogGenerationWorkflow.RunAsync(blogTopic);
```

```csharp
// 结构化输出博客内容
public class BlogContent
{
    public string Title { get; set; }
    public string[] Sections { get; set; }
    public string[] Keywords { get; set; }
    public int TargetWordCount { get; set; }
}
```

```csharp
var blog = await agent.RunAsync<BlogContent>("生成技术博客", thread);
```

## 二、审批工作流 - 人工介入流程
### 2.1 设计要点
风险分级：区分高/低风险工具操作  
审批循环：支持多级审批和条件路由  
审计追踪：完整记录审批过程和结果

### 2.2 执行代码
```csharp
// 1. 包装敏感工具
var transferTool = AIFunctionFactory.Create(TransferMoney);
var approvalTool = new ApprovalRequiredAIFunction(transferTool);

// 2. 审批处理循环
var response = await agent.RunAsync(userRequest, thread);
var pendingRequests = response.UserInputRequests.OfType<FunctionApprovalRequestContent>();

foreach (var request in pendingRequests)
{
    // 人工审批界面
    bool approved = await ShowApprovalDialog(request);
    var approvalResponse = request.CreateResponse(approved);
    
    // 继续执行
    response = await agent.RunAsync(
        new ChatMessage(ChatRole.User, [approvalResponse]), thread);
}
```

### 2.3 业务场景
银行转账：资金操作必须审批  
IT运维：服务器重启、用户删除等敏感操作  
内容发布：公告推送、批量消息发送

## 三、电商客服场景 - 订单查询处理
### 3.1 设计要点（基于多Agent协作）
专门化Agent分工：查询分析→数据检索→答案生成  
订单状态实时性：集成订单系统API  
个性化服务：基于用户历史行为优化回复

### 3.2 执行架构
```csharp
// 电商客服工作流
var workflow = AgentWorkflowBuilder.BuildSequential(
    "EcommerceSupport",
    queryAnalyzer,      // 问题分析Agent
    orderRetriever,     // 订单检索Agent  
    policyChecker,      // 政策检查Agent
    responseGenerator   // 回复生成Agent
);

// 订单检索工具
[Description("根据用户信息查询订单状态")]
static async Task<string> GetOrderStatus(string userId, string orderId)
{
    var order = await orderService.GetOrderAsync(userId, orderId);
    return JsonSerializer.Serialize(new {
        Status = order.Status,
        Items = order.Items,
        ShippingInfo = order.ShippingAddress
    });
}
```

### 3.3 核心能力
退货政策查询： "30天无理由退货"   
物流跟踪： "您的订单已发货，预计明天送达"   
产品咨询： "这款产品的尺寸和材质是..." 

## 四、技术支持场景 - 问题诊断解决
### 14.1 设计要点
多轮诊断：支持渐进式问题排查  
知识库集成：故障代码库、解决方案库  
自动化修复：简单问题自动执行修复脚本

### 4.2 执行方案
```csharp
// 技术支持工作流
var techSupportFlow = new WorkflowBuilder()
    .AddExecutor("diagnoser", diagnosticAgent)    // 问题诊断
    .AddExecutor("solver", solutionAgent)        // 解决方案
    .AddExecutor("executor", fixAgent)            // 修复执行
    .AddConditionalEdge("diagnoser", 
        condition: output => output.Severity == "high" ? "human" : "solver",
        destinations: ["human", "solver"])
    .Build();
```

### 4.3 工具集成示例
```csharp
// 系统诊断工具
[Description("检查系统日志中的错误信息")]
static async Task<string> CheckSystemLogs(string timeframe)
{
    var errors = await logService.GetErrorsAsync(DateTime.Now.AddHours(-1));
    return $"发现{errors.Count}个错误，主要类型: {string.Join(",", errors.GroupBy(x => x.Type))}";
}
```

## 
## 五、内容生成场景 - 博客文章创作
### 5.1 设计要点（基于BlogAgent案例）
多Agent流水线：研究→撰写→审查→发布  
质量保证机制：自动审查和人工审核结合  
风格一致性：维护作者写作风格库

### 5.2 执行代码
```csharp
// 博客生成工作流
var blogWorkflow = AgentWorkflowBuilder.BuildSequential(
    researcherAgent,    // 资料收集
    writerAgent,       // 内容撰写
    reviewerAgent,     // 质量审查
    publisherAgent     // 发布执行
);

// 执行全流程
await using var run = await InProcessExecution.StreamAsync(
    blogWorkflow, 
    new List<ChatMessage> { new(ChatRole.User, input) });
```

### 5.3 生成流程
资料收集：分析主题，检索相关技术文档  
内容撰写：生成结构化技术博客（3000+字）  
质量审查：检查准确性、可读性、SEO优化  
发布执行：保存为Markdown或直接发布

## 六、数据分析场景 - 数据提取洞察
### 6.1 设计要点
自然语言查询：用户用自然语言描述分析需求  
自动代码生成：将需求转换为数据分析代码  
可视化输出：生成图表和洞察报告

### 6.2 执行方案
```csharp
// 数据分析Agent配置
AIAgent dataAnalyst = chatClient.CreateAIAgent(new ChatClientAgentOptions
{
    Instructions = "你是数据分析专家，能将自然语言查询转换为数据分析代码",
    Tools = [AIFunctionFactory.Create(RunDataAnalysis)],
    ResponseFormat = ChatResponseFormat.ForJsonSchema<AnalysisResult>()
});

// 数据分析执行工具
[Description("执行数据分析并返回统计结果")]
static async Task<AnalysisResult> RunDataAnalysis(string query, string dataset)
{
    // 自动生成分析代码并执行
    var code = await GenerateAnalysisCode(query);
    var result = await ExecuteAnalysis(code, dataset);
    return new AnalysisResult {
        Insights = result.Insights,
        Charts = result.Visualizations,
        Recommendations = result.Suggestions
    };
}
```

## 七、场景化最佳实践总结
### 7.1 模式选择指南
| 场景类型 | 推荐模式 | 核心组件 |
| :--- | :--- | :--- |
| 简单问答 | 单Agent + RAG | TextSearchProvider |
| 复杂流程 | 多Agent工作流 | WorkflowBuilder |
| 敏感操作 | 审批工作流 | ApprovalRequiredAIFunction |
| 专业领域 | 专门化Agent | 角色指令+工具集 |


### 7.2 性能优化策略
RAG缓存：对高频查询结果建立缓存  
工作流并行：对独立任务使用并发执行  
增量检索：基于对话历史优化检索策略

### 7.3 质量保证机制
多轮验证：重要输出经过多个Agent交叉验证  
人工审核：关键决策点设置人工介入  
反馈循环：基于用户反馈持续优化Agent表现  
这套解决方案基于文档中的实际案例和技术实现，可以直接应用于生产环境。需要我针对某个特定场景提供更详细的设计吗？



## 八、企业级特性完整实现
### 8.1 依赖注入集成 - .NET IoC容器支持
基于《NET+AI _ MEAI _ 使用依赖注入（10）.md》的最佳实践：

```csharp
// Program.cs - 统一依赖注入配置
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEnterpriseAgentFramework(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. 配置系统
        services.AddConfigurationServices(configuration);
        
        // 2. 安全服务
        services.AddSecurityServices(configuration);
        
        // 3. AI客户端
        services.AddAIClients(configuration);
        
        // 4. 插件系统
        services.AddPluginSystem(configuration);
        
        // 5. 监控与审计
        services.AddMonitoringAndAuditing(configuration);
        
        return services;
    }
    
    private static IServiceCollection AddAIClients(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 基于环境配置不同的AI客户端
        var environment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
        
        services.AddChatClient(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var provider = cfg["AI:Provider"];
            
            return provider switch
            {
                "AzureOpenAI" => CreateAzureOpenAIClient(cfg),
                "OpenAI" => CreateOpenAIClient(cfg),
                "Aliyun" => CreateAliyunClient(cfg), // 基于国内用户运行指南
                _ => throw new InvalidOperationException($"不支持的AI提供商: {provider}")
            };
        })
        .UseLogging()
        .UseDistributedCache() // 基于会话缓存文档
        .UseFunctionInvocation();
        
        // 注册命名客户端用于不同场景
        services.AddKeyedChatClient("fast", (sp, key) => CreateFastClient(configuration));
        services.AddKeyedChatClient("accurate", (sp, key) => CreateAccurateClient(configuration));
        
        return services;
    }
}
```

### 8.2 配置化管理 - appsettings.json配置
基于《告别脆弱配置：.NET配置模式实战指南.md》的强类型配置模式：

```json
// appsettings.json
{
  "AgentFramework": {
    "Version": "1.0.0",
    "Environment": "Development",
    "DefaultModel": "qwen-plus"
  },
  "AI": {
    "Provider": "Aliyun",
    "Endpoints": {
      "AzureOpenAI": " https://your-endpoint.openai.azure.com/ ",
      "OpenAI": " https://api.openai.com/v1 ",
      "Aliyun": " https://dashscope.aliyuncs.com/compatible-mode/v1 "
    },
    "Models": {
      "Fast": "qwen-turbo",
      "Accurate": "qwen-plus",
      "Creative": "qwen-max"
    }
  },
  "Security": {
    "EncryptionKey": "${ENCRYPTION_KEY}",
    "TokenExpirationMinutes": 60,
    "AuditLogRetentionDays": 90
  },
  "Plugins": {
    "Enabled": ["WeatherPlugin", "TimePlugin", "CalculatorPlugin"],
    "ApprovalRequired": ["PaymentPlugin", "UserManagementPlugin"]
  }
}
```

```json
// appsettings.Development.json - 开发环境特定配置
{
  "AgentFramework": {
    "Environment": "Development",
    "EnableDebugLogging": true
  },
  "AI": {
    "Provider": "Aliyun",
    "Model": "qwen-turbo"
  }
}
```

```json
// appsettings.Production.json - 生产环境配置
{
  "AgentFramework": {
    "Environment": "Production",
    "EnableDebugLogging": false
  },
  "AI": {
    "Provider": "AzureOpenAI",
    "Model": "gpt-4"
  },
  "Security": {
    "TokenExpirationMinutes": 30,
    "AuditLogRetentionDays": 365
  }
}
```

```csharp
// 强类型配置类（基于选项模式最佳实践）：
public class AgentFrameworkSettings
{
    public const string SectionName = "AgentFramework";
    
    [Required]
    public string Version { get; set; } = "1.0.0";
    
    [Required]
    public string Environment { get; set; } = "Development";
    
    public bool EnableDebugLogging { get; set; }
    
    [Range(1, 300)]
    public int DefaultTimeoutSeconds { get; set; } = 30;
}

public class AISettings
{
    public const string SectionName = "AI";
    
    [Required]
    public string Provider { get; set; } = "AzureOpenAI";
    
    [Required]
    public string DefaultModel { get; set; } = "gpt-4";
    
    public AIServiceEndpoints Endpoints { get; set; } = new();
    public AIModels Models { get; set; } = new();
}
```

```csharp
// Program.cs中配置验证
builder.Services.Configure<AgentFrameworkSettings>(
    builder.Configuration.GetSection(AgentFrameworkSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 8.3 多环境支持 - 开发/测试/生产环境
基于《dotnet run file 里的两种特殊文件.md》的环境配置机制：

```csharp
public class EnvironmentAwareAgentFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnvironmentAwareAgentFactory> _logger;
    
    public EnvironmentAwareAgentFactory(
        IConfiguration configuration, 
        ILogger<EnvironmentAwareAgentFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public IChatClient CreateEnvironmentSpecificClient()
    {
        var environment = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
        var aiSettings = _configuration.GetSection(AISettings.SectionName).Get<AISettings>();
        
        _logger.LogInformation("创建 {Environment} 环境的AI客户端", environment);
        
        return environment?.ToLower() switch
        {
            "development" => CreateDevelopmentClient(aiSettings),
            "staging" => CreateStagingClient(aiSettings),
            "production" => CreateProductionClient(aiSettings),
            _ => CreateDevelopmentClient(aiSettings)
        };
    }
    
    private IChatClient CreateDevelopmentClient(AISettings settings)
    {
        // 开发环境：使用快速模型，启用详细日志
        return new OpenAIClient(settings.Models.Fast)
            .AsIChatClient()
            .UseDetailedLogging()
            .UseMockServices(); // 可选的模拟服务
    }
    
    private IChatClient CreateProductionClient(AISettings settings)
    {
        // 生产环境：使用准确模型，启用缓存和限流
        return new AzureOpenAIClient(settings.Models.Accurate)
            .AsIChatClient()
            .UseDistributedCache()
            .UseRateLimiting()
            .UseCircuitBreaker(); // 熔断机制
    }
}
```

### 8.4 安全合规 - 数据加密和访问控制
基于《一款开源实用的 .NET Core 加密解密工具类库.md》：

```csharp
public class SecureAgentSessionManager
{
    private readonly IEncryptProvider _encryptProvider;
    private readonly IConfiguration _configuration;
    
    public SecureAgentSessionManager(IEncryptProvider encryptProvider, IConfiguration configuration)
    {
        _encryptProvider = encryptProvider;
        _configuration = configuration;
    }
    
    // 加密会话数据
    public async Task<string> EncryptSessionDataAsync(AgentThread thread)
    {
        var serializedData = thread.Serialize();
        
        // 使用AES加密会话数据
        var encryptionKey = _configuration["Security:EncryptionKey"];
        var encryptedData = _encryptProvider.AESEncrypt(serializedData, encryptionKey);
        
        return encryptedData;
    }
    
    // 解密会话数据
    public async Task<AgentThread> DecryptSessionDataAsync(string encryptedData, AIAgent agent)
    {
        try
        {
            var encryptionKey = _configuration["Security:EncryptionKey"];
            var decryptedData = _encryptProvider.AESDecrypt(encryptedData, encryptionKey);
            
            return agent.DeserializeThread(decryptedData);
        }
        catch (Exception ex)
        {
            throw new SecurityException("会话数据解密失败", ex);
        }
    }
}
```

```csharp
// 基于角色的访问控制
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuthorizePluginAttribute : Attribute
{
    public string[] RequiredRoles { get; }
    
    public AuthorizePluginAttribute(params string[] roles)
    {
        RequiredRoles = roles;
    }
}

public class PluginAuthorizationMiddleware
{
    public override async ValueTask<AIContext> InvokingAsync(
        InvokingContext context, 
        CancellationToken cancellationToken = default)
    {
        // 检查用户权限
        var userRoles = context.GetUserRoles();
        var pluginAttributes = context.TargetMethod.GetCustomAttributes<AuthorizePluginAttribute>();
        
        foreach (var attr in pluginAttributes)
        {
            if (!attr.RequiredRoles.Any(role => userRoles.Contains(role)))
            {
                throw new UnauthorizedAccessException(
                    $"访问插件 {context.TargetMethod.Name} 需要权限: {string.Join(", ", attr.RequiredRoles)}");
            }
        }
        
        return new AIContext();
    }
}
```

### 8.5 审计日志 - 操作记录追踪
基于《从零到多_用Microsoft Agent Framework打造你的AI智能体军团.md》的审计日志：

```csharp
public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly IConfiguration _configuration;
    
    public AuditLogger(ILogger<AuditLogger> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task LogAgentActionAsync(AgentAuditEvent auditEvent)
    {
        using var activity = Diagnostics.ActivitySource.StartActivity("Audit.Log");
        
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventId = Guid.NewGuid(),
            UserId = auditEvent.UserId,
            AgentName = auditEvent.AgentName,
            ActionType = auditEvent.ActionType,
            Input = auditEvent.Input,
            Output = auditEvent.Output,
            DurationMs = auditEvent.DurationMs,
            Success = auditEvent.Success,
            ErrorMessage = auditEvent.ErrorMessage,
            IpAddress = auditEvent.IpAddress,
            UserAgent = auditEvent.UserAgent
        };
        
        // 结构化日志记录
        _logger.LogInformation("Agent审计日志: {@LogEntry}", logEntry);
        
        // 持久化到数据库（可选）
        await SaveToAuditDatabaseAsync(logEntry);
    }
    
    public async Task<IEnumerable<AgentAuditEvent>> QueryAuditLogsAsync(
        AuditQuery query)
    {
        // 支持复杂的审计日志查询
        return await _auditRepository.QueryAsync(query);
    }
}
```

```csharp
// 审计事件定义
public record AgentAuditEvent
{
    public string UserId { get; init; }
    public string AgentName { get; init; }
    public string ActionType { get; init; } // Run, FunctionCall, Error
    public string Input { get; init; }
    public string Output { get; init; }
    public long DurationMs { get; init; }
    public bool Success { get; init; }
    public string ErrorMessage { get; init; }
    public string IpAddress { get; init; }
    public string UserAgent { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### 8.6 版本管理 - 提示词和配置版本控制
基于《自动生成与管理专业提示词，快速落地你的 Prompt 工程.md》的版本管理：

```csharp
public class VersionedPromptManager
{
    private readonly IPromptRepository _promptRepository;
    private readonly IGitVersionService _gitVersionService;
    
    public VersionedPromptManager(
        IPromptRepository promptRepository, 
        IGitVersionService gitVersionService)
    {
        _promptRepository = promptRepository;
        _gitVersionService = gitVersionService;
    }
    
    public async Task<PromptVersion> CreateNewVersionAsync(
        string promptName, 
        string content, 
        string author)
    {
        var version = await _gitVersionService.GetNextVersionAsync(promptName);
        
        var promptVersion = new PromptVersion
        {
            Name = promptName,
            Content = content,
            Version = version,
            Author = author,
            CreatedAt = DateTime.UtcNow,
            Hash = ComputeContentHash(content)
        };
        
        await _promptRepository.SaveVersionAsync(promptVersion);
        return promptVersion;
    }
    
    public async Task<PromptVersion> GetVersionAsync(
        string promptName, 
        string versionSpecifier)
    {
        return versionSpecifier?.ToLower() switch
        {
            "latest" => await _promptRepository.GetLatestVersionAsync(promptName),
            "stable" => await _promptRepository.GetStableVersionAsync(promptName),
            _ => await _promptRepository.GetSpecificVersionAsync(promptName, versionSpecifier)
        };
    }
    
    public async Task<bool> RollbackVersionAsync(string promptName, string targetVersion)
    {
        var currentVersion = await _promptRepository.GetLatestVersionAsync(promptName);
        var target = await _promptRepository.GetSpecificVersionAsync(promptName, targetVersion);
        
        if (target == null)
            return false;
            
        // 创建回滚版本
        var rollbackVersion = new PromptVersion
        {
            Name = promptName,
            Content = target.Content,
            Version = await _gitVersionService.GetNextVersionAsync(promptName),
            Author = "system-rollback",
            CreatedAt = DateTime.UtcNow,
            IsRollback = true,
            RollbackFrom = currentVersion.Version
        };
        
        await _promptRepository.SaveVersionAsync(rollbackVersion);
        return true;
    }
}
```

### 8.7 完整的启动配置示例
```csharp
// Program.cs - 企业级启动配置
var builder = WebApplication.CreateBuilder(args);

// 1. 环境特定的配置加载
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

// 2. 企业级服务注册
builder.Services.AddEnterpriseAgentFramework(builder.Configuration);

// 3. 健康检查
builder.Services.AddHealthChecks()
    .AddCheck<AIServiceHealthCheck>("ai-service")
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<CacheHealthCheck>("redis-cache");

// 4. OpenTelemetry可观测性
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());

// 5. 审计日志中间件
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

var app = builder.Build();

// 环境特定的中间件管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 健康检查端点
app.MapHealthChecks("/health");

// 审计日志查询API（仅限管理员）
app.MapGet("/admin/audit-logs", async (IAuditLogger auditLogger, [AsParameters] AuditQuery query) =>
{
    return await auditLogger.QueryAuditLogsAsync(query);
}).RequireAuthorization("Admin");

app.Run();
```

这个完整的企业级特性实现结合了您提供的所有文档的最佳实践，提供了生产就绪的Agent框架解决方案。































## 引用文档索引
_从_死记硬背_到_主动思考_：用 Microsoft Agent Framework 重新定义 RAG.md  
从零到多_用Microsoft Agent Framework打造你的AI智能体军团.md  
告别脆弱配置：.NET配置模式实战指南.md  
使用 Microsoft Agent Framework 构建你的第一个 Agent 应用.md  
使用 Microsoft Agent Framework 实现结构化数据输出.md  
使用AgentThread实现同一Agent的多轮回话.md  
使用Microsoft Agent Framework调用函数工具.md  
一款开源实用的 .NET Core 加密解密工具类库.md  
一文吃透NuGet：.NET Core开发者的包管理终极指南.md  
用 Microsoft Agent Framework 实现会话记录三方存储，让对话持久化不丢失.md  
用微软Agent Framework打造智能博客生成系统的那些事儿.md  
用Microsoft Agent Framework 实现函数调用人工批准：让 AI 操作更可控.md  
用Microsoft Agent Framework，30 行代码打造会 “干活” 的 AI 代理.md  
在 .NET 10 中使用 C# 实现 CI 脚本.md  
智能体上下文记忆框架MIRIX的简介.md  
自动生成与管理专业提示词，快速落地你的 Prompt 工程.md  
dotnet 10 run file 支持多文件.md  
dotnet run file 里的两种特殊文件.md  
MAF快速入门（2）Agent的花样玩法.md  
MAF快速入门（3）聊天记录持久化到数据库.md  
MCP Gateway 综述与实战指南.md  
Microsoft Agent Framework 调试神器：DevUI 可视化界面，AI 代理开发效率翻倍！.md  
Microsoft Agent Framework 进阶：会话持久化 + 历史缩减，长会话不超模型限制.md  
Microsoft Agent Framework：3 行代码给 Agent 加 RAG，秒对接外部知识库.md  
Microsoft Agent Framework - 把 Agent 暴露为 MCP Server.md  
Microsoft Agent Framework - 持久化 Agent 对话.md  
Microsoft Agent Framework - 对 Agent 进AOP（Middleware）编程.md  
Microsoft Agent Framework - 结构化输出.md  
Microsoft Agent Framework - Agent 调用工具 (Function Call).md  
Microsoft Agent Framework - Agent 多轮对话.md  
Microsoft Agent Framework - AIContextProvider 上下文管理.md  
Microsoft Agent Framework 简单使用.md  
Microsoft Agent Framework_C#：了解Workflows的几种不同模式.md  
Microsoft Agent Framework：推动多智能体应用的统一开源引擎.md  
Microsoft Agent Framework进阶：Agent 工具化核心玩法！跨 Agent 调用 + MCP 标准化暴露.md  
NET+AI _ Agent _ 从 ChatClient 到 AIAgent （1）.md  
NET+AI _ Agent _ 构建插件系统（7）.md  
NET+AI _ Agent _ 会话保存与恢复（4）.md  
NET+AI _ Agent _ 会话压缩（5）.md  
NET+AI _ Agent _ 结构化输出（10）.md  
NET+AI _ Agent _ 启用工具调用（6）.md  
NET+AI _ Agent _ 人机协作（9）.md  
NET+AI _ Agent _ 线程记忆存储（3）.md  
NET+AI _ Agent _ 自定义文件存储（8）.md  
NET+AI _ MEAI _ .NET 平台的 AI 底座 （1）.md  
NET+AI _ MEAI _ 会话缓存（5） (1).md  
NET+AI _ MEAI _ 会话缓存（5）.md  
NET+AI _ MEAI _ 结构化输出（9）.md  
NET+AI _ MEAI _ 上下文压缩（6）.md  
NET+AI _ MEAI _ 使用依赖注入（10）.md  
NET+AI _ MEAI _ 提示工程（11）.md  
NET+AI _ MEAI _ 智能工具筛选（12）.md  
NET+AI _ MEAI _ 智能工具筛选进阶（13）.md  
NET+AI _ MEAI _ ChatOptions 详解（4）.md  
NET+AI _ MEAI _ Function Calling 基础（2）.md  
NET+AI _ MEAI _ Function Calling 实操（3）.md  
NET开发上手Microsoft Agent Framework（一）从开发一个AI美女聊天群组开始.md  
TOON 协议与 AIDotNet.Toon 实践指南.md  
WPF_C#：使用Microsoft Agent Framework框架创建一个带有审批功能的终端Agent.md

