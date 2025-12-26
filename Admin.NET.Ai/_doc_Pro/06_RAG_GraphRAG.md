# RAG 知识检索 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `IRagService.cs` | `Abstractions/` | RAG 服务接口 |
| `IGraphRagService.cs` | `Abstractions/` | GraphRAG 接口 |
| `RagService.cs` | `Services/Rag/` | 向量 RAG 实现 |
| `GraphRagService.cs` | `Services/Rag/` | Neo4j GraphRAG |
| `DocumentChunker.cs` | `Services/Rag/` | 文档分块 |
| `HybridReranker.cs` | `Services/Rag/` | 混合重排 |
| `RagStrategyFactory.cs` | `Services/Rag/` | 策略工厂 |
| `RagDemo.cs` | `Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 接口定义

```csharp
public interface IRagService
{
    Task<List<RetrievalResult>> RetrieveAsync(string query, RetrievalOptions? options = null);
    Task IndexDocumentAsync(string documentId, string content, Dictionary<string, object>? metadata = null);
}

public interface IGraphRagService
{
    Task<List<GraphRetrievalResult>> RetrieveWithRelationsAsync(string query, int depth = 2);
    Task BuildKnowledgeGraphAsync(string documentContent, string documentId);
}
```

### 检索流程

```
Query → [Embedding] → [Vector DB 检索] → [Rerank] → Results
           ↓
      [GraphRAG 检索] → [关系扩展] ─┘
```

---

## 🔧 核心实现

### 1. 文档分块 (DocumentChunker)

```csharp
public class DocumentChunker : IDocumentChunker
{
    private readonly ChunkerOptions _options;
    
    public List<DocumentChunk> Chunk(string content, ChunkerOptions? options = null)
    {
        var opts = options ?? _options;
        var chunks = new List<DocumentChunk>();
        
        // 1. 按段落分割
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        // 2. 滑动窗口合并
        var currentChunk = new StringBuilder();
        foreach (var para in paragraphs)
        {
            if (currentChunk.Length + para.Length > opts.MaxChunkSize)
            {
                chunks.Add(new DocumentChunk { Content = currentChunk.ToString() });
                currentChunk.Clear();
                
                // 保留重叠部分
                if (opts.OverlapSize > 0)
                {
                    currentChunk.Append(para.Substring(0, Math.Min(opts.OverlapSize, para.Length)));
                }
            }
            currentChunk.AppendLine(para);
        }
        
        if (currentChunk.Length > 0)
        {
            chunks.Add(new DocumentChunk { Content = currentChunk.ToString() });
        }
        
        return chunks;
    }
}
```

### 2. 向量检索 (RagService)

```csharp
public class RagService : IRagService
{
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IVectorStore _vectorStore;
    
    public async Task<List<RetrievalResult>> RetrieveAsync(string query, RetrievalOptions? options = null)
    {
        // 1. 生成查询向量
        var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query);
        
        // 2. 向量相似度搜索
        var results = await _vectorStore.SearchAsync(
            queryEmbedding.Vector, 
            topK: options?.TopK ?? 5,
            threshold: options?.MinScore ?? 0.7f);
        
        return results.Select(r => new RetrievalResult
        {
            Content = r.Content,
            Score = r.Score,
            Metadata = r.Metadata
        }).ToList();
    }
    
    public async Task IndexDocumentAsync(string documentId, string content, Dictionary<string, object>? metadata = null)
    {
        // 1. 分块
        var chunks = _chunker.Chunk(content);
        
        // 2. 生成向量并存储
        foreach (var chunk in chunks)
        {
            var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(chunk.Content);
            await _vectorStore.UpsertAsync(new VectorRecord
            {
                Id = $"{documentId}_{chunk.Index}",
                Vector = embedding.Vector,
                Content = chunk.Content,
                Metadata = metadata
            });
        }
    }
}
```

### 3. GraphRAG (Neo4j)

```csharp
public class GraphRagService : IGraphRagService
{
    private readonly IDriver _neo4jDriver;
    private readonly IChatClient _llmClient;
    
