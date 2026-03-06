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

    /// <summary>
    /// 审批回调（结构化）：
    /// 提供完整审批上下文，便于接入企业审批系统（工单、OA、IM Bot）。
    /// 若已设置该回调，将优先于 ApprovalCallback。
    /// </summary>
    public Func<ToolApprovalRequest, Task<bool>>? ApprovalRequestCallback { get; set; }

    /// <summary>
    /// 模型风险判定回调（可选）：
    /// 输入工具名和参数 JSON，输出风险级别与原因。
    /// 可由业务方接入任意模型或策略引擎实现。
    /// </summary>
    public Func<string, string, Task<ToolRiskDecision>>? RiskDecisionCallback { get; set; }

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
        var validationId = Guid.NewGuid().ToString("N")[..8];
        var toolName = context.ToolCall.Name;
        var arguments = context.ToolCall.Arguments;
        var permissionLevel = PermissionLevel.Normal;

        _logger.LogInformation("🔍 [Validation:{ValidationId}] 验证工具调用: {Tool}", validationId, toolName);

        // 1. 规则权限检查 (ToolPermissionManager — 基于角色/频率/级别)
        if (_permissionManager != null && _options.EnablePermissionCheck)
        {
            var permResult = await _permissionManager.CheckPermissionAsync(toolName, arguments);
            
            if (!permResult.IsAllowed)
            {
                _logger.LogWarning("🚫 [Validation:{ValidationId}] 权限拒绝: {Tool}, Reason={Reason}",
                    validationId, toolName, permResult.DeniedReason);
                return new ToolResponse 
                { 
                    Result = $"[Permission Denied][{validationId}] {permResult.DeniedReason}" 
                };
            }

            permissionLevel = permResult.Level;

            // 敏感操作警告
            if (permResult.Level >= PermissionLevel.Sensitive)
            {
                _logger.LogWarning("⚠️ [Validation:{ValidationId}] 敏感操作: {Tool}, Level={Level}",
                    validationId, toolName, permResult.Level);
            }
        }

        // 1.1 模型风险判定（可选）
        if (_options.EnableModelRiskAssessment && RiskDecisionCallback != null)
        {
            var argsJson = arguments != null ? JsonSerializer.Serialize(arguments) : "{}";
            try
            {
                var riskTask = RiskDecisionCallback(toolName, argsJson);
                var riskDecision = _options.RiskAssessmentTimeoutMs > 0
                    ? await riskTask.WaitAsync(TimeSpan.FromMilliseconds(_options.RiskAssessmentTimeoutMs))
                    : await riskTask;

                if (riskDecision.Level > permissionLevel)
                {
                    permissionLevel = riskDecision.Level;
                }

                if (riskDecision.Level >= PermissionLevel.Sensitive)
                {
                    _logger.LogWarning("⚠️ [Validation:{ValidationId}] 模型判定高风险: {Tool}, Level={Level}, Reason={Reason}",
                        validationId, toolName, riskDecision.Level, riskDecision.Reason);
                }
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "⏱️ [Validation:{ValidationId}] 模型风险判定超时: {Tool}, TimeoutMs={TimeoutMs}",
                    validationId, toolName, _options.RiskAssessmentTimeoutMs);

                if (_options.DenyOnRiskAssessmentError)
                {
                    return new ToolResponse
                    {
                        Result = $"[Risk Assessment Timeout][{validationId}] 工具 '{toolName}' 风险判定超时，已按策略拒绝"
                    };
                }

                if (_options.RiskAssessmentFailureFallbackLevel > permissionLevel)
                {
                    permissionLevel = _options.RiskAssessmentFailureFallbackLevel;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Validation:{ValidationId}] 模型风险判定失败: {Tool}",
                    validationId, toolName);
                if (_options.DenyOnRiskAssessmentError)
                {
                    return new ToolResponse
                    {
                        Result = $"[Risk Assessment Error][{validationId}] 工具 '{toolName}' 风险判定失败，已按策略拒绝"
                    };
                }

                if (_options.RiskAssessmentFailureFallbackLevel > permissionLevel)
                {
                    permissionLevel = _options.RiskAssessmentFailureFallbackLevel;
                }
            }
        }

        // 2. 工具自管理审批 (IAiCallableFunction.RequiresApproval — 基于参数动态判断)
        if (_options.EnableSelfManagedApproval)
        {
            var toolMeta = _toolManager?.GetAllTools()
                .FirstOrDefault(t => t.Name == toolName || 
                    t.GetFunctions().Any(f => f.Name == toolName));

            var requiresToolSelfApproval = toolMeta?.RequiresApproval(arguments) ?? false;
            var requiresRiskApproval = _options.EnableRiskBasedApproval
                && permissionLevel >= _options.MinLevelForMandatoryApproval;

            if (requiresToolSelfApproval || requiresRiskApproval)
            {
                _logger.LogWarning("⚠️ [Validation:{ValidationId}] 工具请求审批: {Tool}, RiskLevel={RiskLevel}, ToolSelfApproval={ToolSelfApproval}",
                    validationId, toolName, permissionLevel, requiresToolSelfApproval);

                var argsJson = arguments != null ? JsonSerializer.Serialize(arguments) : "{}";
                var approvalRequest = new ToolApprovalRequest
                {
                    ValidationId = validationId,
                    ToolName = toolName,
                    Arguments = arguments,
                    ArgumentsJson = argsJson,
                    RiskLevel = permissionLevel,
                    RequiresToolSelfApproval = requiresToolSelfApproval,
                    RequiresRiskBasedApproval = requiresRiskApproval
                };

                if (ApprovalRequestCallback != null || ApprovalCallback != null)
                {
                    var approved = ApprovalRequestCallback != null
                        ? await ApprovalRequestCallback(approvalRequest)
                        : await ApprovalCallback!(toolName, argsJson);

                    if (!approved)
                    {
                        _logger.LogWarning("🚫 [Validation:{ValidationId}] 用户拒绝审批: {Tool}", validationId, toolName);
                        return new ToolResponse
                        {
                            Result = $"[Approval Denied][{validationId}] 用户拒绝了工具 '{toolName}' 的调用"
                        };
                    }
                    _logger.LogInformation("✅ [Validation:{ValidationId}] 用户批准: {Tool}", validationId, toolName);
                }
                else
                {
                    if (_options.DenyWhenApprovalCallbackMissing)
                    {
                        _logger.LogWarning("🚫 [Validation:{ValidationId}] 工具需要审批但未配置审批回调，默认拒绝: {Tool}",
                            validationId, toolName);
                        return new ToolResponse
                        {
                            Result = $"[Approval Denied][{validationId}] 工具 '{toolName}' 需要审批，但系统未配置审批回调"
                        };
                    }

                    _logger.LogWarning("⚠️ [Validation:{ValidationId}] 工具需要审批但未配置审批回调，按配置放行: {Tool}",
                        validationId, toolName);
                }
            }
        }

        // 3. 参数验证
        if (_options.ValidateArguments && arguments != null)
        {
            var validationErrors = ValidateArguments(toolName, arguments);
            if (validationErrors.Any())
            {
                _logger.LogWarning("⚠️ [Validation:{ValidationId}] 参数验证失败: {Tool}, Errors={Errors}", 
                    validationId, toolName, string.Join("; ", validationErrors));
                
                if (_options.RejectInvalidArguments)
                {
                    return new ToolResponse 
                    { 
                        Result = $"[Validation Error][{validationId}] {string.Join("; ", validationErrors)}" 
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
                    Result = $"[Execution Error][{validationId}] {sandboxResult.Error}" 
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
                _logger.LogDebug("✂️ [Validation:{ValidationId}] 结果已截断: {Tool}", validationId, toolName);
            }
        }

        _logger.LogInformation("✅ [Validation:{ValidationId}] 验证完成: {Tool}", validationId, toolName);
        return response;
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
    public bool EnableModelRiskAssessment { get; set; } = true;
    public bool DenyOnRiskAssessmentError { get; set; } = true;
    public int RiskAssessmentTimeoutMs { get; set; } = 8000;
    public PermissionLevel RiskAssessmentFailureFallbackLevel { get; set; } = PermissionLevel.Sensitive;
    public bool EnableRiskBasedApproval { get; set; } = true;
    public PermissionLevel MinLevelForMandatoryApproval { get; set; } = PermissionLevel.Sensitive;
    public bool DenyWhenApprovalCallbackMissing { get; set; } = true;
    public bool ValidateArguments { get; set; } = true;
    public bool RejectInvalidArguments { get; set; } = false;
    public bool UseSandbox { get; set; } = true;
    public bool SanitizeResult { get; set; } = true;
    public int TimeoutMs { get; set; } = 30000;
    public int MaxResultSize { get; set; } = 5000;
}

/// <summary>
/// 工具风险判定结果
/// </summary>
public sealed class ToolRiskDecision
{
    public PermissionLevel Level { get; set; } = PermissionLevel.Normal;
    public string? Reason { get; set; }
}

/// <summary>
/// 结构化审批请求
/// </summary>
public sealed class ToolApprovalRequest
{
    public string ValidationId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public IDictionary<string, object?>? Arguments { get; set; }
    public string ArgumentsJson { get; set; } = "{}";
    public PermissionLevel RiskLevel { get; set; } = PermissionLevel.Normal;
    public bool RequiresToolSelfApproval { get; set; }
    public bool RequiresRiskBasedApproval { get; set; }
}
