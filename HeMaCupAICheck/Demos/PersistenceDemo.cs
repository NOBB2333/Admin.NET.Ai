using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

public static class PersistenceDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [10] 对话持久化演示 (MEAI ChatMessage Store) ===");

        var store = sp.GetService<IChatMessageStore>();
        if (store == null)
        {
            Console.WriteLine("未找到 IChatMessageStore 服务。");
            return;
        }

        Console.WriteLine($"当前使用的存储提供商: {store.GetType().Name}");

        // 1. 模拟生成对话 ID
        var sessionId = "demo-session-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        Console.WriteLine($"会话 ID: {sessionId}");

        // 2. 添加消息 (使用 MEAI ChatMessage)
        Console.WriteLine("正在保存消息...");
        var userMsg = new ChatMessage(ChatRole.User, "你好，请把我的数据保存下来。");
        var assistantMsg = new ChatMessage(ChatRole.Assistant, "好的，我会将这些信息持久化存储。");

        await store.SaveMessagesAsync(sessionId, [userMsg, assistantMsg]);

        // 3. 读取消息
        Console.WriteLine("正在读取消息...");
        var history = await store.GetHistoryAsync(sessionId);
        
        var count = history.Count;
        Console.WriteLine($"读取到 {count} 条消息:");
        
        foreach (var msg in history)
        {
            Console.WriteLine($"[{msg.Role}] {msg.Text}");
        }

        // 4. 清理 (可选)
        Console.WriteLine("\n持久化验证完成。");
    }
}
