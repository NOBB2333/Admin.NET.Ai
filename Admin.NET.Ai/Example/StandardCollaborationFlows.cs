using Admin.NET.Ai.Abstractions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using MafWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace Admin.NET.Ai.Services.Workflow.Examples;

/// <summary>
/// 标准协作流设计示例 - 使用 MAF 原生 API
/// </summary>
public static class StandardCollaborationFlows
{
    /// <summary>
    /// 创建内容创作流水线
    /// 需要先创建 Agents，然后使用 IWorkflowService.CreateSequential
    /// </summary>
    /// <example>
    /// var writer = new ChatClientAgent(client, "你是技术作家...");
    /// var reviewer = new ChatClientAgent(client, "你是内容审核...");
    /// var workflow = workflowService.CreateSequential("Pipeline", writer, reviewer);
    /// </example>
    public static MafWorkflow CreateContentPipeline(
        IWorkflowService workflowService,
        AIAgent writer,
        AIAgent reviewer,
        AIAgent publisher)
    {
        return workflowService.CreateSequential(
            "ContentPipeline",
            writer,
            reviewer,
            publisher
        );
    }
}
