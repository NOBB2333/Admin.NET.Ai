using System.Text.Json.Serialization;

namespace Admin.NET.Ai.Models.Workflow;

/// <summary>
/// 会议参与者定义
/// </summary>
public class MeetingParticipant
{
    /// <summary>
    /// Agent 名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Agent 角色描述
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 详细执行指令
    /// </summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;
}

/// <summary>
/// 会议策划方案
/// </summary>
public class MeetingPlan
{
    /// <summary>
    /// 会议主题
    /// </summary>
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 邀请的专家列表
    /// </summary>
    [JsonPropertyName("participants")]
    public List<MeetingParticipant> Participants { get; set; } = new();

    /// <summary>
    /// 会议开场白/引导语
    /// </summary>
    [JsonPropertyName("opening_remarks")]
    public string OpeningRemarks { get; set; } = string.Empty;
}

/// <summary>
/// 剧本创作会议成果 (通用示例)
/// </summary>
public class CreativeMeetingResult
{
    /// <summary>
    /// 剧本内容
    /// </summary>
    [JsonPropertyName("script")]
    public string Script { get; set; } = string.Empty;

    /// <summary>
    /// 分镜描述
    /// </summary>
    [JsonPropertyName("storyboard")]
    public string Storyboard { get; set; } = string.Empty;

    /// <summary>
    /// 绘图/提示词脚本
    /// </summary>
    [JsonPropertyName("image_prompts")]
    public string ImagePrompts { get; set; } = string.Empty;

    /// <summary>
    /// 视频拍摄脚本
    /// </summary>
    [JsonPropertyName("video_scripts")]
    public string VideoScripts { get; set; } = string.Empty;
}
