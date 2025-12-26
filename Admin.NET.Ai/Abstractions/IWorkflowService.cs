using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using System.Collections.Generic;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 工作流服务接口
/// 取代原有的 IWorkflowFactory，整合了工作流的创建（Factory）与执行（Service）功能。
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// 创建顺序工作流构建器
    /// </summary>
    IWorkflowBuilder CreateSequentialBuilder(string name);

    /// <summary>
    /// 创建并行工作流构建器 (API 不稳定，暂时 mockup)
    /// </summary>
    IWorkflowBuilder CreateConcurrentBuilder(string name);
    
    /// <summary>
    /// 创建条件/Handoff 工作流构建器
    /// </summary>
    IWorkflowBuilder CreateHandoffBuilder(string name);

    /// <summary>
    /// 创建群组聊天工作流构建器
    /// </summary>
    IWorkflowBuilder CreateGroupChatBuilder(string name);

    /// <summary>
    /// 执行已命名的工作流
    /// </summary>
    IAsyncEnumerable<AiWorkflowEvent> ExecuteWorkflowAsync(string workflowName, object input);
    
    /// <summary>
    /// 生成并执行自主工作流
    /// </summary>
    IAsyncEnumerable<AiWorkflowEvent> ExecuteAutonomousWorkflowAsync(string requirement);

    /// <summary>
    /// --- 恢复方法 ---
    /// </summary>
    IAsyncEnumerable<AiWorkflowEvent> ResumeWorkflowAsync(string workflowId, string humanInput);
}
