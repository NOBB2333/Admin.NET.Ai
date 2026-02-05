using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Admin.NET.Ai.Services.Cost;

/// <summary>
/// 配额管理器实现 (Token + Budget 多周期)
/// </summary>
public class QuotaManager : IQuotaManager
{
    private readonly ILogger<QuotaManager> _logger;
    private readonly LLMCostControlConfig _config;
    
    // 内存存储 (生产环境应替换为 IQuotaStore 实现)
    private readonly ConcurrentDictionary<string, QuotaUsage> _dailyUsage = new();
    private readonly ConcurrentDictionary<string, QuotaUsage> _monthlyUsage = new();
    private readonly ConcurrentDictionary<string, QuotaUsage> _totalUsage = new();

    public QuotaManager(
        ILogger<QuotaManager> logger,
        IOptions<LLMCostControlConfig> options)
    {
        _logger = logger;
        _config = options.Value;
    }

    public Task<QuotaCheckResult> CheckQuotaAsync(
        string userId, 
        string modelName, 
        CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            return Task.FromResult(new QuotaCheckResult
            {
                IsWithinQuota = true,
                TokenStatus = CreateUnlimitedStatus(QuotaPeriod.Daily),
                BudgetStatus = CreateUnlimitedStatus(QuotaPeriod.Daily)
            });
        }

        var dailyUsage = GetOrCreateUsage(userId, QuotaPeriod.Daily);
        var monthlyUsage = GetOrCreateUsage(userId, QuotaPeriod.Monthly);

        // 检查 Token 配额
        var tokenDailyExceeded = _config.Token.DailyLimit > 0 && dailyUsage.TokensUsed >= _config.Token.DailyLimit;
        var tokenMonthlyExceeded = _config.Token.MonthlyLimit > 0 && monthlyUsage.TokensUsed >= _config.Token.MonthlyLimit;

        // 检查 Budget 配额
        var budgetDailyExceeded = _config.Budget.DailyLimit > 0 && dailyUsage.CostUsed >= _config.Budget.DailyLimit;
        var budgetMonthlyExceeded = _config.Budget.MonthlyLimit > 0 && monthlyUsage.CostUsed >= _config.Budget.MonthlyLimit;

        string? blockReason = null;
        if (tokenDailyExceeded) blockReason = "每日 Token 配额已用尽";
        else if (tokenMonthlyExceeded) blockReason = "每月 Token 配额已用尽";
        else if (budgetDailyExceeded) blockReason = "每日预算已用尽";
        else if (budgetMonthlyExceeded) blockReason = "每月预算已用尽";

        var isWithinQuota = blockReason == null;

        return Task.FromResult(new QuotaCheckResult
        {
            IsWithinQuota = isWithinQuota,
            BlockReason = blockReason,
            TokenStatus = new QuotaStatus
            {
                Used = dailyUsage.TokensUsed,
                Limit = _config.Token.DailyLimit,
                Period = QuotaPeriod.Daily,
                ResetTime = GetNextResetTime(QuotaPeriod.Daily)
            },
            BudgetStatus = new QuotaStatus
            {
                Used = dailyUsage.CostUsed,
                Limit = _config.Budget.DailyLimit,
                Period = QuotaPeriod.Daily,
                ResetTime = GetNextResetTime(QuotaPeriod.Daily)
            }
        });
    }

    public Task<QuotaStatus> GetStatusAsync(
        string userId, 
        QuotaPeriod period, 
        CancellationToken cancellationToken = default)
    {
        var usage = GetOrCreateUsage(userId, period);
        var (tokenLimit, budgetLimit) = period switch
        {
            QuotaPeriod.Daily => (_config.Token.DailyLimit, _config.Budget.DailyLimit),
            QuotaPeriod.Monthly => (_config.Token.MonthlyLimit, _config.Budget.MonthlyLimit),
            QuotaPeriod.Total => (_config.Token.TotalLimit, _config.Budget.TotalLimit),
            _ => (0L, 0m)
        };

        return Task.FromResult(new QuotaStatus
        {
            Used = usage.TokensUsed,
            Limit = tokenLimit,
            Period = period,
            ResetTime = GetNextResetTime(period)
        });
    }

    public Task RecordUsageAsync(
        string userId, 
        string modelName, 
        long tokens, 
        decimal cost, 
        CancellationToken cancellationToken = default)
    {
        // 更新所有周期的使用量
        UpdateUsage(userId, QuotaPeriod.Daily, tokens, cost);
        UpdateUsage(userId, QuotaPeriod.Monthly, tokens, cost);
        UpdateUsage(userId, QuotaPeriod.Total, tokens, cost);

        _logger.LogDebug("📊 用户 {UserId} 使用记录: {Tokens} tokens, ¥{Cost:F4}", userId, tokens, cost);

        return Task.CompletedTask;
    }

    public Task ResetQuotaAsync(
        string userId, 
        QuotaPeriod period, 
        CancellationToken cancellationToken = default)
    {
        var key = $"{userId}:{period}";
        var emptyUsage = new QuotaUsage { PeriodStart = DateTime.UtcNow };

        switch (period)
        {
            case QuotaPeriod.Daily:
                _dailyUsage[key] = emptyUsage;
                break;
            case QuotaPeriod.Monthly:
                _monthlyUsage[key] = emptyUsage;
                break;
            case QuotaPeriod.Total:
                _totalUsage[key] = emptyUsage;
                break;
        }

        _logger.LogInformation("🔄 用户 {UserId} 配额已重置: {Period}", userId, period);
        return Task.CompletedTask;
    }

    #region Private Helpers

    private QuotaUsage GetOrCreateUsage(string userId, QuotaPeriod period)
    {
        var key = $"{userId}:{period}";
        var storage = period switch
        {
            QuotaPeriod.Daily => _dailyUsage,
            QuotaPeriod.Monthly => _monthlyUsage,
            _ => _totalUsage
        };

        return storage.GetOrAdd(key, _ => new QuotaUsage { PeriodStart = DateTime.UtcNow });
    }

    private void UpdateUsage(string userId, QuotaPeriod period, long tokens, decimal cost)
    {
        var key = $"{userId}:{period}";
        var storage = period switch
        {
            QuotaPeriod.Daily => _dailyUsage,
            QuotaPeriod.Monthly => _monthlyUsage,
            _ => _totalUsage
        };

        storage.AddOrUpdate(key,
            _ => new QuotaUsage { TokensUsed = tokens, CostUsed = cost, PeriodStart = DateTime.UtcNow },
            (_, existing) => existing with
            {
                TokensUsed = existing.TokensUsed + tokens,
                CostUsed = existing.CostUsed + cost
            });
    }

    private static QuotaStatus CreateUnlimitedStatus(QuotaPeriod period) => new()
    {
        Used = 0,
        Limit = 0,
        Period = period,
        ResetTime = null
    };

    private static DateTime? GetNextResetTime(QuotaPeriod period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            QuotaPeriod.Daily => now.Date.AddDays(1),
            QuotaPeriod.Monthly => new DateTime(now.Year, now.Month, 1).AddMonths(1),
            _ => null
        };
    }

    #endregion
}
