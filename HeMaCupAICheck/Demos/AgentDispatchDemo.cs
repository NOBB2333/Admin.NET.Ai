using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Services.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// Agent 调度演示 — 展示 LLM 自主发现和调用专业 Agent
/// </summary>
public static class AgentDispatchDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [13] LLM Agent 自主调度 (Auto-Discovery) ===");

        var toolManager = sp.GetRequiredService<ToolManager>();

        // 1. 展示 AgentDispatchTool 发现的所有 Agent
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n🤖 [Agent 发现] 通过 ToolManager 扫描到的工具中的 Agent 调度:");
        Console.ResetColor();

        var allTools = toolManager.GetAllTools();
        var dispatchTool = allTools.FirstOrDefault(t => t.Name == "AgentDispatch");
        
        if (dispatchTool == null)
        {
            Console.WriteLine("  ⚠️ AgentDispatchTool 未找到。");
            Console.WriteLine("  💡 提示: AgentDispatchTool 需要 IAiFactory 注入，Console 程序可能无法自动实例化。");
            Console.WriteLine("\n  以下是所有可用工具:");
            foreach (var t in allTools)
            {
                Console.WriteLine($"    🔧 {t.Name}: {t.Description}");
            }
            
            // 手动展示 IAiAgent 接口的增强
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n📋 [IAiAgent 接口增强] 展示新增的默认属性:");
            Console.ResetColor();

            // 扫描已注册的 Agent
            var agents = new List<(string Name, string Capability, int MaxIter, string? Tools)>();
            
            try
            {
                var sentimentAgent = sp.GetService<Agents.BuiltIn.SentimentAnalysisAgent>();
                if (sentimentAgent != null)
                {
                    agents.Add((sentimentAgent.Name, 
                        ((IAiAgent)sentimentAgent).Capability,
                        ((IAiAgent)sentimentAgent).MaxIterations,
                        ((IAiAgent)sentimentAgent).AllowedTools != null 
                            ? string.Join(", ", ((IAiAgent)sentimentAgent).AllowedTools!) 
                            : "全部"));
                }
            }
            catch { /* ignore */ }
            
            try
            {
                var kgAgent = sp.GetService<Agents.BuiltIn.KnowledgeGraphAgent>();
                if (kgAgent != null)
                {
                    agents.Add((kgAgent.Name,
                        ((IAiAgent)kgAgent).Capability,
                        ((IAiAgent)kgAgent).MaxIterations,
                        ((IAiAgent)kgAgent).AllowedTools != null 
                            ? string.Join(", ", ((IAiAgent)kgAgent).AllowedTools!) 
                            : "全部"));
                }
            }
            catch { /* ignore */ }
            
            try
            {
                var qaAgent = sp.GetService<Agents.BuiltIn.QualityEvaluatorAgent>();
                if (qaAgent != null)
                {
                    agents.Add((qaAgent.Name,
                        ((IAiAgent)qaAgent).Capability,
                        ((IAiAgent)qaAgent).MaxIterations,
                        ((IAiAgent)qaAgent).AllowedTools != null 
                            ? string.Join(", ", ((IAiAgent)qaAgent).AllowedTools!) 
                            : "全部"));
                }
            }
            catch { /* ignore */ }

            if (agents.Count > 0)
            {
                Console.WriteLine($"\n  发现 {agents.Count} 个已注册 Agent:\n");
                foreach (var (name, capability, maxIter, tools) in agents)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"  🤖 {name}");
                    Console.ResetColor();
                    Console.WriteLine($" (最大迭代: {maxIter}, 工具: {tools})");
                    Console.WriteLine($"     能力: {capability}");
                    Console.WriteLine();
                }

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("  💡 当 AgentDispatchTool 被 LLM 使用时，它会将以上 Agent 暴露为:");
                Console.WriteLine("     call_agent(agentName, task) — LLM 自行选择调用哪个 Agent");
                Console.ResetColor();
            }
            return;
        }

        // 如果 AgentDispatchTool 可用，展示其函数定义
        var functions = dispatchTool.GetFunctions().ToList();
        Console.WriteLine($"  AgentDispatchTool 暴露了 {functions.Count} 个函数:");
        foreach (var f in functions)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  📌 {f.Name}");
            Console.ResetColor();
            Console.WriteLine($"     {f.Description}");
        }

        // 2. 交互式 Agent 调用
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n📝 [交互演示] 输入任务描述，LLM 将自动选择合适的 Agent:");
        Console.ResetColor();

        var factory = sp.GetRequiredService<IAiFactory>();
        var chatClient = factory.GetDefaultChatClient();
        if (chatClient == null)
        {
            Console.WriteLine("  ❌ 无法获取 ChatClient");
            return;
        }

        var allFunctions = toolManager.GetAllAiFunctions().ToList();
        Console.Write("\n请输入任务（或按 Enter 使用默认）: ");
        var userTask = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(userTask))
            userTask = "请分析以下文本的情感：'今天天气真好，出去玩太开心了！但是晚上下起了大雨，心情变差了。'";

        Console.WriteLine($"\n🎯 任务: {userTask}");
        Console.WriteLine("AI 正在决定是否需要调用 Agent...\n");

        var options = new ChatOptions
        {
            Tools = allFunctions.Cast<AITool>().ToList()
        };

        await chatClient.GetStreamingResponseAsync(userTask, options).WriteToConsoleAsync();
        Console.WriteLine();
    }
}
