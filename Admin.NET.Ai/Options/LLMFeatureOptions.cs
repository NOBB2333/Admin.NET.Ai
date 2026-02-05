using Furion.ConfigurableOptions;
using System.Text.Json.Serialization;

namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM Agent 工作流配置
/// </summary>
[OptionsSettings("LLM-AgentWorkflow")]
public sealed class LLMAgentWorkflowConfig : IConfigurableOptions
{
    /// <summary> 是否启用 Agent 工作流 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 工作流定义路径 </summary>
    public string? WorkflowPath { get; set; }

    /// <summary> 最大执行步骤数 </summary>
    public int MaxSteps { get; set; } = 50;

    /// <summary> 超时时间（秒） </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary> 是否启用并行执行 </summary>
    public bool EnableParallelExecution { get; set; } = false;

    /// <summary> 最大并行任务数 </summary>
    public int MaxParallelTasks { get; set; } = 3;

    /// <summary> 工具链配置 </summary>
    public AgentToolchainConfig Toolchain { get; set; } = new();
}

/// <summary>
/// Agent 工具链配置
/// </summary>
public sealed class AgentToolchainConfig
{
    /// <summary> 可用工具列表 </summary>
    public List<string> AvailableTools { get; set; } = new();

    /// <summary> 工具配置字典 </summary>
    public Dictionary<string, Dictionary<string, object>> ToolConfigs { get; set; } = new();

    /// <summary> 是否启用工具自动发现 </summary>
    public bool EnableAutoDiscovery { get; set; } = true;
}

/// <summary>
/// LLM 文档处理配置
/// </summary>
[OptionsSettings("LLM-Document")]
public sealed class LLMDocumentConfig : IConfigurableOptions
{
    /// <summary> 文档解析器配置 </summary>
    public DocumentParserConfig Parser { get; set; } = new();

    /// <summary> 文档预处理配置 </summary>
    public DocumentPreprocessConfig Preprocess { get; set; } = new();

    /// <summary> 文档存储配置 </summary>
    public DocumentStorageConfig Storage { get; set; } = new();
}

/// <summary>
/// 文档解析器配置
/// </summary>
public sealed class DocumentParserConfig
{
    /// <summary> 支持的文档格式 </summary>
    public List<string> SupportedFormats { get; set; } = new() { "pdf", "docx", "txt", "md", "html", "xlsx", "pptx" };

    /// <summary> 最大文件大小（MB） </summary>
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary> 是否启用 OCR </summary>
    public bool EnableOcr { get; set; } = false;

    /// <summary> OCR 提供商 </summary>
    public string? OcrProvider { get; set; }

    /// <summary> OCR 配置 </summary>
    public Dictionary<string, object> OcrConfig { get; set; } = new();
}

/// <summary>
/// 文档预处理配置
/// </summary>
public sealed class DocumentPreprocessConfig
{
    /// <summary> 是否启用文本清理 </summary>
    public bool EnableTextCleaning { get; set; } = true;

    /// <summary> 是否启用去重 </summary>
    public bool EnableDeduplication { get; set; } = true;

    /// <summary> 是否启用语言检测 </summary>
    public bool EnableLanguageDetection { get; set; } = true;

    /// <summary> 是否启用敏感信息过滤 </summary>
    public bool EnableSensitiveInfoFilter { get; set; } = false;
}

/// <summary>
/// 文档存储配置
/// </summary>
public sealed class DocumentStorageConfig
{
    /// <summary> 存储类型: Local, S3, AzureBlob, OSS </summary>
    public string? Type { get; set; } = "Local";

    /// <summary> 存储路径或连接字符串 </summary>
    public string? Path { get; set; }

    /// <summary> 是否启用版本控制 </summary>
    public bool EnableVersionControl { get; set; } = true;

    /// <summary> 存储配置 </summary>
    public Dictionary<string, object> Config { get; set; } = new();
}

/// <summary>
/// LLM 成本控制配置 (含配额管理)
/// </summary>
[OptionsSettings("LLM-CostControl")]
public sealed class LLMCostControlConfig : IConfigurableOptions
{
    /// <summary> 是否启用成本控制 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> Token 配额配置 (多周期) </summary>
    public TokenQuotaConfig Token { get; set; } = new();

    /// <summary> 预算配额配置 (多周期) </summary>
    public BudgetQuotaConfig Budget { get; set; } = new();

    /// <summary> 限流配置 </summary>
    public RateLimitConfig RateLimiting { get; set; } = new();

    /// <summary> 超预算审批配置 </summary>
    public OverBudgetApprovalConfig OverBudgetApproval { get; set; } = new();

    /// <summary> 告警配置 </summary>
    public CostAlertConfig Alerts { get; set; } = new();
}

/// <summary>
/// Token 配额配置 (多周期)
/// </summary>
public sealed class TokenQuotaConfig
{
    /// <summary> 每日 Token 限额 (0 = 不限制) </summary>
    public long DailyLimit { get; set; } = 10_000_000;
    
    /// <summary> 每月 Token 限额 (0 = 不限制) </summary>
    public long MonthlyLimit { get; set; } = 100_000_000;
    
    /// <summary> 总 Token 限额 (0 = 不限制) </summary>
    public long TotalLimit { get; set; } = 0;
}

