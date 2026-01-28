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
  2. 并行编排 (Fan-out/Fan-in) - 多平台并行查询
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

        // 追踪 Agent 阶段
        var agentStages = new[] { "🔬 研究员", "✍️ 作家", "📝 编辑" };
        var currentStage = 0;
        var stageStarted = false;
        var completedStages = new HashSet<int>(); // 防止重复显示完成

        // 监听事件流
        await foreach (var evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentRunUpdateEvent agentEvt:
                    // 首次收到内容时显示当前 Agent 名称
                    if (!stageStarted && currentStage < agentStages.Length)
                    {
                        Console.WriteLine(); // 确保换行，避免日志混在一起
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"\n{agentStages[currentStage]}：");
                        Console.ResetColor();
                        stageStarted = true;
                    }
                    Console.Write(agentEvt.Update.Text);
                    break;
                    
                case ExecutorCompletedEvent:
                    // 只显示预期数量的阶段完成，且不重复
                    if (currentStage < agentStages.Length && !completedStages.Contains(currentStage))
                    {
                        Console.WriteLine(); // 确保换行，避免日志混在一起
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✅ {agentStages[currentStage]} 完成");
                        Console.ResetColor();
                        completedStages.Add(currentStage);
                        currentStage++;
                        stageStarted = false;
                    }
                    break;
            }
        }

        Console.WriteLine("\n--- 顺序编排完成 ---");
    }

    /// <summary>
    /// 2. 并行工作流 - Fan-out / Fan-in 模式
    /// 场景：电商多平台价格监控
    /// </summary>
    private static async Task RunConcurrentWorkflowAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 并行工作流 (Fan-out / Fan-in) ===\n");
        Console.WriteLine("📌 场景: 电商多平台价格监控 - 并行查询多个平台后汇总分析\n");
        
        var chatClient = aiFactory.GetDefaultChatClient()!;
        
        Console.Write("请输入商品名称 (如 iPhone 15 Pro): ");
        var productName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(productName)) productName = "iPhone 15 Pro 256GB";

        // 定义并行 Agent - 各平台价格查询
        var amazonAgent = new PlatformPriceExecutor(
            "🛒 Amazon",
            chatClient,
            "你是Amazon平台价格分析师。根据商品名称，模拟返回该商品在Amazon的价格信息。格式：价格=$XXX，库存=充足/紧张，配送=Prime免运费。只输出数据，不要解释。"
        );
        
        var ebayAgent = new PlatformPriceExecutor(
            "🏷️ eBay",
            chatClient,
            "你是eBay平台价格分析师。根据商品名称，模拟返回该商品在eBay的价格信息。格式：价格=$XXX，状态=全新/二手，运费=包邮/买家付。只输出数据，不要解释。"
        );
        
        var jdAgent = new PlatformPriceExecutor(
            "🔴 京东",
            chatClient,
            "你是京东平台价格分析师。根据商品名称，模拟返回该商品在京东的价格信息。格式：价格=¥XXX，库存=有货/无货，配送=京东物流。只输出数据，不要解释。"
        );

        // 起始 Executor - 广播查询请求
        var startExecutor = new QueryBroadcastExecutor();
        
        // 聚合 Executor - Fan-in 汇总结果
        var aggregator = new PriceAggregatorExecutor(3); // 等待3个平台结果

        // 构建 Fan-out / Fan-in 工作流
        var workflow = new WorkflowBuilder(startExecutor)
            .AddFanOutEdge(startExecutor, [amazonAgent, ebayAgent, jdAgent])  // 并行分发
            .AddFanInEdge([amazonAgent, ebayAgent, jdAgent], aggregator)       // 汇聚结果
            .WithOutputFrom(aggregator)
            .Build();

        Console.WriteLine("📋 工作流结构:");
        Console.WriteLine("                    ┌─→ 🛒 Amazon ─┐");
        Console.WriteLine("    📡 广播 ────────┼─→ 🏷️ eBay ───┼──→ 📊 汇总分析");
        Console.WriteLine("                    └─→ 🔴 京东 ───┘\n");

        // 执行工作流
        var query = new PriceQueryRequest(productName, "CN");
        await using var run = await InProcessExecution.StreamAsync(workflow, query);

        Console.WriteLine($"� 正在并行查询 '{productName}' 的价格...\n");

        await foreach (var evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case ExecutorInvokedEvent started:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"🚀 {started.ExecutorId} 启动");
                    Console.ResetColor();
                    break;
                    
                case ExecutorCompletedEvent completed:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ {completed.ExecutorId} 完成");
                    Console.ResetColor();
                    break;
                    
                case WorkflowOutputEvent output:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n" + output.Data);
                    Console.ResetColor();
                    break;
            }
        }

        Console.WriteLine("\n--- 并行工作流完成 ---");
    }

    /// <summary>
    /// 3. 翻译链 - 多语言转换 (使用自定义 Executor)
    /// </summary>
    private static async Task RunTranslationChainAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 翻译链 (Translation Chain) ===\n");
        
        var chatClient = aiFactory.GetDefaultChatClient()!;

        // 使用自定义 Executor 确保严格翻译
        var frenchTranslator = new TranslatorExecutor(
            "🇫🇷 法语翻译",
            chatClient,
            "French",
            "法语"
        );
        
        var spanishTranslator = new TranslatorExecutor(
            "🇪🇸 西班牙语翻译",
            chatClient,
            "Spanish", 
            "西班牙语"
        );
        
        var englishTranslator = new TranslatorExecutor(
            "🇺🇸 英语翻译",
            chatClient,
            "English",
            "英语"
        );

        // 起始 Executor
        var startExecutor = new TranslationStartExecutor();

        // 顺序链工作流
        var workflow = new WorkflowBuilder(startExecutor)
            .AddEdge(startExecutor, frenchTranslator)
            .AddEdge(frenchTranslator, spanishTranslator)
            .AddEdge(spanishTranslator, englishTranslator)
            .WithOutputFrom(englishTranslator)
            .Build();

        Console.Write("请输入要翻译的句子: ");
        var text = Console.ReadLine() ?? "Hello, how are you today?";

        Console.WriteLine($"\n📥 原文: {text}");
        Console.WriteLine("📋 翻译流程: 原文 → 🇫🇷 French → 🇪🇸 Spanish → 🇺🇸 English\n");

        // 执行工作流
        await using var run = await InProcessExecution.StreamAsync(workflow, text);

        await foreach (var evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case ExecutorInvokedEvent started:
                    if (started.ExecutorId != nameof(TranslationStartExecutor))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"{started.ExecutorId}: ");
                        Console.ResetColor();
                    }
                    break;
                    
                case ExecutorCompletedEvent completed:
                    if (completed.ExecutorId != nameof(TranslationStartExecutor))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($" ✓");
                        Console.ResetColor();
                    }
                    break;

                case WorkflowOutputEvent output:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n📤 最终结果: {output.Data}");
                    Console.ResetColor();
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