    public async Task<List<GraphRetrievalResult>> RetrieveWithRelationsAsync(string query, int depth = 2)
    {
        // 1. 提取查询中的实体
        var entities = await ExtractEntitiesAsync(query);
        
        // 2. 图查询 - N 层关系探索
        var cypher = @"
            MATCH (e:Entity)-[r*1..{depth}]-(related)
            WHERE e.name IN $entities
            RETURN e, r, related
            LIMIT 50";
        
        await using var session = _neo4jDriver.AsyncSession();
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(cypher, new { entities, depth });
            return await cursor.ToListAsync();
        });
        
        // 3. 构建知识子图
        return BuildSubGraph(result);
    }
    
    public async Task BuildKnowledgeGraphAsync(string content, string documentId)
    {
        // 使用 LLM 提取三元组
        var prompt = $@"
从以下文本中提取实体和关系，以 (主体, 关系, 客体) 格式返回:
{content}";
        
        var response = await _llmClient.GetResponseAsync(prompt);
        var triples = ParseTriples(response.Text);
        
        // 写入 Neo4j
        foreach (var (subject, relation, obj) in triples)
        {
            await CreateTripleAsync(subject, relation, obj, documentId);
        }
    }
}
```

### 4. 混合重排 (HybridReranker)

```csharp
public class HybridReranker : IReranker
{
    private readonly IChatClient _rerankerModel;
    
    public async Task<List<RetrievalResult>> RerankAsync(
        string query, 
        List<RetrievalResult> candidates,
        int topK = 3)
    {
        // 1. 批量计算相关性得分
        var scores = new List<(RetrievalResult Result, double Score)>();
        
        foreach (var candidate in candidates)
        {
            var prompt = $@"
判断以下文本与查询的相关性 (0-10分):
查询: {query}
文本: {candidate.Content}
只返回数字分数:";
            
            var response = await _rerankerModel.GetResponseAsync(prompt);
            if (double.TryParse(response.Text.Trim(), out var score))
            {
                scores.Add((candidate, score));
            }
        }
        
        // 2. 按得分排序
        return scores
            .OrderByDescending(s => s.Score)
            .Take(topK)
            .Select(s => s.Result with { Score = (float)s.Score })
            .ToList();
    }
}
```

---

## 📊 策略模式

```csharp
public class RagStrategyFactory
{
    public IRagStrategy CreateStrategy(RagStrategyType type)
    {
        return type switch
        {
            RagStrategyType.VectorOnly => new VectorOnlyStrategy(_ragService),
            RagStrategyType.GraphOnly => new GraphOnlyStrategy(_graphRagService),
            RagStrategyType.Hybrid => new HybridStrategy(_ragService, _graphRagService, _reranker),
            RagStrategyType.HyDE => new HyDEStrategy(_ragService, _llmClient), // 假设文档扩展
            _ => throw new ArgumentException($"Unknown strategy: {type}")
        };
    }
}
```

---

## ⚙️ 配置

```json
{
  "LLM-Rag": {
    "VectorStore": {
      "Provider": "Qdrant",
      "Endpoint": "http://localhost:6333"
    },
    "GraphStore": {
      "Provider": "Neo4j",
      "Uri": "bolt://localhost:7687",
      "Username": "neo4j",
      "Password": "password"
    },
    "Chunker": {
      "MaxChunkSize": 500,
      "OverlapSize": 50
    },
    "Retrieval": {
      "TopK": 5,
      "MinScore": 0.7
    }
  }
}
```

---

## 🚀 使用示例

```csharp
var ragService = sp.GetRequiredService<IRagService>();

// 索引文档
await ragService.IndexDocumentAsync("doc_001", "这是文档内容...");

// 检索
var results = await ragService.RetrieveAsync("相关问题", new RetrievalOptions { TopK = 3 });
foreach (var r in results)
{
    Console.WriteLine($"[{r.Score:P0}] {r.Content}");
}
```
