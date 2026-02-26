using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace HeMaCupAICheck.Demos;

public class MemoryRagDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = "";
    public string SourceName { get; set; } = "";
    public ReadOnlyMemory<float> Vector { get; set; }
}

/// <summary>
/// 场景17: RAG 知识检索 (真正意义上的向量化语义检索 RAG)
///
/// 【本方案说明】
/// 演示了现代 RAG 最核心的"检索"环节：
/// 1. 文档切片 (Chunking)
/// 2. 向量化 (Embedding)：调用 Embedding 模型把文本变成浮点数数组
/// 3. 本地向量缓存：首次 Embedding 后保存到 JSON 文件，后续直接加载（模拟向量数据库）
/// 4. 语义匹配 (Similarity)：计算用户提问向量与文档向量的余弦相似度
/// </summary>
public static class RagDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [17] RAG 知识检索 (GraphRAG & Vector) ===\n");

        // 从 DI 获取 EmbeddingGenerator (由 ServiceCollectionInit 从 LLM-Rag 配置注册)
        var embeddingGenerator = sp.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        if (embeddingGenerator == null)
        {
            Console.WriteLine("⚠️ 未找到 EmbeddingGenerator。");
            Console.WriteLine("💡 请在 LLMAgent.Rag.json 的 Embedding 节配置 ApiKey，或在 LLM-Clients 中添加同名 Provider。");
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
        
        Console.WriteLine($"    共 {documents.Count} 个知识分块");

        // 向量缓存文件 — 首次 Embedding 后保存，后续直接加载（模拟向量数据库持久化）
        var cacheFile = Path.Combine(staticPath, ".vector_cache.json");
        
        if (File.Exists(cacheFile))
        {
            // 从缓存加载向量（无需再调 API）
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    📦 从本地缓存加载已有向量（跳过 Embedding API 调用）");
            Console.ResetColor();
            
            var cachedData = JsonSerializer.Deserialize<List<CachedVector>>(await File.ReadAllTextAsync(cacheFile));
            if (cachedData != null)
            {
                foreach (var doc in documents)
                {
                    var cached = cachedData.FirstOrDefault(c => c.Content == doc.Content);
                    if (cached != null)
                    {
                        doc.Vector = new ReadOnlyMemory<float>(cached.Vector);
                    }
                }
            }
            
            // 对没有缓存的新文档做 Embedding
            var uncached = documents.Where(d => d.Vector.Length == 0).ToList();
            if (uncached.Count > 0)
            {
                Console.WriteLine($"    🆕 发现 {uncached.Count} 个新文档，调用 Embedding API...");
                foreach (var doc in uncached)
                {
                    doc.Vector = await embeddingGenerator.GenerateVectorAsync(doc.Content);
                }
                // 更新缓存
                await SaveVectorCacheAsync(cacheFile, documents);
            }
        }
        else
        {
            // 首次运行：全量 Embedding + 保存缓存
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"    ⏳ 首次运行，正在对 {documents.Count} 个分块调用 Embedding API...");
            Console.ResetColor();
            
            foreach (var doc in documents)
            {
                doc.Vector = await embeddingGenerator.GenerateVectorAsync(doc.Content);
            }
            
            await SaveVectorCacheAsync(cacheFile, documents);
            Console.WriteLine($"    💾 向量已缓存到 {Path.GetFileName(cacheFile)}，下次启动无需重新 Embedding");
        }

        Console.WriteLine("    ✅ 向量加载完成！");

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

        // ===== 3. 交互式问答 =====
        Console.WriteLine("\n--- 3. 交互式向量检索问答 ---");
        Console.WriteLine("输入问题，AI 将基于向量检索结果回答 (输入 'q' 或 'exit' 退出):");

        var chatClient = sp.GetRequiredService<IAiFactory>().GetDefaultChatClient();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\n🙋 你的问题: ");
            Console.ResetColor();

            var userQuestion = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userQuestion)) continue;
            if (userQuestion.Trim().Equals("q", StringComparison.OrdinalIgnoreCase) ||
                userQuestion.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            // 向量化用户问题
            var userQueryVector = await embeddingGenerator.GenerateVectorAsync(userQuestion);

            // 余弦相似度检索 Top-3
            var hits = documents.Select(doc => new
            {
                Doc = doc,
                Score = CosineSimilarity(userQueryVector.ToArray(), doc.Vector.ToArray())
            })
            .OrderByDescending(x => x.Score)
            .Take(3)
            .ToList();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"📚 检索到 {hits.Count} 条相关文档 (Top scores: {string.Join(", ", hits.Select(h => $"{h.Score:F4}"))})");
            Console.ResetColor();

            // RAG 增强 Prompt
            var context = string.Join("\n\n", hits.Select(h => $"【{h.Doc.SourceName}】(相似度:{h.Score:F4})\n{h.Doc.Content}"));

            var ragPrompt = $"""
            你是一个企业知识库助手。请基于以下检索到的知识库内容回答用户问题。
            如果知识库中没有相关信息，请明确说明"知识库中未找到相关信息"。
            回答要简洁准确。

            === 知识库检索结果 ===
            {context}

            === 用户问题 ===
            {userQuestion}

            请回答：
            """;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("🤖 助手: ");
            await chatClient!.GetStreamingResponseAsync(ragPrompt).WriteToConsoleAsync();
            Console.ResetColor();
            Console.WriteLine();
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

    /// <summary>
    /// 向量缓存序列化结构
    /// </summary>
    private record CachedVector(string Content, string SourceName, float[] Vector);

    /// <summary>
    /// 保存向量到本地缓存文件
    /// </summary>
    private static async Task SaveVectorCacheAsync(string cacheFile, List<MemoryRagDocument> documents)
    {
        var data = documents.Select(d => new CachedVector(d.Content, d.SourceName, d.Vector.ToArray())).ToList();
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(cacheFile, json);
    }
}
