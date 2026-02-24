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
/// AI 工厂实现 (MEAI 管道 + 懒加载 + 配置驱动 + 企业级标准)
/// 架构简化：统一使用 OpenAI 兼容协议，能力声明完全由配置驱动
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
        
        _logger.LogInformation("AiFactory initialized with {ClientCount} clients, default: {DefaultProvider}", 
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
        IChatClient innerClient = CreateBaseClient(config, name);

        // 2. 构建中间件管道 (Pipeline Construction) - 支持配置驱动
        var builder = new ChatClientBuilder(innerClient);
        var pipeline = config.Pipeline ?? new PipelineConfig();
        
        // --- 中间件管道 (执行顺序: 外 -> 内) ---
        // 请求流向: 外层 → 内层 → 模型
        // 响应流向: 模型 → 内层 → 外层
        
        // ========== 第一层：基础设施 ==========
        
        // [1] 重试 (最外层，处理所有下游的网络/瞬态错误)
        if (pipeline.EnableRetry)
        {
            builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.RetryMiddleware>(_serviceProvider, inner));
        }
        
        // [2] 限流 (尽早拒绝)
        if (pipeline.EnableRateLimiting)
        {
            builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.RateLimitingMiddleware>(_serviceProvider, inner));
        }
        
        // ========== 第二层：可观测性 ==========
        
        // [3] 日志 (记录所有请求)
        if (pipeline.EnableLogging)
        {
            builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.LoggingMiddleware>(_serviceProvider, inner));
        }
        
        // [4] 审计 (业务审计)
        if (pipeline.EnableAudit)
        {
            builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.AuditMiddleware>(_serviceProvider, inner));
        }
        
        // [5] RAG 追踪 (TODO: 转换为 DelegatingChatClient)
        // 注意：RAGTracingMiddleware 实现 IRunMiddleware，需要重构才能用于 ChatClientBuilder
        // if (pipeline.EnableRAGTracing)
        // {
        //     builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.RAGTracingMiddleware>(_serviceProvider, inner));
        // }
        
        // ========== 第三层：业务处理 ==========
        
        // [6] 边界检查 (TODO: 转换为 DelegatingChatClient)
        // 注意：BoundaryCheckMiddleware 实现 IRunMiddleware，需要重构
        // if (pipeline.EnableBoundaryCheck)
        // {
        //     builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.BoundaryCheckMiddleware>(_serviceProvider, inner));
        // }
        
        // [7] 指令注入 (TODO: 转换为 DelegatingChatClient)
        // 注意：InstructionMiddleware 实现 IRunMiddleware，需要重构
        // if (pipeline.EnableInstruction)
        // {
        //     builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.InstructionMiddleware>(_serviceProvider, inner));
        // }
        
        // [8] 缓存 (如果在缓存中找到，则短路后续调用)
        if (pipeline.EnableCaching)
        {
            builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.CachingMiddleware>(_serviceProvider, inner));
        }
        
        // ========== 第四层：成本控制 ==========
        
        // [9] Token 监控与计费
        if (pipeline.EnableTokenMonitoring)
        {
            var modelName = config.ModelId ?? name; // 使用配置的模型名或客户端名称
            builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.TokenMonitoringMiddleware>(
                _serviceProvider, inner, modelName));
        }
        
        // ========== 第五层：上下文管理 ==========
        
        // [10] Chat Reducer (上下文压缩)
        // 注意：项目已有 IChatReducer 接口和多种实现
        // 详见 Admin.NET.Ai.Services.Context 目录
        // 实际的压缩配置使用独立的 CompressionConfig
        if (pipeline.EnableChatReducer)
        {
            _logger.LogDebug("Chat Reducer enabled: Type={Type}", 
                pipeline.ChatReducerType ?? "MessageCounting (default)");
            // TODO: 需要实现 ReducerChatClient 包装器来集成 IChatReducer 到 ChatClientBuilder
        }
        
        // ========== 第六层：工具处理 ==========
        
        // [11] 工具验证 (TODO: 转换为 DelegatingChatClient)
        // 注意：ToolValidationMiddleware 实现 IRunMiddleware，需要重构
        // if (pipeline.EnableToolValidation)
        // {
        //     builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.ToolValidationMiddleware>(_serviceProvider, inner));
        // }
        
        // [12] 工具监控 (TODO: 转换为 DelegatingChatClient)
        // 注意：ToolMonitoringMiddleware 实现 IRunMiddleware，需要重构
        // if (pipeline.EnableToolMonitoring)
        // {
        //     builder.Use(inner => ActivatorUtilities.CreateInstance<Admin.NET.Ai.Middleware.ToolMonitoringMiddleware>(_serviceProvider, inner));
        // }
        
        // [13] Tool Reduction (工具精简 - 需要 Embedding 服务)
        // if (pipeline.EnableToolReduction)
        // {
        //     var embeddingGenerator = _serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        //     if (embeddingGenerator != null)
        //     {
        //         var strategy = new EmbeddingToolReductionStrategy(embeddingGenerator, pipeline.ToolLimit)
        //         {
        //             IsRequiredTool = tool => pipeline.RequiredToolPrefixes?.Any(p => tool.Name.StartsWith(p)) ?? false
        //         };
        //         builder.UseToolReduction(strategy);
        //     }
        // }
        
        // [14] Function Calling (使用 MEAI 内置 - 最内层)
        if (pipeline.EnableFunctionInvocation)
        {
            builder.UseFunctionInvocation(configure: options =>
            {
                options.AllowConcurrentInvocation = pipeline.AllowConcurrentInvocation;
                options.IncludeDetailedErrors = pipeline.IncludeDetailedErrors;
                options.MaximumIterationsPerRequest = pipeline.MaxIterationsPerRequest;
            });
        }

        return builder.Build();
    }

    /// <summary>
    /// 创建基础客户端 - 统一使用 OpenAI 兼容协议
    /// 所有主流供应商都兼容 OpenAI API，只需配置不同的 BaseUrl
    /// </summary>
    private IChatClient CreateBaseClient(LLMClientConfig config, string clientName)
    {
        // 验证必要配置
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            throw new ArgumentException($"ApiKey is required for client '{clientName}'");
        }
        if (string.IsNullOrEmpty(config.ModelId))
        {
            throw new ArgumentException($"ModelId is required for client '{clientName}'");
        }
        
        // 统一创建 - 所有供应商都走 OpenAI 兼容协议
        var options = new OpenAIClientOptions();
        
        // 自定义端点（DeepSeek, Qwen, Claude, Gemini 等都用 BaseUrl 区分）
        if (!string.IsNullOrEmpty(config.BaseUrl))
        {
            options.Endpoint = new Uri(config.BaseUrl);
        }
        
        // 超时设置
        if (config.TimeoutSeconds > 0)
        {
            options.NetworkTimeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
        }
        
        var client = new OpenAIClient(new ApiKeyCredential(config.ApiKey), options);
        
        _logger.LogDebug("Created client '{ClientName}' -> {BaseUrl}/{ModelId}", 
            clientName, 
            config.BaseUrl ?? "api.openai.com", 
            config.ModelId);
        
        // 1. 获取官方 IChatClient (MEAI 标准实现)
        var innerClient = new OpenAIChatClientAdapter(client.GetChatClient(config.ModelId), config.ModelId);

        // 2. 挂载图片下载中间件 (替代 OpenAIChatClientAdapter 的功能)
        // 自动下载 HTTP 图片并转换为 DataContent，支持内网/代理场景
        return ActivatorUtilities.CreateInstance<Core.Adapters.UriImageAdapter>(_serviceProvider, innerClient);
    }
    
    /// <summary>
    /// 获取客户端配置（合并默认值）
    /// </summary>
    public LLMClientConfig? GetClientConfig(string name)
    {
        if (!_options.Clients.TryGetValue(name, out var config))
            return null;
        
        // 如果有默认配置，合并
        if (_options.Defaults != null)
        {
            return config.MergeWithDefaults(_options.Defaults);
        }
        
        return config;
    }
    
    /// <summary>
    /// 检查客户端是否支持指定能力
    /// </summary>
    public bool SupportsCapability(string name, string capability)
    {
        var config = GetClientConfig(name);
        if (config == null) return false;
        
        return capability.ToLowerInvariant() switch
        {
            "vision" => config.SupportsVision,
            "functioncalling" or "tools" => config.SupportsFunctionCalling,
            "jsonschema" or "structuredoutput" => config.SupportsJsonSchema,
            "thinking" => config.SupportsThinking,
            "websearch" or "search" => config.SupportsWebSearch,
            "streaming" => config.EnableStreaming,
            _ => false
        };
    }

    /// <summary>
    /// 私有适配器，用于将 OpenAI.Chat.ChatClient 连接到 Microsoft.Extensions.AI.IChatClient
    /// 绕过预览包中缺失的扩展。
    /// </summary>

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
        
        var innerClient = new OpenAIChatClientAdapter(openAIClient.GetChatClient(config.ModelId), config.ModelId);
        return ActivatorUtilities.CreateInstance<Core.Adapters.UriImageAdapter>(_serviceProvider, innerClient);
    }

    public TAgent? CreateAgent<TAgent>(string clientName, string? agentName = null, string? instructions = null) where TAgent : class
    {
         // 1. 获取底层 ChatClient (通过配置名称)
         var client = GetChatClient(clientName);
         if (client == null) return null;
         
         // Agent 名称默认使用 clientName
         agentName ??= clientName;

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
                     // 设置名称和指令 (优先使用 IAiAgent 接口)
                     if (agent is Admin.NET.Ai.Abstractions.IAiAgent aiAgent)
                     {
                         if (!string.IsNullOrEmpty(agentName)) aiAgent.Name = agentName;
                         if (!string.IsNullOrEmpty(instructions)) aiAgent.Instructions = instructions;
                     }
                     else 
                     {
                        // 兼容方案：对于没有实现接口的 Agent，尝试使用反射兜底
                        if (!string.IsNullOrEmpty(agentName))
                        {
                            var nameProp = agent.GetType().GetProperty("Name");
                            if (nameProp != null && nameProp.CanWrite) nameProp.SetValue(agent, agentName);
                        }
                        if (!string.IsNullOrEmpty(instructions))
                        {
                            var instProp = agent.GetType().GetProperty("Instructions");
                            if (instProp != null && instProp.CanWrite) instProp.SetValue(agent, instructions);
                        }
                     }
                     
                     return agent;
                  }
 
                     return agent;
                  }
              catch (Exception ex)
              {
                  Console.WriteLine($"Error creating agent '{agentName}' with client '{clientName}': {ex.Message}");
              }
          }
          
          return default; 
     }
 

 
    public TAgent? CreateDefaultAgent<TAgent>(string? agentName = null, string? instructions = null) where TAgent : class
    {
        var defaultProvider = _options.DefaultProvider;
        if (string.IsNullOrEmpty(defaultProvider)) return null;
        return CreateAgent<TAgent>(defaultProvider, agentName, instructions);
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
