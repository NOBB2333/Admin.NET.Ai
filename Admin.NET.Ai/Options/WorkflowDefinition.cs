using System.Text.Json.Serialization;

namespace Admin.NET.Ai.Models.Workflow;

/// <summary>
/// 工作流定义
/// </summary>
public class WorkflowDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowType Type { get; set; } = WorkflowType.Sequential;
    public bool EnableCheckpointing { get; set; }
    public string? CheckpointPath { get; set; }
    public List<WorkflowStep> Steps { get; set; } = [];
}

/// <summary>
/// 工作流类型
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkflowType
{
    Sequential,
    Concurrent, // Parallel
    Handoff,    // Handoff/Router
    GroupChat   // Group Chat
}

/// <summary>
/// 工作流步骤
/// </summary>
public class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent Name (Optional, if referencing an existing agent)
    /// </summary>
    public string? AgentName { get; set; }
    
    public StepType Type { get; set; } = StepType.Prompt;
    
    /// <summary>
    /// 步骤内容 (Prompt模板, Instructions 或 工具名称)
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 变量映射 (Key: 模板变量名, Value: 上下文Key/表达式)
    /// </summary>
    public Dictionary<string, string> Inputs { get; set; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StepType
{
    Prompt, // Execute prompt/instructions
    Tool,   // Execute tool
    Script, // C# Script
    Workflow // Sub-workflow
}
