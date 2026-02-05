namespace Admin.NET.Ai.Core.Exceptions;

/// <summary>
/// 限流异常
/// </summary>
public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}

/// <summary>
/// 预算超限异常
/// </summary>
public class BudgetExceededException : Exception
{
    public BudgetExceededException(string message) : base(message) { }
}

/// <summary>
/// 配额超限异常 (Token 或 Budget)
/// </summary>
public class QuotaExceededException : Exception
{
    public QuotaExceededException(string message) : base(message) { }
}

/// <summary>
/// 内容安全异常
/// </summary>
public class ContentSafetyException : Exception
{
    public string ViolationType { get; }
    
    public ContentSafetyException(string message, string violationType = "unknown") 
        : base(message)
    {
        ViolationType = violationType;
    }
}

/// <summary>
/// AI 服务通用异常
/// </summary>
public class AiServiceException : Exception
{
    public string? Provider { get; }
    public string? ModelId { get; }
    
    public AiServiceException(string message, string? provider = null, string? modelId = null) 
        : base(message)
    {
        Provider = provider;
        ModelId = modelId;
    }
    
    public AiServiceException(string message, Exception innerException, string? provider = null, string? modelId = null) 
        : base(message, innerException)
    {
        Provider = provider;
        ModelId = modelId;
    }
}
