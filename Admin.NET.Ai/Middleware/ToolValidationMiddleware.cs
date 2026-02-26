using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 工具验证中间件
/// 职责: 权限检查 → 自管理审批 → 参数验证 → 沙箱执行 → 结果脱敏
/// 与 ToolManager 联动支持 IAiCallableFunction.RequiresApproval()
/// </summary>
public class ToolValidationMiddleware : IToolCallingMiddleware
{
    private readonly ILogger<ToolValidationMiddleware> _logger;
    private readonly IToolPermissionManager? _permissionManager;
    private readonly IToolExecutionSandbox? _sandbox;
    private readonly ToolManager? _toolManager;
    private readonly ToolValidationOptions _options;

    /// <summary>
    /// 审批回调：工具名 + 参数JSON → 是否批准
    /// 可以是 Console 交互、API 调用、UI 弹窗等
    /// </summary>
    public Func<string, string, Task<bool>>? ApprovalCallback { get; set; }

    public ToolValidationMiddleware(
        ILogger<ToolValidationMiddleware> logger,
        IToolPermissionManager? permissionManager = null,
        IToolExecutionSandbox? sandbox = null,
        ToolManager? toolManager = null,
        ToolValidationOptions? options = null)
    {
        _logger = logger;
        _permissionManager = permissionManager;
        _sandbox = sandbox;
        _toolManager = toolManager;
        _options = options ?? new ToolValidationOptions();
    }

    public async Task<ToolResponse> InvokeAsync(ToolCallingContext context, NextToolCallingMiddleware next)
    {
        var toolName = context.ToolCall.Name;
        var arguments = context.ToolCall.Arguments;

        _logger.LogInformation("🔍 [Validation] 验证工具调用: {Tool}", toolName);

        // 1. 规则权限检查 (ToolPermissionManager — 基于角色/频率/级别)
        if (_permissionManager != null && _options.EnablePermissionCheck)
        {
            var userId = GetUserId(context);
            var permResult = await _permissionManager.CheckPermissionAsync(userId, toolName, arguments);
            
            if (!permResult.IsAllowed)
            {
                _logger.LogWarning("🚫 [Validation] 权限拒绝: {Tool}, Reason={Reason}", toolName, permResult.DeniedReason);
                return new ToolResponse 
                { 
                    Result = $"[Permission Denied] {permResult.DeniedReason}" 
                };
            }

            // 敏感操作警告
            if (permResult.Level >= PermissionLevel.Sensitive)
            {
                _logger.LogWarning("⚠️ [Validation] 敏感操作: {Tool}, Level={Level}", toolName, permResult.Level);
            }
        }

        // 2. 工具自管理审批 (IAiCallableFunction.RequiresApproval — 基于参数动态判断)
        if (_options.EnableSelfManagedApproval)
        {
            var toolMeta = _toolManager?.GetAllTools()
                .FirstOrDefault(t => t.Name == toolName || 
                    t.GetFunctions().Any(f => f.Name == toolName));

            if (toolMeta != null && toolMeta.RequiresApproval(arguments))
            {
                _logger.LogWarning("⚠️ [Validation] 工具请求审批: {Tool}", toolName);

                if (ApprovalCallback != null)
                {
                    var argsJson = arguments != null ? JsonSerializer.Serialize(arguments) : "{}";
                    var approved = await ApprovalCallback(toolName, argsJson);
                    if (!approved)
                    {
                        _logger.LogWarning("🚫 [Validation] 用户拒绝审批: {Tool}", toolName);
                        return new ToolResponse
                        {
                            Result = $"[Approval Denied] 用户拒绝了工具 '{toolName}' 的调用"
                        };
                    }
                    _logger.LogInformation("✅ [Validation] 用户批准: {Tool}", toolName);
                }
                else
                {
                    _logger.LogWarning("⚠️ [Validation] 工具需要审批但未配置 ApprovalCallback，默认放行: {Tool}", toolName);
                }
            }
        }

        // 3. 参数验证
        if (_options.ValidateArguments && arguments != null)
        {
            var validationErrors = ValidateArguments(toolName, arguments);
            if (validationErrors.Any())
            {
                _logger.LogWarning("⚠️ [Validation] 参数验证失败: {Tool}, Errors={Errors}", 
                    toolName, string.Join("; ", validationErrors));
                
                if (_options.RejectInvalidArguments)
                {
                    return new ToolResponse 
                    { 
                        Result = $"[Validation Error] {string.Join("; ", validationErrors)}" 
                    };
                }
            }
        }

        // 4. 沙箱执行
        ToolResponse response;
        if (_sandbox != null && _options.UseSandbox)
        {
            var sandboxResult = await _sandbox.ExecuteAsync(
                toolName,
                async () => 
                {
                    var r = await next(context);
                    return r.Result;
                },
                new SandboxOptions
                {
                    TimeoutMs = _options.TimeoutMs,
                    MaxResultSize = _options.MaxResultSize,
                    CaptureExceptions = true
                });

            if (!sandboxResult.Success)
            {
                return new ToolResponse 
                { 
                    Result = $"[Execution Error] {sandboxResult.Error}" 
                };
            }

            response = new ToolResponse { Result = sandboxResult.Result };
        }
        else
        {
            response = await next(context);
        }

        // 5. 结果验证和脱敏
        if (_options.SanitizeResult && response.Result != null)
        {
            response.Result = SanitizeResult(response.Result);
        }

        // 6. 结果截断
        if (_options.MaxResultSize > 0 && response.Result != null)
        {
            var resultStr = response.Result.ToString() ?? "";
            if (resultStr.Length > _options.MaxResultSize)
            {
                response.Result = resultStr.Substring(0, _options.MaxResultSize) + "... [Truncated]";
                _logger.LogDebug("✂️ [Validation] 结果已截断: {Tool}", toolName);
            }
        }

        _logger.LogInformation("✅ [Validation] 验证完成: {Tool}", toolName);
        return response;
    }

