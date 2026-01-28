using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using MafWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace Admin.NET.Ai.Services.Monitoring;

/// <summary>
/// 工作流监控器 - 使用 MAF 原生事件
/// </summary>
public class WorkflowMonitor
{
    private readonly ILogger<WorkflowMonitor> _logger;
    private readonly AgentTelemetry _telemetry;
    
    public WorkflowMonitor(ILogger<WorkflowMonitor> logger, AgentTelemetry telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }
    
    /// <summary>
    /// 监听 MAF 工作流事件流
    /// </summary>
    public async Task MonitorAsync(
        IAsyncEnumerable<WorkflowEvent> eventStream, 
        string workflowId,
        string workflowName = "Unknown")
    {
        using var workflowActivity = _telemetry.StartWorkflowActivity(workflowName, workflowId);

        await foreach (var evt in eventStream)
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent agentUpdate:
                    LogAgentProgress(workflowId, agentUpdate);
                    break;
                    
                case ExecutorCompletedEvent completed:
                    LogExecutorCompleted(workflowId, completed, workflowActivity);
                    break;
                    
                case ExecutorFailedEvent failed:
                    LogExecutorFailed(workflowId, failed, workflowActivity);
                    break;

                case WorkflowOutputEvent output:
                    LogWorkflowOutput(workflowId, output, workflowActivity);
                    break;
                    
                case RequestInfoEvent requestInfo:
                    LogHumanInputRequest(workflowId, requestInfo);
                    break;
            }
        }
    }
    
    private void LogAgentProgress(string workflowId, AgentResponseUpdateEvent update)
    {
        using var activity = _telemetry.StartAgentActivity(update.ExecutorId, "Processing");
        
        _logger.LogInformation(
            "Workflow {WorkflowId} - Agent {AgentId}: {Text}",
            workflowId, update.ExecutorId, update.Update.Text);
    }
    
    private void LogExecutorCompleted(string workflowId, ExecutorCompletedEvent completed, Activity? parentActivity)
    {
        _logger.LogInformation(
            "Workflow {WorkflowId} - Executor {ExecutorId} Completed",
            workflowId, completed.ExecutorId);
    }
    
    private void LogExecutorFailed(string workflowId, ExecutorFailedEvent failed, Activity? parentActivity)
    {
        _logger.LogError(
            "Workflow {WorkflowId} - Executor {ExecutorId} Failed: {Error}",
            workflowId, failed.ExecutorId, failed.Data?.Message);
            
        parentActivity?.SetStatus(ActivityStatusCode.Error, failed.Data?.Message);
    }
    
    private void LogWorkflowOutput(string workflowId, WorkflowOutputEvent output, Activity? parentActivity)
    {
        _logger.LogInformation(
            "Workflow {WorkflowId} Completed. Output from: {Source}",
            workflowId, output.SourceId);
        
        parentActivity?.SetStatus(ActivityStatusCode.Ok);
        parentActivity?.SetTag("workflow.source", output.SourceId);
    }

    private void LogHumanInputRequest(string workflowId, RequestInfoEvent requestInfo)
    {
        _logger.LogWarning(
            "✋ Workflow {WorkflowId} Waiting for Human Input",
            workflowId);
            
        Activity.Current?.AddEvent(new ActivityEvent("HumanInputRequested"));
    }
}
