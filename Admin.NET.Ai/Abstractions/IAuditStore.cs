namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 审计存储接口
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// 保存审计日志
    /// </summary>
    Task SaveAuditLogAsync(string requestId, string prompt, object? result, IDictionary<string, object?>? metadata = null);

    /// <summary>
    /// 根据 TraceId 获取审计日志
    /// </summary>
    /// <param name="traceId"></param>
    /// <returns></returns>
    Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(string traceId);
}

public class AuditLogEntry
{
    public string RequestId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public object? Result { get; set; }
    public IDictionary<string, object?>? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
