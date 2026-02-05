using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

public static class RagDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [8] RAG 知识检索演示 ===");

        var ragService = sp.GetService<IGraphRagService>();
        if (ragService == null)
        {
            Console.WriteLine("未找到 IGraphRagService 服务。");
            return;
        }

        // 1. 模拟插入知识 (使用 IndexAsync + RagDocument)
        Console.WriteLine("1. 正在插入模拟知识...");
        var documents = new[]
        {
            new RagDocument("Admin.NET.Ai 是一个强大的 .NET AI 开发框架，支持多 Agent 协作。"),
            new RagDocument("GraphRAG 是一种结合了知识图谱和向量检索的增强生成技术。")
        };
        await ragService.IndexAsync(documents);

        // 2. 执行搜索
        var query = "什么是 Admin.NET.Ai?";
        Console.WriteLine($"\n2. 正在搜索: {query}");
        
        // 默认使用 Naive 策略 (如果 Neo4j 未配置)
        var result = await ragService.SearchAsync(query, new RagSearchOptions 
        { 
            Strategy = RagStrategy.Naive 
        });

        Console.WriteLine($"--- 搜索结果 (Naive) - 耗时: {result.ElapsedTime.TotalMilliseconds:F0}ms ---");
        foreach (var doc in result.Documents)
        {
            Console.WriteLine($"[Score: {doc.Score:F2}] {doc.Content}");
        }

        // 3. 模拟 Graph 搜索
        Console.WriteLine($"\n3. 正在尝试 Graph 搜索: {query}");
        var graphResult = await ragService.GraphSearchAsync(query, new GraphRagSearchOptions 
        { 
            MaxHops = 2,
            IncludeRelations = true
        });
        
        Console.WriteLine($"--- 搜索结果 (Graph) - 耗时: {graphResult.ElapsedTime.TotalMilliseconds:F0}ms ---");
        foreach (var doc in graphResult.Documents)
        {
            Console.WriteLine($"[Score: {doc.Score:F2}] {doc.Content}");
            if (doc.Metadata?.TryGetValue("RelatedContents", out var related) == true)
            {
                Console.WriteLine($"  └─ Related: {related}");
            }
        }
    }
}
