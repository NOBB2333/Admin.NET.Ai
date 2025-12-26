namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 文档分块器接口
/// </summary>
public interface IDocumentChunker
{
    /// <summary>
    /// 将文档分割成块
    /// </summary>
    IEnumerable<DocumentChunk> ChunkDocument(string content, ChunkingOptions? options = null);
    
    /// <summary>
    /// 批量分块
    /// </summary>
    IEnumerable<DocumentChunk> ChunkDocuments(IEnumerable<RawDocument> documents, ChunkingOptions? options = null);
}

/// <summary>
/// 分块选项
/// </summary>
public class ChunkingOptions
{
    /// <summary>
    /// 分块策略
    /// </summary>
    public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.FixedSize;
    
    /// <summary>
    /// 每块最大字符数
    /// </summary>
    public int MaxChunkSize { get; set; } = 500;
    
    /// <summary>
    /// 块之间的重叠字符数
    /// </summary>
    public int Overlap { get; set; } = 50;
    
    /// <summary>
    /// 是否保留元数据
    /// </summary>
    public bool PreserveMetadata { get; set; } = true;
}

/// <summary>
/// 分块策略
/// </summary>
public enum ChunkingStrategy
{
    /// <summary>
    /// 固定大小分块
    /// </summary>
    FixedSize,
    
    /// <summary>
    /// 按句子边界分块
    /// </summary>
    SentenceBoundary,
    
    /// <summary>
    /// 按段落分块
    /// </summary>
    Paragraph,
    
    /// <summary>
    /// 语义分块 (需要 Embedding 模型)
    /// </summary>
    Semantic
}

/// <summary>
/// 原始文档
/// </summary>
public class RawDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = "";
    public string? SourceName { get; set; }
    public string? SourceUri { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 文档块
/// </summary>
public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = "";
    public string Content { get; set; } = "";
    public int ChunkIndex { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    
    /// <summary>
    /// 来源信息
    /// </summary>
    public string? SourceName { get; set; }
    public string? SourceUri { get; set; }
    
    /// <summary>
    /// 继承的元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// 向量嵌入 (可选，由 VectorStore 填充)
    /// </summary>
    public float[]? Embedding { get; set; }
}
