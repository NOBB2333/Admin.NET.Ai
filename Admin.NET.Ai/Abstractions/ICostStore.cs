namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 成本存储接口
/// </summary>
public interface ICostStore
{
    /// <summary>
    /// 保存成本记录
    /// </summary>
    /// <param name="requestId">请求ID</param>
    /// <param name="inputTokens">输入Token</param>
    /// <param name="outputTokens">输出Token</param>
    /// <param name="model">模型名称</param>
    /// <param name="additionalData">其他数据</param>
    /// <returns></returns>
    Task SaveCostAsync(string requestId, int inputTokens, int outputTokens, string model, IDictionary<string, object?>? additionalData = null);
}
