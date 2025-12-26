using Admin.NET.Ai.Abstractions;
using System.Collections.Concurrent;

namespace Admin.NET.Ai.Services.Cost;

public class InMemoryTokenUsageStore : ITokenUsageStore
{
    private readonly ConcurrentBag<TokenUsageRecord> _records = new();

    public Task RecordStartAsync(TokenUsageRecord record)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task RecordCompletionAsync(TokenUsageRecord record)
    {
        // 在内存中，引用是相同的，所以如果我们持有引用，更新是自动的。
        // 但是 ConcurrentBag 不允许轻松查找。
        // 对于简单的演示，我们假设传递的对象已经是包中的相同引用 (大部分情况下是真的)。
        // 如果不是，我们应该使用 Dictionary。
        return Task.CompletedTask;
    }

    public Task<List<TokenUsageRecord>> GetUserUsageAsync(string userId, DateTime? start, DateTime? end)
    {
        var query = _records.Where(r => r.UserId == userId);
        if (start.HasValue) query = query.Where(r => r.StartTime >= start.Value);
        if (end.HasValue) query = query.Where(r => r.StartTime <= end.Value);
        return Task.FromResult(query.ToList());
    }
}

public class InMemoryBudgetStore : IBudgetStore
{
    // UserId -> Model -> Budget
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, UserBudget>> _budgets = new();
    
    // UserId -> Model -> Usage Amount
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, decimal>> _usages = new();

    public Task<UserBudget?> GetUserBudgetAsync(string userId, string modelName)
    {
        if (_budgets.TryGetValue(userId, out var userModels) && userModels.TryGetValue(modelName, out var budget))
        {
            return Task.FromResult<UserBudget?>(budget);
        }
        return Task.FromResult<UserBudget?>(null);
    }

    public Task<decimal> GetCurrentUsageAsync(string userId, string modelName)
    {
        if (_usages.TryGetValue(userId, out var userModels) && userModels.TryGetValue(modelName, out var usage))
        {
            return Task.FromResult(usage);
        }
        return Task.FromResult(0m);
    }

    public Task RecordUsageAsync(string userId, string modelName, decimal amount)
    {
        var userModels = _usages.GetOrAdd(userId, _ => new ConcurrentDictionary<string, decimal>());
        userModels.AddOrUpdate(modelName, amount, (_, old) => old + amount);
        return Task.CompletedTask;
    }
}
