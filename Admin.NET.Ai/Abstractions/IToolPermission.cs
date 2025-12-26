namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 工具权限接口
/// </summary>
public interface IToolPermissionManager
{
    /// <summary>
    /// 检查用户是否有权限执行工具
    /// </summary>
    Task<ToolPermissionResult> CheckPermissionAsync(string userId, string toolName, IDictionary<string, object?>? arguments = null);
    
    /// <summary>
    /// 获取用户可用的工具列表
    /// </summary>
    Task<IEnumerable<string>> GetAllowedToolsAsync(string userId);
    
    /// <summary>
    /// 注册工具权限规则
    /// </summary>
    void RegisterRule(ToolPermissionRule rule);
}

/// <summary>
/// 权限检查结果
/// </summary>
public class ToolPermissionResult
{
    public bool IsAllowed { get; set; }
    public string? DeniedReason { get; set; }
    public PermissionLevel Level { get; set; } = PermissionLevel.Normal;
    
    public static ToolPermissionResult Allow() => new() { IsAllowed = true };
    public static ToolPermissionResult Deny(string reason) => new() { IsAllowed = false, DeniedReason = reason };
}

/// <summary>
/// 权限级别
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// 普通操作
    /// </summary>
    Normal,
    
    /// <summary>
    /// 敏感操作 (需要额外确认)
    /// </summary>
    Sensitive,
    
    /// <summary>
    /// 危险操作 (需要管理员审批)
    /// </summary>
    Dangerous,
    
    /// <summary>
    /// 禁止操作
    /// </summary>
    Forbidden
}

/// <summary>
/// 权限规则
/// </summary>
public class ToolPermissionRule
{
    public string? ToolNamePattern { get; set; } // 支持通配符，如 "delete_*"
    public HashSet<string> AllowedRoles { get; set; } = new();
    public HashSet<string> DeniedRoles { get; set; } = new();
    public PermissionLevel Level { get; set; } = PermissionLevel.Normal;
    public int MaxCallsPerMinute { get; set; } = 100;
    public bool RequireAudit { get; set; }
}

/// <summary>
/// 工具执行沙箱接口
/// </summary>
public interface IToolExecutionSandbox
{
    /// <summary>
    /// 在沙箱中执行工具
    /// </summary>
    Task<ToolExecutionResult> ExecuteAsync(string toolName, Func<Task<object?>> executor, SandboxOptions? options = null);
}

/// <summary>
/// 沙箱选项
/// </summary>
public class SandboxOptions
{
    /// <summary>
    /// 执行超时 (毫秒)
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// 最大返回结果大小 (字符)
    /// </summary>
    public int MaxResultSize { get; set; } = 10000;
    
    /// <summary>
    /// 是否隔离执行
    /// </summary>
    public bool Isolated { get; set; } = false;
    
    /// <summary>
    /// 是否捕获异常
    /// </summary>
    public bool CaptureExceptions { get; set; } = true;
}

/// <summary>
/// 工具执行结果
/// </summary>
public class ToolExecutionResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool WasTruncated { get; set; }
    public bool TimedOut { get; set; }
}
