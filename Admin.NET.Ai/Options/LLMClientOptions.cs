using Furion.ConfigurableOptions;

namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM 客户端配置
/// </summary>
[OptionsSettings("LLM-Clients")]
public sealed class LLMClientsConfig : IConfigurableOptions
{
    /// <summary> 默认提供商 </summary>
    public string? DefaultProvider { get; set; }
    
    /// <summary> 全局默认配置（所有客户端继承） </summary>
    public LLMClientDefaults? Defaults { get; set; }

    /// <summary> LLM 客户端配置字典 </summary>
    public Dictionary<string, LLMClientConfig> Clients { get; set; } = new();
    
    /// <summary> 全局降级客户端列表 (按优先级排序) </summary>
    public List<string>? FallbackClients { get; set; }
    
    /// <summary>
    /// 获取合并了默认值的客户端配置
    /// </summary>
    public LLMClientConfig GetMergedConfig(string name)
    {
        if (!Clients.TryGetValue(name, out var config))
        {
            throw new KeyNotFoundException($"Client '{name}' not found in configuration");
        }
        
        if (Defaults == null)
        {
            return config;
        }
        
        // 应用默认值（配置优先，未设置的使用默认值）
        return config.MergeWithDefaults(Defaults);
    }
}

/// <summary>
/// 全局默认配置（可被客户端覆盖）
/// </summary>
public sealed class LLMClientDefaults
{
    /// <summary> 超时时间（秒） </summary>
    public int TimeoutSeconds { get; set; } = 60;
    
    /// <summary> 最大重试次数 </summary>
    public int MaxRetryCount { get; set; } = 2;
    
    /// <summary> 启用流式传输 </summary>
    public bool EnableStreaming { get; set; } = true;
    
    /// <summary> 是否支持函数调用 </summary>
    public bool SupportsFunctionCalling { get; set; } = true;
    
    /// <summary> 是否支持结构化 JSON Schema 输出 </summary>
    public bool SupportsJsonSchema { get; set; } = true;
    
    /// <summary> 是否支持视觉/图像理解 </summary>
    public bool SupportsVision { get; set; } = false;
    
    /// <summary> 是否支持思考过程输出（模型能力） </summary>
    public bool SupportsThinking { get; set; } = false;
    
    /// <summary> 是否支持搜索增强 </summary>
    public bool SupportsWebSearch { get; set; } = false;
    
    /// <summary> 最大上下文窗口 (tokens) </summary>
    public int MaxContextTokens { get; set; } = 4096;
    
    /// <summary> 每分钟请求限制 </summary>
    public int RateLimitPerMinute { get; set; } = 60;
    
    /// <summary> 并发请求限制 </summary>
    public int ConcurrencyLimit { get; set; } = 10;
    
    /// <summary> 思考模式配置 </summary>
    public ThinkingConfig? Thinking { get; set; }
    
    /// <summary> 中间件管道配置 </summary>
    public PipelineConfig? Pipeline { get; set; }
}

/// <summary>
/// 思考模式配置（用于支持思考的模型，如 o1、DeepSeek-R1 等）
/// </summary>
public sealed class ThinkingConfig
{
    /// <summary> 
    /// 默认是否启用思考模式
    /// 即使模型支持思考，也可以选择不启用
    /// </summary>
    public bool EnableByDefault { get; set; } = true;
    
    /// <summary> 
    /// 是否在响应中显示思考过程
    /// 设为 false 只返回最终结果，不返回思考内容
    /// </summary>
    public bool ShowThinkingProcess { get; set; } = true;
    
    /// <summary>
    /// 思考 Token 预算（某些模型如 Claude 支持）
    /// 0 表示使用模型默认值
    /// </summary>
    public int ThinkingBudgetTokens { get; set; } = 0;
    
    /// <summary>
    /// 思考模式参数名（不同供应商可能不同）
    /// 例如：DeepSeek 用 "reasoning_content", Claude 用 "thinking"
    /// </summary>
    public string? ThinkingParamName { get; set; }
}

/// <summary>
/// LLM 客户端配置
/// </summary>
public sealed class LLMClientConfig
{
    #region 基础配置
    
    /// <summary> API 密钥 </summary>
    public string? ApiKey { get; set; }

    /// <summary> 模型 ID </summary>
    public string? ModelId { get; set; }

    /// <summary> API 基础地址 </summary>
    public string? BaseUrl { get; set; }

    /// <summary> 超时时间（秒） </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary> 最大重试次数 </summary>
    public int MaxRetryCount { get; set; } = 2;

    /// <summary> 启用流式传输 </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary> 提供商类型 (e.g., azure, openai, deepseek, qwen, claude, zhipu, moonshot, siliconflow) </summary>
    public string? Provider { get; set; }
    
    #endregion

