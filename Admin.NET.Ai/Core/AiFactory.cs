using System.Collections.Concurrent;
using System.Diagnostics;
using Admin.NET.Ai.Options;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using System.ClientModel;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Core;
using MEAI = Microsoft.Extensions.AI;

// 使用别名以避免同时打开两个命名空间时的冲突
using AzureOpenAIClient = Azure.AI.OpenAI.AzureOpenAIClient;

/// <summary>
/// AI 工厂实现 (MEAI 管道 + 懒加载 + 多供应商 + 企业级五星标准)
/// 支持：健康检查、降级重试、配置热重载、完整生命周期管理
/// </summary>
public class AiFactory : IAiFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiFactory> _logger;
    private readonly IOptionsMonitor<LLMClientsConfig> _optionsMonitor;
    private readonly IDisposable? _optionsChangeToken;
    private bool _disposed;
    
    // 当前配置快照
    private LLMClientsConfig _options;
    
    // 显式懒加载捕获 - 线程安全的客户端缓存
    private readonly ConcurrentDictionary<string, Lazy<IChatClient>> _clients = new();
    
    // 健康检查结果缓存（避免频繁检查）
    private readonly ConcurrentDictionary<string, (ClientHealthStatus Status, DateTime CachedAt)> _healthCache = new();
    private static readonly TimeSpan HealthCacheDuration = TimeSpan.FromMinutes(1);

    public AiFactory(
        IOptionsMonitor<LLMClientsConfig> optionsMonitor, 
        IServiceProvider serviceProvider,
        ILogger<AiFactory> logger)
    {
        _optionsMonitor = optionsMonitor;
        _options = optionsMonitor.CurrentValue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // 监听配置变更，自动刷新客户端
        _optionsChangeToken = _optionsMonitor.OnChange(OnConfigurationChanged);
        
        _logger.LogInformation("AiFactory initialized with {ClientCount} client configurations, default: {DefaultProvider}", 
            _options.Clients.Count, _options.DefaultProvider);
    }
    
    /// <summary>
    /// 配置变更回调
    /// </summary>
    private void OnConfigurationChanged(LLMClientsConfig newConfig)
    {
        _logger.LogInformation("LLM configuration changed, refreshing all clients...");
        _options = newConfig;
        RefreshClient(null); // 刷新所有客户端
    }

    #region IAiFactory 核心实现
    
    /// <inheritdoc/>
    public string? DefaultProvider => _options.DefaultProvider;


    public IChatClient? GetDefaultChatClient()
    {
        var defaultProvider = _options.DefaultProvider;
        if (string.IsNullOrEmpty(defaultProvider))
        {
            return null;
        }
        return GetChatClient(defaultProvider);
    }

    public IChatClient? GetChatClient(string name)
    {
        // 线程安全的懒初始化
        var lazyClient = _clients.GetOrAdd(name, key => new Lazy<IChatClient>(() => CreatePipeline(key), LazyThreadSafetyMode.ExecutionAndPublication));
        
        try 
        {
            return lazyClient.Value;
        }
        catch (Exception ex)
        {
            // 如果创建失败，移除错误的 Lazy 对象以便重试
            _clients.TryRemove(name, out _);
            throw new InvalidOperationException($"Failed to create chat client '{name}': {ex.Message}", ex);
        }
    }

    private IChatClient CreatePipeline(string name)
    {
        if (!_options.Clients.TryGetValue(name, out var config))
        {
            throw new ArgumentException($"AI Client configuration '{name}' not found.");
        }

        // 1. 根据供应商类型创建基础客户端
        IChatClient innerClient = CreateBaseClient(config);

        // 2. 构建中间件管道 (Pipeline Construction)
        var builder = new ChatClientBuilder(innerClient);
        
        // --- 核心中间件链 (执行顺序: 外 -> 内) ---
        // 注意：Builder.Use 的顺序是 "Outer to Inner" (当请求进入时)
        // 也就是说，第一个 Use 的中间件是最外层。
        
        // [1] 重试 (最外层，处理所有下游的网络/瞬态错误)
        builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.RetryMiddleware>(_serviceProvider, inner));
        
        // [2] 限流 (尽早拒绝)
        builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.RateLimitingMiddleware>(_serviceProvider, inner));
        
        // [3] 日志 (记录所有请求，包括被缓存拦截的)
        builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.LoggingMiddleware>(_serviceProvider, inner));
        
        // [4] 审计 (业务审计，同样记录用户意图)
        builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.AuditMiddleware>(_serviceProvider, inner));
        
        // [5] 缓存 (如果在缓存中找到，则短路后续调用)
        builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.CachingMiddleware>(_serviceProvider, inner));
        
        // [6] Token监控与计费 (仅针对实际穿透缓存到达模型的请求进行计费)
        builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.TokenMonitoringMiddleware>(_serviceProvider, inner));

        return builder.Build();
    }

    private IChatClient CreateBaseClient(LLMClientConfig config)
    {
        // 规范化供应商名称
        var provider = config.Provider?.ToLowerInvariant() ?? "generic";

        return provider switch
        {
            "azure" or "azureopenai" => CreateAzureClient(config),
            "openai" => CreateOpenAIClient(config),
            _ => CreateGenericClient(config) // DeepSeek、SiliconFlow 等兼容 OpenAI
        };
    }

    private IChatClient CreateAzureClient(LLMClientConfig config)
    {
        var endpoint = new Uri(config.BaseUrl); // Azure 需要 Endpoint
        var credential = new ApiKeyCredential(config.ApiKey);
        
        // 使用 Azure.AI.OpenAI SDK
        var azureClient = new AzureOpenAIClient(endpoint, credential);
        return new PrivateOpenAIAdapter(azureClient.GetChatClient(config.ModelId));
    }

    private IChatClient CreateOpenAIClient(LLMClientConfig config)
    {
        // 标准 OpenAI
        var credential = new ApiKeyCredential(config.ApiKey);
        var openAIClient = new OpenAIClient(credential); 
        return new PrivateOpenAIAdapter(openAIClient.GetChatClient(config.ModelId));
    }

    /// <summary>
    /// 私有适配器，用于将 OpenAI.Chat.ChatClient 连接到 Microsoft.Extensions.AI.IChatClient
    /// 绕过预览包中缺失的扩展。
    /// </summary>
    private class PrivateOpenAIAdapter(OpenAI.Chat.ChatClient client) : MEAI.IChatClient
    {
        public MEAI.ChatClientMetadata Metadata => new(nameof(PrivateOpenAIAdapter));

        public object? GetService(Type serviceType, object? key = null)
        {
            return serviceType.IsInstanceOfType(client) ? client : null;
        }

        public async Task<MEAI.ChatResponse> GetResponseAsync(IEnumerable<MEAI.ChatMessage> chatMessages, MEAI.ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var messages = await ToOpenAIMessagesAsync(chatMessages, cancellationToken);
            OpenAI.Chat.ChatCompletion completion = await client.CompleteChatAsync(messages, ToOpenAIOptions(options), cancellationToken);
            return ToMEAIResponse(completion);
        }

        public async IAsyncEnumerable<MEAI.ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<MEAI.ChatMessage> chatMessages, MEAI.ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
             var messages = await ToOpenAIMessagesAsync(chatMessages, cancellationToken);
             var updates = client.CompleteChatStreamingAsync(messages, ToOpenAIOptions(options), cancellationToken);
             await foreach (var update in updates)
             {
                 foreach (var part in update.ContentUpdate)
                 {
                     yield return new MEAI.ChatResponseUpdate(ToMEAIRole(update.Role), part.Text);
                 }
             }
        }
        
        private async Task<IEnumerable<OpenAI.Chat.ChatMessage>> ToOpenAIMessagesAsync(IEnumerable<MEAI.ChatMessage> messages, CancellationToken ct)
        {
            var result = new List<OpenAI.Chat.ChatMessage>();
            // 在此适配器范围内为简单起见使用瞬态 HttpClient，或者最好注入它。
            // 考虑到范围，对于此逻辑修复，每个请求一个新的客户端是可以接受的，但重用会更好。
            using var httpClient = new HttpClient();
            // Add User-Agent to avoid 403 Forbidden from sites like Wikimedia
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            foreach(var m in messages)
            {
                if (m.Role == MEAI.ChatRole.User)
                {
                    if (m.Contents != null && m.Contents.Count > 0)
                    {
                        var parts = new List<OpenAI.Chat.ChatMessageContentPart>();
                        foreach(var content in m.Contents)
                        {
                            if (content is MEAI.TextContent tc)
                            {
                                parts.Add(OpenAI.Chat.ChatMessageContentPart.CreateTextPart(tc.Text));
                            }
                            else if (content is MEAI.DataContent dc)
                            {
                                 parts.Add(OpenAI.Chat.ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(dc.Data.ToArray()), dc.MediaType));
                            }
                            else if (content is MEAI.UriContent uc)
                            {
                                 if (uc.Uri.IsFile)
                                 {
                                     // 智能处理：读取本地文件
                                     var bytes = await File.ReadAllBytesAsync(uc.Uri.LocalPath, ct); 
                                     string mediaType = "image/png"; 
                                     if (uc.Uri.LocalPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || uc.Uri.LocalPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) 
                                        mediaType = "image/jpeg";
                                     else if (uc.Uri.LocalPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                                        mediaType = "image/webp";
                                     
                                     parts.Add(OpenAI.Chat.ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), mediaType));
                                 }
                                 else if (uc.Uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                 {
                                     // 智能处理：下载远程文件以绕过供应商访问问题
                                     try 
                                     {
                                         var bytes = await httpClient.GetByteArrayAsync(uc.Uri, ct);
                                         string mediaType = "image/jpeg"; // 默认
                                         if (uc.Uri.AbsoluteUri.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) mediaType = "image/png";
                                         else if (uc.Uri.AbsoluteUri.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) mediaType = "image/webp";
                                         else if (uc.Uri.AbsoluteUri.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) mediaType = "image/gif";
                                         
                                         parts.Add(OpenAI.Chat.ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), mediaType));
                                     }
                                     catch (Exception ex)
                                     {
                                         Console.WriteLine($"[AiFactory] Network Image Download Failed: {ex.Message}. Fallback to passing URI.");
                                         parts.Add(OpenAI.Chat.ChatMessageContentPart.CreateImagePart(uc.Uri));
                                     }
                                 }
                                 else
                                 {
                                     parts.Add(OpenAI.Chat.ChatMessageContentPart.CreateImagePart(uc.Uri));
                                 }
                            }
                        }
                        result.Add(new OpenAI.Chat.UserChatMessage(parts));
                    }
                    else 
                    {
                        result.Add(new OpenAI.Chat.UserChatMessage(m.Text));
                    }
                }
                else if (m.Role == MEAI.ChatRole.System) result.Add(new OpenAI.Chat.SystemChatMessage(m.Text));
                else if (m.Role == MEAI.ChatRole.Assistant) result.Add(new OpenAI.Chat.AssistantChatMessage(m.Text));
                else result.Add(new OpenAI.Chat.UserChatMessage(m.Text));
            }
            return result;
        }
        
        private OpenAI.Chat.ChatCompletionOptions ToOpenAIOptions(MEAI.ChatOptions? options)
        {
            if (options == null) return new();
            return new OpenAI.Chat.ChatCompletionOptions 
            {
                 Temperature = options.Temperature,
            };
        }

        private MEAI.ChatResponse ToMEAIResponse(OpenAI.Chat.ChatCompletion completion)
        {
            var content = completion.Content?.Count > 0 ? completion.Content[0].Text : string.Empty;
            return new MEAI.ChatResponse(new MEAI.ChatMessage(MEAI.ChatRole.Assistant, content));
        }
        
        private MEAI.ChatRole ToMEAIRole(OpenAI.Chat.ChatMessageRole? role)
        {
            if (!role.HasValue) return MEAI.ChatRole.Assistant;
            if (role == OpenAI.Chat.ChatMessageRole.User) return MEAI.ChatRole.User;
            if (role == OpenAI.Chat.ChatMessageRole.System) return MEAI.ChatRole.System;
            return MEAI.ChatRole.Assistant;
        }

        public void Dispose() { }
    }
    private IChatClient CreateGenericClient(LLMClientConfig config)
    {
        // 通用 OpenAI 兼容 (DeepSeek 等)
        // 需要自定义端点
        var openAIClientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(config.BaseUrl))
        {
             openAIClientOptions.Endpoint = new Uri(config.BaseUrl);
        }

        var credential = new ApiKeyCredential(config.ApiKey);
        var openAIClient = new OpenAIClient(credential, openAIClientOptions);
        
        return new PrivateOpenAIAdapter(openAIClient.GetChatClient(config.ModelId));
    }

    public TAgent? CreateAgent<TAgent>(string clientName, string agentName, string? instructions = null) where TAgent : class
    {
         // 1. 获取底层 ChatClient (通过配置名称)
         var client = GetChatClient(clientName);
         if (client == null) return null;

         // 2. 处理 Microsoft.Agents.AI.ChatCompletionAgent (MAF)
         // 假设 TAgent 是或者继承自 Microsoft.Agents.AI.Agent
         if (typeof(TAgent).Name == "ChatClientAgent" || typeof(TAgent).IsSubclassOf(typeof(Microsoft.Agents.AI.AIAgent)))
         {
             try 
             {
                 TAgent? agent = null;

                 // 手动处理已知类型以避免 DI 歧义和 Activator 问题
                 if (typeof(TAgent) == typeof(Microsoft.Agents.AI.ChatClientAgent))
                 {
                     // 显式的构造函数编译时绑定
                     agent = new Microsoft.Agents.AI.ChatClientAgent(client) as TAgent;
                 }
                 else
                 {
                    // 如果可能，通过 DI 创建实例，或者使用 Activator
                    agent = ActivatorUtilities.CreateInstance(_serviceProvider, typeof(TAgent)) as TAgent;
                 }

                 if (agent != null)
                 {
                    var type = typeof(TAgent);
                    
                     // 1. 设置 ChatClient (MEAI) - 如果通过构造函数传递则是多余的，但是是安全的
                     // 使用 SetAgentProperty 助手以避免只读异常
                     var clientPropName = (type.GetProperty("ChatClient") != null) ? "ChatClient" : "Client";
                     SetAgentProperty(agent, clientPropName, client);
                    
                     // 2. 设置名称 (动态代理名称)
                     SetAgentProperty(agent, "Name", agentName);
                     
                     // 3. 设置指令 (角色)
                     if (!string.IsNullOrEmpty(instructions))
                     {
                         SetAgentProperty(agent, "Instructions", instructions);
                         SetAgentProperty(agent, "Description", instructions);
                     }
 
                     return agent;
                  }
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"Error creating agent '{agentName}' with client '{clientName}': {ex.Message}");
              }
          }
          
          return default; 
     }
 
     private void SetAgentProperty(object agent, string propertyName, object value)
     {
         var type = agent.GetType();
         // 尝试通过属性设置 (Public/Private Setter)
         try 
         {
             var prop = type.GetProperty(propertyName);
             if (prop != null && prop.CanWrite)
             {
                 prop.SetValue(agent, value);
                 return;
             }
         }
         catch { } // 忽略属性设置失败，假定为只读或错误

         // 回退：直接设置支持字段 (反射)
         try
         {
             var field = type.GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
             if (field != null)
             {
                 field.SetValue(agent, value);
             }
         }
         catch { } // 忽略字段设置失败
     }
 

    public TAgent? CreateDefaultAgent<TAgent>(string agentName, string? instructions = null) where TAgent : class
    {
        var defaultProvider = _options.DefaultProvider;
        if (string.IsNullOrEmpty(defaultProvider)) return null;
        return CreateAgent<TAgent>(defaultProvider, agentName, instructions);
    }

    public TAgent? GetAgent<TAgent>(string name, string? instructions = null) where TAgent : class
    {
         // 遗留行为：代理名称 == 客户端配置名称
         return CreateAgent<TAgent>(name, name, instructions);
    }

    public TAgent? GetDefaultAgent<TAgent>(string? instructions = null) where TAgent : class
    {
        var defaultProvider = _options.DefaultProvider;
        if (string.IsNullOrEmpty(defaultProvider)) return null;
        return GetAgent<TAgent>(defaultProvider, instructions);
    }
    public T? GetDefaultClient<T>() where T : class
    {
        var defaultProvider = _options.DefaultProvider;
        if (string.IsNullOrEmpty(defaultProvider)) return null;
        return GetClient<T>(defaultProvider);
    }

    public T? GetClient<T>(string name) where T : class
    {
        if (typeof(T) == typeof(IChatClient))
        {
            return GetChatClient(name) as T;
        }

        if (typeof(T) == typeof(Kernel))
        {
        if (!_options.Clients.TryGetValue(name, out var config))
            {
                throw new ArgumentException($"AI Client configuration '{name}' not found.");
            }

            // 使用 SK 连接器直接创建 Kernel
            var builder = Kernel.CreateBuilder();
            
            var provider = config.Provider?.ToLowerInvariant() ?? "generic";
            switch (provider)
            {
                case "azure":
                case "azureopenai":
                    builder.AddAzureOpenAIChatCompletion(config.ModelId, config.BaseUrl, config.ApiKey);
                    break;
                case "openai":
                    builder.AddOpenAIChatCompletion(config.ModelId, config.ApiKey);
                    break;
                default: // Generic
                     // 对于 DeepSeek/Generic，使用带有端点的 AddOpenAIChatCompletion
                     builder.AddOpenAIChatCompletion(
                        modelId: config.ModelId,
                        apiKey: config.ApiKey,
                        endpoint: !string.IsNullOrEmpty(config.BaseUrl) ? new Uri(config.BaseUrl) : null
                     );
                    break;
            }
            
            return builder.Build() as T;
        }

        return default;
    }

    #endregion

    #region 客户端发现与管理

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAvailableClients()
    {
        return _options.Clients.Keys.ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public void RefreshClient(string? name = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            // 刷新所有客户端
            _clients.Clear();
            _healthCache.Clear();
            _logger.LogInformation("All AI clients have been refreshed");
        }
        else
        {
            // 刷新指定客户端
            if (_clients.TryRemove(name, out var removed))
            {
                // 如果客户端已创建，尝试释放资源
                if (removed.IsValueCreated && removed.Value is IDisposable disposable)
                {
                    try { disposable.Dispose(); } catch { /* ignore */ }
                }
            }
            _healthCache.TryRemove(name, out _);
            _logger.LogInformation("AI client '{ClientName}' has been refreshed", name);
        }
    }

    #endregion

    #region 降级与重试

    /// <inheritdoc/>
    public async Task<IChatClient> GetChatClientWithFallbackAsync(
        string name, 
        IEnumerable<string>? fallbackNames = null, 
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<string> { name };
        if (fallbackNames != null)
        {
            candidates.AddRange(fallbackNames);
        }

        Exception? lastException = null;
        
        foreach (var clientName in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                // 先检查健康状态（使用缓存）
                var health = await CheckHealthAsync(clientName, cancellationToken);
                if (!health.IsHealthy)
                {
                    _logger.LogWarning("Client '{ClientName}' is unhealthy: {Error}, trying next...", 
                        clientName, health.ErrorMessage);
                    continue;
                }

                var client = GetChatClient(clientName);
                if (client != null)
                {
                    if (clientName != name)
                    {
                        _logger.LogWarning("Primary client '{Primary}' unavailable, using fallback '{Fallback}'", 
                            name, clientName);
                    }
                    return client;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Failed to get client '{ClientName}', trying next...", clientName);
            }
        }

        throw new InvalidOperationException(
            $"All AI clients failed. Tried: {string.Join(", ", candidates)}", 
            lastException);
    }

    #endregion

    #region 健康检查

    /// <inheritdoc/>
    public async Task<ClientHealthStatus> CheckHealthAsync(string name, CancellationToken cancellationToken = default)
    {
        // 检查缓存
        if (_healthCache.TryGetValue(name, out var cached) && 
            DateTime.UtcNow - cached.CachedAt < HealthCacheDuration)
        {
            return cached.Status;
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (!_options.Clients.ContainsKey(name))
            {
                var status = new ClientHealthStatus(name, false, null, $"Configuration '{name}' not found");
                _healthCache[name] = (status, DateTime.UtcNow);
                return status;
            }

            var client = GetChatClient(name);
            if (client == null)
            {
                var status = new ClientHealthStatus(name, false, null, "Failed to create client");
                _healthCache[name] = (status, DateTime.UtcNow);
                return status;
            }

            // 发送简单的测试消息来验证连接
            var testMessages = new List<ChatMessage>
            {
                new(ChatRole.User, "ping")
            };
            
            var options = new ChatOptions { MaxOutputTokens = 5 };
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10)); // 健康检查超时10秒
            
            var response = await client.GetResponseAsync(testMessages, options, cts.Token);
            stopwatch.Stop();
            
            var healthStatus = new ClientHealthStatus(
                name, 
                true, 
                stopwatch.Elapsed,
                null
            );
            
            _healthCache[name] = (healthStatus, DateTime.UtcNow);
            _logger.LogDebug("Health check passed for '{ClientName}' in {Duration}ms", 
                name, stopwatch.ElapsedMilliseconds);
            
            return healthStatus;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            var status = new ClientHealthStatus(name, false, stopwatch.Elapsed, "Health check timed out");
            _healthCache[name] = (status, DateTime.UtcNow);
            _logger.LogWarning("Health check timed out for '{ClientName}'", name);
            return status;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var status = new ClientHealthStatus(name, false, stopwatch.Elapsed, ex.Message);
            _healthCache[name] = (status, DateTime.UtcNow);
            _logger.LogWarning(ex, "Health check failed for '{ClientName}'", name);
            return status;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClientHealthStatus>> CheckAllHealthAsync(CancellationToken cancellationToken = default)
    {
        var clientNames = GetAvailableClients();
        var tasks = clientNames.Select(name => CheckHealthAsync(name, cancellationToken));
        var results = await Task.WhenAll(tasks);
        
        var healthyCount = results.Count(r => r.IsHealthy);
        _logger.LogInformation("Health check complete: {Healthy}/{Total} clients healthy", 
            healthyCount, results.Length);
        
        return results.ToList().AsReadOnly();
    }

    #endregion

    #region IDisposable / IAsyncDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            // 释放配置变更监听
            _optionsChangeToken?.Dispose();
            
            // 释放所有已创建的客户端
            foreach (var kvp in _clients)
            {
                if (kvp.Value.IsValueCreated && kvp.Value.Value is IDisposable disposable)
                {
                    try { disposable.Dispose(); } catch { /* ignore */ }
                }
            }
            _clients.Clear();
            _healthCache.Clear();
            
            _logger.LogInformation("AiFactory disposed");
        }
        
        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        _optionsChangeToken?.Dispose();
        
        foreach (var kvp in _clients)
        {
            if (kvp.Value.IsValueCreated)
            {
                if (kvp.Value.Value is IAsyncDisposable asyncDisposable)
                {
                    try { await asyncDisposable.DisposeAsync(); } catch { /* ignore */ }
                }
                else if (kvp.Value.Value is IDisposable disposable)
                {
                    try { disposable.Dispose(); } catch { /* ignore */ }
                }
            }
        }
        _clients.Clear();
        _healthCache.Clear();
        
        _logger.LogInformation("AiFactory disposed asynchronously");
    }

    #endregion
}
