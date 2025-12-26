using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Execution;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Models.Workflow;

/// <summary>
/// 代理工作流包装器
/// </summary>
public class AgentWorkflow
{
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 包装 Microsoft.Agents.Workflows.AgentWorkflow
    /// </summary>
    public Microsoft.Agents.AI.Workflows.Workflow? InternalWorkflow { get; set; }

    public override string ToString() => $"AgentWorkflow: {Name}";

    /// <summary>
    /// 执行并监视工作流事件流
    /// </summary>
    public virtual async IAsyncEnumerable<AiWorkflowEvent> WatchStreamAsync(IEnumerable<ChatMessage>? messages = null)
    {
        if (InternalWorkflow == null)
        {
            yield return new AiWorkflowErrorEvent { ErrorMessage = "Internal workflow is not initialized." };
            yield break;
        }

        // 使用 MAF 的执行引擎
        var run = await InProcessExecution.StreamAsync(InternalWorkflow, messages ?? []);
        
        // 使用 run.WatchStreamAsync() 进行迭代
        await foreach (var @event in run.WatchStreamAsync(default)) 
        {
            // 将 MAF 事件映射到我们的内部事件
            if (@event.GetType().Name == "AgentRunUpdateEvent")
            {
                // 之前属性访问失败，如果需要，使用 ToString() 或反射。
                // 目前，使用通用描述
                yield return new AiAgentRunUpdateEvent 
                { 
                    AgentName = "Agent", // 占位符
                    Step = @event.ToString() ?? "Processing..." 
                };
            }
            else if (@event.GetType().Name == "WorkflowOutputEvent")
            {
                 // 尝试通过 dynamic 访问 Output 或仅使用 ToString
                 // yield return new AiWorkflowOutputEvent { Output = output.Output }; // 失败
                 yield return new AiWorkflowOutputEvent { Output = @event.ToString() };
            }
            else if (@event.GetType().Name == "WorkflowErrorEvent")
            {
                 yield return new AiWorkflowErrorEvent { ErrorMessage = @event.ToString() ?? "Unknown Error" };
            }
            // 回退
            else 
            {
                // yield return new AiWorkflowOutputEvent { Output = @event.ToString() };
            }
        }
    }
}
