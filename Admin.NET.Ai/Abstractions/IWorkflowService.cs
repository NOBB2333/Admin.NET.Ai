using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using MafWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 工作流服务接口 - 基于 MAF (Microsoft Agent Framework)
/// 简化版：直接使用 MAF 原生类型
/// </summary>
public interface IWorkflowService
{
    #region 工厂方法 (创建工作流)
    
    /// <summary>
    /// 创建顺序工作流 (Agent 链式处理)
    /// </summary>
    /// <param name="name">工作流名称</param>
    /// <param name="agents">Agent 列表（按执行顺序）</param>
    MafWorkflow CreateSequential(string name, params AIAgent[] agents);

    /// <summary>
    /// 创建并发工作流 (多 Agent 并行 → 汇总)
    /// </summary>
    /// <param name="name">工作流名称</param>  
    /// <param name="workers">并行执行的 Agent 列表</param>
    /// <param name="aggregator">汇总 Agent</param>
    MafWorkflow CreateParallel(string name, AIAgent[] workers, AIAgent aggregator);

    #endregion

    #region 执行方法
    
    /// <summary>
    /// 执行工作流（流式，返回 MAF 原生事件）
    /// </summary>
    IAsyncEnumerable<WorkflowEvent> ExecuteAsync(MafWorkflow workflow, ChatMessage input);

    /// <summary>
    /// 执行工作流（流式，字符串输入）
    /// </summary>
    IAsyncEnumerable<WorkflowEvent> ExecuteAsync(MafWorkflow workflow, string input);

    #endregion

    #region 自主工作流

    /// <summary>
    /// 生成并执行自主工作流（根据需求自动规划）
    /// </summary>
    IAsyncEnumerable<WorkflowEvent> ExecuteAutonomousAsync(string requirement);

    #endregion

    #region 人机协作

    /// <summary>
    /// 恢复被挂起的工作流（Human-in-the-loop）
    /// </summary>
    IAsyncEnumerable<WorkflowEvent> ResumeAsync(string workflowId, string humanInput);

    #endregion
}
