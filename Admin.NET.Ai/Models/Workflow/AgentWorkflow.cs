using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Execution;
using Microsoft.Extensions.AI;
using MafWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace Admin.NET.Ai.Models.Workflow;

/// <summary>
/// Agent 工作流包装器 - 简化版
/// 直接封装 MAF Workflow，无额外抽象
/// </summary>
public class AgentWorkflow
{
    /// <summary>
    /// 工作流名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MAF 原生工作流对象
    /// </summary>
    public MafWorkflow? InternalWorkflow { get; set; }

    public override string ToString() => $"AgentWorkflow: {Name}";

    /// <summary>
    /// 执行工作流并返回 MAF 原生事件流
    /// </summary>
    public async IAsyncEnumerable<WorkflowEvent> ExecuteAsync(ChatMessage input)
    {
        if (InternalWorkflow == null)
        {
            yield return new WorkflowErrorEvent(new Exception("工作流未初始化"));
            yield break;
        }

        await using var run = await InProcessExecution.StreamAsync(InternalWorkflow, input);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (var evt in run.WatchStreamAsync())
        {
            yield return evt;
        }
    }

    /// <summary>
    /// 执行工作流（字符串输入）
    /// </summary>
    public IAsyncEnumerable<WorkflowEvent> ExecuteAsync(string input)
    {
        return ExecuteAsync(new ChatMessage(ChatRole.User, input));
    }
}
