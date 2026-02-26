using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// AI 工具接口 (用于自动发现和注册 Function Calling)
/// 支持工具自管理审批和执行上下文
/// </summary>
public interface IAiCallableFunction
{
    /// <summary>
    /// 工具名称 (唯一标识)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 工具描述 (提供给 LLM 理解用途)
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 如果工具逻辑是静态的，或者您希望直接提供 AIFunction 对象，可实现此方法。
    /// 否则，ToolManager 会扫描实现类中的 [Description] 方法。
    /// </summary>
    IEnumerable<AIFunction> GetFunctions();

    /// <summary>
    /// 工具自行判断给定参数下是否需要用户审批
    /// 默认不需要审批，敏感工具可 override 此方法
    /// </summary>
    /// <param name="arguments">工具调用参数</param>
    /// <returns>true 表示需要审批</returns>
    bool RequiresApproval(IDictionary<string, object?>? arguments = null) => false;

    /// <summary>
    /// 工具执行上下文，由 ToolManager 在执行前注入
    /// 包含会话、调用方 Agent、工作目录等信息
    /// 现有工具无需实现此属性，ToolManager 通过 SetContext 注入
    /// </summary>
    ToolExecutionContext? Context { get => null; set { } }
}

/// <summary>
/// 工具执行上下文 — 携带运行时元数据
/// 由 ToolManager 在调用工具前自动注入
/// </summary>
public class ToolExecutionContext
{
    /// <summary>
    /// 当前会话 ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 调用方 Agent 名称
    /// </summary>
    public string? CallerAgentName { get; set; }

    /// <summary>
    /// 工作目录限制（文件类工具用于路径安全检查）
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// 用户 ID（权限检查用）
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 跨工具调用的共享状态
    /// </summary>
    public Dictionary<string, object?> SharedState { get; set; } = new();
}