/// <summary>
/// 预算配额配置 (多周期)
/// </summary>
public sealed class BudgetQuotaConfig
{
    /// <summary> 每日预算限额 (0 = 不限制) </summary>
    public decimal DailyLimit { get; set; } = 1000;
    
    /// <summary> 每月预算限额 (0 = 不限制) </summary>
    public decimal MonthlyLimit { get; set; } = 30000;
    
    /// <summary> 总预算限额 (0 = 不限制) </summary>
    public decimal TotalLimit { get; set; } = 0;
}

/// <summary>
/// 限流配置
/// </summary>
public sealed class RateLimitConfig
{
    /// <summary> 每分钟最大请求数 </summary>
    public int MaxRequestsPerMinute { get; set; } = 60;
    
    /// <summary> 最大并发请求数 </summary>
    public int MaxConcurrentRequests { get; set; } = 10;
    
    /// <summary> 每秒补充的令牌数 (用于令牌桶算法) </summary>
    public int TokensPerSecond { get; set; } = 10;
    
    /// <summary> 令牌桶容量 </summary>
    public int BucketCapacity { get; set; } = 100;
}

/// <summary>
/// 超预算审批配置
/// </summary>
public sealed class OverBudgetApprovalConfig
{
    /// <summary> 是否启用超预算审批 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 单次审批最大额度（元） </summary>
    public decimal MaxApprovalAmount { get; set; } = 500;

    /// <summary> 审批额度有效期（小时，24小时即当天一次性） </summary>
    public int ApprovalExpiryHours { get; set; } = 24;

    /// <summary> 超出预算是否需要审批 </summary>
    public bool RequireApproval { get; set; } = true;
}

/// <summary>
/// 成本告警配置
/// </summary>
public sealed class CostAlertConfig
{
    /// <summary> 是否启用告警 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 告警阈值（0-1之间，如0.8表示80%） </summary>
    public double Threshold { get; set; } = 0.8;

    /// <summary> 告警邮箱 </summary>
    public string? Email { get; set; }
}

/// <summary>
/// 成本追踪配置
/// </summary>
public sealed class CostTrackingConfig
{
    /// <summary> 是否启用成本追踪 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 成本数据存储位置: Database, File, Redis </summary>
    public string? Storage { get; set; } = "Database";

    /// <summary> 数据保留天数 </summary>
    public int RetentionDays { get; set; } = 90;
}

/// <summary>
/// 通用大模型能力扩展配置
/// </summary>
[OptionsSettings("LLM-Capabilities")]
public sealed class LLMCapabilitiesConfig : IConfigurableOptions
{
    /// <summary> 函数调用配置 </summary>
    public FunctionCallingConfig FunctionCalling { get; set; } = new();

    /// <summary> 视觉能力配置 </summary>
    public VisionConfig Vision { get; set; } = new();

    /// <summary> 缓存配置 </summary>
    public CachingConfig Caching { get; set; } = new();

    /// <summary> 遥测监控配置 </summary>
    public TelemetryConfig Telemetry { get; set; } = new();
}

/// <summary>
/// 函数调用配置
/// </summary>
public sealed class FunctionCallingConfig
{
    /// <summary> 是否启用函数调用 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 函数调用安全审核配置 </summary>
    public SafetyGuardConfig SafetyGuard { get; set; } = new();
}

/// <summary>
/// 安全审核配置
/// </summary>
public sealed class SafetyGuardConfig
{
    /// <summary> 函数调用安全审核提供商 </summary>
    public string? Provider { get; set; }

    /// <summary> 安全审核模型 </summary>
    public string? Model { get; set; }
}

/// <summary>
/// 视觉能力配置
/// </summary>
public sealed class VisionConfig
{
    /// <summary> 多模态能力提供商 </summary>
    public string? Provider { get; set; }

    /// <summary> 视觉模型 </summary>
    public string? Model { get; set; }

    /// <summary> 最大图片大小（MB） </summary>
    public int MaxImageSizeMB { get; set; } = 5;
}

/// <summary>
/// 缓存配置
/// </summary>
public sealed class CachingConfig
{
    /// <summary> 缓存服务类型: Redis, Memory, File </summary>
    public string? Provider { get; set; }

    /// <summary> 缓存连接字符串 </summary>
    public string? ConnectionString { get; set; }

    /// <summary> 默认缓存时间（秒） </summary>
    public int DefaultTtlSeconds { get; set; } = 600;
}

/// <summary>
/// 遥测监控配置
/// </summary>
public sealed class TelemetryConfig
{
    /// <summary> 是否开启调用监控 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 监控数据接收端 </summary>
    public string? Sink { get; set; }

    /// <summary> 上报地址 </summary>
    public string? Endpoint { get; set; }
}

/// <summary>
/// AI 持久化存储配置
/// </summary>
[OptionsSettings("LLM-Persistence")]
public sealed class LLMPersistenceConfig : IConfigurableOptions
{
    /// <summary> 存储提供商: Database, File, Redis, Memory </summary>
    public string? Provider { get; set; } = "Memory";

    /// <summary> 文件存储路径 (Provider=File时有效) </summary>
    public string? FilePath { get; set; }

    /// <summary> Redis 连接字符串 (Provider=Redis时有效) </summary>
    public string? RedisConnectionString { get; set; }
}
