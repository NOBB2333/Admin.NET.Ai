namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// MCP 资源接口
/// 用于暴露可被 MCP 客户端访问的资源
/// </summary>
public interface IMcpResource
{
    /// <summary>
    /// 资源 URI
    /// </summary>
    string Uri { get; }
    
    /// <summary>
    /// 资源名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 资源描述
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// MIME 类型
    /// </summary>
    string MimeType { get; }
    
    /// <summary>
    /// 读取资源内容
    /// </summary>
    Task<McpResourceContent> ReadAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// MCP 资源内容
/// </summary>
public class McpResourceContent
{
    public string Uri { get; set; } = "";
    public string? Text { get; set; }
    public byte[]? Blob { get; set; }
    public string MimeType { get; set; } = "text/plain";
}

/// <summary>
/// MCP 提示模板接口
/// </summary>
public interface IMcpPrompt
{
    /// <summary>
    /// 提示名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 提示描述
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// 参数定义
    /// </summary>
    IEnumerable<McpPromptArgument> Arguments { get; }
    
    /// <summary>
    /// 生成提示消息
    /// </summary>
    Task<IEnumerable<McpPromptMessage>> GetMessagesAsync(IDictionary<string, string> arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// MCP 提示参数
/// </summary>
public class McpPromptArgument
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool Required { get; set; }
}

/// <summary>
/// MCP 提示消息
/// </summary>
public class McpPromptMessage
{
    public string Role { get; set; } = "user";
    public McpPromptContent Content { get; set; } = new();
}

/// <summary>
/// MCP 提示内容
/// </summary>
public class McpPromptContent
{
    public string Type { get; set; } = "text";
    public string? Text { get; set; }
}

/// <summary>
/// MCP 连接池接口
/// </summary>
public interface IMcpConnectionPool
{
    /// <summary>
    /// 获取或创建连接
    /// </summary>
    Task<IMcpConnection> GetConnectionAsync(string serverName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 释放连接
    /// </summary>
    void ReleaseConnection(string serverName);
    
    /// <summary>
    /// 获取连接状态
    /// </summary>
    McpConnectionStatus GetStatus(string serverName);
}

/// <summary>
/// MCP 连接接口
/// </summary>
public interface IMcpConnection : IAsyncDisposable
{
    string ServerName { get; }
    bool IsConnected { get; }
    DateTime LastActiveTime { get; }
    
    Task<T?> SendAsync<T>(string method, object? parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// MCP 连接状态
/// </summary>
public enum McpConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}
