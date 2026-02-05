namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// Token 使用量与成本管理统一接口
/// 整合：使用记录存储 + 成本计算 + 成本存储
/// </summary>
public interface ITokenUsageStore
{
    #region 使用记录
    
    /// <summary>
    /// 记录请求开始
    /// </summary>
    Task RecordStartAsync(TokenUsageRecord record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 记录请求完成
    /// </summary>
    Task RecordCompletionAsync(TokenUsageRecord record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户使用记录
    /// </summary>
    Task<List<TokenUsageRecord>> GetUserUsageAsync(string userId, DateTime? start, DateTime? end, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 成本计算与存储
    
    /// <summary>
    /// 计算成本
    /// </summary>
    decimal CalculateCost(TokenUsage usage, string modelName);
    
    /// <summary>
    /// 保存成本记录
    /// </summary>
    Task SaveCostAsync(string requestId, int inputTokens, int outputTokens, string model, IDictionary<string, object?>? additionalData = null, CancellationToken cancellationToken = default);
    
    #endregion
}

#region Models

/// <summary>
/// Token 使用量
/// </summary>
public record TokenUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Token 使用记录
/// </summary>
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

/// <summary>
/// Token 使用状态
/// </summary>
public enum TokenUsageStatus { Running, Completed, Failed }

#endregion
