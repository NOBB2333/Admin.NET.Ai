using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Admin.NET.Ai.Example.Meeting;

/// <summary>
/// 会议秘书：负责记录并总结会议成果，产出最终结构化文档
/// </summary>
public class MeetingSecretary(IAiFactory aiFactory, ILogger<MeetingSecretary> logger)
{
    private readonly IChatClient _chatClient = aiFactory.GetDefaultChatClient() 
        ?? throw new Exception("Default Chat Client not configured");

    /// <summary>
    /// 整理会议纪要，产出最终成果
    /// </summary>
    /// <typeparam name="T">期望的输出结构类型</typeparam>
    /// <param name="topic">会议原题</param>
    /// <param name="history">讨论历史记录</param>
    /// <param name="template">输出示例/要求</param>
    /// <returns></returns>
    public async Task<T> ConsolidateResultsAsync<T>(string topic, string history, string? template = null) where T : class
    {
        logger.LogInformation("正在整理会议纪要并生成最终成果...");

        var prompt = $@"
            你是一位专业的高级秘书和文档整理专家。
            根据以下多位专家的讨论纪要，请归纳总结出最终的成果文档。

            会议主题: {topic}
            讨论历史:
            ----------------------
            {history}
            ----------------------

            {(string.IsNullOrEmpty(template) ? "" : $"输出要求/示例模板: {template}")}

            请将讨论中的核心观点、剧本细节、分镜创意等进行结构化提取。
            请仅返回符合 JSON 格式的内容，确保字段名称与需求一致。
            ";

        // 使用结构化输出服务（如果可用）或直接解析 JSON
        // 这里为了演示通用性，使用带有 JSON 约束的请求
        var response = await _chatClient.GetResponseAsync(prompt);
        var json = CleanJson(response.Text);

        try
        {
            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return result ?? throw new Exception("Meeting result deserialization failed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析最终成果 JSON 失败: {Json}", json);
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
