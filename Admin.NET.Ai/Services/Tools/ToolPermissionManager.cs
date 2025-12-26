using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Admin.NET.Ai.Services.Tools;

/// <summary>
/// 工具权限管理器实现
/// </summary>
public class ToolPermissionManager : IToolPermissionManager
{
    private readonly ILogger<ToolPermissionManager> _logger;
    private readonly List<ToolPermissionRule> _rules = new();
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _callTracker = new();

    public ToolPermissionManager(ILogger<ToolPermissionManager> logger)
    {
        _logger = logger;
        InitializeDefaultRules();
    }

    private void InitializeDefaultRules()
    {
        // 默认规则：危险操作
        RegisterRule(new ToolPermissionRule
        {
            ToolNamePattern = "delete_*",
            Level = PermissionLevel.Dangerous,
            AllowedRoles = new HashSet<string> { "admin" },
            RequireAudit = true
        });

        RegisterRule(new ToolPermissionRule
        {
            ToolNamePattern = "execute_*",
            Level = PermissionLevel.Sensitive,
            MaxCallsPerMinute = 10,
            RequireAudit = true
        });
    }

    public void RegisterRule(ToolPermissionRule rule)
    {
        _rules.Add(rule);
        _logger.LogDebug("📋 [Permission] 注册规则: {Pattern}", rule.ToolNamePattern);
    }

    public async Task<ToolPermissionResult> CheckPermissionAsync(string userId, string toolName, IDictionary<string, object?>? arguments = null)
    {
        _logger.LogDebug("🔐 [Permission] 检查权限: User={User}, Tool={Tool}", userId, toolName);

        // 查找匹配的规则
        var matchingRules = _rules.Where(r => MatchesPattern(toolName, r.ToolNamePattern)).ToList();

        foreach (var rule in matchingRules)
        {
            // 1. 角色检查
            var userRoles = await GetUserRolesAsync(userId);
            
            if (rule.DeniedRoles.Any() && userRoles.Any(r => rule.DeniedRoles.Contains(r)))
            {
                return ToolPermissionResult.Deny($"角色被禁止访问此工具");
            }

            if (rule.AllowedRoles.Any() && !userRoles.Any(r => rule.AllowedRoles.Contains(r)))
            {
                return ToolPermissionResult.Deny($"需要以下角色之一: {string.Join(", ", rule.AllowedRoles)}");
            }

            // 2. 频率限制
            if (!CheckRateLimit(userId, toolName, rule.MaxCallsPerMinute))
            {
                return ToolPermissionResult.Deny($"超出调用频率限制 ({rule.MaxCallsPerMinute}/分钟)");
            }

            // 3. 权限级别检查
            if (rule.Level == PermissionLevel.Forbidden)
            {
                return ToolPermissionResult.Deny("此工具已被禁用");
            }

            // 记录审计
            if (rule.RequireAudit)
            {
                _logger.LogWarning("⚠️ [Audit] 敏感工具调用: User={User}, Tool={Tool}, Level={Level}", 
                    userId, toolName, rule.Level);
            }

            return new ToolPermissionResult { IsAllowed = true, Level = rule.Level };
        }

        // 没有匹配规则，默认允许
        RecordCall(userId, toolName);
        return ToolPermissionResult.Allow();
    }

    public async Task<IEnumerable<string>> GetAllowedToolsAsync(string userId)
    {
        // 简化实现：返回所有非禁止的工具
        var userRoles = await GetUserRolesAsync(userId);
        
        return _rules
            .Where(r => r.Level != PermissionLevel.Forbidden)
            .Where(r => !r.AllowedRoles.Any() || userRoles.Any(ur => r.AllowedRoles.Contains(ur)))
            .Select(r => r.ToolNamePattern ?? "*")
            .Distinct();
    }

    private bool MatchesPattern(string toolName, string? pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern == "*") return true;
        
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(toolName, regexPattern, RegexOptions.IgnoreCase);
    }

    private bool CheckRateLimit(string userId, string toolName, int maxPerMinute)
    {
        var key = $"{userId}:{toolName}";
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);

        var calls = _callTracker.GetOrAdd(key, _ => new Queue<DateTime>());

        lock (calls)
        {
            // 清理过期记录
            while (calls.Count > 0 && calls.Peek() < windowStart)
            {
                calls.Dequeue();
            }

            if (calls.Count >= maxPerMinute)
            {
                return false;
            }

            calls.Enqueue(now);
        }

        return true;
    }

    private void RecordCall(string userId, string toolName)
    {
        var key = $"{userId}:{toolName}";
        var calls = _callTracker.GetOrAdd(key, _ => new Queue<DateTime>());
        lock (calls)
        {
            calls.Enqueue(DateTime.UtcNow);
        }
    }

    private Task<HashSet<string>> GetUserRolesAsync(string userId)
    {
        // 简化实现：从 userId 推断角色
        // 生产环境应该从 Identity/Claims 系统获取
        var roles = new HashSet<string> { "user" };
        if (userId.Contains("admin", StringComparison.OrdinalIgnoreCase))
        {
            roles.Add("admin");
        }
        return Task.FromResult(roles);
    }
}

/// <summary>
/// 工具执行沙箱实现
/// </summary>
public class ToolExecutionSandbox : IToolExecutionSandbox
{
    private readonly ILogger<ToolExecutionSandbox> _logger;

    public ToolExecutionSandbox(ILogger<ToolExecutionSandbox> logger)
    {
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, Func<Task<object?>> executor, SandboxOptions? options = null)
    {
        options ??= new SandboxOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new ToolExecutionResult();

        _logger.LogDebug("🔒 [Sandbox] 执行工具: {Tool}, Timeout={Timeout}ms", toolName, options.TimeoutMs);

        try
        {
            using var cts = new CancellationTokenSource(options.TimeoutMs);
            
            var task = executor();
            var completedTask = await Task.WhenAny(task, Task.Delay(options.TimeoutMs, cts.Token));

            if (completedTask == task)
            {
                result.Result = await task;
                result.Success = true;

                // 结果截断
                if (result.Result != null)
                {
                    var resultStr = result.Result.ToString() ?? "";
                    if (resultStr.Length > options.MaxResultSize)
                    {
                        result.Result = resultStr.Substring(0, options.MaxResultSize) + "... [Truncated]";
                        result.WasTruncated = true;
                        _logger.LogWarning("⚠️ [Sandbox] 结果已截断: {Tool}", toolName);
                    }
                }
            }
            else
            {
                result.Success = false;
                result.TimedOut = true;
                result.Error = $"执行超时 ({options.TimeoutMs}ms)";
                _logger.LogWarning("⏰ [Sandbox] 执行超时: {Tool}", toolName);
            }
        }
        catch (Exception ex) when (options.CaptureExceptions)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError(ex, "❌ [Sandbox] 执行异常: {Tool}", toolName);
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogDebug("✅ [Sandbox] 完成: {Tool}, Success={Success}, Time={Time}ms", 
            toolName, result.Success, result.ExecutionTimeMs);

        return result;
    }
}
