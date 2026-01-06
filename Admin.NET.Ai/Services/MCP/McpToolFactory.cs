using Admin.NET.Ai.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using System.Collections.Concurrent;

namespace Admin.NET.Ai.Services.MCP;

/// <summary>
/// MCP 工具工厂 - 使用官方 ModelContextProtocol SDK
/// 
/// 📌 核心功能:
/// - 管理多个 MCP 服务器连接
/// - 自动加载工具 (作为 AITool)
/// - 支持 Stdio 传输
/// 
/// 📖 使用方式:
/// var tools = await factory.LoadAllToolsAsync();
/// var result = await factory.CallToolAsync("serverName", "toolName", args);
/// </summary>
public class McpToolFactory : IAsyncDisposable
{
    private readonly ILogger<McpToolFactory> _logger;
    private readonly IOptions<LLMMcpConfig> _options;
    private readonly ConcurrentDictionary<string, McpClient> _clients = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public McpToolFactory(
        ILogger<McpToolFactory> logger,
        IOptions<LLMMcpConfig> options)
    {
        _logger = logger;
        _options = options;
    }

    #region 核心工具加载方法

    /// <summary>
    /// 加载所有已配置服务器的工具 (用于 Agent ChatOptions)
    /// </summary>
    public async Task<List<AITool>> LoadAllToolsAsync()
    {
        var allTools = new List<AITool>();

        foreach (var serverConfig in _options.Value.Servers.Where(s => s.Enabled))
        {
            try
            {
                var tools = await GetServerToolsAsync(serverConfig.Name);
                allTools.AddRange(tools);
                _logger.LogInformation("✅ [MCP] 加载 {Count} 个工具: {Server}", tools.Count, serverConfig.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [MCP] 加载工具失败: {Server}", serverConfig.Name);
            }
        }

        return allTools;
    }

    /// <summary>
    /// 获取指定服务器的所有工具
    /// </summary>
    public async Task<List<AITool>> GetServerToolsAsync(string serverName)
    {
        var client = await GetOrCreateClientAsync(serverName);
        var tools = await client.ListToolsAsync();
        
        // SDK 的 McpClientTool 直接实现 AITool 接口
        return tools.Cast<AITool>().ToList();
    }

    /// <summary>
    /// 调用指定工具
    /// </summary>
    public async Task<object?> CallToolAsync(
        string serverName, 
        string toolName, 
        IReadOnlyDictionary<string, object?>? arguments = null)
    {
        _logger.LogInformation("🔧 [MCP] 调用: {Server}.{Tool}", serverName, toolName);
        
        var client = await GetOrCreateClientAsync(serverName);
        var result = await client.CallToolAsync(toolName, arguments);
        
        return result;
    }

    #endregion

    #region 资源和提示访问

    /// <summary>
    /// 获取服务器资源列表
    /// </summary>
    public async Task<IEnumerable<object>> GetResourcesAsync(string serverName)
    {
        var client = await GetOrCreateClientAsync(serverName);
        return await client.ListResourcesAsync();
    }

    /// <summary>
    /// 获取服务器提示模板列表
    /// </summary>
    public async Task<IEnumerable<object>> GetPromptsAsync(string serverName)
    {
        var client = await GetOrCreateClientAsync(serverName);
        return await client.ListPromptsAsync();
    }

    #endregion

    #region 连接管理

    /// <summary>
    /// 获取原生 SDK 客户端 (用于高级场景)
    /// </summary>
    public async Task<McpClient> GetClientAsync(string serverName)
    {
        return await GetOrCreateClientAsync(serverName);
    }

    private async Task<McpClient> GetOrCreateClientAsync(string serverName)
    {
        if (_clients.TryGetValue(serverName, out var existing))
        {
            return existing;
        }

        await _connectionLock.WaitAsync();
        try
        {
            if (_clients.TryGetValue(serverName, out existing))
            {
                return existing;
            }

            var config = GetServerConfig(serverName);
            _logger.LogInformation("🔌 [MCP] 连接: {Server}", serverName);

            var client = await CreateClientAsync(config);
            _clients[serverName] = client;

            _logger.LogInformation("✅ [MCP] 已连接: {Server} (Tools={Tools}, Resources={Resources})",
                serverName,
                client.ServerCapabilities?.Tools != null,
                client.ServerCapabilities?.Resources != null);

            return client;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private McpServerConfig GetServerConfig(string serverName)
    {
        var config = _options.Value.Servers.FirstOrDefault(s => s.Name == serverName);
        if (config == null)
        {
            throw new ArgumentException($"MCP 服务器配置不存在: {serverName}");
        }
        return config;
    }

    private async Task<McpClient> CreateClientAsync(McpServerConfig config)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = config.Name,
            Command = config.Command ?? "dotnet",
            Arguments = config.Arguments ?? []
        });

        _logger.LogDebug("[MCP] Stdio: {Command} {Args}", 
            config.Command, 
            string.Join(" ", config.Arguments ?? []));

        return await McpClient.CreateAsync(transport, new McpClientOptions
        {
            ClientInfo = new() { Name = "Admin.NET.Ai", Version = "1.0.0" }
        });
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        foreach (var (name, client) in _clients)
        {
            try
            {
                await client.DisposeAsync();
                _logger.LogDebug("[MCP] 已释放: {Server}", name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MCP] 释放失败: {Server}", name);
            }
        }
        _clients.Clear();
        _connectionLock.Dispose();
    }
}
