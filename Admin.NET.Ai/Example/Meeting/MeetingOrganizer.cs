using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Admin.NET.Ai.Example.Meeting;

/// <summary>
/// 会议组织者：负责分析主题并动态定义所需角色
/// </summary>
public class MeetingOrganizer(IAiFactory aiFactory, ILogger<MeetingOrganizer> logger)
{
    private readonly IChatClient _chatClient = aiFactory.GetDefaultChatClient() 
        ?? throw new Exception("Default Chat Client not configured");

    /// <summary>
    /// 根据主题策划会议方案
    /// </summary>
    /// <param name="topic">会议主题/原始素材</param>
    /// <param name="maxParticipants">建议最大参与者数量</param>
    /// <returns></returns>
    public async Task<MeetingPlan> OrganizeMeetingAsync(string topic, int maxParticipants = 5)
    {
        logger.LogInformation("正在策划会议内容，主题: {Topic}", topic);

        var prompt = $@"
            你是一位专家级会议策划师和组织者。
            请根据以下主题或素材，确定最适合参与讨论的【专家角色】。
            你需要产生一个包含 3-5 个不同背景专家的列表，并为每位专家提供详细的执行指令。

            主题/素材: {topic}
            建议最大人数: {maxParticipants}

            要求:
            1. 角色之间应有互补性。
            2. 指令(Instructions)应明确该角色在讨论中关注的重点。
            3. 产生一段【开场白】，引导专家们进入状态。

            请仅返回以下 JSON 格式的内容:
            {{
            ""topic"": ""简短的主题概括"",
            ""opening_remarks"": ""开场白内容"",
            ""participants"": [
                {{
                ""name"": ""角色名称"",
                ""role"": ""角色身份描述"",
                ""instructions"": ""该角色参与讨论的详细指令""
                }}
            ]
            }}
            ";

        var response = await _chatClient.GetResponseAsync(prompt);
        var json = CleanJson(response.Text);

        try
        {
            var plan = JsonSerializer.Deserialize<MeetingPlan>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return plan ?? throw new Exception("Meeting plan deserialization failed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析会议计划 JSON 失败: {Json}", json);
            throw;
        }
    }

    private string CleanJson(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "{}";
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start) return text.Substring(start, end - start + 1);
        return text;
    }
}
