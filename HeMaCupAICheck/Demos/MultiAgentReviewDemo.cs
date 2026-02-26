using Admin.NET.Ai.Core;
using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 场景20: 多 Agent 文档审核
/// 
/// 📌 展示 MAF Workflow 流水线能力
/// 
/// 流程: Writer → Reviewer → Editor
/// 1. Writer: 根据主题生成初稿
/// 2. Reviewer: 审核内容、指出问题
/// 3. Editor: 最终修订润色
/// </summary>
public static class MultiAgentReviewDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("MultiAgentReviewDemo");
        var aiFactory = sp.GetRequiredService<IAiFactory>();

        Console.WriteLine("\n=== [15] 多 Agent 文档审核 (Writer→Reviewer→Editor) ===\n");

        // ===== 1. 定义 Agent 角色 =====
        Console.WriteLine("--- 1. Agent 角色定义 ---");
        
        var agents = new Dictionary<string, string>
        {
            ["Writer"] = """
                你是一位专业的技术文档撰写者。
                根据给定主题，撰写清晰、准确、结构化的技术文档。
                包含：概述、核心功能、使用方法、示例代码。
                """,
            ["Reviewer"] = """
                你是一位严格的文档审核专家。
                审核文档的：
                1. 技术准确性
                2. 表述清晰度
                3. 结构完整性
                4. 代码示例正确性
                指出所有问题并给出具体修改建议。
                """,
            ["Editor"] = """
                你是一位资深的技术编辑。
                基于审核意见，对文档进行最终修订：
                1. 修正所有指出的问题
                2. 优化语言表述
                3. 确保格式统一
                输出最终可发布版本。
                """
        };

        foreach (var agent in agents)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n🤖 {agent.Key}:");
            Console.ResetColor();
            Console.WriteLine($"   {agent.Value.Split('\n')[0].Trim()}...");
        }

        // ===== 2. 模拟流水线执行 =====
        Console.WriteLine("\n--- 2. 文档审核流水线 ---");

        var topic = "Admin.NET.Ai 的 MCP 工具集成功能";
        Console.WriteLine($"\n📝 主题: {topic}\n");

        try
        {
            var chatClient = aiFactory.GetDefaultChatClient();

            // Stage 1: Writer
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("▶️ Stage 1: Writer 生成初稿...");
            Console.ResetColor();

            var writerPrompt = $"{agents["Writer"]}\n\n请为以下主题撰写技术文档:\n{topic}";
            Console.Write("📄 初稿: ");
            var draft = await chatClient!.GetStreamingResponseAsync(writerPrompt).WriteToConsoleAsync();
            Console.WriteLine();

            // Stage 2: Reviewer
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n▶️ Stage 2: Reviewer 审核...");
            Console.ResetColor();

            var reviewPrompt = $"{agents["Reviewer"]}\n\n请审核以下文档:\n\n{draft}";
            Console.Write("📋 审核意见: ");
            var review = await chatClient.GetStreamingResponseAsync(reviewPrompt).WriteToConsoleAsync();
            Console.WriteLine();

            // Stage 3: Editor
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n▶️ Stage 3: Editor 最终修订...");
            Console.ResetColor();

            var editPrompt = $"""
                {agents["Editor"]}
                
                === 原始文档 ===
                {draft}
                
                === 审核意见 ===
                {review}
                
                请输出最终修订版本:
                """;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("✅ 最终文档: ");
            await chatClient.GetStreamingResponseAsync(editPrompt).WriteToConsoleAsync();
            Console.ResetColor();
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 演示需要配置 LLM: {ex.Message}");
        }

        // ===== 3. MAF Workflow 代码示例 =====
        Console.WriteLine("\n--- 3. MAF Workflow 实现 ---");
        Console.WriteLine(@"
// 使用 MAF WorkflowBuilder 实现
var workflow = new WorkflowBuilder()
    .AddAgent(""Writer"", writerAgent)
    .AddAgent(""Reviewer"", reviewerAgent)
    .AddAgent(""Editor"", editorAgent)
    .AddEdge(""Writer"", ""Reviewer"")
    .AddEdge(""Reviewer"", ""Editor"")
    .Build();

// 执行工作流
await foreach (var evt in workflow.ExecuteAsync(topic))
{
    switch (evt)
    {
        case MessageEvent msg:
            Console.WriteLine($""{msg.AgentName}: {msg.Content}"");
            break;
        case TurnCompleteEvent turn:
            Console.WriteLine($""== {turn.AgentName} 完成 =="");
            break;
    }
}
");

        Console.WriteLine("\n========== 多 Agent 文档审核演示结束 ==========");
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "(空)";
        if (text.Length <= maxLength) return text;
        return text[..maxLength] + "...";
    }
}
