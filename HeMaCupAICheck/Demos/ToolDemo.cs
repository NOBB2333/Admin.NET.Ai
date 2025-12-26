using Admin.NET.Ai.Services.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

public static class ToolDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [4] 智能工具与审批流程 ===");

        // 1. 自动发现工具
        var toolManager = sp.GetRequiredService<ToolManager>();
        var alltools = toolManager.GetAllAiFunctions();
        Console.WriteLine($"[ToolManager] 自动发现系统中可用的 AI 函数: {alltools.Count()} 个");
        foreach(var f in alltools)
        {
            Console.WriteLine($" - {f.Name}: {f.Description}");
        }

        // 2. 演示敏感操作审批流
        Console.WriteLine("\n[场景] 演示敏感操作: 只有用户输入 'y' 才能执行删除操作");
        
        var riskyFunc = AIFunctionFactory.Create(
            (string userId) => $"[SYSTEM] 用户 {userId} 的数据已从生产库彻底删除。", 
            "DeleteUserData", 
            "危险操作：删除指定用户的所有历史数据"
        );

        // 使用本地包装器代替 broken 的库方法，以正确处理参数元数据
        var approvedFunc = AIFunctionFactory.Create(async (string userId, CancellationToken ct) => 
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n🚨 [审批请求] AI 申请调用敏感工具: {riskyFunc.Name}");
            Console.WriteLine($"📝 参数明细: userId={userId}");
            Console.ResetColor();
            Console.Write("⚠️ 是否批准该操作? (y/n): ");
            var input = Console.ReadLine();
            
            if (input?.ToLower() == "y")
            {
                return await riskyFunc.InvokeAsync(new AIFunctionArguments { ["userId"] = userId }, ct);
            }
            return "[Operation Cancelled] 用户拒绝了该操作。";
        }, riskyFunc.Name, riskyFunc.Description);

        Console.WriteLine("\n正在调用带审批的函数...");
        try 
        {
            var result = await approvedFunc.InvokeAsync(new AIFunctionArguments { ["userId"] = "HEMA_001" });
            Console.WriteLine($"执行结果: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 执行被拦截或失败: {ex.Message}");
        }
    }
}
