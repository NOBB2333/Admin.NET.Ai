using Admin.NET.Ai.Middleware; // For models like TokenUsage

namespace Admin.NET.Ai.Abstractions;

public interface ICostCalculator
{
    decimal CalculateCost(TokenUsage usage, string modelName);
}

public interface IBudgetManager
{
    Task<BudgetCheckResult> CheckBudgetAsync(string userId, string modelName);
    Task<BudgetStatus> GetBudgetStatusAsync(string userId, string modelName);
    Task RecordUsageAsync(string userId, string modelName, decimal amount);
}

public interface ITokenUsageStore
{
    Task RecordStartAsync(TokenUsageRecord record);
    Task RecordCompletionAsync(TokenUsageRecord record);
    Task<List<TokenUsageRecord>> GetUserUsageAsync(string userId, DateTime? start, DateTime? end);
}

public interface IBudgetStore
{
    Task<UserBudget?> GetUserBudgetAsync(string userId, string modelName);
    Task<decimal> GetCurrentUsageAsync(string userId, string modelName);
    Task RecordUsageAsync(string userId, string modelName, decimal amount);
}

// Models
public class TokenUsageRecord
{
    public string RequestId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Model { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal Cost { get; set; }
    public string? InputMessage { get; set; }
    public string? ResponseMessage { get; set; }
    public TokenUsageStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TokenUsageStatus { Running, Completed, Failed }

public record TokenUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

public class UserBudget
{
    public string UserId { get; set; } = null!;
    public string Model { get; set; } = null!;
    public decimal MonthlyLimit { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record BudgetCheckResult
{
    public bool IsWithinBudget { get; init; }
    public decimal UsedAmount { get; init; }
    public decimal BudgetAmount { get; init; }
    public decimal UsagePercentage { get; init; }
}

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
