using Admin.NET.Ai.Options;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// Graph RAG 服务接口
/// </summary>
public interface IGraphRagService
{
    /// <summary>
    /// 搜索相关知识 (混合检索：向量 + 图谱)
    /// </summary>
    /// <param name="query">查询语句</param>
    /// <param name="options">搜索选项</param>
    /// <returns>相关文本片段</returns>
    Task<List<string>> SearchAsync(string query, RagSearchOptions? options = null);

    /// <summary>
    /// 插入知识 (构建图谱)
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <returns></returns>
    Task InsertAsync(string text);
}
