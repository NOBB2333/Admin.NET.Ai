using System.Text.Json.Serialization;

namespace Admin.NET.Ai.Services.Prompt;

/// <summary>
/// 提示词配置模版
/// </summary>
public class PromptConfig
{
    /// <summary>
    /// 模版名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模版内容 (支持 Liquid/Handlebars 风格变量 {{var}})
    /// </summary>
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// 角色定义 (System, User, Assistant)
    /// 用于构建多轮对话模版
    /// </summary>
    public List<PromptMessage> Messages { get; set; } = new();

    /// <summary>
    /// 输入参数定义 (用于验证和自动生成 UI)
    /// </summary>
    public List<PromptInputParameter> InputParameters { get; set; } = new();
    
    /// <summary>
    /// 模型配置参数 (Temperature, MaxTokens 等)
    /// </summary>
    public Dictionary<string, object> ModelSettings { get; set; } = new();
}

public class PromptMessage
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PromptRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
}

public enum PromptRole
{
    System,
    User,
    Assistant
}

public class PromptInputParameter
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
}
