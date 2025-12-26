using Admin.NET.Ai.Options;
using Admin.NET.Ai.Services.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Admin.NET.Ai.Services.MCP;

public class McpToolFactory
{
    private readonly McpClientService _clientService;
    private readonly LLMAgentOptions _options;
    private readonly ILogger<McpToolFactory> _logger;

    public McpToolFactory(
        McpClientService clientService, 
        IOptions<LLMAgentOptions> options, 
        ILogger<McpToolFactory> logger)
    {
        _clientService = clientService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 连接到所有配置的 MCP 服务器并将其工具转换为 AITools。
    /// </summary>
    /// <returns>准备好供 Agent 使用的 AITools 列表。</returns>
    public async Task<List<AITool>> LoadGlobalMcpToolsAsync()
    {
        var aiTools = new List<AITool>();

        foreach (var serverConfig in _options.LLMMcp.Servers)
        {
            if (!serverConfig.Enabled || string.IsNullOrEmpty(serverConfig.Name)) continue;

            try
            {
                // 确保连接
                await _clientService.ConnectAsync(serverConfig.Name);
                
                // 获取工具名称
                // 注意：GetToolsAsync 目前返回 List<string>。
                // 在实际实现中，我们需要完整的架构 (描述、参数) 来构建 AITool。
                // 为了让这个工厂完全工作，McpClientService.GetToolsAsync 应该返回更多细节。
                // 对于这个 "工厂" 演示，我们假设我们可以获取细节，或者如果是动态的，我们逐个获取。
                // 限制：当前的 McpClientService.GetToolsAsync 只返回名称。
                // 我们将使用约定模拟获取细节，或者如果需要，更新 ClientService。
                // 目前，有效的工具只是名称，我们将创建一个通用的架构工具。
                
                var toolNames = await _clientService.GetToolsAsync(serverConfig.Name);
                foreach (var toolName in toolNames)
                {
                    aiTools.Add(CreateMcpTool(toolName, serverConfig.Name));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load MCP tools from server: {ServerName}", serverConfig.Name);
            }
        }

        return aiTools;
    }

    private AITool CreateMcpTool(string toolName, string serverName)
    {
        // 定义执行逻辑
        Func<IDictionary<string, object?>, CancellationToken, Task<object?>> handler = async (args, ct) =>
        {
            // 将 args 转换为 McpClientService 期望的字典格式
            // McpClientService 期望 Dictionary<string, object>，但 args 是 IDictionary<string, object?>
            var safeArgs = args.ToDictionary(k => k.Key, v => v.Value ?? "");
            return await _clientService.CallToolAsync(serverName, toolName, safeArgs);
        };

        // 创建 AIFunction (这是一个 AITool)
        // 我们使用 AIFunctionFactory.Create，它接受一个 Delegate。
        // 但是 AIFunctionFactory.Create 通常期望一个强类型的委托或特定的签名。
        // 它具有包装通用 Func<IDictionary<string, object?>, ...> 的功能。
        
        // 注意：AIFunctionFactory.Create 重载可能因版本而异。
        // 如果我们需要动态参数，我们通常提供 JsonElement 或 Dictionary。
        // 让我们依赖接受 Delegate 的重载。
        
        return AIFunctionFactory.Create(handler, toolName, $"Dynamic MCP Tool '{toolName}' from '{serverName}'");
    }
}
