using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Services.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

public static class StructuredOutputDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [3] 结构化数据提取与 TOON 协议 ===");
        
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var client = aiFactory.GetDefaultChatClient();
        
        if (client == null) return;

        // 1. 结构化提取 (利用 RunAsync<T> 扩展)
        var rawText = "张三，男，35岁，现任阿里巴巴架构师，擅长 C#、Cloud Native 和多模态 AI。";
        Console.WriteLine($"原始文本: {rawText}");
        Console.WriteLine("正在尝试提取结构化模型 (PersonInfo)...");

        try 
        {
            var person = await client.RunAsync<PersonInfo>(rawText, sp);
            
            if (person != null)
            {
                Console.WriteLine("\n[成功提取对象]:");
                Console.WriteLine($"姓名: {person.Name}");
                Console.WriteLine($"年龄: {person.Age}");
                Console.WriteLine($"职业: {person.Occupation}");
                Console.WriteLine($"技能: {string.Join(", ", person.Skills)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 提取失败: {ex.Message}");
        }

        // 2. TOON 协议演示 (Token-Optimized Object Notation)
        Console.WriteLine("\n[TOON 协议序列化演示]:");
        var list = new List<PersonInfo>
        {
            new() { Name = "Alice", Age = 25, Occupation = "Dev" },
            new() { Name = "Bob", Age = 30, Occupation = "Manager" }
        };

        var toonOutput = ToonCodec.Serialize(list);
        Console.WriteLine($"TOON 输出 (更省 Token):\n{toonOutput}");
    }
}
