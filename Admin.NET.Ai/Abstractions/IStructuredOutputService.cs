using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 结构化输出服务接口 (4.0)
/// </summary>
public interface IStructuredOutputService
{
    /// <summary>
    /// 解析文本为强类型对象
    /// </summary>
    T? Parse<T>(string text);

    /// <summary>
    /// 为指定类型生成 JSON Schema
    /// </summary>
    string GenerateJsonSchema(Type type, string description = "");

    /// <summary>
    /// 为指定类型配置 ChatOptions (自动适配不同模型的 Schema 策略)
    /// </summary>
    ChatOptions CreateOptions<T>(string provider, string? instructions = null);

    /// <summary>
    /// 尝试解析，失败时自动调用 LLM 进行修复 (自愈)
    /// </summary>
    Task<T?> ParseWithRetryAsync<T>(string text, Func<string, Task<string>> fixCallback, int maxRetries = 2);
}
