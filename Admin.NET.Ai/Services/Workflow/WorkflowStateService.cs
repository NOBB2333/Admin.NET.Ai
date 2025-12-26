using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 工作流状态持久化服务
/// 负责保存和恢复工作流执行上下文
/// </summary>
public class WorkflowStateService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<WorkflowStateService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowStateService(IDistributedCache cache, ILogger<WorkflowStateService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public async Task SaveStateAsync(string workflowId, WorkflowContext context, TimeSpan? expiry = null)
    {
        var key = GetKey(workflowId);
        var json = JsonSerializer.Serialize(context, _jsonOptions);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromDays(7)
        };

        await _cache.SetStringAsync(key, json, options);
        _logger.LogDebug("💾 [WorkflowState] 保存状态: {WorkflowId}", workflowId);
    }

    public async Task<WorkflowContext?> LoadStateAsync(string workflowId)
    {
        var key = GetKey(workflowId);
        var json = await _cache.GetStringAsync(key);
        
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try 
        {
            return JsonSerializer.Deserialize<WorkflowContext>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [WorkflowState] 加载状态失败: {WorkflowId}", workflowId);
            return null;
        }
    }

    public async Task DeleteStateAsync(string workflowId)
    {
        var key = GetKey(workflowId);
        await _cache.RemoveAsync(key);
        _logger.LogDebug("🗑️ [WorkflowState] 删除状态: {WorkflowId}", workflowId);
    }

    private string GetKey(string workflowId) => $"workflow:state:{workflowId}";
}

/// <summary>
/// 工作流上下文 (可序列化状态)
/// </summary>
public class WorkflowContext
{
    public string WorkflowId { get; set; } = "";
    public string Status { get; set; } = "Pending"; // Pending, Running, Suspended, Completed, Failed
    public string CurrentStep { get; set; } = "";
    public Dictionary<string, object> Variables { get; set; } = new();
    public List<string> History { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string? Error { get; set; }
}