    private string GetUserId(ToolCallingContext context)
    {
        if (context.ServiceProvider != null)
        {
            var httpContextAccessor = context.ServiceProvider.GetService(
                typeof(Microsoft.AspNetCore.Http.IHttpContextAccessor)) 
                as Microsoft.AspNetCore.Http.IHttpContextAccessor;
            
            var httpContext = httpContextAccessor?.HttpContext;
            return httpContext?.User?.Identity?.Name 
                ?? httpContext?.Request.Headers["X-User-Id"].ToString() 
                ?? "anonymous";
        }
        return "anonymous";
    }

    private List<string> ValidateArguments(string toolName, IDictionary<string, object?> arguments)
    {
        var errors = new List<string>();

        foreach (var (key, value) in arguments)
        {
            if (value is string strValue)
            {
                if (ContainsSqlInjection(strValue))
                {
                    errors.Add($"参数 '{key}' 包含潜在的 SQL 注入");
                }

                if (key.Contains("path", StringComparison.OrdinalIgnoreCase) 
                    && (strValue.Contains("..") || strValue.Contains("~/")))
                {
                    errors.Add($"参数 '{key}' 包含潜在的路径遍历");
                }
            }
        }

        return errors;
    }

    private bool ContainsSqlInjection(string value)
    {
        var sqlPatterns = new[] { "'; --", "1=1", "OR 1=1", "DROP TABLE", "DELETE FROM" };
        return sqlPatterns.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private object SanitizeResult(object result)
    {
        var str = result.ToString() ?? "";
        
        var patterns = new Dictionary<string, string>
        {
            { "\\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}\\b", "[EMAIL]" },
            { "\\b\\d{3}[-.]?\\d{4}[-.]?\\d{4}\\b", "[PHONE]" },
            { "\\b\\d{6}(?:19|20)\\d{2}(?:0[1-9]|1[0-2])(?:0[1-9]|[12]\\d|3[01])\\d{3}[\\dXx]\\b", "[ID_CARD]" },
            { "(password|secret|token|apikey)[\"']?\\s*[:=]\\s*[\"']?[^\\s\"']+", "[REDACTED]" }
        };

        foreach (var (pattern, replacement) in patterns)
        {
            str = System.Text.RegularExpressions.Regex.Replace(str, pattern, replacement, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return str;
    }
}

/// <summary>
/// 工具验证选项
/// </summary>
public class ToolValidationOptions
{
    public bool EnablePermissionCheck { get; set; } = true;
    /// <summary>
    /// 启用工具自管理审批 (IAiCallableFunction.RequiresApproval)
    /// </summary>
    public bool EnableSelfManagedApproval { get; set; } = true;
    public bool ValidateArguments { get; set; } = true;
    public bool RejectInvalidArguments { get; set; } = false;
    public bool UseSandbox { get; set; } = true;
    public bool SanitizeResult { get; set; } = true;
    public int TimeoutMs { get; set; } = 30000;
    public int MaxResultSize { get; set; } = 5000;
}
