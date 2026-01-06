using Admin.NET.Ai.Abstractions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// MAF Workflow 演示 - 基于 Microsoft Agent Framework
/// 支持: WorkflowBuilder, Edge, WatchStreamAsync, TurnToken
/// </summary>
public static class WorkflowDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [MAF Workflow] 工作流演示 ===\n");
        
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var providers = aiFactory.GetAvailableClients();
        
        Console.WriteLine($"可用 LLM 供应商: {string.Join(", ", providers)}");
        Console.WriteLine($"默认供应商: {aiFactory.DefaultProvider}\n");
        
        Console.WriteLine(@"
选择工作流模式:
  1. 顺序编排 (Sequential) - Agent 链式处理
  2. 并发编排 (Concurrent) - FanOut → FanIn
  3. 翻译链 (Translation Chain) - 多语言转换
  4. 自定义 Executor - 非 AI 处理步骤
  5. 完整监控 - WatchStreamAsync 事件流
");
        Console.Write("请选择 (1-5): ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1": await RunSequentialWorkflowAsync(aiFactory); break;
            case "2": await RunConcurrentWorkflowAsync(aiFactory); break;
            case "3": await RunTranslationChainAsync(aiFactory); break;
            case "4": await RunCustomExecutorAsync(aiFactory); break;
            case "5": await RunWithMonitoringAsync(aiFactory); break;
            default: Console.WriteLine("无效选择"); break;
        }
    }

    /// <summary>
    /// 1. 顺序编排 - Agent 链式处理
    /// </summary>
    private static async Task RunSequentialWorkflowAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 顺序编排 (Sequential) ===\n");
        
        var chatClient = aiFactory.GetDefaultChatClient()!;
        
        Console.Write("请输入主题: ");
        var topic = Console.ReadLine() ?? "C# 14 新特性";

        // 创建 Agent 链: 研究员 → 作家 → 编辑
        var researcher = new ChatClientAgent(
            chatClient,
            "你是技术研究员。请总结给定主题的5个核心要点，每点一句话。"
        );
        
        var writer = new ChatClientAgent(
            chatClient,
            "你是技术博主。根据以上要点写一篇300字技术博客。"
        );
        
        var editor = new ChatClientAgent(
            chatClient,
            "你是资深编辑。检查并优化文章，直接输出最终版本。"
        );

        // 使用 WorkflowBuilder 构建工作流
        var workflow = new WorkflowBuilder(researcher)
            .AddEdge(researcher, writer)
            .AddEdge(writer, editor)
            .WithOutputFrom(editor)
            .Build();

        Console.WriteLine("\n📋 工作流结构: 研究员 → 作家 → 编辑\n");

        // 执行工作流
        var input = new ChatMessage(ChatRole.User, topic);
        await using var run = await InProcessExecution.StreamAsync(workflow, input);
        
        // 发送 TurnToken 触发 Agent 执行
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        // 监听事件流
        await foreach (var evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentRunUpdateEvent agentEvt:
                    // 实时流式输出 Agent 的响应
                    Console.Write(agentEvt.Update.Text);
                    break;
                    
                case ExecutorCompletedEvent completed:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n\n✅ [{completed.ExecutorId}] 完成");
                    Console.ResetColor();
                    break;
            }
        }

        Console.WriteLine("\n--- 顺序编排完成 ---");
    }

    /// <summary>
    /// 2. 并发编排 - 多 Agent 并行分析后汇总
    /// 注意：MAF 的 FanOut/FanIn 需要特定的 API，这里用顺序链演示多视角分析
    /// </summary>
    private static async Task RunConcurrentWorkflowAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 多视角分析 (Multi-Agent) ===\n");
        
        var chatClient = aiFactory.GetDefaultChatClient()!;
        
        Console.Write("请输入分析主题: ");
        var topic = Console.ReadLine() ?? "AI 对软件开发的影响";

        // 创建分析 Agent（使用顺序链模拟多视角）
        var analyst = new ChatClientAgent(
            chatClient,
            @"你是多视角分析专家。请从以下三个角度分析给定主题：
1. 技术角度 - 3个关键点
2. 经济角度 - 3个关键点  
3. 伦理角度 - 3个关键点
每点50字以内。"
        );
        
        var summarizer = new ChatClientAgent(
            chatClient,
            "你是综合分析师。整合以上分析，给出200字的综合结论。"
        );

        // 顺序工作流
        var workflow = new WorkflowBuilder(analyst)
            .AddEdge(analyst, summarizer)
            .WithOutputFrom(summarizer)
            .Build();

        Console.WriteLine("📋 工作流: 多视角分析 → 综合总结\n");

        var input = new ChatMessage(ChatRole.User, $"分析主题: {topic}");
        await using var run = await InProcessExecution.StreamAsync(workflow, input);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (var evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentRunUpdateEvent agentEvt:
                    Console.Write(agentEvt.Update.Text);
                    break;
                    
                case ExecutorCompletedEvent completed:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n\n【{completed.ExecutorId}】✓");
                    Console.ResetColor();
                    break;
            }
        }

        Console.WriteLine("\n--- 多视角分析完成 ---");
    }

    /// <summary>
    /// 3. 翻译链 - 多语言转换
    /// </summary>
    private static async Task RunTranslationChainAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 翻译链 (Translation Chain) ===\n");
        
        var chatClient = aiFactory.GetDefaultChatClient()!;

        // 创建翻译 Agent 链: 法语 → 西班牙语 → 英语
        var frenchAgent = new ChatClientAgent(
            chatClient,
            "你是翻译助手，将输入文本翻译成法语。只输出翻译结果。"
        );
        
        var spanishAgent = new ChatClientAgent(
            chatClient,
            "你是翻译助手，将输入文本翻译成西班牙语。只输出翻译结果。"
        );
        
        var englishAgent = new ChatClientAgent(
            chatClient,
            "你是翻译助手，将输入文本翻译成英语。只输出翻译结果。"
        );

        var workflow = new WorkflowBuilder(frenchAgent)
            .AddEdge(frenchAgent, spanishAgent)
            .AddEdge(spanishAgent, englishAgent)
            .WithOutputFrom(englishAgent)
            .Build();

        Console.Write("请输入英文句子: ");
        var text = Console.ReadLine() ?? "Artificial Intelligence is transforming the world!";

        Console.WriteLine($"\n📥 原始输入: {text}");
        Console.WriteLine("📋 翻译流程: English → French → Spanish → English\n");

        var input = new ChatMessage(ChatRole.User, text);
        await using var run = await InProcessExecution.StreamAsync(workflow, input);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        int step = 1;
        await foreach (var evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case ExecutorCompletedEvent completed:
                    var lang = step switch
                    {
                        1 => "🇫🇷 法语",
                        2 => "🇪🇸 西班牙语",
                        3 => "🇺🇸 英语",
                        _ => completed.ExecutorId
                    };
                    Console.WriteLine($"Step {step} ({lang}): {completed.Data}");
                    step++;
                    break;
            }
        }

        Console.WriteLine("\n--- 翻译链完成 ---");
    }

    /// <summary>
    /// 4. 自定义 Executor - 非 AI 处理步骤
    /// </summary>
    private static async Task RunCustomExecutorAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 自定义 Executor ===\n");

        // 自定义 Executor: 转大写
        var uppercaseExecutor = new UppercaseExecutor();
        
        // 自定义 Executor: 反转
        var reverseExecutor = new ReverseExecutor();
        
        // 自定义 Executor: 添加前缀
        var prefixExecutor = new PrefixExecutor("【处理结果】");

        var workflow = new WorkflowBuilder(uppercaseExecutor)
            .AddEdge(uppercaseExecutor, reverseExecutor)
            .AddEdge(reverseExecutor, prefixExecutor)
            .WithOutputFrom(prefixExecutor)
            .Build();

        Console.WriteLine("📋 工作流: 转大写 → 反转 → 添加前缀\n");
        
        Console.Write("请输入文本: ");
        var text = Console.ReadLine() ?? "Hello Workflow";

        Console.WriteLine($"\n📥 输入: {text}\n");
        
        // 使用流式执行获取输出
        await using var run = await InProcessExecution.StreamAsync(workflow, text);
        
        string? finalOutput = null;
        await foreach (var evt in run.WatchStreamAsync())
        {
            if (evt is WorkflowOutputEvent outputEvt)
            {
                finalOutput = outputEvt.Data?.ToString();
            }
        }
        
        Console.WriteLine($"\n📤 输出: {finalOutput}");
        
        Console.WriteLine("\n--- 自定义 Executor 完成 ---");
    }

    /// <summary>
    /// 5. 完整监控 - WatchStreamAsync 事件流
    /// </summary>
    private static async Task RunWithMonitoringAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 完整监控 (Event Stream) ===\n");
        
        var chatClient = aiFactory.GetDefaultChatClient()!;

        var agent1 = new ChatClientAgent(chatClient, "用一句话回答问题。");
        var agent2 = new ChatClientAgent(chatClient, "将回答翻译成中文。");

        var workflow = new WorkflowBuilder(agent1)
            .AddEdge(agent1, agent2)
            .WithOutputFrom(agent2)
            .Build();

        Console.Write("请输入问题: ");
        var question = Console.ReadLine() ?? "What is machine learning?";

        var input = new ChatMessage(ChatRole.User, question);
        await using var run = await InProcessExecution.StreamAsync(workflow, input);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        Console.WriteLine("\n--- 事件流监控 ---\n");

        int eventCount = 0;
        await foreach (var evt in run.WatchStreamAsync())
        {
            eventCount++;
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{eventCount:D3}] ");
            Console.ResetColor();

            switch (evt)
            {
                case SuperStepStartedEvent stepStart:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"🔄 SuperStep #{stepStart.StepNumber} 开始");
                    Console.ResetColor();
                    break;

                case ExecutorInvokedEvent invoked:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"▶️  {invoked.ExecutorId} 开始执行");
                    Console.ResetColor();
                    break;

                case AgentRunUpdateEvent agentEvt:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"💬 ");
                    Console.Write(agentEvt.Update.Text);
                    Console.ResetColor();
                    break;

                case ExecutorCompletedEvent completed:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✅ {completed.ExecutorId} 完成");
                    Console.ResetColor();
                    break;

                case ExecutorFailedEvent failed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ {failed.ExecutorId} 失败: {failed.Data?.Message}");
                    Console.ResetColor();
                    break;

                case SuperStepCompletedEvent stepEnd:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"✓  SuperStep #{stepEnd.StepNumber} 完成");
                    Console.ResetColor();
                    break;

                case WorkflowOutputEvent output:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"📤 工作流输出 (来自: {output.SourceId})");
                    Console.ResetColor();
                    break;
            }
        }

        Console.WriteLine($"\n--- 监控完成，共 {eventCount} 个事件 ---");
    }
}

