namespace Admin.NET.Ai.Models.Workflow;

public abstract class AiWorkflowEvent { }

public class AiAgentRunUpdateEvent : AiWorkflowEvent
{
    public string AgentName { get; set; } = string.Empty;
    public string Step { get; set; } = string.Empty;
}

public class AiFunctionCallingEvent : AiWorkflowEvent
{
    public string FunctionName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

public class AiWorkflowOutputEvent : AiWorkflowEvent
{
    public object? Output { get; set; }
}

public class AiWorkflowErrorEvent : AiWorkflowEvent
{
    public string ErrorMessage { get; set; } = string.Empty;
}

public class AiWorkflowHumanInputEvent : AiWorkflowEvent
{
    public string Prompt { get; set; } = string.Empty;
    public string WorkflowId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
}
