using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Admin.NET.Ai.Services.Monitoring;

/// <summary>
/// Agent Telemetry Service
/// Centralized management for OpenTelemetry Activities and Metrics
/// </summary>
public class AgentTelemetry
{
    public const string SourceName = "Admin.NET.Ai";
    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(SourceName);

    // Metrics
    private static readonly Counter<long> TokenConsumption = Meter.CreateCounter<long>("ai.token.consumption", "token");
    private static readonly Histogram<double> AgentExecutionDuration = Meter.CreateHistogram<double>("ai.agent.duration", "ms");
    private static readonly Counter<long> WorkflowExecutions = Meter.CreateCounter<long>("ai.workflow.executions", "count");

    /// <summary>
    /// Start a new activity for an Agent Run
    /// </summary>
    public Activity? StartAgentActivity(string agentName, string stepName)
    {
        var activity = ActivitySource.StartActivity($"Agent.{agentName}", ActivityKind.Internal);
        if (activity != null)
        {
            activity.SetTag("agent.name", agentName);
            activity.SetTag("agent.step", stepName);
        }
        return activity;
    }

    /// <summary>
    /// Start a new activity for a Workflow
    /// </summary>
    public Activity? StartWorkflowActivity(string workflowName, string workflowId)
    {
        WorkflowExecutions.Add(1);
        var activity = ActivitySource.StartActivity($"Workflow.{workflowName}", ActivityKind.Server);
        if (activity != null)
        {
            activity.SetTag("workflow.id", workflowId);
            activity.SetTag("workflow.name", workflowName);
        }
        return activity;
    }

    /// <summary>
    /// Start a new activity for a Tool Call
    /// </summary>
    public Activity? StartToolActivity(string toolName)
    {
        var activity = ActivitySource.StartActivity($"Tool.{toolName}", ActivityKind.Client);
        activity?.SetTag("tool.name", toolName);
        return activity;
    }

    /// <summary>
    /// Record Token Usage
    /// </summary>
    public void RecordTokenUsage(long tokens, string modelId)
    {
        TokenConsumption.Add(tokens, new KeyValuePair<string, object?>("model.id", modelId));
    }

    /// <summary>
    /// Record error
    /// </summary>
    public void RecordError(Activity? activity, Exception ex)
    {
        if (activity == null) return;
        
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", ex.GetType().FullName },
            { "exception.message", ex.Message },
            { "exception.stacktrace", ex.StackTrace }
        }));
    }
}
