#pragma warning disable MEAI001 // IChatReducer 是预览 API
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace HeMaCupAICheck.Demos;

public static class CompressionDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [19] 上下文压缩策略 (Compression Reducers) ===");

        // 1. 获取 Reducer (MEAI 接口)
        var reducer = sp.GetService<IChatReducer>();
        
        if (reducer == null)
        {
            Console.WriteLine("未注册默认 IChatReducer (AdaptiveCompressionReducer).");
            return;
        }

        Console.WriteLine($"当前使用的 Reducer: {reducer.GetType().Name}");

        // 2. 模拟长对话历史 (使用 MEAI ChatMessage 类型)
        var history = new List<ChatMessage>();
        for (int i = 0; i < 20; i++)
        {
            history.Add(new ChatMessage(ChatRole.User, $"这是第 {i+1} 条用户消息，包含一些详细的上下文信息需要被保留或压缩。"));
            history.Add(new ChatMessage(ChatRole.Assistant, $"这是第 {i+1} 条助手回复，包含对应的信息。"));
        }
        
        // 插入关键指令（应该被保护）
        history.Add(new ChatMessage(ChatRole.System, "IMPORTANT: Always answer in JSON."));

        Console.WriteLine($"\n原始消息数量: {history.Count}");

        // 3. 执行压缩
        Console.WriteLine("正在执行压缩...");
        var reduced = await reducer.ReduceAsync(history, CancellationToken.None);
        var reducedList = reduced.ToList();

        Console.WriteLine($"压缩后消息数量: {reducedList.Count}");
        
        Console.WriteLine("\n--- 压缩后保留的消息摘要 ---");
        foreach (var msg in reducedList)
        {
            var text = msg.Text ?? "";
            var preview = text.Length > 50 ? text.Substring(0, 47) + "..." : text;
            Console.WriteLine($"[{msg.Role}] {preview}");
        }
    }
}
