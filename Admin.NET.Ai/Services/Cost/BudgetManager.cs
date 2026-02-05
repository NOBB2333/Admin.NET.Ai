using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Cost;

public class BudgetManager : IBudgetManager
{
    private readonly IBudgetStore _budgetStore;
    private readonly ILogger<BudgetManager> _logger;

    public BudgetManager(IBudgetStore budgetStore, ILogger<BudgetManager> logger)
    {
        _budgetStore = budgetStore;
        _logger = logger;
    }

    public async Task<BudgetCheckResult> CheckBudgetAsync(string userId, string modelName, CancellationToken cancellationToken = default)
    {
        var budget = await _budgetStore.GetUserBudgetAsync(userId, modelName, cancellationToken) 
                     ?? CreateDefaultBudget(userId, modelName);

        var currentUsage = await _budgetStore.GetCurrentUsageAsync(userId, modelName, cancellationToken);

        return new BudgetCheckResult
        {
            IsWithinBudget = currentUsage < budget.MonthlyLimit,
            UsedAmount = currentUsage,
            BudgetAmount = budget.MonthlyLimit,
            UsagePercentage = budget.MonthlyLimit > 0 ? currentUsage / budget.MonthlyLimit : 1.0m
        };
    }

    public async Task<BudgetStatus> GetBudgetStatusAsync(string userId, string modelName, CancellationToken cancellationToken = default)
    {
        var budget = await _budgetStore.GetUserBudgetAsync(userId, modelName, cancellationToken) 
                     ?? CreateDefaultBudget(userId, modelName);

        var usage = await _budgetStore.GetCurrentUsageAsync(userId, modelName, cancellationToken);

        return new BudgetStatus
        {
            UserId = userId,
            Model = modelName,
            MonthlyLimit = budget.MonthlyLimit,
            CurrentUsage = usage,
            UsagePercentage = budget.MonthlyLimit > 0 ? usage / budget.MonthlyLimit : 1.0m,
            Remaining = budget.MonthlyLimit - usage,
            ResetDate = GetNextResetDate()
        };
    }

    public async Task RecordUsageAsync(string userId, string modelName, decimal amount, CancellationToken cancellationToken = default)
    {
        await _budgetStore.RecordUsageAsync(userId, modelName, amount, cancellationToken);

        var status = await GetBudgetStatusAsync(userId, modelName, cancellationToken);
        if (status.UsagePercentage >= 0.9m)
        {
            _logger.LogWarning("🔔 用户 {UserId} {Model} 预算使用已达 {Percentage:P0}", 
                userId, modelName, status.UsagePercentage);
        }
    }

    private UserBudget CreateDefaultBudget(string userId, string modelName)
    {
        return new UserBudget
        {
            UserId = userId,
            Model = modelName,
            MonthlyLimit = 100m, // 默认100元/月
            CreatedAt = DateTime.UtcNow
        };
    }

    private DateTime GetNextResetDate()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1).AddMonths(1);
    }
}
