using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Services.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 全能对话智能体 — 加载所有工具和 Agent，LLM 自行判断是否调用
/// 支持多轮对话、工具调用、Agent 调度、文件操作、搜索等
/// </summary>
public static class UniversalAgentDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(@"
    ╔══════════════════════════════════════════════════╗
    ║     ★ 1. 综合性对话智能体 (All-in-One Agent)     ║
    ║  所有工具 + Agent 已加载，AI 自主决策            ║
    ║  输入 'exit' 或 'quit' 退出                     ║
    ╚══════════════════════════════════════════════════╝");
        Console.ResetColor();

        // 1. 获取 ChatClient
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var chatClient = aiFactory.GetDefaultChatClient();
        if (chatClient == null)
        {
            Console.WriteLine("❌ 无法获取 ChatClient，请检查配置。");
            return;
        }

        // 2. 加载全部工具并注入上下文
        var toolManager = sp.GetRequiredService<ToolManager>();
        var context = new ToolExecutionContext
        {
            SessionId = $"universal-{Guid.NewGuid():N}",
            CallerAgentName = "UniversalAgent",
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UserId = "interactive-user"
        };

        var allFunctions = toolManager.GetAllAiFunctions(context).ToList();

        // 展示已加载能力
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  📦 已加载 {allFunctions.Count} 个函数:");
        foreach (var f in allFunctions)
        {
            Console.WriteLine($"     • {f.Name}");
        }
        Console.WriteLine($"  📂 工作目录: {context.WorkingDirectory}");
        Console.ResetColor();

        // 3. 构建带中间件的 Agent（Token 监控等已在 Builder 中配置）
        var agent = chatClient.CreateAIAgent(sp).Build();

        // 4. 动态构建系统指令 — 从实际注册的工具/Agent 自动生成
        var capabilities = new System.Text.StringBuilder();
        capabilities.AppendLine("你是一个全能 AI 助手。以下是你当前拥有的全部能力（由系统自动发现），请根据用户需求自行判断是否使用：");
        capabilities.AppendLine();

        var allTools = toolManager.GetAllTools();
        foreach (var tool in allTools)
        {
            var approvalTag = tool.RequiresApproval() ? " [需用户审批]" : "";
            capabilities.AppendLine($"【{tool.Name}】{tool.Description}{approvalTag}");
            foreach (var func in tool.GetFunctions())
            {
                capabilities.AppendLine($"  - {func.Name}: {func.Description}");
            }
        }

        capabilities.AppendLine();
        capabilities.AppendLine("使用原则:");
        capabilities.AppendLine("1. 简单问答直接回答，不要多余调用工具");
        capabilities.AppendLine("2. 需要操作文件、搜索、执行命令时主动使用对应工具");
        capabilities.AppendLine("3. 复杂专业任务可以调度专业 Agent（如果可用）");
        capabilities.AppendLine("4. 总是用中文回复");

        var systemMessage = new ChatMessage(ChatRole.System, capabilities.ToString());

        // 5. 多轮对话
        var history = new List<ChatMessage> { systemMessage };
        var options = new ChatOptions
        {
            Tools = allFunctions.Cast<AITool>().ToList()
        };

        Console.WriteLine("\n💬 开始对话（输入 'exit' 退出）\n");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("你: ");
            Console.ResetColor();

            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\n👋 再见！");
                break;
            }

            history.Add(new ChatMessage(ChatRole.User, input));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\nAI: ");
            Console.ResetColor();

            try
            {
                var response = await agent.GetStreamingResponseAsync(history, options).WriteToConsoleAsync();
                Console.WriteLine(); // 换行

                // 将 AI 回复加入历史
                history.Add(new ChatMessage(ChatRole.Assistant, response));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ 错误: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }
    }
}
