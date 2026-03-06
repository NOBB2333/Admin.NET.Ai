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
        var traceId = workflowActivity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        var spanId = workflowActivity?.SpanId.ToString() ?? Activity.Current?.SpanId.ToString() ?? "none";
        var sessionId = workflowId;

        await foreach (var evt in eventStream)
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent agentUpdate:
                    LogAgentProgress(workflowId, agentUpdate, traceId, spanId, sessionId);
                    break;
                    
                case ExecutorCompletedEvent completed:
                    LogExecutorCompleted(workflowId, completed, workflowActivity, traceId, spanId, sessionId);
                    break;
                    
                case ExecutorFailedEvent failed:
                    LogExecutorFailed(workflowId, failed, workflowActivity, traceId, spanId, sessionId);
                    break;

                case WorkflowOutputEvent output:
                    LogWorkflowOutput(workflowId, output, workflowActivity, traceId, spanId, sessionId);
                    break;
                    
                case RequestInfoEvent requestInfo:
                    LogHumanInputRequest(workflowId, requestInfo, traceId, spanId, sessionId);
                    break;
            }
        }
    }
    
    private void LogAgentProgress(
        string workflowId,
        AgentResponseUpdateEvent update,
        string traceId,
        string spanId,
        string sessionId)
    {
        using var activity = _telemetry.StartAgentActivity(update.ExecutorId, "Processing");
        
        _logger.LogInformation(
            "Workflow {WorkflowId} - Agent {AgentId}: {Text}. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
            workflowId,
            update.ExecutorId,
            update.Update.Text,
            traceId,
            spanId,
            sessionId);
    }
    
    private void LogExecutorCompleted(
        string workflowId,
        ExecutorCompletedEvent completed,
        Activity? parentActivity,
        string traceId,
        string spanId,
        string sessionId)
    {
        _logger.LogInformation(
            "Workflow {WorkflowId} - Executor {ExecutorId} Completed. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
            workflowId,
            completed.ExecutorId,
            traceId,
            spanId,
            sessionId);
    }
    
    private void LogExecutorFailed(
        string workflowId,
        ExecutorFailedEvent failed,
        Activity? parentActivity,
        string traceId,
        string spanId,
        string sessionId)
    {
        _logger.LogError(
            "Workflow {WorkflowId} - Executor {ExecutorId} Failed: {Error}. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
            workflowId,
            failed.ExecutorId,
            failed.Data?.Message,
            traceId,
            spanId,
            sessionId);
            
        parentActivity?.SetStatus(ActivityStatusCode.Error, failed.Data?.Message);
    }
    
    private void LogWorkflowOutput(
        string workflowId,
        WorkflowOutputEvent output,
        Activity? parentActivity,
        string traceId,
        string spanId,
        string sessionId)
    {
        _logger.LogInformation(
            "Workflow {WorkflowId} Completed. Output from: {Source}. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
            workflowId,
            output.ExecutorId,
            traceId,
            spanId,
            sessionId);
        
        parentActivity?.SetStatus(ActivityStatusCode.Ok);
        parentActivity?.SetTag("workflow.source", output.ExecutorId);
    }

    private void LogHumanInputRequest(
        string workflowId,
        RequestInfoEvent requestInfo,
        string traceId,
        string spanId,
        string sessionId)
    {
        _logger.LogWarning(
            "✋ Workflow {WorkflowId} Waiting for Human Input. TraceId: {TraceId}. SpanId: {SpanId}. SessionId: {SessionId}",
            workflowId,
            traceId,
            spanId,
            sessionId);
            
        Activity.Current?.AddEvent(new ActivityEvent("HumanInputRequested"));
    }
}
