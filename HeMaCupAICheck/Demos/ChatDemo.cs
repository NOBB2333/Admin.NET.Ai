using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Middleware;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

public static class ChatDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [1] 基础对话与中间件演示 ===");
        
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        // var client = aiFactory.GetDefaultChatClient();    // 获取默认的提供商
        var client = aiFactory.GetChatClient("DeepSeek");    // 获取DeepSeek

        if (client == null)
        {
            Console.WriteLine("❌ 未能通过配置获取默认 ChatClient，请检查 appsettings.json。");
            return;
        }

        // 1. 构建 Agent (中间件已在 ChatClientBuilder 中配置)
        // UseMiddleware<T> 用于 IRunMiddleware，这里直接使用 ChatClient
        var agent = client.CreateAIAgent(sp).Build();

        Console.Write("请输入你想对 AI 说的话: ");
        var input = Console.ReadLine() ?? "你好，请自我介绍并预测.NET 10的发展。";

        Console.WriteLine("AI 正在思考...");
        
        try 
        {
            // 使用扩展方法一行代码实现流式打印
            var fullResponse = await agent.GetStreamingResponseAsync(input).WriteToConsoleAsync();

            Console.WriteLine(); 
            var traceId = "Streaming-Trace"; 
            Console.WriteLine($"[统计信息] TraceId: {traceId}, Length: {fullResponse.Length}");
            // In streaming mode, accurate token usage is often calculated post-stream or by middleware.
            // Console.WriteLine($"[消耗] Input: {response.Usage.InputTokenCount}, Output: {response.Usage.OutputTokenCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 执行出错: {ex.Message}");
        }
    }
}
