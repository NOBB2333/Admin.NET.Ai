using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Configuration;
using Admin.NET.Ai.Core;
using Admin.NET.Ai.Middleware;
using Admin.NET.Ai.Services;
using Admin.NET.Ai.Services.Context;
using Admin.NET.Ai.Services.Cost;
using Admin.NET.Ai.Services.Data;
using Admin.NET.Ai.Services.Prompt;
using Admin.NET.Ai.Services.Rag;
using Admin.NET.Ai.Services.Tools;
using Admin.NET.Ai.Services.Workflow;
using Admin.NET.Ai.Storage;
using Admin.NET.Ai.Services.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using IChatReducer = Admin.NET.Ai.Abstractions.IChatReducer;

// 用于 CSharpScriptEngine，如果没有移动？我移动了它。
// using Admin.NET.Ai.Services.Workflow.Checkpoint;

namespace Admin.NET.Ai;

/// <summary>
/// Admin.NET.Ai 扩展方法
/// </summary>
public static class ServiceCollectionInit
{
    /// <summary>
    /// 注册 Admin.NET.Ai 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration">配置管理器</param>
    /// <returns></returns>
    public static IServiceCollection AddAdminNetAi(this IServiceCollection services, IConfigurationManager configuration)
    {
        // 0. 自动扫描并加载 Configuration 目录下的配置文件
        // 使用 AppContext.BaseDirectory 确保在所有环境(IIS, Docker, SingleFile)下稳定
        var baseDir = AppContext.BaseDirectory;
        var configDir = Path.Combine(baseDir, "Configuration");
        
        if (Directory.Exists(configDir))
        {
            var jsonFiles = Directory.GetFiles(configDir, "*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                // 使用支持注释的 JSON 加载器（允许 // 和 /* */ 注释，与 Furion 框架一致）
                // 这样配置会自动合并到 IConfiguration 中，后续的 Options 绑定也会自动生效
                configuration.AddJsonFileWithComments(file, optional: true, reloadOnChange: true);
                Console.WriteLine($"[Config] Loaded: {file}");
            }
        }
        else
        {
            Console.WriteLine($"[Config] Directory not found: {configDir}");
            Console.WriteLine($"[Config] BaseDirectory: {baseDir}");
        }

        // 调试：检查 LLM-Clients 部分是否存在
        var section = configuration.GetSection("LLM-Clients");
        var defaultProvider = section["DefaultProvider"];
        Console.WriteLine($"[Config] LLM-Clients:DefaultProvider = {defaultProvider}");

        // 0.1 配置注入
        // 注意：这里显式使用 configuration.GetSection 而不是 AddConfigurableOptions，
        // 防止 Console 程序中 Furion App 未初始化导致的 TypeInitializationException
        services.Configure<Admin.NET.Ai.Options.CompressionConfig>(configuration.GetSection("Compression"));
        services.Configure<Admin.NET.Ai.Options.LLMClientsConfig>(configuration.GetSection("LLM-Clients"));
        services.Configure<Admin.NET.Ai.Options.LLMMcpConfig>(configuration.GetSection("LLM-Mcp"));
        services.Configure<Admin.NET.Ai.Options.LLMAgentWorkflowConfig>(configuration.GetSection("LLM-AgentWorkflow"));
        services.Configure<Admin.NET.Ai.Options.LLMDocumentConfig>(configuration.GetSection("LLM-Document"));
        services.Configure<Admin.NET.Ai.Options.LLMCostControlConfig>(configuration.GetSection("LLM-CostControl"));
        services.Configure<Admin.NET.Ai.Options.LLMCapabilitiesConfig>(configuration.GetSection("LLM-Capabilities"));
        services.Configure<Admin.NET.Ai.Options.ContentSafetyOptions>(configuration.GetSection("ContentSafety"));
        services.Configure<Admin.NET.Ai.Options.LLMPersistenceConfig>(configuration.GetSection("LLM-Persistence"));
        
        // 0.2 兼容性绑定: 将 LLMAgentOptions 绑定到根配置，以支持尚未重构的服务
        services.Configure<Admin.NET.Ai.Options.LLMAgentOptions>(configuration);
        
        // 0.3 媒体生成配置
        services.Configure<Admin.NET.Ai.Options.LLMTtsConfig>(configuration.GetSection("LLM-Tts"));
        services.Configure<Admin.NET.Ai.Options.LLMAsrConfig>(configuration.GetSection("LLM-Asr"));
        services.Configure<Admin.NET.Ai.Options.LLMImageGenConfig>(configuration.GetSection("LLM-ImageGen"));
        services.Configure<Admin.NET.Ai.Options.LLMVideoGenConfig>(configuration.GetSection("LLM-VideoGen"));

        // 1. 注册核心服务
        services.TryAddScoped<IAiService, AiService>();
        services.TryAddSingleton<IAiFactory, AiFactory>();
        services.TryAddSingleton<AiPipelineBuilder>();
        
        // 1.1 注册 Keyed ChatClients (MEAI 标准)
        services.AddKeyedChatClients(configuration);
        
        // DevUI 集成 (MAF 真实实现)
        services.AddMafDevUI();
        
        // 2. 注册存储 (默认内存，实际可配置)
        services.TryAddSingleton<IChatMessageStore, InMemoryChatMessageStore>();
        services.TryAddSingleton<ICostStore, InMemoryCostStore>();
        services.TryAddSingleton<IAuditStore, InMemoryAuditStore>();

        // 3. 注册中间件
        // 3.1 新的 Agent 运行中间件和依赖项 (MEAI 风格)
        services.TryAddSingleton<ICostCalculator, ModelCostCalculator>();
        services.TryAddSingleton<IBudgetManager, BudgetManager>();
                services.TryAddSingleton<ITokenUsageStore, InMemoryTokenUsageStore>();
        services.TryAddSingleton<IBudgetStore, InMemoryBudgetStore>();
        services.TryAddSingleton<IRateLimiter, Admin.NET.Ai.Services.TokenBucketRateLimiter>();
        
        // 3.2 语义缓存 (简单关键词版)
        services.TryAddSingleton<ISemanticCache, Admin.NET.Ai.Services.Cache.SimpleSemanticCache>();
        
        // 3.3 工具权限和沙箱
        services.TryAddSingleton<IToolPermissionManager, Admin.NET.Ai.Services.Tools.ToolPermissionManager>();
        services.TryAddSingleton<IToolExecutionSandbox, Admin.NET.Ai.Services.Tools.ToolExecutionSandbox>();

        services.TryAddScoped<CachingMiddleware>();
        services.TryAddScoped<RateLimitingMiddleware>();
        services.TryAddScoped<TokenMonitoringMiddleware>();
        services.TryAddScoped<AuditMiddleware>();
        services.TryAddScoped<ToolMonitoringMiddleware>();
        services.TryAddScoped<ToolValidationMiddleware>();

        // 4. 注册业务服务
        // 工具和提示
        services.TryAddSingleton<IPromptManager, PromptManager>();
        services.TryAddSingleton<ToolManager>();
        
        // MCP 工具工厂 (使用官方 SDK)
        services.TryAddSingleton<Admin.NET.Ai.Services.MCP.McpToolFactory>();
        services.TryAddSingleton<Admin.NET.Ai.Services.MCP.McpToolDiscoveryService>();
        services.TryAddSingleton<Admin.NET.Ai.Services.MCP.McpServerService>();


        // RAG
        services.TryAddScoped<IGraphRagService, GraphRagService>();
        services.TryAddScoped<IRagService, RagService>();
        services.TryAddSingleton<RagStrategyFactory>();
        
        // RAG 增强组件
        services.TryAddSingleton<IDocumentChunker, Admin.NET.Ai.Services.Rag.DocumentChunker>();
        services.TryAddSingleton<Admin.NET.Ai.Services.Rag.IReranker, Admin.NET.Ai.Services.Rag.HybridReranker>();

        // 工作流和代理
        services.TryAddScoped<IWorkflowService, WorkflowService>();
        services.TryAddSingleton<NatashaScriptEngine>();
        services.TryAddSingleton<WorkflowStateService>(); // Worklfow Persistence
        services.TryAddScoped<HumanInputStepHandler>();   // Human Input Handler
        
        // 会话、数据和跟踪
        services.TryAddScoped<IConversationService, ConversationService>();
        services.TryAddSingleton<IStructuredOutputService, StructuredOutputService>();
        services.TryAddScoped<TraceService>();
        
        // 5. 媒体生成服务 (TTS, ASR, ImageGen, VideoGen)
        services.AddHttpClient<IMediaGenerationService, Admin.NET.Ai.Services.Media.MediaGenerationService>();

        // 6. 监控增强 (OpenTelemetry)
        services.TryAddSingleton<Admin.NET.Ai.Services.Monitoring.AgentTelemetry>();
        services.TryAddScoped<Admin.NET.Ai.Services.Monitoring.WorkflowMonitor>();
        
        // 7. 对话增强
        services.TryAddScoped<ConversationSummarizer>();
        services.TryAddSingleton<HybridChatMessageStore>();

        // 8. 高级 AI 能力 (Phase 3)
        services.TryAddScoped<Admin.NET.Ai.Services.Thinking.ReasoningService>();
        // PromptManager 应该已经注册过，或者如果是新加的依赖:
        // services.TryAddSingleton<IPromptManager, PromptManager>(); // 假设框架会自动扫描或已在上文注册
        // 如果没有，这里显式添加:
        services.TryAddSingleton<IPromptManager, PromptManager>();

        // 9. 内置 Agents (最佳实践)
        services.TryAddScoped<Admin.NET.Ai.Services.Processing.BatchProcessingService>();
        services.TryAddScoped<Admin.NET.Ai.Agents.BuiltIn.SentimentAnalysisAgent>();
        services.TryAddScoped<Admin.NET.Ai.Agents.BuiltIn.KnowledgeGraphAgent>();
        services.TryAddScoped<Admin.NET.Ai.Agents.BuiltIn.QualityEvaluatorAgent>();
        
        // 5. 上下文缩减器 (压缩)
        services.TryAddSingleton<CompressionMonitor>();
        
        // 注册具体策略
        services.TryAddScoped<MessageCountingReducer>();
        services.TryAddScoped<SummarizingReducer>();
        services.TryAddScoped<KeywordAwareReducer>();
        services.TryAddScoped<SystemMessageProtectionReducer>();
        services.TryAddScoped<FunctionCallPreservationReducer>();
        services.TryAddScoped<AdaptiveCompressionReducer>();
        services.TryAddScoped<LayeredCompressionReducer>();
        
        // 注册默认 Reducer (使用 Adaptive 作为默认智能入口，或者 Composite)
        // 这里我们默认使用 AdaptiveCompressionReducer，因为它涵盖了多维判断
        services.TryAddScoped<IChatReducer, AdaptiveCompressionReducer>();

        // 存储策略
        services.TryAddSingleton<FileChatMessageStore>();
        services.TryAddSingleton<InMemoryChatMessageStore>();
        services.TryAddSingleton<DatabaseChatMessageStore>();
        services.TryAddSingleton<RedisChatMessageStore>();
        services.TryAddSingleton<VectorChatMessageStore>();
        services.TryAddSingleton<CosmosDBChatMessageStore>();
        
        // MAF 存储 (Microsoft Agent Framework)
        services.TryAddScoped<IAgentChatMessageStore, DatabaseAgentChatMessageStore>();
        
        // 基于配置的动态注册 (基于 SK)
        services.AddSingleton<IChatMessageStore>(sp => 
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Admin.NET.Ai.Options.LLMPersistenceConfig>>().Value;
            var provider = options.Provider?.ToLowerInvariant();

            return provider switch
            {
                "database" => sp.GetRequiredService<DatabaseChatMessageStore>(),
                "redis" => sp.GetRequiredService<RedisChatMessageStore>(),
                "memory" => sp.GetRequiredService<InMemoryChatMessageStore>(),
                "vector" => sp.GetRequiredService<VectorChatMessageStore>(),
                "cosmos" or "cloud" => sp.GetRequiredService<CosmosDBChatMessageStore>(),
                _ => sp.GetRequiredService<FileChatMessageStore>() // 默认
            };
        });

