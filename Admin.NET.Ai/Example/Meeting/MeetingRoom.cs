using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Example.Meeting;

/// <summary>
/// 会议室：在应用层封装“策划-讨论-汇总”模式
/// 这是对底层能力的一个组合示例
/// </summary>
public class MeetingRoom(
    IServiceProvider serviceProvider,
    ILogger<MeetingRoom> logger,
    MeetingOrganizer organizer,
    MeetingSecretary secretary)
{
    /// <summary>
    /// 执行会议工作流
    /// </summary>
    public async Task<T> ExecuteMeetingWorkflowAsync<T>(string topic, string? template = null) where T : class
    {
        logger.LogInformation("正在开启多智能体会议讨论 (应用层封装)，主题: {Topic}", topic);

        // 1. 策划阶段：生成参与者和开场白
        var plan = await organizer.OrganizeMeetingAsync(topic);
        logger.LogInformation("会议策划完成，生成了 {Count} 位专家。", plan.Participants.Count);

        // 2. 讨论阶段
        var discussionHistory = new System.Text.StringBuilder();
        discussionHistory.AppendLine($"会议主题：{plan.Topic}");
        discussionHistory.AppendLine($"开场白：{plan.OpeningRemarks}");
        discussionHistory.AppendLine();

        var aiFactory = serviceProvider.GetRequiredService<IAiFactory>();
        var chatClient = aiFactory.GetDefaultChatClient() ?? throw new Exception("Default Chat Client not configured");

        foreach (var participant in plan.Participants)
        {
            logger.LogInformation("专家 {Name} ({Role}) 正在发言...", participant.Name, participant.Role);

            var prompt = $@"
你现在是【{participant.Name}】，你的背景是【{participant.Role}】。
你的指令是：{participant.Instructions}

当前会议讨论历史如下：
{discussionHistory}

请根据你的角色定位和指令，对当前讨论做出贡献或提出新的见解。
直接返回你的发言内容。
";
            var response = await chatClient.GetResponseAsync(prompt);
            var speech = response.Text;

            discussionHistory.AppendLine($"【{participant.Name} ({participant.Role})】:");
            discussionHistory.AppendLine(speech);
            discussionHistory.AppendLine();
        }

        // 3. 汇总阶段
        return await secretary.ConsolidateResultsAsync<T>(topic, discussionHistory.ToString(), template);
    }
}
