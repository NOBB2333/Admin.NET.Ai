using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Models.Workflow;
using Admin.NET.Ai.Example.Meeting; // 使用应用层会议逻辑
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Example;

/// <summary>
/// 多智能体会议讨论示例
/// </summary>
public class MeetingExample
{
    public static async Task RunAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddAdminNetAi(builder.Configuration); // 核心：注册 Admin.NET.Ai 模块
        
        // --- 注册应用层专属的“会议能力” ---
        builder.Services.AddScoped<MeetingOrganizer>();
        builder.Services.AddScoped<MeetingSecretary>();
        builder.Services.AddScoped<MeetingRoom>();

        var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var meetingRoom = scope.ServiceProvider.GetRequiredService<MeetingRoom>();

        // --- 场景 1：小说改剧本 (电影摄制组) ---
        Console.WriteLine("\n=== 场景 1：电影剧本创作会议 ===");
        
        string novelContent = @"
在一个被赛博朋克霓虹灯覆盖的城市里，侦探林浩接到了一个神秘的任务。
一个消失了十年的数字灵魂突然出现在他的显示器上，向他求救。
林浩必须深入城市的贫民窟，寻找那台被称为“灵魂中转站”的旧式服务器。
";

        string creativeTemplate = @"
结果应包含：
1. 剧本文字 (Script)
2. 每一个镜头的视觉描述 (Storyboard)
3. 关键场景的绘图 Prompt (ImagePrompts)
";

        try
        {
            var movieResult = await meetingRoom.ExecuteMeetingWorkflowAsync<CreativeMeetingResult>(
                novelContent, 
                creativeTemplate);

            Console.WriteLine("\n[最终剧本成果]");
            Console.WriteLine(movieResult.Script);
            
            Console.WriteLine("\n[分镜描述]");
            Console.WriteLine(movieResult.Storyboard);

            Console.WriteLine("\n[绘图 Prompt]");
            Console.WriteLine(movieResult.ImagePrompts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创作会议失败: {ex.Message}");
        }

        // --- 场景 2：基于历史数据的经营计划 ---
        Console.WriteLine("\n=== 场景 2：年度执行计划会议 ===");

        string businessData = @"
2024年数据回顾：
- 总营收：5000万，同比增长 15%
- 获客成本：上涨 20%
- 主要增长点：短视频带货
- 痛点：退货率在第四季度大幅上升
";
        
        // 匿名对象作为输出结构示例
        try
        {
            var plan = await meetingRoom.ExecuteMeetingWorkflowAsync<dynamic>(
                $"基于以下数据制定2025年计划：{businessData}",
                "输出：重点目标、风险控制策略、预算分配建议");

            Console.WriteLine("\n[2025年计划汇总]");
            Console.WriteLine(plan?.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"经营规划会议失败: {ex.Message}");
        }
    }
}
