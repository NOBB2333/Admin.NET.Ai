using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Memory;
using System.Collections.Concurrent;
using Admin.NET.Ai.Services.RAG;

namespace Admin.NET.Ai.Services.Rag;

/// <summary>
/// 向量搜索提供者
/// 基于 Semantic Kernel Memory 实现
/// </summary>
public class VectorSearchProvider : ITextSearchProvider
{
    private readonly ILogger<VectorSearchProvider> _logger;
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private readonly IDocumentChunker _chunker;
    
    // 简单的内存向量存储 (生产环境应使用 Qdrant/Pinecone/Redis 等)
    private readonly ConcurrentDictionary<string, (DocumentChunk Chunk, float[] Embedding)> _vectorStore = new();
    
    // 集合名称
    private const string DefaultCollection = "default";

    public VectorSearchProvider(
        ILogger<VectorSearchProvider> logger,
        IDocumentChunker chunker,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        _logger = logger;
        _chunker = chunker;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<SearchResults> SearchAsync(string query, SearchOptions options)
    {
        _logger.LogInformation("🔍 [VectorSearch] 搜索: {Query}", query);

        if (_embeddingGenerator == null)
        {
            _logger.LogWarning("未配置 Embedding 生成器，返回模拟结果");
            return new SearchResults(new List<TextSearchResult>
            {
                new() { Text = $"[模拟向量搜索结果] Query: {query}", Score = 0.95 }
            });
        }

        // 1. 生成查询向量
        var queryEmbedding = await _embeddingGenerator.GenerateAsync(query);
        var queryVector = queryEmbedding.Vector.ToArray();

        // 2. 计算余弦相似度
        var results = new List<(DocumentChunk Chunk, double Score)>();
        
        foreach (var (id, (chunk, embedding)) in _vectorStore)
        {
            var score = CosineSimilarity(queryVector, embedding);
            if (score >= options.MinScore)
            {
                results.Add((chunk, score));
            }
        }

        // 3. 排序并返回
        var topResults = results
            .OrderByDescending(r => r.Score)
            .Take(options.MaxResults)
            .Select(r => new TextSearchResult
            {
                Text = r.Chunk.Content,
                Score = r.Score,
                SourceName = r.Chunk.SourceName ?? "Unknown",
                SourceLink = r.Chunk.SourceUri ?? "",
                Metadata = r.Chunk.Metadata
            })
            .ToList();

        _logger.LogInformation("🔍 [VectorSearch] 返回 {Count} 条结果", topResults.Count);
        return new SearchResults(topResults);
    }

    public async Task<IEnumerable<ChunkedDocument>> ChunkAndIndexAsync(IEnumerable<Document> documents)
    {
        var rawDocs = documents.Select(d => new RawDocument
        {
            Content = d.Content,
            Metadata = d.Metadata
        });

        var chunks = _chunker.ChunkDocuments(rawDocs).ToList();
        
        _logger.LogInformation("📦 [VectorSearch] 分块完成: {Count} 块", chunks.Count);

        // 生成向量并存储
        if (_embeddingGenerator != null)
        {
            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingGenerator.GenerateAsync(chunk.Content);
                var vector = embedding.Vector.ToArray();
                chunk.Embedding = vector;
                
                _vectorStore[chunk.Id] = (chunk, vector);
            }
            
            _logger.LogInformation("✅ [VectorSearch] 向量索引完成: {Count} 条", chunks.Count);
        }

        return chunks.Select(c => new ChunkedDocument
        {
            Content = c.Content,
            Metadata = c.Metadata
        });
    }

    /// <summary>
    /// 直接添加文档块到索引
    /// </summary>
    public async Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks)
    {
        if (_embeddingGenerator == null)
        {
            _logger.LogWarning("未配置 Embedding 生成器，跳过索引");
            return;
        }

        foreach (var chunk in chunks)
        {
            if (chunk.Embedding == null)
            {
                var embedding = await _embeddingGenerator.GenerateAsync(chunk.Content);
                chunk.Embedding = embedding.Vector.ToArray();
            }

            _vectorStore[chunk.Id] = (chunk, chunk.Embedding);
        }

        _logger.LogInformation("✅ [VectorSearch] 索引更新: {Count} 条", chunks.Count());
    }

    /// <summary>
    /// 删除文档的所有块
    /// </summary>
    public void RemoveDocument(string documentId)
    {
        var keysToRemove = _vectorStore
            .Where(kv => kv.Value.Chunk.DocumentId == documentId)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _vectorStore.TryRemove(key, out _);
        }

        _logger.LogInformation("🗑️ [VectorSearch] 已删除文档 {DocId} 的 {Count} 个块", documentId, keysToRemove.Count);
    }

    /// <summary>
    /// 计算余弦相似度
    /// </summary>
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        double dotProduct = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator == 0 ? 0 : dotProduct / denominator;
    }

    /// <summary>
    /// 获取索引统计
    /// </summary>
    public (int TotalChunks, int TotalDocuments) GetStats()
    {
        var docCount = _vectorStore.Values.Select(v => v.Chunk.DocumentId).Distinct().Count();
        return (_vectorStore.Count, docCount);
    }
}
