# 04. RAG 与 GraphRAG 混合检索

## 🎯 设计思维 (Mental Model)
传统向量检索 (Vector RAG) 的局限性在于：它只能通过片段相似度匹配，丢失了实体间的语义逻辑。例如，“张三是李四的哥哥”，在向量空间中这两点可能很近，但 LLM 很难直接推断出“李四是张三的弟弟”这种关系型知识。

`Admin.NET.Ai` 引入了 **混合 RAG** 架构：
1.  **Vector RAG**: 负责处理非结构化文本的模糊匹配。
2.  **GraphRAG (Neo4j)**: 负责存储和检索实体、关系、属性的图谱。

---

## 🏗️ 架构设计
### 核心组件
- **`IRagService`**: 基础 RAG 服务，管理向量索引的注入与检索。
- **`IGraphRagService`**: 专门负责图数据库 (Neo4j) 的操作。
- **`RagStrategyFactory`**: 内置了 21 种 RAG 策略，可根据查询复杂度自动选择（如：Simple, HyDE, Multi-hop）。

---

## 🛠️ 技术实现 (Implementation)

### 1. 依赖库
- `Neo4j.Driver`: 连接 Neo4j 图数据库。
- `Microsoft.SemanticKernel.Connectors.Memory.*`: 提供向量数据库（如 Chroma, Qdrant）的支持。

### 2. GraphRAG 实现细节 (`Services/Rag/GraphRagService.cs`)
系统利用 Cypher 语言对图数据进行深度遍历。

```csharp
public async Task<List<string>> SearchAsync(string query, RagSearchOptions? options = null)
{
    // 1. 检查配置是否启用 Neo4j
    var neo4jConfig = _options.LLMGraphRag.GraphDatabase;
    
    // 2. 建立会话并运行 Cypher 查询
    await using var session = _driver.AsyncSession();
    
    // 示例代码：查找包含关键词的文档节点
    // 在高级实现中，这会包含 "3跳" 路径查询 (Match (n)-[*1..3]-(m))
    var cypher = "MATCH (n:Document) WHERE n.content CONTAINS $query RETURN n.content AS content LIMIT 5";
    var cursor = await session.RunAsync(cypher, new { query });
    
    return await cursor.ToListAsync(record => record["content"].As<string>());
}
```

### 3. 重排 (Rerank)
检索出来的结果往往包含噪音。管道支持接入 `BGE-Reranker` 等模型，对召回的结果进行二次精细化打分排序。

---

## 🚀 代码示例 (Usage Example)

### 基础 RAG 检索
```csharp
// 注入 IRagService
var ragService = serviceProvider.GetRequiredService<IRagService>();

// 执行检索 (内部自动根据策略选择 Vector 还是 Graph)
var contexts = await ragService.RetrieveContextAsync("Admin.NET 的作者是谁？");
```

### GraphRAG 数据注入
```csharp
// 插入一段事实
await graphRagService.InsertAsync("Admin.NET 开源项目的核心贡献者包括 zhangsan 等人。");
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
    "Search": {
      "Strategy": "MultiHop",
      "MaxNodes": 20,
      "Depth": 3
    }
  }
}
```

---

## 💡 RAG 策略列表 (Partial)
- **Simple**: 基础 TopK 检索。
- **HyDE (Hypothetical Document Embeddings)**: 通过模型先生成伪答案，再用伪答案查库，大幅提升中英文语义匹配准确度。
- **GraphAnalysis**: 利用 Neo4j 的社区发现算法分析全局知识。
