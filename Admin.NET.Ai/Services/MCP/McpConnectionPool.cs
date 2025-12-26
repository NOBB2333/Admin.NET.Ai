using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;

namespace Admin.NET.Ai.Services.MCP;

/// <summary>
/// MCP 连接池实现
/// </summary>
public class McpConnectionPool : IMcpConnectionPool, IAsyncDisposable
{
    private readonly ILogger<McpConnectionPool> _logger;
    private readonly IOptions<LLMMcpConfig> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, McpConnection> _connections = new();
    private readonly ConcurrentDictionary<string, McpConnectionStatus> _statuses = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public McpConnectionPool(
        ILogger<McpConnectionPool> logger,
        IOptions<LLMMcpConfig> options,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _options = options;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IMcpConnection> GetConnectionAsync(string serverName, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(serverName, out var existing) && existing.IsConnected)
        {
            existing.UpdateLastActive();
            return existing;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            // 双重检查
            if (_connections.TryGetValue(serverName, out existing) && existing.IsConnected)
            {
                existing.UpdateLastActive();
                return existing;
            }

            // 创建新连接
            var config = _options.Value.Servers.FirstOrDefault(s => s.Name == serverName);
            if (config == null)
            {
                throw new ArgumentException($"MCP Server '{serverName}' 配置不存在");
            }

            _statuses[serverName] = McpConnectionStatus.Connecting;
            _logger.LogInformation("🔌 [MCP Pool] 创建连接: {Server}", serverName);

            var connection = new McpConnection(
                serverName,
                config.Url,
                _httpClientFactory.CreateClient("mcp"),
                _logger);

            await connection.ConnectAsync(cancellationToken);
            
            _connections[serverName] = connection;
            _statuses[serverName] = McpConnectionStatus.Connected;

            _logger.LogInformation("✅ [MCP Pool] 连接成功: {Server}", serverName);
            return connection;
        }
        catch (Exception ex)
        {
            _statuses[serverName] = McpConnectionStatus.Failed;
            _logger.LogError(ex, "❌ [MCP Pool] 连接失败: {Server}", serverName);
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public void ReleaseConnection(string serverName)
    {
        if (_connections.TryRemove(serverName, out var connection))
        {
            _ = connection.DisposeAsync();
            _statuses[serverName] = McpConnectionStatus.Disconnected;
            _logger.LogInformation("🔓 [MCP Pool] 释放连接: {Server}", serverName);
        }
    }

    public McpConnectionStatus GetStatus(string serverName)
    {
        return _statuses.GetValueOrDefault(serverName, McpConnectionStatus.Disconnected);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (name, connection) in _connections)
        {
            await connection.DisposeAsync();
        }
        _connections.Clear();
        _statuses.Clear();
        _connectionLock.Dispose();
    }
}

/// <summary>
/// MCP 连接实现
/// </summary>
public class McpConnection : IMcpConnection
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _url;
    private readonly Channel<string> _eventChannel;
    private CancellationTokenSource? _sseCts;
    private Task? _sseTask;

    public string ServerName { get; }
    public bool IsConnected { get; private set; }
    public DateTime LastActiveTime { get; private set; }

    public McpConnection(string serverName, string url, HttpClient httpClient, ILogger logger)
    {
        ServerName = serverName;
        _url = url;
        _httpClient = httpClient;
        _logger = logger;
        _eventChannel = Channel.CreateUnbounded<string>();
        LastActiveTime = DateTime.UtcNow;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _sseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // 启动 SSE 监听
        _sseTask = Task.Run(async () =>
        {
            try
            {
                _httpClient.Timeout = TimeSpan.FromMinutes(60);
                using var stream = await _httpClient.GetStreamAsync(_url, _sseCts.Token);
                using var reader = new StreamReader(stream);

                IsConnected = true;
                _logger.LogDebug("[MCP] SSE 连接建立: {Server}", ServerName);

                while (!_sseCts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(_sseCts.Token);
                    if (line == null) break;
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith("data:"))
                    {
                        var data = line.Substring(5).Trim();
                        await _eventChannel.Writer.WriteAsync(data, _sseCts.Token);
                        UpdateLastActive();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MCP] SSE 连接错误: {Server}", ServerName);
            }
            finally
            {
                IsConnected = false;
            }
        }, _sseCts.Token);

        // 等待连接建立
        await Task.Delay(100, cancellationToken);
    }

    public async Task<T?> SendAsync<T>(string method, object? parameters, CancellationToken cancellationToken = default)
    {
        var postUrl = _url.Replace("/sse", "/messages");
        if (postUrl == _url) postUrl += "/messages";

        var jsonRpc = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = method,
            @params = parameters
        };

        var response = await _httpClient.PostAsJsonAsync(postUrl, jsonRpc, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (json.TryGetProperty("error", out var error))
        {
            throw new Exception($"MCP Error: {error.GetProperty("message").GetString()}");
        }

        if (json.TryGetProperty("result", out var result))
        {
            UpdateLastActive();
            return JsonSerializer.Deserialize<T>(result.GetRawText());
        }

        return default;
    }

    public void UpdateLastActive()
    {
        LastActiveTime = DateTime.UtcNow;
    }

    public async ValueTask DisposeAsync()
    {
        _sseCts?.Cancel();
        if (_sseTask != null)
        {
            try { await _sseTask; } catch { }
        }
        _sseCts?.Dispose();
        _eventChannel.Writer.Complete();
        IsConnected = false;
    }
}