    #region 供应商个性化配置 (关键设计 - 避免抽象膨胀)
    
    /// <summary>
    /// 供应商特定配置 (由各适配器解读)
    /// 常见键:
    /// - api_version: API 版本 (Azure)
    /// - deployment_name: 部署名称 (Azure)
    /// - organization: 组织 ID (OpenAI)
    /// - region: 区域 (部分国内供应商)
    /// - enable_thinking: 启用思考过程 (DeepSeek R1)
    /// - anthropic_version: API 版本 (Claude)
    /// - custom_headers: 自定义请求头
    /// </summary>
    public Dictionary<string, object?>? ProviderOptions { get; set; }
    
    #endregion

    #region 能力声明
    
    /// <summary> 是否支持函数调用 </summary>
    public bool SupportsFunctionCalling { get; set; } = true;
    
    /// <summary> 是否支持结构化 JSON Schema 输出 </summary>
    public bool SupportsJsonSchema { get; set; } = true;
    
    /// <summary> 是否支持视觉/图像理解 </summary>
    public bool SupportsVision { get; set; } = false;
    
    /// <summary> 是否支持思考过程输出 (DeepSeek R1, o1, Claude 3.5) </summary>
    public bool SupportsThinking { get; set; } = false;
    
    /// <summary> 是否支持搜索增强 </summary>
    public bool SupportsWebSearch { get; set; } = false;
    
    /// <summary> 最大上下文窗口 (tokens) </summary>
    public int MaxContextTokens { get; set; } = 4096;
    
    /// <summary> 思考模式配置（覆盖 Defaults） </summary>
    public ThinkingConfig? Thinking { get; set; }
    
    #endregion

    #region 生成策略默认值
    
    /// <summary> 默认温度 (0.0-2.0) </summary>
    public float? DefaultTemperature { get; set; }
    
    /// <summary> 默认 TopP (0.0-1.0) </summary>
    public float? DefaultTopP { get; set; }
    
    /// <summary> 默认最大输出 tokens </summary>
    public int? DefaultMaxOutputTokens { get; set; }
    
    /// <summary> 停止序列 </summary>
    public List<string>? StopSequences { get; set; }
    
    #endregion

    #region 速率限制
    
    /// <summary> 每分钟请求限制 </summary>
    public int RateLimitPerMinute { get; set; } = 60;
    
    /// <summary> 并发请求限制 </summary>
    public int ConcurrencyLimit { get; set; } = 10;
    
    #endregion

    #region 中间件管道配置
    
    /// <summary> 中间件管道配置 </summary>
    public PipelineConfig? Pipeline { get; set; }
    
    #endregion

    #region 成本配置
    
    /// <summary> 输入价格（元/百万 tokens） </summary>
    public decimal InPrice { get; set; }

    /// <summary> 输出价格（元/百万 tokens） </summary>
    public decimal OutPrice { get; set; }
    
    /// <summary> 缓存输入价格（元/百万 tokens，部分供应商支持） </summary>
    public decimal CacheInPrice { get; set; }
    
    #endregion
    
    #region 降级配置
    
    /// <summary> 该客户端的降级备选列表 </summary>
    public List<string>? FallbackTo { get; set; }
    
    #endregion
    
    #region 配置合并
    
    /// <summary>
    /// 合并默认配置（客户端配置优先）
    /// </summary>
    public LLMClientConfig MergeWithDefaults(LLMClientDefaults defaults)
    {
        // 如果客户端未显式设置，使用默认值
        // 注意：由于 JSON 反序列化会设置默认值，这里用简单策略：
        // 只有当值等于类的初始默认值时，才使用全局默认值
        
        return new LLMClientConfig
        {
            // 必须的连接信息（不继承）
            ApiKey = ApiKey,
            ModelId = ModelId,
            BaseUrl = BaseUrl,
            Provider = Provider,
            ProviderOptions = ProviderOptions,
            
            // 可继承的配置
            TimeoutSeconds = TimeoutSeconds == 60 ? defaults.TimeoutSeconds : TimeoutSeconds,
            MaxRetryCount = MaxRetryCount == 2 ? defaults.MaxRetryCount : MaxRetryCount,
            EnableStreaming = EnableStreaming || defaults.EnableStreaming,
            
            // 能力声明（显式声明优先）
            SupportsFunctionCalling = SupportsFunctionCalling && defaults.SupportsFunctionCalling ? defaults.SupportsFunctionCalling : SupportsFunctionCalling,
            SupportsJsonSchema = SupportsJsonSchema && defaults.SupportsJsonSchema ? defaults.SupportsJsonSchema : SupportsJsonSchema,
            SupportsVision = SupportsVision || defaults.SupportsVision ? SupportsVision : defaults.SupportsVision,
            SupportsThinking = SupportsThinking || defaults.SupportsThinking ? SupportsThinking : defaults.SupportsThinking,
            SupportsWebSearch = SupportsWebSearch || defaults.SupportsWebSearch ? SupportsWebSearch : defaults.SupportsWebSearch,
            MaxContextTokens = MaxContextTokens == 4096 ? defaults.MaxContextTokens : MaxContextTokens,
            
            // 速率限制
            RateLimitPerMinute = RateLimitPerMinute == 60 ? defaults.RateLimitPerMinute : RateLimitPerMinute,
            ConcurrencyLimit = ConcurrencyLimit == 10 ? defaults.ConcurrencyLimit : ConcurrencyLimit,
            
            // Pipeline 配置
            Pipeline = Pipeline ?? defaults.Pipeline,
            
            // 生成策略（不继承，按客户端设置）
            DefaultTemperature = DefaultTemperature,
            DefaultTopP = DefaultTopP,
            DefaultMaxOutputTokens = DefaultMaxOutputTokens,
            StopSequences = StopSequences,
            
            // 成本（不继承）
            InPrice = InPrice,
            OutPrice = OutPrice,
            CacheInPrice = CacheInPrice,
            
            // 降级（不继承）
            FallbackTo = FallbackTo
        };
    }
    
