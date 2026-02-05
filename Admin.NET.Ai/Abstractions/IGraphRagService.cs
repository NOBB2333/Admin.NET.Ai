namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// Graph RAG 服务接口 (继承 IRagService，扩展图谱检索能力)
/// </summary>
public interface IGraphRagService : IRagService
{
    /// <summary>
    /// 图谱增强检索 (向量 + 知识图谱)
    /// </summary>
    /// <param name="query">查询语句</param>
    /// <param name="options">图谱检索选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>检索结果 (包含图谱关联信息)</returns>
    Task<RagSearchResult> GraphSearchAsync(
        string query, 
        Admin.NET.Ai.Options.GraphRagSearchOptions? options = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 构建知识图谱
    /// </summary>
    /// <param name="documents">文档列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BuildGraphAsync(
        IEnumerable<RagDocument> documents, 
        CancellationToken cancellationToken = default);
}
