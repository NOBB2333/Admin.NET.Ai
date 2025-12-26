namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// MCP 客户端接口 (Model Context Protocol)
/// </summary>
public interface IMcpClient
{
    /// <summary>
    /// 连接到 MCP 服务器
    /// </summary>
    /// <param name="serverName">服务器名称</param>
    /// <returns></returns>
    Task ConnectAsync(string serverName);

    /// <summary>
    /// 获取可用工具列表
    /// </summary>
    /// <param name="serverName">服务器名称</param>
    /// <returns></returns>
    Task<List<string>> GetToolsAsync(string serverName);

    /// <summary>
    /// 调用工具
    /// </summary>
    /// <param name="serverName">服务器名称</param>
    /// <param name="toolName">工具名称</param>
    /// <param name="arguments">参数</param>
    /// <returns></returns>
    Task<object?> CallToolAsync(string serverName, string toolName, Dictionary<string, object> arguments);

    /// <summary>
    /// 获取事件流 (Channel)
    /// </summary>
    /// <param name="serverName">服务器名称</param>
    /// <returns></returns>
    System.Threading.Channels.ChannelReader<string> GetEventStream(string serverName);
}
