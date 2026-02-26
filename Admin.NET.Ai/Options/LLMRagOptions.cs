namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM RAG 配置
/// </summary>
public sealed class LLMRagConfig
{
    /// <summary> 向量数据库配置 </summary>
    public VectorDatabaseConfig VectorDatabase { get; set; } = new();

    /// <summary> 嵌入模型配置 </summary>
    public EmbeddingConfig Embedding { get; set; } = new();

    /// <summary> 检索配置 </summary>
    public RetrievalConfig Retrieval { get; set; } = new();

    /// <summary> 重排序配置 </summary>
    public RerankConfig Rerank { get; set; } = new();
}

/// <summary> 
/// 向量数据库配置 
/// </summary>
public sealed class VectorDatabaseConfig
{
    /// <summary> 向量数据库类型: Chroma, Qdrant, Milvus, FAISS </summary>
    public string? Type { get; set; }

    /// <summary> 连接字符串 </summary>
    public string? ConnectionString { get; set; }

    /// <summary> 集合名称 </summary>
    public string? CollectionName { get; set; }
}

/// <summary> 
/// 嵌入模型配置
/// </summary>
public sealed class EmbeddingConfig
{
    /// <summary> 嵌入提供商: AliyunBailian, OpenAI, Local </summary>
    public string? Provider { get; set; }

    /// <summary> 模型名称 </summary>
    public string? Model { get; set; }

    /// <summary> 向量维度 </summary>
    public int Dimension { get; set; } = 1024;

    /// <summary> API Key (可选 — 为空时从 LLM-Clients 中的同名 Provider 读取) </summary>
    public string? ApiKey { get; set; }

    /// <summary> Base URL (可选 — 为空时从 LLM-Clients 中的同名 Provider 读取) </summary>
    public string? BaseUrl { get; set; }
}

/// <summary> 
/// 检索配置
/// </summary>
public sealed class RetrievalConfig
{
    /// <summary> 返回的最相关文档数量 </summary>
    public int TopK { get; set; } = 5;

    /// <summary> 相似度阈值 </summary>
    public double SimilarityThreshold { get; set; } = 0.7;

    /// <summary> 文档分块大小 </summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary> 分块重叠大小 </summary>
    public int ChunkOverlap { get; set; } = 200;
}

/// <summary>
/// 重排序配置
/// </summary>
public sealed class RerankConfig
{
    /// <summary> 重排序模型提供商 </summary>
    public string? Provider { get; set; }

    /// <summary> 重排序模型 </summary>
    public string? Model { get; set; }

    /// <summary> 重新排序后的返回数量 </summary>
    public int TopK { get; set; } = 3;

    /// <summary> 置信度阈值 </summary>
    public double ScoreThreshold { get; set; } = 0.2;
}