#region Custom Executors

/// <summary>
/// 自定义 Executor: 转大写
/// </summary>
public class UppercaseExecutor : Executor<string, string>
{
    public UppercaseExecutor() : base("UppercaseExecutor") { }

    public override ValueTask<string> HandleAsync(
        string input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var result = input.ToUpper();
        Console.WriteLine($"  [{Id}] {input} → {result}");
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// 自定义 Executor: 反转字符串
/// </summary>
public class ReverseExecutor : Executor<string, string>
{
    public ReverseExecutor() : base("ReverseExecutor") { }

    public override ValueTask<string> HandleAsync(
        string input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var result = new string(input.Reverse().ToArray());
        Console.WriteLine($"  [{Id}] {input} → {result}");
        return ValueTask.FromResult(result);
    }
}

/// <summary>
/// 自定义 Executor: 添加前缀
/// </summary>
public class PrefixExecutor : Executor<string, string>
{
    private readonly string _prefix;

    public PrefixExecutor(string prefix) : base("PrefixExecutor")
    {
        _prefix = prefix;
    }

    public override ValueTask<string> HandleAsync(
        string input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var result = _prefix + input;
        Console.WriteLine($"  [{Id}] {input} → {result}");
        return ValueTask.FromResult(result);
    }
}

#endregion
