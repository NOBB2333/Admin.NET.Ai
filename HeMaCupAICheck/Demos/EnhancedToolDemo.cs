using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 增强工具系统演示 — 展示 FileSystem/Search/Shell 工具 + 上下文注入 + 自管理审批
/// </summary>
public static class EnhancedToolDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [8] 增强工具系统 (FileSystem/Search/Shell) ===");

        var toolManager = sp.GetRequiredService<ToolManager>();

        // 1. 展示所有自动发现的工具及其审批状态
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n📦 [工具发现] 自动扫描到的工具:");
        Console.ResetColor();

        var tools = toolManager.GetAllTools();
        foreach (var tool in tools)
        {
            var functions = tool.GetFunctions().ToList();
            var approvalDefault = tool.RequiresApproval() ? "⚠️ 需审批" : "✅ 免审批";
            Console.WriteLine($"  🔧 {tool.Name} ({approvalDefault}) - {tool.Description}");
            foreach (var func in functions)
            {
                Console.WriteLine($"     ├─ {func.Name}: {func.Description}");
            }
        }

        // 2. 演示上下文注入
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n🔑 [上下文注入] 为工具注入执行上下文:");
        Console.ResetColor();

        var context = new ToolExecutionContext
        {
            SessionId = "demo-session-001",
            CallerAgentName = "DemoAgent",
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UserId = "demo-user"
        };
        Console.WriteLine($"  SessionId: {context.SessionId}");
        Console.WriteLine($"  WorkingDirectory: {context.WorkingDirectory}");
        Console.WriteLine($"  UserId: {context.UserId}");

        // 3. 获取带上下文的函数（审批由 ToolValidationMiddleware 统一处理）
        var allFunctions = toolManager.GetAllAiFunctions(context).ToList();
        Console.WriteLine($"\n  已加载 {allFunctions.Count} 个带上下文的函数");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  💡 审批拦截统一在 ToolValidationMiddleware 中处理，ToolManager 只负责发现和上下文注入");
        Console.ResetColor();

        // 4. 调用演示：读取文件（免审批）
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n📖 [调用演示] 列出当前目录文件（免审批操作）:");
        Console.ResetColor();

        var listDirFunc = allFunctions.FirstOrDefault(f => f.Name == "list_directory");
        if (listDirFunc != null)
        {
            var result = await listDirFunc.InvokeAsync(new AIFunctionArguments
            {
                ["dirPath"] = context.WorkingDirectory,
                ["maxDepth"] = 1
            });
            Console.WriteLine(result?.ToString());
        }
        else
        {
            Console.WriteLine("  ⚠️ list_directory 函数未找到");
        }

        // 5. 调用演示：搜索文件（免审批）
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n🔍 [调用演示] 搜索 Demo 文件（免审批操作）:");
        Console.ResetColor();

        var globFunc = allFunctions.FirstOrDefault(f => f.Name == "glob_search");
        if (globFunc != null)
        {
            var result = await globFunc.InvokeAsync(new AIFunctionArguments
            {
                ["directory"] = context.WorkingDirectory,
                ["pattern"] = "*Demo*.cs",
                ["maxDepth"] = 2,
                ["maxResults"] = 10
            });
            Console.WriteLine(result?.ToString());
        }

        // 6. 展示审批状态判断（不实际执行 Shell）
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n⚡ [审批检查] 模拟工具审批判断:");
        Console.ResetColor();

        foreach (var tool in tools)
        {
            var testArgs = new Dictionary<string, object?>();
            var needsApproval = tool.RequiresApproval(testArgs);
            var icon = needsApproval ? "🔴" : "🟢";
            Console.WriteLine($"  {icon} {tool.Name}: RequiresApproval() = {needsApproval}");
        }

        // FileSystem 写入路径外文件的审批判断
        var fsTool = tools.FirstOrDefault(t => t.Name == "FileSystemTools");
        if (fsTool != null)
        {
            var insideArgs = new Dictionary<string, object?> { ["filePath"] = Path.Combine(context.WorkingDirectory, "test.txt") };
            var outsideArgs = new Dictionary<string, object?> { ["filePath"] = "/tmp/test.txt" };
            Console.WriteLine($"\n  📂 FileSystemTools 路径感知审批:");
            Console.WriteLine($"     工作目录内写入: RequiresApproval = {fsTool.RequiresApproval(insideArgs)}");
            Console.WriteLine($"     工作目录外写入: RequiresApproval = {fsTool.RequiresApproval(outsideArgs)}");
        }
    }
}
