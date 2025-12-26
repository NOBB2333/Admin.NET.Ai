namespace Admin.NET.Ai.Abstractions;

public interface IRateLimiter
{
    Task<bool> CheckLimitAsync(string userId);
    Task RecordRequestAsync(string userId);
}

public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}
