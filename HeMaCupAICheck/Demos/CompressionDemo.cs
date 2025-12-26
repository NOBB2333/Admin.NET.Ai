using Microsoft.Extensions.DependencyInjection;
using IChatReducer = Admin.NET.Ai.Abstractions.IChatReducer;

namespace HeMaCupAICheck.Demos;

public static class CompressionDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [6] 上下文压缩策略演示 ===");

        // 1. 获取 Reducer
        // 也可以获取具体的 Reducer via sp.GetRequiredService<AdaptiveCompressionReducer>()
        var reducer = sp.GetService<IChatReducer>();
        
        if (reducer == null)
        {
            Console.WriteLine("未注册默认 IChatReducer (AdaptiveCompressionReducer).");
            return;
        }

        Console.WriteLine($"当前使用的 Reducer: {reducer.GetType().Name}");

        // 2. 模拟长对话历史 (使用 Semantic Kernel 类型以匹配 IChatReducer)
        var history = new List<Microsoft.SemanticKernel.ChatMessageContent>();
        for (int i = 0; i < 20; i++)
        {
            history.Add(new Microsoft.SemanticKernel.ChatMessageContent(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User, $"这是第 {i+1} 条用户消息，包含一些详细的上下文信息需要被保留或压缩。"));
            history.Add(new Microsoft.SemanticKernel.ChatMessageContent(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.Assistant, $"这是第 {i+1} 条助手回复，包含对应的信息。"));
        }
        
        // 插入关键指令（应该被保护）
        // 插入关键指令（应该被保护）
        history.Add(new Microsoft.SemanticKernel.ChatMessageContent(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.System, "IMPORTANT: Always answer in JSON."));

        Console.WriteLine($"\n原始消息数量: {history.Count}");

        // 3. 执行压缩
        Console.WriteLine("正在执行压缩...");
        var reduced = await reducer.ReduceAsync(history, CancellationToken.None);
        var reducedList = reduced.ToList();

        Console.WriteLine($"压缩后消息数量: {reducedList.Count}");
        
        Console.WriteLine("\n--- 压缩后保留的消息摘要 ---");
        foreach (var msg in reducedList)
        {
            var preview = msg.Content.Length > 50 ? msg.Content.Substring(0, 47) + "..." : msg.Content;
            Console.WriteLine($"[{msg.Role}] {preview}");
        }
    }
}
