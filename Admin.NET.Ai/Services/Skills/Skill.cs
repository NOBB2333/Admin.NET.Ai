using System.Text.Json.Serialization;

namespace Admin.NET.Ai.Services.Skills;

public enum SkillType { LanguageSkill, ToolSkill, WorkflowSkill }

public class SkillManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 触发短语或意图关键字
    /// </summary>
    [JsonPropertyName("trigger")]
    public SkillTrigger Trigger { get; set; } = new();
    
    [JsonPropertyName("type")]
    public string TypeString { get; set; } = "language_skill"; // 默认
    
    [JsonIgnore]
    public SkillType Type => TypeString switch 
    {
        "language_skill" => SkillType.LanguageSkill,
        "tool_skill" => SkillType.ToolSkill,
        "workflow_skill" => SkillType.WorkflowSkill,
        _ => SkillType.LanguageSkill
    };

    [JsonPropertyName("input_schema")]
    public object? InputSchema { get; set; }
    
    [JsonPropertyName("output_schema")]
    public object? OutputSchema { get; set; }

    [JsonPropertyName("prompt_template")]
    public string PromptTemplate { get; set; } = "";
}

public class SkillTrigger
{
    [JsonPropertyName("detect_by")]
    public List<string> DetectBy { get; set; } = new();
    
    [JsonPropertyName("confidence_threshold")]
    public double ConfidenceThreshold { get; set; } = 0.5;
}

public class LoadingSkill
{
    public string Id { get; set; }
    public SkillManifest Manifest { get; set; }
    // 如果是工具/工作流，可以持有对可执行逻辑的引用
}
