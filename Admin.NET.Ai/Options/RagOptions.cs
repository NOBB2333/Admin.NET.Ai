namespace Admin.NET.Ai.Options;

/// <summary>
/// RAG 策略枚举 (21种常见策略)
/// </summary>
public enum RagStrategy
{
    Auto = 0,
    Naive = 1,              // 朴素 RAG
    Advanced = 2,           // 高级 RAG (Pre-retrieval, Post-retrieval)
    Modular = 3,            // 模块化 RAG
    SentenceWindow = 4,     // 句子窗口检索
    ParentDocument = 5,     // 父文档检索
    Hierarchical = 6,       // 层级索引
    Hypothetical = 7,       // 假设性文档嵌入 (HyDE)
    Rewrite = 8,            // 查询重写
    SubQuery = 9,           // 子查询分解
    StepBack = 10,          // 后退一步提示 (Step-back Prompting)
    Fusion = 11,            // 融合检索 (RAG-Fusion)
    MultiQuery = 12,        // 多路查询
    ContextualCompression = 13, // 上下文压缩
    ReRank = 14,            // 重排序
    Graph = 15,             // 图谱增强 (GraphRAG)
    Hybrid = 16,            // 混合检索 (Vector + Keyword + Graph)
    SelfRAG = 17,           // 自我反思 RAG
    Corrective = 18,        // 纠正性 RAG (CRAG)
    Adaptive = 19,          // 自适应 RAG
    Agentic = 20            // Agent 驱动 RAG
}

/// <summary>
/// RAG 搜索选项
/// </summary>
public class RagSearchOptions
{
    /// <summary>
    /// RAG 策略
    /// </summary>
    public RagStrategy Strategy { get; set; } = RagStrategy.Auto;

    /// <summary>
    /// 返回数量 (TopK)
    /// </summary>
    public int TopK { get; set; } = 3;

    /// <summary>
    /// 相似度/置信度阈值
    /// </summary>
    public double ScoreThreshold { get; set; } = 0.5;

    /// <summary>
    /// 是否启用重排序
    /// </summary>
    public bool EnableRerank { get; set; } = true;

    /// <summary>
    /// 重排序模型名称
    /// </summary>
    public string? RerankModel { get; set; }

    /// <summary>
    /// 集合/索引名称
    /// </summary>
    public string? CollectionName { get; set; }
}

/// <summary>
/// Graph RAG 检索选项 (扩展 RagSearchOptions)
/// 对应配置: LLMAgent.Rag.json -> LLM-GraphRag -> Query
/// </summary>
public class GraphRagSearchOptions : RagSearchOptions
{
    /// <summary>
    /// 图谱遍历最大跳数
    /// 对应配置: LLM-GraphRag.Query.MaxDepth
    /// </summary>
    public int MaxHops { get; set; } = 2;
    
    /// <summary>
    /// 是否包含关系信息
    /// 对应配置: LLM-GraphRag.Query.ExpandRelations
    /// </summary>
    public bool IncludeRelations { get; set; } = true;
    
    /// <summary>
    /// 是否启用混合融合检索
    /// 对应配置: LLM-GraphRag.Query.HybridFusion
    /// </summary>
    public bool HybridFusion { get; set; } = true;
}
