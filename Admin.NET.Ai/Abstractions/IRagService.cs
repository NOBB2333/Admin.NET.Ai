namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// RAG 检索结果
/// </summary>
public record RagSearchResult(
    IReadOnlyList<RagDocument> Documents,
    TimeSpan ElapsedTime
);

/// <summary>
/// RAG 文档
/// </summary>
public record RagDocument(
    string Content,
    double Score = 0,
    string? Source = null,
    IDictionary<string, object>? Metadata = null
);

// 注意: RagSearchOptions 定义在 Admin.NET.Ai.Options 命名空间

/// <summary>
/// RAG 服务接口 (向量检索)
/// </summary>
public interface IRagService
{
    /// <summary>
    /// 检索相关文档
    /// </summary>
    /// <param name="query">查询语句</param>
    /// <param name="options">检索选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>检索结果</returns>
    Task<RagSearchResult> SearchAsync(
        string query, 
        Admin.NET.Ai.Options.RagSearchOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 索引文档
    /// </summary>
    /// <param name="documents">文档列表</param>
    /// <param name="collection">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task IndexAsync(
        IEnumerable<RagDocument> documents, 
        string? collection = null, 
        CancellationToken cancellationToken = default);
}
