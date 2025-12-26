using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 缓存响应结果
/// </summary>
public record CachedResponse(
    string Query,
    string Response,
    string[] Keywords,
    DateTime CachedAt,
    double Similarity = 1.0
);

/// <summary>
/// 语义缓存接口
/// 支持向量相似度检索和关键词相似度检索
/// </summary>
public interface ISemanticCache
{
    #region 向量检索（需要 Embedding 模型）

    /// <summary>
    /// 查找相似的已缓存响应（向量版）
    /// </summary>
    /// <param name="embedding">当前请求的向量嵌入</param>
    /// <param name="threshold">相似度阈值 (0-1)</param>
    /// <returns>缓存的响应内容，如果没有找到相似项则返回 null</returns>
    Task<ChatResponse?> GetSimilarAsync(ReadOnlyMemory<float> embedding, double threshold = 0.8, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存响应到语义缓存（向量版）
    /// </summary>
    /// <param name="embedding">请求向量</param>
    /// <param name="response">响应内容</param>
    Task SetAsync(ReadOnlyMemory<float> embedding, ChatResponse response, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    #endregion

    #region 关键词检索（无需外部依赖）

    /// <summary>
    /// 查找语义相似的缓存响应（关键词版）
    /// </summary>
    /// <param name="query">用户查询文本</param>
    /// <param name="threshold">相似度阈值 (0-1)</param>
    /// <returns>匹配的缓存响应，如果未找到则返回 null</returns>
    Task<CachedResponse?> FindSimilarAsync(
        string query, 
        double threshold = 0.85, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加响应到语义缓存（关键词版）
    /// </summary>
    /// <param name="query">原始查询</param>
    /// <param name="response">响应内容</param>
    /// <param name="keywords">提取的关键词（用于相似度匹配）</param>
    /// <param name="expiration">过期时间</param>
    Task AddAsync(
        string query, 
        string response, 
        string[]? keywords = null,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    #endregion

    /// <summary>
    /// 清除所有语义缓存
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
