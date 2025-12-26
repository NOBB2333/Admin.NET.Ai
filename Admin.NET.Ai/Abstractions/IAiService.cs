namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// AI 服务核心接口
/// </summary>
public interface IAiService
{
    /// <summary>
    /// 执行 AI 请求
    /// </summary>
    /// <param name="prompt">用户输入</param>
    /// <param name="options">可选参数</param>
    /// <returns>AI 响应结果</returns>
    Task<object?> ExecuteAsync(string prompt, Dictionary<string, object?>? options = null);

    /// <summary>
    /// 执行 AI 请求 (泛型返回)
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="prompt"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<TResult?> ExecuteAsync<TResult>(string prompt, Dictionary<string, object?>? options = null);

    /// <summary>
    /// 执行 AI 请求 (流式返回)
    /// </summary>
    /// <param name="prompt">用户输入</param>
    /// <param name="options">可选参数</param>
    /// <returns>流式结果</returns>
    IAsyncEnumerable<string> ExecuteStreamAsync(string prompt, Dictionary<string, object?>? options = null);
}
