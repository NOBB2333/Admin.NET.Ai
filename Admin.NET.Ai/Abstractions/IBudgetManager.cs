namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 预算管理接口
/// 处理预算检查和使用量记录
/// </summary>
public interface IBudgetManager
{
    Task<BudgetCheckResult> CheckBudgetAsync(string userId, string modelName, CancellationToken cancellationToken = default);
    Task<BudgetStatus> GetBudgetStatusAsync(string userId, string modelName, CancellationToken cancellationToken = default);
    Task RecordUsageAsync(string userId, string modelName, decimal amount, CancellationToken cancellationToken = default);
}

/// <summary>
/// 预算存储接口
/// 持久化用户预算配置和使用量
/// </summary>
public interface IBudgetStore
{
    Task<UserBudget?> GetUserBudgetAsync(string userId, string modelName, CancellationToken cancellationToken = default);
    Task<decimal> GetCurrentUsageAsync(string userId, string modelName, CancellationToken cancellationToken = default);
    Task RecordUsageAsync(string userId, string modelName, decimal amount, CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户预算配置
/// </summary>
public class UserBudget
{
    public string UserId { get; set; } = null!;
    public string Model { get; set; } = null!;
    public decimal MonthlyLimit { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 预算检查结果
/// </summary>
public record BudgetCheckResult
{
    public bool IsWithinBudget { get; init; }
    public decimal UsedAmount { get; init; }
    public decimal BudgetAmount { get; init; }
    public decimal UsagePercentage { get; init; }
}

/// <summary>
/// 预算状态
/// </summary>
public record BudgetStatus
{
    public string UserId { get; init; } = null!;
    public string Model { get; init; } = null!;
    public decimal MonthlyLimit { get; init; }
    public decimal CurrentUsage { get; init; }
    public decimal UsagePercentage { get; init; }
    public decimal Remaining { get; init; }
    public DateTime ResetDate { get; init; }
}