#region Fan-out / Fan-in Executor 类

/// <summary>
/// 价格查询请求
/// </summary>
public record PriceQueryRequest(string ProductName, string TargetRegion);

/// <summary>
/// 广播 Executor - 将查询请求分发给所有平台 Agent
/// </summary>
public sealed class QueryBroadcastExecutor() : Executor<PriceQueryRequest>(nameof(QueryBroadcastExecutor))
{
    public override async ValueTask HandleAsync(
        PriceQueryRequest query, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        var prompt = $"商品: {query.ProductName}\n区域: {query.TargetRegion}\n\n请查询该商品的当前价格信息。";
        await context.SendMessageAsync(new ChatMessage(ChatRole.User, prompt), cancellationToken);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken);
        Console.WriteLine("📡 查询请求已广播到所有平台");
    }
}

/// <summary>
/// 平台价格查询 Executor - 封装 LLM 调用
/// </summary>
public sealed class PlatformPriceExecutor : Executor<ChatMessage>
{
    private readonly IChatClient _chatClient;
    private readonly string _instructions;

    public PlatformPriceExecutor(string platformName, IChatClient chatClient, string instructions) 
        : base(platformName)
    {
        _chatClient = chatClient;
        _instructions = instructions;
    }

    public override async ValueTask HandleAsync(
        ChatMessage message, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _instructions),
            message
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        var reply = new ChatMessage(ChatRole.Assistant, response.Text ?? "(无数据)")
        {
            AuthorName = this.Id
        };

