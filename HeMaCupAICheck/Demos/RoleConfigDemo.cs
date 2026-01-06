using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 角色配置演示 - 展示如何使用 ChatOptions 配置不同角色
/// 正确做法：Factory 只提供客户端，角色配置在调用时通过 ChatOptions 传入
/// </summary>
public static class RoleConfigDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [角色配置演示 - 使用 ChatOptions] ===\n");
        
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        
        // 1. 从工厂获取基础客户端（Factory 只负责这个）
        var client = aiFactory.GetChatClient("DeepSeek");
        if (client == null)
        {
            Console.WriteLine("❌ 无法获取 ChatClient");
            return;
        }
        
        // ========== 示例 1: 研究员角色 ==========
        Console.WriteLine("--- 1. 研究员角色 ---\n");
        
        var researcherOptions = new ChatOptions
        {
            Instructions = "你是一位专业的研究员，擅长收集、分析和整理信息。请提供准确、有据可查的答案。",
            Temperature = 0.3f
        };
        
        await client.GetStreamingResponseAsync(
            "请简要分析 .NET 10 的三个主要新特性", 
            researcherOptions
        ).WriteToConsoleAsync();
        
        Console.WriteLine("\n");
        
        // ========== 示例 2: 代码助手角色 ==========
        Console.WriteLine("--- 2. 代码助手角色 ---\n");
        
        var coderOptions = new ChatOptions
        {
            Instructions = "你是一位专业的软件工程师，擅长编写高质量代码。请提供清晰、可维护的代码解决方案。",
            Temperature = 0.1f,
            MaxOutputTokens = 500
        };
        
        await client.GetStreamingResponseAsync(
            "用 C# 写一个计算斐波那契数列的函数", 
            coderOptions
        ).WriteToConsoleAsync();
        
        Console.WriteLine("\n");
        
        // ========== 示例 3: 翻译员角色 ==========
        Console.WriteLine("--- 3. 翻译员角色 ---\n");
        
        var translatorOptions = new ChatOptions
        {
            Instructions = "你是一位专业的中英文翻译专家，请将用户输入的内容翻译成英文。保持原有的格式和语气。",
            Temperature = 0.2f
        };
        
        await client.GetStreamingResponseAsync(
            "欢迎使用 Admin.NET.Ai 框架，这是一个企业级的 AI 集成解决方案。", 
            translatorOptions
        ).WriteToConsoleAsync();
        
        Console.WriteLine("\n");
        
        // ========== 示例 4: 带会话 ID 的多轮对话 ==========
        Console.WriteLine("--- 4. 带会话上下文的对话 ---\n");
        
        var sessionOptions = new ChatOptions
        {
            ConversationId = "session-demo-001",
            Instructions = "你是一个友好的助手，请记住用户的上下文。",
            Temperature = 0.5f
        };
        
        Console.WriteLine("第一轮：");
        await client.GetStreamingResponseAsync(
            "我叫小明，我是一名程序员", 
            sessionOptions
        ).WriteToConsoleAsync();
        
        Console.WriteLine("\n\n第二轮：");
        await client.GetStreamingResponseAsync(
            "你还记得我叫什么名字吗？", 
            sessionOptions
        ).WriteToConsoleAsync();
        
        Console.WriteLine("\n\n=== 角色配置演示结束 ===");
        Console.WriteLine("\n💡 要点：");
        Console.WriteLine("   - Factory 只负责创建客户端");
        Console.WriteLine("   - 角色配置通过 ChatOptions.Instructions 传入");
        Console.WriteLine("   - 这是 MEAI 框架的标准做法");
    }
}
//
// /// <summary>
// /// 流式输出扩展方法
// /// </summary>
// public static class StreamingExtensions
// {
//     public static async Task<string> WriteToConsoleAsync(this IAsyncEnumerable<ChatResponseUpdate> updates)
//     {
//         var fullText = new System.Text.StringBuilder();
//         
//         await foreach (var update in updates)
//         {
//             Console.Write(update.Text);
//             fullText.Append(update.Text);
//         }
//         
//         return fullText.ToString();
//     }
// }
