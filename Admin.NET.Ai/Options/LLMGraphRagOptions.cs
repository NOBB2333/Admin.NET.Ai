namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM Graph RAG 配置
/// </summary>
public sealed class LLMGraphRagConfig
{
    /// <summary> 图数据库配置 </summary>
    public GraphDatabaseConfig GraphDatabase { get; set; } = new();

    /// <summary> 图谱模式配置 </summary>
    public GraphSchemaConfig Schema { get; set; } = new();

    /// <summary> 图谱数据抽取配置 </summary>
    public GraphIngestionConfig Ingestion { get; set; } = new();

    /// <summary> 图谱查询配置 </summary>
    public GraphQueryConfig Query { get; set; } = new();
}

/// <summary>
/// 图数据库配置
/// </summary>
public sealed class GraphDatabaseConfig
{
    /// <summary> 图数据库类型: Neo4j, ArangoDB, NebulaGraph </summary>
    public string? Type { get; set; }

    /// <summary> 连接字符串 </summary>
    public string? ConnectionString { get; set; }

    /// <summary> 登录用户名 </summary>
    public string? Username { get; set; }

    /// <summary> 登录密码 </summary>
    public string? Password { get; set; }
}

/// <summary>
/// 图谱模式配置
/// </summary>
public sealed class GraphSchemaConfig
{
    /// <summary> 图谱中的节点类型枚举 </summary>
    public List<string> NodeTypes { get; set; } = new();

    /// <summary> 节点之间的关系类型 </summary>
    public List<string> RelationTypes { get; set; } = new();
}

/// <summary>
/// 图谱数据抽取配置
/// </summary>
public sealed class GraphIngestionConfig
{
    /// <summary> 解析文档并抽取节点/关系的模型 </summary>
    public string? LLMProvider { get; set; }

    /// <summary> 图谱抽取提示词模板 </summary>
    public string? PromptTemplate { get; set; }

    /// <summary> 单批次处理的文档数量 </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary> 最大并发任务数 </summary>
    public int MaxConcurrency { get; set; } = 4;
}

/// <summary>
/// 图谱查询配置
/// </summary>
public sealed class GraphQueryConfig
{
    /// <summary> 查询时沿关系展开的最大深度 </summary>
    public int MaxDepth { get; set; } = 3;

    /// <summary> 是否自动展开图谱关系 </summary>
    public bool ExpandRelations { get; set; } = true;

    /// <summary> 图谱与向量混合召回 </summary>
    public bool HybridFusion { get; set; } = true;

    /// <summary> 汇总回答所用模型 </summary>
    public string? AnswerSynthesisModel { get; set; }
}
