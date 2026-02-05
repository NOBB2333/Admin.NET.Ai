# 04. RAG 与 GraphRAG 混合检索

## 🎯 设计思维 (Mental Model)
传统向量检索 (Vector RAG) 的局限性在于：它只能通过片段相似度匹配，丢失了实体间的语义逻辑。例如，"张三是李四的哥哥"，在向量空间中这两点可能很近，但 LLM 很难直接推断出"李四是张三的弟弟"这种关系型知识。

`Admin.NET.Ai` 引入了 **混合 RAG** 架构：
1.  **Vector RAG**: 负责处理非结构化文本的模糊匹配。
2.  **GraphRAG (Neo4j)**: 负责存储和检索实体、关系、属性的图谱。

---

## 🏗️ 架构设计
### 核心组件
- **`IRagService`**: 基础 RAG 服务接口 - 向量检索 + 索引
- **`IGraphRagService`**: 继承 `IRagService`，扩展图谱检索能力
- **`RagStrategyFactory`**: 内置 21 种 RAG 策略

### 核心接口 (2026-02 更新)

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

---

## 🛠️ 技术实现 (Implementation)

### Options 配置类 (`Options/RagOptions.cs`)

```csharp
// 基础 RAG 选项
public class RagSearchOptions
{
    public RagStrategy Strategy { get; set; } = RagStrategy.Auto;
    public int TopK { get; set; } = 3;
    public double ScoreThreshold { get; set; } = 0.5;
    public bool EnableRerank { get; set; } = true;
    public string? RerankModel { get; set; }
    public string? CollectionName { get; set; }
}

// Graph RAG 扩展选项 (继承 RagSearchOptions)
public class GraphRagSearchOptions : RagSearchOptions
{
    public int MaxHops { get; set; } = 2;
    public bool IncludeRelations { get; set; } = true;
    public bool HybridFusion { get; set; } = true;
}
```

---

## 🚀 代码示例 (Usage Example)

### 基础 RAG 检索
```csharp
var ragService = sp.GetRequiredService<IRagService>();

// 索引文档
await ragService.IndexAsync([
    new RagDocument("Admin.NET.Ai 是一个 .NET AI 开发框架"),
    new RagDocument("GraphRAG 结合了知识图谱和向量检索")
]);

// 执行检索
var result = await ragService.SearchAsync("Admin.NET 是什么?");
foreach (var doc in result.Documents)
{
    Console.WriteLine($"[{doc.Score:F2}] {doc.Content}");
}
```

### GraphRAG 图谱检索
```csharp
var graphRagService = sp.GetRequiredService<IGraphRagService>();

// 图谱增强检索 (自动关联相关实体)
var result = await graphRagService.GraphSearchAsync("Admin.NET 的作者", new GraphRagSearchOptions
{
    MaxHops = 2,
    IncludeRelations = true
});

foreach (var doc in result.Documents)
{
    Console.WriteLine($"[{doc.Score:F2}] {doc.Content}");
    if (doc.Metadata?.TryGetValue("RelatedContents", out var related) == true)
    {
        Console.WriteLine($"  └─ Related: {related}");
    }
}
```

---

## ⚙️ 模型配置 (`LLMAgent.Rag.json`)
```json
{
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

## 💡 RAG 策略列表 (21 种)
| 策略 | 说明 |
|------|------|
| Naive | 朴素 RAG (TopK 检索) |
| Advanced | 高级 RAG (Pre/Post-retrieval) |
| SentenceWindow | 句子窗口检索 |
| HyDE | 假设性文档嵌入 |
| Graph | 图谱增强 (GraphRAG) |
| Hybrid | 混合检索 (Vector + Keyword + Graph) |
| ReRank | 重排序 |
| Agentic | Agent 驱动 RAG |
