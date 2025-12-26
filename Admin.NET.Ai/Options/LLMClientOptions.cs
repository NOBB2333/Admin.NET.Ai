using Furion.ConfigurableOptions;

namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM 客户端配置
/// </summary>
[OptionsSettings("LLM-Clients")]
public sealed class LLMClientsConfig : IConfigurableOptions
{
    /// <summary> 默认提供商 </summary>
    public string? DefaultProvider { get; set; }

    /// <summary> LLM 客户端配置字典 </summary>
    public Dictionary<string, LLMClientConfig> Clients { get; set; } = new();
}

/// <summary>
/// LLM 客户端配置
/// </summary>
public sealed class LLMClientConfig
{
    /// <summary> API 密钥 </summary>
    public string? ApiKey { get; set; }

    /// <summary> 模型 ID </summary>
    public string? ModelId { get; set; }

    /// <summary> API 基础地址 </summary>
    public string? BaseUrl { get; set; }

    /// <summary> 超时时间（秒） </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary> 最大重试次数 </summary>
    public int MaxRetryCount { get; set; } = 2;

    /// <summary> 启用流式传输 </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary> 提供商类型 (e.g., Azure, OpenAI, DeepSeek) </summary>
    public string? Provider { get; set; }

    /// <summary> 输入价格（元/百万 tokens） </summary>
    public decimal InPrice { get; set; }

    /// <summary> 输出价格（元/百万 tokens） </summary>
    public decimal OutPrice { get; set; }
}
