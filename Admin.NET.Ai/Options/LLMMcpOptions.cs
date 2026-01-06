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

/// <summary>
/// MCP 服务器配置
/// 
/// 支持两种传输方式:
/// 1. Stdio (默认) - 启动本地进程
/// 2. HTTP/SSE - 连接远程服务器
/// </summary>
public sealed class McpServerConfig
{
    /// <summary> 服务器名称 </summary>
    public string Name { get; set; } = "";

    /// <summary> 服务器类型 (tools/resources 等) </summary>
    public string? Type { get; set; }

    /// <summary> 是否启用 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 传输类型: "stdio" (默认) 或 "http"/"sse"
    /// </summary>
    public string TransportType { get; set; } = "stdio";

    #region Stdio 传输配置

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

    #endregion

    #region HTTP/SSE 传输配置

    /// <summary>
    /// 服务器地址 (HTTP/SSE 模式)
    /// 例如: "http://localhost:3000/sse"
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary> 请求头信息 </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    #endregion

    /// <summary> 额外配置 </summary>
    public Dictionary<string, object> Config { get; set; } = new();
}