    #endregion
}

/// <summary>
/// 中间件管道配置
/// </summary>
public sealed class PipelineConfig
{
    #region 核心中间件（推荐启用）
    
    /// <summary> 启用日志中间件 </summary>
    public bool EnableLogging { get; set; } = true;
    
    /// <summary> 启用审计中间件（合规审计） </summary>
    public bool EnableAudit { get; set; } = true;
    
    /// <summary> 启用 Token 监控中间件（成本监控） </summary>
    public bool EnableTokenMonitoring { get; set; } = true;
    
    #endregion
    
    #region 可选中间件
    
    /// <summary> 启用重试中间件 </summary>
    public bool EnableRetry { get; set; } = true;
    
    /// <summary> 启用限流中间件 </summary>
    public bool EnableRateLimiting { get; set; } = true;
    
    /// <summary> 启用缓存中间件 </summary>
    public bool EnableCaching { get; set; } = false;
    
    /// <summary> 缓存持续时间（分钟） </summary>
    public int CacheDurationMinutes { get; set; } = 5;
    
    /// <summary> 启用边界检查中间件（输入验证） </summary>
    public bool EnableBoundaryCheck { get; set; } = false;
    
    /// <summary> 启用指令中间件（系统提示注入） </summary>
    public bool EnableInstruction { get; set; } = false;
    
    /// <summary> 启用 RAG 追踪中间件 </summary>
    public bool EnableRAGTracing { get; set; } = false;
    
    #endregion
    
    #region 工具相关中间件
    
    /// <summary> 启用工具监控中间件 </summary>
    public bool EnableToolMonitoring { get; set; } = false;
    
    /// <summary> 启用工具验证中间件 </summary>
    public bool EnableToolValidation { get; set; } = false;
    
    #endregion
    
    #region Function Calling 配置
    
    /// <summary> 启用函数调用（UseFunctionInvocation） </summary>
    public bool EnableFunctionInvocation { get; set; } = true;
    
    /// <summary> 允许并发调用多个函数 </summary>
    public bool AllowConcurrentInvocation { get; set; } = false;
    
    /// <summary> 包含详细错误信息 </summary>
    public bool IncludeDetailedErrors { get; set; } = true;
    
    /// <summary> 每个请求最大迭代次数（防止无限循环） </summary>
    public int MaxIterationsPerRequest { get; set; } = 10;
    
    #endregion
    
    #region Tool Reduction 配置
    
    /// <summary> 启用工具精简（需要 Embedding 服务） </summary>
    public bool EnableToolReduction { get; set; } = false;
    
    /// <summary> 保留工具数量限制 </summary>
    public int ToolLimit { get; set; } = 5;
    
    /// <summary> 必需工具前缀（以此前缀开头的工具始终保留） </summary>
    public List<string>? RequiredToolPrefixes { get; set; }
    
    #endregion
    
    #region Chat Reducer 配置（上下文压缩）
    
    /// <summary> 
    /// 是否启用 Chat Reducer 中间件
    /// 注意：实际的压缩配置使用独立的 CompressionConfig
    /// </summary>
    public bool EnableChatReducer { get; set; } = false;
    
    /// <summary>
    /// Chat Reducer 类型（覆盖默认）
    /// - MessageCounting: 消息计数压缩
    /// - Summarizing: 智能摘要压缩
    /// - Adaptive: 自适应压缩
    /// - LayeredCompression: 分层压缩
    /// - KeywordAware: 关键词感知压缩
    /// </summary>
    public string? ChatReducerType { get; set; }
    
    #endregion
}
