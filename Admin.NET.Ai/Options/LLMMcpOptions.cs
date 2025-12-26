using Furion.ConfigurableOptions;

namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM MCP 服务器配置
/// </summary>
[OptionsSettings("LLM-Mcp")]
public sealed class LLMMcpConfig : IConfigurableOptions
{
    /// <summary> MCP 服务器列表 </summary>
    public List<McpServerConfig> Servers { get; set; } = new();
}

/// <summary> MCP 服务器配置 </summary>
public sealed class McpServerConfig
{
    /// <summary> 服务器名称 </summary>
    public string? Name { get; set; }

    /// <summary> 服务器类型 </summary>
    public string? Type { get; set; }

    /// <summary> 是否启用 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 服务器地址 </summary>
    public string? Url { get; set; }

    /// <summary> 配置信息 </summary>
    public Dictionary<string, object> Config { get; set; } = new();

    /// <summary> 请求头信息 </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}
