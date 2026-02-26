using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 工具监控中间件 (增强版)
/// 职责: 记录工具实际执行情况，分类为 Tool/Agent/Skill，追踪审批状态
/// 与 ToolManager 联动获取 IAiCallableFunction 元数据
/// </summary>
public class ToolMonitoringMiddleware : IToolCallingMiddleware
{
    private readonly ILogger<ToolMonitoringMiddleware> _logger;
    private readonly ToolManager? _toolManager;

    public ToolMonitoringMiddleware(
        ILogger<ToolMonitoringMiddleware> logger,
        ToolManager? toolManager = null)
    {
        _logger = logger;
        _toolManager = toolManager;
    }

    public async Task<ToolResponse> InvokeAsync(
        ToolCallingContext context, 
        NextToolCallingMiddleware next)
    {
        var startTime = DateTime.UtcNow;
        var toolName = context.ToolCall.Name;
        var parameters = context.ToolCall.Arguments;

        // 通过 ToolManager 查找对应的 IAiCallableFunction 元数据
        var toolMeta = _toolManager?.GetAllTools()
            .FirstOrDefault(t => t.Name == toolName || 
                t.GetFunctions().Any(f => f.Name == toolName));

        var category = ClassifyTool(toolName);
        var categoryIcon = category switch
        {
            ToolCategory.Agent => "🤖",
            ToolCategory.Skill => "⚡",
            _ => "🔧"
        };

        // 检查审批状态
        var needsApproval = toolMeta?.RequiresApproval(parameters) ?? false;
        var approvalTag = needsApproval ? " [需审批]" : "";

        _logger.LogInformation(
            "{Icon} [{Category}] 开始调用: {Tool}{Approval} | 参数: {Params}",
            categoryIcon, category, toolName, approvalTag,
            TruncateJson(parameters));

        try
        {
            var result = await next(context);
            
            var duration = DateTime.UtcNow - startTime;
            var resultPreview = TruncateResult(result.Result);

            _logger.LogInformation(
                "{Icon} [{Category}] 调用完成: {Tool} | 耗时: {Duration}ms | 结果: {Result}",
                categoryIcon, category, toolName, 
                (int)duration.TotalMilliseconds, resultPreview);

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(
                "{Icon} [{Category}] 调用失败: {Tool} | 耗时: {Duration}ms | 错误: {Error}",
                categoryIcon, category, toolName,
                (int)duration.TotalMilliseconds, ex.Message);
            throw;
        }
    }

    #region 分类和辅助

    private enum ToolCategory { Tool, Agent, Skill }

    /// <summary>
    /// 按函数名分类: call_agent → Agent, skill_ → Skill, 其他 → Tool
    /// </summary>
    private static ToolCategory ClassifyTool(string? name)
    {
        if (string.IsNullOrEmpty(name)) return ToolCategory.Tool;
        var lower = name.ToLowerInvariant();

        if (lower.StartsWith("call_agent") || lower.Contains("agent"))
            return ToolCategory.Agent;
        if (lower.StartsWith("skill_") || lower.Contains("skill"))
            return ToolCategory.Skill;
        return ToolCategory.Tool;
    }

    private static string TruncateJson(IDictionary<string, object?>? args)
    {
        if (args == null || args.Count == 0) return "{}";
        try
        {
            var json = JsonSerializer.Serialize(args);
            return json.Length > 200 ? json[..200] + "..." : json;
        }
        catch
        {
            return "{...}";
        }
    }

    private static string TruncateResult(object? result)
    {
        if (result == null) return "(null)";
        var str = result.ToString() ?? "";
        return str.Length > 150 ? str[..150] + "..." : str;
    }

    #endregion
}