        return services;
    }
    
    /// <summary>
    /// 注册 Keyed ChatClients (MEAI 标准)
    /// 支持通过 [FromKeyedServices("ClientName")] 注入
    /// </summary>
    private static void AddKeyedChatClients(this IServiceCollection services, IConfiguration configuration)
    {
        var clientsSection = configuration.GetSection("LLM-Clients:Clients");
        var clientNames = clientsSection.GetChildren().Select(c => c.Key).ToList();
        
        foreach (var name in clientNames)
        {
            // 使用工厂方法注册 Keyed Singleton
            services.AddKeyedSingleton<IChatClient>(name, (sp, key) =>
            {
                var factory = sp.GetRequiredService<IAiFactory>();
                var clientName = key?.ToString() ?? name;
                return factory.GetChatClient(clientName) 
                    ?? throw new InvalidOperationException($"Failed to create ChatClient '{clientName}'");
            });
        }
        
        Console.WriteLine($"[DI] Registered {clientNames.Count} Keyed ChatClients: {string.Join(", ", clientNames)}");
    }
}


// 简单的 Mock 实现，避免编译错误
public class InMemoryCostStore : ICostStore
{
    public Task SaveCostAsync(string requestId, int inputTokens, int outputTokens, string model, IDictionary<string, object?>? additionalData = null)
    {
        return Task.CompletedTask;
    }
}

public class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentBag<Abstractions.AuditLogEntry> _logs = new();

    public Task SaveAuditLogAsync(string requestId, string prompt, object? result, IDictionary<string, object?>? metadata = null)
    {
        _logs.Add(new Abstractions.AuditLogEntry 
        { 
            RequestId = requestId, 
            Prompt = prompt, 
            Result = result, 
            Metadata = metadata 
        });
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Abstractions.AuditLogEntry>> GetAuditLogsAsync(string traceId)
    {
        // 简单通过 RequestId 或 Metadata 中的 TraceId 进行过滤
        var results = _logs.Where(l => l.RequestId == traceId || 
            (l.Metadata != null && l.Metadata.TryGetValue("TraceId", out var tid) && tid?.ToString() == traceId))
            .OrderBy(l => l.Timestamp);
        
        return Task.FromResult<IEnumerable<Abstractions.AuditLogEntry>>(results);
    }
}
