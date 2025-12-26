using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Services.Rag;

/// <summary>
/// RAG 服务接口 (Generic)
/// </summary>
public interface IRagService
{
    /// <summary>
    /// 检索相关文档
    /// </summary>
    Task<IEnumerable<string>> SearchAsync(string query, string collectionName, int topK = 3);
    
    /// <summary>
    /// 索引文档
    /// </summary>
    Task IndexAsync(string content, string collectionName, Dictionary<string, object>? metadata = null);
}

public class RagService : IRagService
{
    public Task<IEnumerable<string>> SearchAsync(string query, string collectionName, int topK = 3) => Task.FromResult(Enumerable.Empty<string>());
    public Task IndexAsync(string content, string collectionName, Dictionary<string, object>? metadata = null) => Task.CompletedTask;
}
