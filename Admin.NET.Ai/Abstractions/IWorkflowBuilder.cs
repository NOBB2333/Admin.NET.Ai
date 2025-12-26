using Admin.NET.Ai.Models.Workflow;
using Microsoft.SemanticKernel;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 工作流构建器接口
/// </summary>
public interface IWorkflowBuilder
{
    /// <summary>
    /// 添加 Agent 节点
    /// </summary>
    /// <param name="name">Agent 名称</param>
    /// <param name="instructions">指令/Prompt</param>
    /// <returns></returns>
    IWorkflowBuilder AddAgent(string name, string instructions);
    
    /// <summary>
    /// 添加已实例化的 Agent
    /// </summary>
    IWorkflowBuilder AddAgent(Microsoft.Agents.AI.AIAgent agent);

    /// <summary>
    /// 添加一组顺序执行的 Agent (TensorFlow 风格)
    /// </summary>
    /// <param name="agentNames">Agent 名称列表</param>
    /// <returns></returns>
    IWorkflowBuilder Sequential(params string[] agentNames);

    /// <summary>
    /// 构建工作流
    /// </summary>
    /// <returns></returns>
    AgentWorkflow Build();
}
