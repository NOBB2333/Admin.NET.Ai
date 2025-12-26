using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Admin.NET.Ai.Services.Monitoring;

public class WorkflowMonitor
{
    private readonly ILogger<WorkflowMonitor> _logger;
    private readonly AgentTelemetry _telemetry;
    
    public WorkflowMonitor(ILogger<WorkflowMonitor> logger, AgentTelemetry telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }
    
    // 监听工作流事件
    public async Task MonitorWorkflowEventsAsync(AgentWorkflow workflow, string workflowId)
    {
        using var workflowActivity = _telemetry.StartWorkflowActivity(workflow.Name ?? "Unknown", workflowId);

        await foreach (var evt in workflow.WatchStreamAsync())
        {
            switch (evt)
            {
                case AiAgentRunUpdateEvent agentUpdate:
                    LogAgentProgress(workflowId, agentUpdate);
                    break;
                    
                case AiFunctionCallingEvent functionCall:
                    LogFunctionCall(workflowId, functionCall);
                    break;
                    
                case AiWorkflowOutputEvent output:
                    LogWorkflowCompletion(workflowId, output, workflowActivity);
                    break;
                    
                case AiWorkflowErrorEvent error:
                    LogWorkflowError(workflowId, error, workflowActivity);
                    break;

                case AiWorkflowHumanInputEvent humanInput:
                    LogHumanInputRequest(workflowId, humanInput);
                    break;
            }
        }
    }
    
    private void LogAgentProgress(string workflowId, AiAgentRunUpdateEvent update)
    {
        // Start a short-lived activity for this update or use a span if possible.
        // Since this is event-driven stream, mapping to spans strictly is hard without knowing end signal.
        // We will just log an event in the current activity (which is the Workflow Activity presumably flowing, 
        // but here we are in an async loop, so context might not flow automatically unless AsyncLocal is used).
        
        // For better visualization, we assume each "Update" is a significant step.
        using var activity = _telemetry.StartAgentActivity(update.AgentName, update.Step);
        
        _logger.LogInformation(
            "Workflow {WorkflowId} - Agent {AgentName} Step: {Step}",
            workflowId, update.AgentName, update.Step);
    }
    
    private void LogFunctionCall(string workflowId, AiFunctionCallingEvent call)
    {
        using var activity = _telemetry.StartToolActivity(call.FunctionName);
        activity?.SetTag("tool.args", call.Arguments);
        
        _logger.LogInformation("Workflow {WorkflowId} - Calling {Func}({Args})", workflowId, call.FunctionName, call.Arguments);
    }
    
    private void LogWorkflowCompletion(string workflowId, AiWorkflowOutputEvent output, Activity? parentActivity)
    {
        _logger.LogInformation("Workflow {WorkflowId} Completed. Output: {Out}", workflowId, output.Output);
        
        parentActivity?.SetStatus(ActivityStatusCode.Ok);
        parentActivity?.SetTag("workflow.output", output.Output?.ToString());
    }
    
    private void LogWorkflowError(string workflowId, AiWorkflowErrorEvent error, Activity? parentActivity)
    {
        _logger.LogError("Workflow {WorkflowId} Error: {Err}", workflowId, error.ErrorMessage);
        
        parentActivity?.SetStatus(ActivityStatusCode.Error, error.ErrorMessage);
    }

    private void LogHumanInputRequest(string workflowId, AiWorkflowHumanInputEvent input)
    {
        _logger.LogWarning("✋ Workflow {WorkflowId} Waiting for Human Input: {Prompt}", workflowId, input.Prompt);
        // Add event to trace
        Activity.Current?.AddEvent(new ActivityEvent("HumanInputRequested", tags: new ActivityTagsCollection { { "prompt", input.Prompt } }));
    }
}
