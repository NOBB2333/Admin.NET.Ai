using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Admin.NET.Ai.Extensions;

namespace HeMaCupAICheck.Demos;

public static class ScenarioDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [11] 综合场景演示: 智能知识问答 ===");

        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var ragService = sp.GetService<IGraphRagService>();
        var client = aiFactory.GetDefaultChatClient();

        if (client == null || ragService == null)
        {
            Console.WriteLine("缺少必要服务 (ChatClient 或 RagService)。");
            return;
        }

        while (true)
        {
            Console.Write("\n请输入问题 (输入 'exit' 退出): ");
            var question = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(question) || question.ToLower() == "exit") break;

            Console.WriteLine("1. [Thinking] 正在检索相关知识...");
            
            // RAG 检索
            var searchResult = await ragService.SearchAsync(question, new RagSearchOptions { Strategy = RagStrategy.Naive });
            var context = string.Join("\n", searchResult.Documents.Select(d => d.Content));
            
            Console.WriteLine($"   检索到 {searchResult.Documents.Count} 条记录。");

            Console.WriteLine("2. [Thinking] 正在生成回答...");

            // 构造 Prompt
            var prompt = $"""
                你是一个智能助手。请基于以下参考资料回答用户问题。
                如果参考资料不足以回答，请回答"我不确定"。

                参考资料:
                {context}

                用户问题: {question}
                回答:
                """;
            
            var messages = new[] { new ChatMessage(ChatRole.User, prompt) };
            
            try 
            {
                await client.GetStreamingResponseAsync(messages).WriteToConsoleAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error]: {ex.Message}");
            }
        }
    }
}
