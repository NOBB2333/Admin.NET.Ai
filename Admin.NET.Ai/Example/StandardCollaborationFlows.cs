using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.NET.Ai.Services.Workflow.Examples;

/// <summary>
/// 标准协作流设计：作家-审核-发布
/// </summary>
public static class StandardCollaborationFlows
{
    /// <summary>
    /// 创建内容创作流水线
    /// </summary>
    public static AgentWorkflow CreateContentPipeline(IWorkflowService workflowService, string topic)
    {
        return workflowService.CreateSequentialBuilder($"ContentPipeline-{topic}")
            .Sequential(
                "TechnicalWriter",   // 第一步：撰写初稿
                "ContentReviewer",   // 第二步：审核与修改建议
                "ArticlePublisher"   // 第三步：格式化并发布
            )
            .Build();
    }
}
