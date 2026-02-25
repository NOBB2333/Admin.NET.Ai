using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

public class MemoryRagDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = "";
    public string SourceName { get; set; } = "";
    public ReadOnlyMemory<float> Vector { get; set; }
}

/// <summary>
/// 场景8: RAG 知识检索 (真正意义上的向量化语义检索 RAG)
///
/// 【本方案说明】
/// 演示了现代 RAG 最核心的“检索”环节。它不是简单匹配关键字，而是：
/// 1. 文档切片 (Chunking)
/// 2. 向量化 (Embedding)：调用文本向量模型，把文本变成浮点数数组。
/// 3. 语义匹配 (Similarity)：计算用户提问的向量与文档字典的余弦相似度 (Cosine Similarity)。
/// 
/// 【现代化落地方向】
/// 本 Demo 采用内存计算。在实际的现代化落地方案中，应该引入：
/// 1. MEAI 标准的 `Microsoft.Extensions.VectorData.IVectorStore`
/// 2. 挂载真实的向量数据库 (如 Milvus, Qdrant, Redis Vector)
/// 代替本文件里的 MemoryRagDocument 数组和 CosineSimilarity 函数。
/// </summary>
public static class RagDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [8] RAG 知识检索演示 (基于内存余弦相似度) ===");

        // 初始化 Embedding 生成器
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var embeddingGenerator = aiFactory.GetDefaultClient<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
        if (embeddingGenerator == null)
        {
            Console.WriteLine("⚠️ 未找到 EmbeddingGenerator，无法演示 RAG。请检查配置！");
            return;
        }

        // 1. 读取本地文档
        var loader = sp.GetRequiredService<Admin.NET.Ai.Services.Rag.LocalTextDocumentLoader>();
        var staticPath = Path.Combine(AppContext.BaseDirectory, "Demos", "Static", "RagFile");
        var rawDocs = await loader.LoadDirectoryAsync(staticPath);
        
        var chunker = sp.GetRequiredService<IDocumentChunker>();
        var chunks = chunker.ChunkDocuments(rawDocs, new ChunkingOptions { MaxChunkSize = 300, Overlap = 30 });
        
        var documents = chunks.Select(c => new MemoryRagDocument
        {
            Id = Guid.NewGuid().ToString(),
            Content = c.Content,
            SourceName = c.Metadata.TryGetValue("SourceName", out var name) ? name?.ToString() : "Unknown"
        }).ToList();

        if (documents.Count == 0)
        {
            Console.WriteLine("⚠️ 未能在 Static/RagFile 中找到知识库文件，已添加一条默认知识...");
            documents.Add(new MemoryRagDocument 
            { 
                Id = Guid.NewGuid().ToString(),
                Content = "Admin.NET.Ai 是一个强大的 .NET AI 开发框架，支持多 Agent 协作。",
                SourceName = "默认数据"
            });
        }
        
        Console.WriteLine($"    正在对 {documents.Count} 个知识分块进行 Embedding (调用大模型) ...");

        // 生成 Embedding 
        foreach (var doc in documents)
        {
            doc.Vector = await embeddingGenerator.GenerateVectorAsync(doc.Content);
        }

        Console.WriteLine("    Embedding 初始化完成！");

        // 2. 执行搜索
        var query = "什么是 Admin.NET.Ai? 差旅标准是多少？";
        Console.WriteLine($"\n2. 正在向量搜索: {query}");
        
        var queryVector = await embeddingGenerator.GenerateVectorAsync(query);
        
        // 计算余弦相似度并排序
        var searchResults = documents.Select(doc => new 
        {
            Doc = doc,
            Score = CosineSimilarity(queryVector.ToArray(), doc.Vector.ToArray())
        })
        .OrderByDescending(x => x.Score)
        .Take(3)
        .ToList();

        Console.WriteLine($"--- 搜索结果 (Top 3) ---");
        foreach (var result in searchResults)
        {
            Console.WriteLine($"[Score: {result.Score:F4}] 来源:{result.Doc.SourceName}\n{result.Doc.Content}\n");
        }

        Console.WriteLine("\n========== RAG 演示结束 ==========");
    }

    /// <summary>
    /// 计算余弦相似度
    /// </summary>
    private static float CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0;

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = (float)Math.Sqrt(magnitude1);
        magnitude2 = (float)Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }
}
