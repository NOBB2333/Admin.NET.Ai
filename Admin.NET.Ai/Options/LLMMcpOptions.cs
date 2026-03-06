using Furion.ConfigurableOptions;

namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM MCP 服务器配置
/// </summary>
[OptionsSettings("LLM-Mcp")]
public sealed class LLMMcpConfig : IConfigurableOptions
{
    /// <summary> MCP 服务器映射（key 为服务器标识） </summary>
    public Dictionary<string, McpServerConfig> Servers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// MCP 服务器配置
/// </summary>
public sealed class McpServerConfig
{
    /// <summary> 服务器名称 </summary>
    public string? Name { get; set; }

    /// <summary> 服务器描述 </summary>
    public string? Description { get; set; }

    /// <summary> 是否启用 </summary>
    public bool Enable { get; set; } = true;

    /// <summary>
    /// 传输类型: "stdio" (默认) 或 "http"/"sse"
    /// </summary>
    public string TransportType { get; set; } = "stdio";

    /// <summary>
    /// 启动命令 (Stdio 模式)
    /// 例如: "dotnet", "node", "python", "dnx"
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// 命令参数 (Stdio 模式)
    /// 例如: ["run", "--project", "MyServer"]
    /// </summary>
    public string[] Arguments { get; set; } = [];

    /// <summary> 服务地址（http/sse） </summary>
    public string? BaseUrl { get; set; }

    /// <summary> 请求头 </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary> API 密钥（可选） </summary>
    public string? ApiKey { get; set; }

    /// <summary> 额外配置 </summary>
    public Dictionary<string, object> Config { get; set; } = new();
}
