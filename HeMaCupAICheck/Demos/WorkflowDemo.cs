using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Services.Workflow;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 多 Agent 工作流演示 - 支持多供应商、工具调用、线程隔离
/// </summary>
public static class WorkflowDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [2] 多 Agent 协作工作流演示 ===\n");
        
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var providers = aiFactory.GetAvailableClients();
        
        Console.WriteLine($"可用 LLM 供应商: {string.Join(", ", providers)}");
        Console.WriteLine($"默认供应商: {aiFactory.DefaultProvider}\n");
        
        Console.WriteLine(@"
选择工作流模式:
  ✓ 多供应商支持: 不同 Agent 可使用不同 LLM
  ✓ 工具调用: 支持联网搜索、知识库、MCP
  ✓ 线程隔离: 每个 Agent 独立对话历史
  ✓ Token 优化: 只共享观点摘要

    1. 顺序执行 (Sequential) - 研究→写作→编辑
    2. 并发执行 (Parallel) - 多视角同时分析
    3. 编排者模式 (Orchestrator) - AI 动态分配任务
    4. 圆桌讨论 (Roundtable) - 多供应商多角色讨论
    5. ★ 增强模式 - 多供应商 + 工具调用演示
");
        Console.Write("请选择 (1-5): ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1": await RunSequentialWorkflowAsync(aiFactory); break;
            case "2": await RunParallelWorkflowAsync(aiFactory); break;
            case "3": await RunOrchestratorWorkflowAsync(aiFactory); break;
            case "4": await RunRoundtableDiscussionAsync(aiFactory); break;
            case "5": await RunEnhancedMultiAgentAsync(aiFactory); break;
            default: Console.WriteLine("无效选择"); break;
        }
    }

    /// <summary>
    /// 1. 顺序执行模式
    /// </summary>
    private static async Task RunSequentialWorkflowAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 顺序执行模式 ===\n");
        
        var chatClient = aiFactory.GetDefaultChatClient()!;
        
        Console.Write("请输入主题: ");
        var topic = Console.ReadLine() ?? "C# 14 新特性";

        var agents = new[]
        {
            ("研究员", "你是技术研究员。请总结给定主题的5个核心要点，每点一句话。"),
            ("作家", "你是技术博主。根据以上要点写一篇300字技术博客。"),
            ("编辑", "你是资深编辑。检查并直接输出最终版本。")
        };

        string currentContent = topic;

        foreach (var (name, instruction) in agents)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n【{name}】正在处理...");
            Console.ResetColor();
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, instruction),
                new(ChatRole.User, currentContent)
            };

            var sb = new System.Text.StringBuilder();
            await foreach (var chunk in chatClient.GetStreamingResponseAsync(messages))
            {
                foreach (var text in chunk.Contents.OfType<TextContent>())
                {
                    Console.Write(text.Text);
                    sb.Append(text.Text);
                }
            }
            currentContent = sb.ToString();
            Console.WriteLine();
        }

        Console.WriteLine("\n--- 顺序执行完成 ---");
    }

    /// <summary>
    /// 2. 并发执行模式
    /// </summary>
    private static async Task RunParallelWorkflowAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 并发执行模式 ===\n");
        
        Console.Write("请输入分析主题: ");
        var topic = Console.ReadLine() ?? "AI 对软件开发的影响";

        var providers = aiFactory.GetAvailableClients();
        
        // 使用不同供应商进行分析
        var analysts = new[]
        {
            ("技术专家", "从技术角度分析，3个关键点"),
            ("经济学家", "从经济角度分析，3个关键点"),
            ("伦理学者", "从伦理角度分析，3个关键点"),
            ("产品经理", "从产品角度分析，3个关键点")
        };

        Console.WriteLine($"启动 {analysts.Length} 个并发分析...\n");

        var tasks = analysts.Select(async (analyst, i) =>
        {
            var (role, instruction) = analyst;
            // 循环使用不同供应商
            var provider = providers[i % providers.Count];
            var client = aiFactory.GetChatClient(provider) ?? aiFactory.GetDefaultChatClient()!;
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, $"你是{role}。{instruction}，每点50字以内。"),
                new(ChatRole.User, $"分析: {topic}")
            };
            
            var response = await client.GetResponseAsync(messages);
            return (Role: role, Provider: provider, Result: response.Messages.LastOrDefault()?.Text ?? "");
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (role, provider, result) in results)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"【{role}】({provider}) ✓");
            Console.ResetColor();
            Console.WriteLine($"{result}\n");
        }

        // 汇总
        Console.WriteLine("--- 综合汇总 ---\n");
        var summaryClient = aiFactory.GetDefaultChatClient()!;
        var summaryMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "综合以下分析，给出200字结论。"),
            new(ChatRole.User, string.Join("\n\n", results.Select(r => $"【{r.Role}】:\n{r.Result}")))
        };
        
        await foreach (var chunk in summaryClient.GetStreamingResponseAsync(summaryMessages))
        {
            foreach (var text in chunk.Contents.OfType<TextContent>())
            {
                Console.Write(text.Text);
            }
        }
        Console.WriteLine("\n");
    }

    /// <summary>
    /// 3. 编排者模式
    /// </summary>
    private static async Task RunOrchestratorWorkflowAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 编排者模式 ===\n");
        
        Console.Write("请输入任务需求: ");
        var requirement = Console.ReadLine() ?? "创建一个电商网站技术方案";

        var orchestrator = new EnhancedMultiAgentOrchestrator(aiFactory, new EnhancedAgentOptions
        {
            MaxSummaryLength = 200,
            DelayBetweenAgentsMs = 100
        });

        Console.WriteLine("\n[Orchestrator] 分析需求并分配任务...\n");

        await foreach (var evt in orchestrator.RunTaskAllocationAsync(requirement))
        {
            switch (evt.Type)
            {
                case TaskEventType.Analyzing:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case TaskEventType.TasksAllocated:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n{evt.Content}");
                    foreach (var task in evt.Tasks ?? new())
                    {
                        Console.WriteLine($"  [{task.Id}] {task.AssignedAgent}: {task.Description}");
                    }
                    Console.ResetColor();
                    Console.WriteLine();
                    break;
                case TaskEventType.TaskCompleted:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"【{evt.AgentName}】完成:");
                    Console.ResetColor();
                    Console.WriteLine($"{evt.Content}\n");
                    break;
                case TaskEventType.Summarizing:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case TaskEventType.StreamingContent:
                    Console.Write(evt.Content);
                    break;
                case TaskEventType.Completed:
                    Console.WriteLine(evt.Content);
                    break;
            }
        }
    }

    /// <summary>
    /// 4. 圆桌讨论模式 - 多供应商
    /// </summary>
    private static async Task RunRoundtableDiscussionAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== 圆桌讨论模式 (多供应商) ===\n");
        
        var providers = aiFactory.GetAvailableClients();
        Console.WriteLine($"可用供应商: {string.Join(", ", providers)}\n");

        Console.Write("请输入讨论议题: ");
        var topic = Console.ReadLine() ?? "是否应该用微服务架构？";

        Console.Write("讨论轮数 (1-3): ");
        var roundsInput = Console.ReadLine();
        var rounds = int.TryParse(roundsInput, out var r) ? Math.Min(3, Math.Max(1, r)) : 2;

        var orchestrator = new EnhancedMultiAgentOrchestrator(aiFactory, new EnhancedAgentOptions
        {
            MaxSummaryLength = 100,
            MaxContextPoints = 6,
            MaxResponseLength = 100,
            DelayBetweenAgentsMs = 200
        });

        // 使用不同供应商注册 Agent
        if (providers.Count >= 3)
        {
            orchestrator
                .AddAgent("保守派", "你倾向于稳定可靠的方案", providers[0], "保守谨慎")
                .AddAgent("创新派", "你支持新技术和现代化方案", providers[1], "激进前瞻")
                .AddAgent("务实派", "你追求可行性和平衡", providers[2], "务实中立");
        }
        else
        {
            orchestrator
                .AddAgent("保守派", "你倾向于稳定可靠的方案", null, "保守谨慎")
                .AddAgent("创新派", "你支持新技术和现代化方案", null, "激进前瞻")
                .AddAgent("务实派", "你追求可行性和平衡", null, "务实中立");
        }

        Console.WriteLine($"\n议题: {topic}\n");

        await foreach (var evt in orchestrator.RunDiscussionAsync(topic, rounds))
        {
            switch (evt.Type)
            {
                case DiscussionEventType.Started:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case DiscussionEventType.RoundStarted:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case DiscussionEventType.AgentSpeaking:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"\n{evt.Content.Replace("正在思考...", "")}");
                    Console.ResetColor();
                    break;
                case DiscussionEventType.StreamingContent:
                    Console.Write(evt.Content);
                    break;
                case DiscussionEventType.AgentCompleted:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case DiscussionEventType.Summarizing:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case DiscussionEventType.Completed:
                    Console.WriteLine(evt.Content);
                    break;
            }
        }
    }

    /// <summary>
    /// 5. 增强模式 - 多供应商 + 工具调用
    /// </summary>
    private static async Task RunEnhancedMultiAgentAsync(IAiFactory aiFactory)
    {
        Console.WriteLine("\n=== ★ 增强模式 (多供应商 + 工具调用) ===\n");
        
        var providers = aiFactory.GetAvailableClients();
        Console.WriteLine($"可用供应商: {string.Join(", ", providers)}\n");

        Console.Write("请输入讨论议题: ");
        var topic = Console.ReadLine() ?? "2024年AI发展趋势分析";

        var orchestrator = new EnhancedMultiAgentOrchestrator(aiFactory, new EnhancedAgentOptions
        {
            MaxSummaryLength = 150,
            MaxContextPoints = 6,
            MaxResponseLength = 150,
            DelayBetweenAgentsMs = 300
        });

        // 模拟工具函数
        Func<string, Task<string>> mockWebSearch = async (query) =>
        {
            await Task.Delay(500); // 模拟网络延迟
            return $"[搜索结果] 关于'{query}'的最新资讯: AI技术在2024年持续快速发展，大模型竞争加剧...";
        };

        Func<string, Task<string>> mockRagSearch = async (query) =>
        {
            await Task.Delay(300);
            return $"[知识库] 关于'{query}'的内部文档: 企业AI应用指南建议采用渐进式部署策略...";
        };

        Func<string, Task<string>> mockMcpTool = async (query) =>
        {
            await Task.Delay(200);
            return $"[MCP数据] 市场分析数据: AI市场规模预计2025年达到5000亿美元...";
        };

        // 使用不同供应商注册 Agent，并配置工具
        var p = providers.Count > 0 ? providers : new List<string> { "default" }.AsReadOnly();
        
        orchestrator
            .AddAgent("数据分析师", 
                "你是数据分析师，擅长用数据说话", 
                p[0 % p.Count], 
                "数据驱动，逻辑严谨")
            .WithSearchTool("数据分析师", mockWebSearch)
            .WithMcpTool("数据分析师", "market_data", mockMcpTool);

        orchestrator
            .AddAgent("行业专家", 
                "你是AI行业专家，了解技术趋势", 
                p[1 % p.Count], 
                "前瞻性强，技术敏锐")
            .WithRagTool("行业专家", mockRagSearch);

        orchestrator
            .AddAgent("投资顾问", 
                "你是投资顾问，关注商业价值", 
                p[2 % p.Count], 
                "关注ROI，风险意识");

        Console.WriteLine($"议题: {topic}");
        Console.WriteLine("\n配置:");
        Console.WriteLine("  - 数据分析师: 配有 web_search + mcp_market_data 工具");
        Console.WriteLine("  - 行业专家: 配有 knowledge_base 工具");
        Console.WriteLine("  - 投资顾问: 无工具（纯推理）\n");

        await foreach (var evt in orchestrator.RunDiscussionAsync(topic, rounds: 2))
        {
            switch (evt.Type)
            {
                case DiscussionEventType.Started:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case DiscussionEventType.RoundStarted:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case DiscussionEventType.AgentSpeaking:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n{evt.Content}");
                    Console.ResetColor();
                    break;
                case DiscussionEventType.ToolCalling:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"  🔧 {evt.Content}");
                    Console.ResetColor();
                    break;
                case DiscussionEventType.ToolResult:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"  📊 {evt.Content}");
                    Console.ResetColor();
                    break;
                case DiscussionEventType.StreamingContent:
                    Console.Write(evt.Content);
                    break;
                case DiscussionEventType.AgentCompleted:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case DiscussionEventType.Summarizing:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(evt.Content);
                    Console.ResetColor();
                    break;
                case DiscussionEventType.Completed:
                    Console.WriteLine(evt.Content);
                    break;
            }
        }

        Console.WriteLine(@"
技术说明:
  ✓ 多供应商: 每个 Agent 可指定不同 LLM (Qwen/DeepSeek/Gemini/Grok)
  ✓ 工具调用: 第一轮时调用配置的工具获取数据
  ✓ 线程隔离: 每个 Agent 独立 ConversationHistory
  ✓ Token 优化: 只共享摘要 (MaxSummaryLength=150)
");
    }
}
