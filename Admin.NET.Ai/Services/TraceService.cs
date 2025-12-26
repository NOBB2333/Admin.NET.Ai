using Admin.NET.Ai.Abstractions;
using System.Text.Json;

namespace Admin.NET.Ai.Services;

/// <summary>
/// 追踪服务，用于获取 Agent 执行的时间轴数据
/// </summary>
public class TraceService(IAuditStore auditStore)
{
    private readonly IAuditStore _auditStore = auditStore;

    /// <summary>
    /// 根据 TraceId 获取执行时间轴
    /// </summary>
    /// <param name="traceId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<TraceNode>> GetTimelineAsync(string traceId)
    {
        var logs = await _auditStore.GetAuditLogsAsync(traceId);
        
        return logs.Select(l => new TraceNode 
        { 
            NodeName = l.Metadata?.Keys.Contains("AgentName") == true ? l.Metadata["AgentName"]?.ToString() ?? "Agent" : "Node",
            Action = l.Metadata?.Keys.Contains("Action") == true ? l.Metadata["Action"]?.ToString() ?? "Processing" : "Step",
            Timestamp = l.Timestamp,
            Metadata = l.Metadata
        });
    }
}

public class TraceNode
{
    public string NodeName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Duration { get; set; }
    public object? Metadata { get; set; }
}
