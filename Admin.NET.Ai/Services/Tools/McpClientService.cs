using Admin.NET.Ai.Options;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using System.Collections.Concurrent;

namespace Admin.NET.Ai.Services.Tools;

/// <summary>
/// MCP 客户端服务实现 (SSE + JSON-RPC)
/// </summary>
/// <summary>
/// MCP 客户端服务实现 (SSE + JSON-RPC)
/// </summary>
public class McpClientService(ILogger<McpClientService> logger, IOptions<LLMMcpConfig> options, IHttpClientFactory httpClientFactory) : IMcpClient
{
    private readonly LLMMcpConfig _options = options.Value;
    // 存储 SSE 连接任务 (实际应更复杂，处理重连等)
    private readonly ConcurrentDictionary<string, Task> _connections = new();
    // 存储事件通道
    private readonly ConcurrentDictionary<string, Channel<string>> _channels = new();

    public async Task ConnectAsync(string serverName)
    {
        logger.LogInformation("Connecting to MCP Server: {ServerName}...", serverName);
        
        var config = _options.Servers.FirstOrDefault(s => s.Name == serverName);
        if (config == null)
        {
            logger.LogWarning("MCP Server configuration not found: {ServerName}", serverName);
            return;
        }

        if (_connections.ContainsKey(serverName)) return;

        var channel = _channels.GetOrAdd(serverName, _ => Channel.CreateUnbounded<string>());

        // 启动 SSE 监听
        var task = Task.Run(async () => 
        {
            try 
            {
                using var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(60); // Long timeout for SSE
                
                using var stream = await client.GetStreamAsync(config.Url);
                using var reader = new StreamReader(stream);
                
                logger.LogInformation("Connected to SSE stream: {Url}", config.Url);

                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line is null) break;
                    if (string.IsNullOrEmpty(line)) continue;
                    
                    if (line.StartsWith("data:"))
                    {
                        var data = line.Substring(5).Trim();
                        logger.LogDebug("MCP Event: {Data}", data);
                        
                        // 写入通道
                        await channel.Writer.WriteAsync(data);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MCP Connection Error: {ServerName}", serverName);
            }
        });

        _connections.TryAdd(serverName, task);
    }

    public ChannelReader<string> GetEventStream(string serverName)
    {
        var channel = _channels.GetOrAdd(serverName, _ => Channel.CreateUnbounded<string>());
        return channel.Reader;
    }

    public async Task<List<string>> GetToolsAsync(string serverName)
    {
        // 发送 JSON-RPC 请求: tools/list
        var response = await SendJsonRpcAsync(serverName, "tools/list", new { });
        
        // 解析结果 (假设标准 MCP 响应)
        if (response.TryGetProperty("result", out var result) && result.TryGetProperty("tools", out var tools))
        {
            return tools.EnumerateArray().Select(t => t.GetProperty("name").GetString() ?? "").ToList();
        }
        
        return [];
    }

    public async Task<object?> CallToolAsync(string serverName, string toolName, Dictionary<string, object> arguments)
    {
        logger.LogInformation("Calling MCP Tool: {ServerName}.{ToolName}", serverName, toolName);
        
        var request = new 
        { 
            name = toolName,
            arguments = arguments
        };

        var response = await SendJsonRpcAsync(serverName, "tools/call", request);
        
        if (response.TryGetProperty("result", out var result))
        {
            return result.ToString(); // 返回 JSON 字符串
        }
        
        return null;
    }

    private async Task<JsonElement> SendJsonRpcAsync(string serverName, string method, object? parameters)
    {
        var config = _options.Servers.FirstOrDefault(s => s.Name == serverName);
        if (config == null) throw new ArgumentException("Server not found");

        // 假设 POST URL 是 SSE URL + "/messages" 或者配置中有 (这里简化假设)
        // 实际上 MCP SSE 协议会在 SSE 握手时返回 endpoint，或者约定俗成
        var postUrl = config.Url.Replace("/sse", "/messages"); 
        if (postUrl == config.Url) postUrl += "/messages"; // Fallback

        using var client = httpClientFactory.CreateClient();
        
        var jsonRpc = new 
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = method,
            @params = parameters
        };

        var response = await client.PostAsJsonAsync(postUrl, jsonRpc);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        if (json.TryGetProperty("error", out var error))
        {
            throw new Exception($"MCP Error: {error.GetProperty("message").GetString()}");
        }

        return json;
    }
}
