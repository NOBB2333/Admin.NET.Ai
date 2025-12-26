using Admin.NET.Ai.Services.Prompt;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

public static class PromptDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [7] 提示词工程演示 ===");

        var promptManager = sp.GetRequiredService<IPromptManager>();

        // 1. 注册提示词模板
        var templateName = "WeeklyReport";
        var templateContent = @"
            你是一个专业的周报助手。请根据以下信息生成周报：
            姓名: {{Name}}
            部门: {{Department}}
            本周工作内容:
            {{WorkItems}}

            要求风格职业、简洁。
            ";
        Console.WriteLine($"正在注册提示词模板: {templateName}");
        await promptManager.RegisterPromptAsync(templateName, templateContent);

        // 2. 准备变量
        var vars = new Dictionary<string, object>
        {
            { "Name", "王工" },
            { "Department", "AI 研发部" },
            { "WorkItems", "- 完成了 PromptManager 的开发\n- 修复了配置加载 Bug\n- 编写了演示案例" }
        };

        // 3. 渲染提示词
        Console.WriteLine("正在渲染提示词...");
        var rendered = await promptManager.GetRenderedPromptAsync(templateName, vars);

        Console.WriteLine("\n--- 渲染结果 ---");
        Console.WriteLine(rendered);
        Console.WriteLine("----------------");

    }
}