        await context.SendMessageAsync(reply, cancellationToken);
    }
}

/// <summary>
/// 价格聚合 Executor - Fan-in 汇总所有平台结果
/// </summary>
public sealed class PriceAggregatorExecutor(int targetCount) : Executor<ChatMessage>(nameof(PriceAggregatorExecutor))
{
    private readonly List<ChatMessage> _results = [];
    private readonly int _targetCount = targetCount;

    public override async ValueTask HandleAsync(
        ChatMessage message, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        _results.Add(message);
        Console.WriteLine($"📊 已收集 {_results.Count}/{_targetCount} 个平台数据");

        if (_results.Count >= _targetCount)
        {
            var platformData = string.Join("\n", _results.Select(m => $"  • {m.AuthorName}: {m.Text}"));
            
            var report = $"""
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 多平台价格汇总 (共 {_results.Count} 个平台)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

{platformData}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💡 智能定价建议
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
基于以上数据分析：
- 建议参考最低价平台进行竞价
- 考虑库存状态调整定价策略
- 可结合促销活动提升竞争力
""";
            
            await context.YieldOutputAsync(report, cancellationToken);
        }
    }
}

#endregion

#region 翻译链 Executor 类

/// <summary>
/// 翻译起始 Executor - 发送原文并触发翻译链
/// </summary>
public sealed class TranslationStartExecutor() : Executor<string>(nameof(TranslationStartExecutor))
{
    public override async ValueTask HandleAsync(
        string originalText, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        // 发送原文到下一个翻译器
        var msg = new ChatMessage(ChatRole.User, originalText);
        await context.SendMessageAsync(msg, cancellationToken);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken);
    }
}

/// <summary>
/// 翻译器 Executor - 严格执行翻译任务
/// </summary>
public sealed class TranslatorExecutor : Executor<ChatMessage>
{
    private readonly IChatClient _chatClient;
    private readonly string _targetLang;
    private readonly string _targetLangCn;

    public TranslatorExecutor(string name, IChatClient chatClient, string targetLang, string targetLangCn) 
        : base(name)
    {
        _chatClient = chatClient;
        _targetLang = targetLang;
        _targetLangCn = targetLangCn;
    }

    public override async ValueTask HandleAsync(
        ChatMessage message, 
        IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        // 使用非常严格的翻译提示
        var systemPrompt = $"""
            You are a professional translator. Your ONLY task is to translate text into {_targetLang}.
            
            STRICT RULES:
            1. Output ONLY the translated text in {_targetLang}
            2. Do NOT add any explanations, comments, or extra content
            3. Do NOT chat or respond to the content meaning
            4. Do NOT refuse to translate - just translate literally
            5. Preserve the original tone and style
            
            Example:
            Input: "I love you"
            Output: "{GetExample()}"
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, $"Translate to {_targetLang}: {message.Text}")
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        var translatedText = response.Text?.Trim() ?? "(翻译失败)";
        
        // 打印翻译结果
        Console.Write(translatedText);
        
        // 发送给下一个翻译器
        var reply = new ChatMessage(ChatRole.User, translatedText);
        await context.SendMessageAsync(reply, cancellationToken);
        
        // 如果是最后一个翻译器，输出最终结果
        if (_targetLang == "English")
        {
            await context.YieldOutputAsync(translatedText, cancellationToken);
        }
    }

    private string GetExample() => _targetLang switch
    {
        "French" => "Je t'aime",
        "Spanish" => "Te amo",
        "English" => "I love you",
        _ => "..."
    };
}

#endregion
