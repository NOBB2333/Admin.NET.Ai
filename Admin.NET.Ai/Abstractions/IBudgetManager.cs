namespace Admin.NET.Ai.Abstractions;

#region 配额管理接口

/// <summary>
/// 配额管理接口 (Token + Budget 统一管理)
/// 支持每日/每月/总量配额检查
/// </summary>
public interface IQuotaManager
{
    /// <summary>
    /// 检查用户配额 (Token + Budget)
    /// </summary>
    Task<QuotaCheckResult> CheckQuotaAsync(
        string userId, 
        string modelName, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取指定周期的配额状态
    /// </summary>
    Task<QuotaStatus> GetStatusAsync(
        string userId, 
        QuotaPeriod period, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 记录使用量
    /// </summary>
    Task RecordUsageAsync(
        string userId, 
        string modelName, 
        long tokens, 
        decimal cost, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 重置用户配额 (管理操作)
    /// </summary>
    Task ResetQuotaAsync(
        string userId, 
        QuotaPeriod period, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 配额周期
/// </summary>
public enum QuotaPeriod
{
    Daily,
    Monthly,
    Total
}

#endregion

#region 配额存储接口

/// <summary>
/// 配额存储接口 (持久化)
/// </summary>
public interface IQuotaStore
{
    Task<QuotaUsage> GetUsageAsync(string userId, QuotaPeriod period, CancellationToken ct = default);
    Task RecordUsageAsync(string userId, long tokens, decimal cost, CancellationToken ct = default);
    Task ResetUsageAsync(string userId, QuotaPeriod period, CancellationToken ct = default);
}

/// <summary>
/// 配额使用量
/// </summary>
public record QuotaUsage
{
    public long TokensUsed { get; init; }
    public decimal CostUsed { get; init; }
    public DateTime PeriodStart { get; init; }
}

#endregion

#region 检查结果和状态

/// <summary>
/// 配额检查结果
/// </summary>
public record QuotaCheckResult
{
    /// <summary>是否在配额内</summary>
    public bool IsWithinQuota { get; init; }
    
    /// <summary>Token 配额状态</summary>
    public QuotaStatus TokenStatus { get; init; } = null!;
    
    /// <summary>Budget 配额状态</summary>
    public QuotaStatus BudgetStatus { get; init; } = null!;
    
    /// <summary>被限制的原因</summary>
    public string? BlockReason { get; init; }
}

/// <summary>
/// 配额状态
/// </summary>
public record QuotaStatus
{
    /// <summary>已使用量</summary>
    public decimal Used { get; init; }
    
    /// <summary>配额上限 (0 = 不限制)</summary>
    public decimal Limit { get; init; }
    
    /// <summary>使用率 (0-1)</summary>
    public double UsagePercentage => Limit > 0 ? (double)(Used / Limit) : 0;
    
    /// <summary>剩余量</summary>
    public decimal Remaining => Limit > 0 ? Math.Max(0, Limit - Used) : decimal.MaxValue;
    
    /// <summary>是否超额</summary>
    public bool IsExceeded => Limit > 0 && Used >= Limit;
    
    /// <summary>重置时间</summary>
    public DateTime? ResetTime { get; init; }
    
    /// <summary>周期</summary>
    public QuotaPeriod Period { get; init; }
}

#endregion

#region 兼容性别名 (deprecated)

/// <summary>
/// [Deprecated] 使用 IQuotaManager 代替
/// 保留此接口仅为兼容性，继承自 IQuotaManager
/// </summary>
[Obsolete("Use IQuotaManager instead")]
public interface IBudgetManager : IQuotaManager { }

#endregion
