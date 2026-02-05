# RAG 知识检索 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `IRagService.cs` | `Abstractions/` | RAG 服务接口 |
| `IGraphRagService.cs` | `Abstractions/` | GraphRAG 接口 (继承 IRagService) |
| `RagOptions.cs` | `Options/` | RAG 检索选项配置 |
| `GraphRagService.cs` | `Services/Rag/` | Neo4j GraphRAG 实现 |
| `RagStrategyFactory.cs` | `Services/Rag/` | 策略工厂 |
| `RagDemo.cs` | `HeMaCupAICheck/Demos/` | 演示代码 |

---

## 🏗️ 架构设计 (2026-02 更新)

### 接口定义

```csharp
// IRagService - 基础向量检索
public interface IRagService
{
    Task<RagSearchResult> SearchAsync(
        string query, 
        RagSearchOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    Task IndexAsync(
        IEnumerable<RagDocument> documents, 
        string? collection = null, 
        CancellationToken cancellationToken = default);
}

// IGraphRagService - 继承 IRagService，扩展图谱检索
public interface IGraphRagService : IRagService
{
    Task<RagSearchResult> GraphSearchAsync(
        string query, 
        GraphRagSearchOptions? options = null, 
        CancellationToken cancellationToken = default);

    Task BuildGraphAsync(
        IEnumerable<RagDocument> documents, 
        CancellationToken cancellationToken = default);
}

// 返回类型
public record RagSearchResult(
    IReadOnlyList<RagDocument> Documents,
    TimeSpan ElapsedTime
);

public record RagDocument(
    string Content,
    double Score = 0,
    string? Source = null,
    IDictionary<string, object>? Metadata = null
);
```

### 检索流程

```
Query → [Embedding] → [Vector DB 检索] → [Rerank] → RagSearchResult
           ↓
      [GraphRAG 检索] → [关系扩展] ─┘
```

---

## 🔧 核心实现

### 1. Options 配置 (`Options/RagOptions.cs`)

```csharp
// 基础选项
public class RagSearchOptions
{
    public RagStrategy Strategy { get; set; } = RagStrategy.Auto;
    public int TopK { get; set; } = 3;
    public double ScoreThreshold { get; set; } = 0.5;
    public bool EnableRerank { get; set; } = true;
    public string? RerankModel { get; set; }
    public string? CollectionName { get; set; }
}

// Graph RAG 扩展选项
public class GraphRagSearchOptions : RagSearchOptions
{
    public int MaxHops { get; set; } = 2;           // 图遍历深度
    public bool IncludeRelations { get; set; } = true; // 包含关系信息
    public bool HybridFusion { get; set; } = true;  // 混合融合检索
}
```

### 2. GraphRAG 实现 (`Services/Rag/GraphRagService.cs`)

```csharp
public class GraphRagService : IGraphRagService
{
    private readonly IDriver _driver;
    private readonly LLMAgentOptions _options;
    
    // 基础向量检索
    public async Task<RagSearchResult> SearchAsync(
        string query, 
        RagSearchOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        options ??= new RagSearchOptions();
        
        await using var session = _driver.AsyncSession();
        var cypher = "MATCH (n:Document) WHERE toLower(n.content) CONTAINS toLower($query) RETURN n.content LIMIT $limit";
        var cursor = await session.RunAsync(cypher, new { query, limit = options.TopK });
        
        var results = (await cursor.ToListAsync())
            .Select(r => new RagDocument(r["content"].As<string>(), 1.0, "Neo4j"))
            .ToList();
        
        sw.Stop();
        return new RagSearchResult(results, sw.Elapsed);
    }
    
    // 图谱增强检索
    public async Task<RagSearchResult> GraphSearchAsync(
        string query, 
        GraphRagSearchOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        options ??= new GraphRagSearchOptions();
        
        await using var session = _driver.AsyncSession();
        var cypher = @"
            MATCH (n:Document)-[r*1..$maxHops]-(related)
            WHERE toLower(n.content) CONTAINS toLower($query)
            RETURN n.content AS content, collect(DISTINCT related.content) AS relatedContents
            LIMIT $limit";
        
        var cursor = await session.RunAsync(cypher, 
            new { query, maxHops = options.MaxHops, limit = options.TopK });
        
        var results = new List<RagDocument>();
        await foreach (var record in cursor)
        {
            results.Add(new RagDocument(
                Content: record["content"].As<string>(),
                Score: 1.0,
                Source: "Neo4j-Graph",
                Metadata: options.IncludeRelations 
                    ? new Dictionary<string, object> { ["RelatedContents"] = record["relatedContents"].As<List<string>>() } 
                    : null
            ));
        }
        
        sw.Stop();
        return new RagSearchResult(results, sw.Elapsed);
    }
    
    // 索引文档
    public async Task IndexAsync(
        IEnumerable<RagDocument> documents, 
        string? collection = null, 
        CancellationToken cancellationToken = default)
    {
        await using var session = _driver.AsyncSession();
        foreach (var doc in documents)
        {
            await session.RunAsync(
                "CREATE (n:Document {content: $content, source: $source})", 
                new { content = doc.Content, source = doc.Source ?? "unknown" });
        }
    }
    
    // 构建知识图谱
    public async Task BuildGraphAsync(
        IEnumerable<RagDocument> documents, 
        CancellationToken cancellationToken = default)
    {
        await IndexAsync(documents, null, cancellationToken);
    }
}
```

---

## 📊 策略模式

```csharp
public enum RagStrategy
{
    Auto = 0,
    Naive = 1,              // 朴素 RAG
    Advanced = 2,           // 高级 RAG
    SentenceWindow = 4,     // 句子窗口检索
    Hypothetical = 7,       // HyDE
    Graph = 15,             // 图谱增强
    Hybrid = 16,            // 混合检索
    Agentic = 20            // Agent 驱动
}
```

---

## ⚙️ 配置 (`LLMAgent.Rag.json`)

```json
{
  "LLM-Rag": {
    "VectorStore": { "Provider": "Qdrant", "Endpoint": "http://localhost:6333" },
    "Retrieval": { "TopK": 5, "MinScore": 0.7 }
  },
  "LLMGraphRag": {
    "GraphDatabase": {
      "Type": "Neo4j",
      "ConnectionString": "bolt://localhost:7687",
      "Username": "neo4j",
      "Password": "password"
    },
    "Query": {
      "MaxDepth": 2,
      "ExpandRelations": true,
      "HybridFusion": true
    }
  }
}
```

---

## 🚀 使用示例

```csharp
var ragService = sp.GetRequiredService<IGraphRagService>();

// 索引文档
await ragService.IndexAsync([
    new RagDocument("Admin.NET.Ai 是一个 .NET AI 开发框架"),
    new RagDocument("GraphRAG 结合了知识图谱和向量检索")
]);

// 基础检索
var result = await ragService.SearchAsync("Admin.NET", new RagSearchOptions { TopK = 3 });
Console.WriteLine($"检索到 {result.Documents.Count} 条，耗时 {result.ElapsedTime.TotalMilliseconds:F0}ms");

// 图谱检索
var graphResult = await ragService.GraphSearchAsync("Admin.NET 的作者", new GraphRagSearchOptions
{
    MaxHops = 2,
    IncludeRelations = true
});

foreach (var doc in graphResult.Documents)
{
    Console.WriteLine($"[{doc.Score:F2}] {doc.Content}");
}
```
