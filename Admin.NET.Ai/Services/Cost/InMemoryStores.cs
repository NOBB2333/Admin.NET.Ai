using Admin.NET.Ai.Abstractions;
using System.Collections.Concurrent;

namespace Admin.NET.Ai.Services.Cost;

/// <summary>
/// 内存 Token 使用记录存储 (包含成本计算)
/// </summary>
public class InMemoryTokenUsageStore : ITokenUsageStore
{
    private readonly ConcurrentBag<TokenUsageRecord> _records = new();
    private readonly ConcurrentBag<CostRecord> _costRecords = new();

    // 模型价格配置 (每 1K Token)
    private static readonly Dictionary<string, (decimal input, decimal output)> ModelPrices = new()
    {
        { "deepseek-chat", (0.001m, 0.002m) },
        { "deepseek-reasoner", (0.004m, 0.016m) },
        { "qwen-plus", (0.0008m, 0.002m) },
        { "gpt-4o", (0.005m, 0.015m) },
        { "gpt-4o-mini", (0.00015m, 0.0006m) },
        { "claude-3-5-sonnet", (0.003m, 0.015m) }
    };

    #region 使用记录

    public Task RecordStartAsync(TokenUsageRecord record, CancellationToken cancellationToken = default)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task RecordCompletionAsync(TokenUsageRecord record, CancellationToken cancellationToken = default)
    {
        // 内存中引用相同，更新是自动的
        return Task.CompletedTask;
    }

    public Task<List<TokenUsageRecord>> GetUserUsageAsync(string userId, DateTime? start, DateTime? end, CancellationToken cancellationToken = default)
    {
        var query = _records.Where(r => r.UserId == userId);
        if (start.HasValue) query = query.Where(r => r.StartTime >= start.Value);
        if (end.HasValue) query = query.Where(r => r.StartTime <= end.Value);
        return Task.FromResult(query.ToList());
    }

    #endregion

    #region 成本计算与存储

    public decimal CalculateCost(TokenUsage usage, string modelName)
    {
        var normalizedName = modelName.ToLowerInvariant();
        
        // 查找匹配的模型价格
        var priceKey = ModelPrices.Keys.FirstOrDefault(k => normalizedName.Contains(k));
        if (priceKey == null)
        {
            // 默认价格
            return (usage.PromptTokens * 0.001m + usage.CompletionTokens * 0.002m) / 1000;
        }
        
        var (inputPrice, outputPrice) = ModelPrices[priceKey];
        return (usage.PromptTokens * inputPrice + usage.CompletionTokens * outputPrice) / 1000;
    }

    public Task SaveCostAsync(string requestId, int inputTokens, int outputTokens, string model, IDictionary<string, object?>? additionalData = null, CancellationToken cancellationToken = default)
    {
        var usage = new TokenUsage { PromptTokens = inputTokens, CompletionTokens = outputTokens };
        var cost = CalculateCost(usage, model);
        
        _costRecords.Add(new CostRecord
        {
            RequestId = requestId,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Model = model,
            Cost = cost,
            CreatedAt = DateTime.UtcNow
        });
        
        return Task.CompletedTask;
    }

    #endregion

    private class CostRecord
    {
        public string RequestId { get; set; } = null!;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public string Model { get; set; } = null!;
        public decimal Cost { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
