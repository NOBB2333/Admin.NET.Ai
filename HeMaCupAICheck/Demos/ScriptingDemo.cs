using Admin.NET.Ai.Services.Workflow;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

public static class ScriptingDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [5] Natasha 动态脚本热重载演示 ===");
        var scriptEngine = sp.GetRequiredService<NatashaScriptEngine>();

        // 从目录加载所有脚本文件
        var scriptDir = Path.Combine(AppContext.BaseDirectory, "Demos/NatashaHotReloadScript");
        if (!Directory.Exists(scriptDir))
        {
            Console.WriteLine($"[警告] 脚本目录不存在: {scriptDir}");
            return;
        }

        var scriptFiles = Directory.GetFiles(scriptDir, "*.cs");
        if (scriptFiles.Length == 0)
        {
             Console.WriteLine($"[警告] 目录中未找到任何 .cs 脚本文件: {scriptDir}");
             return;
        }

        var scripts = new List<string>();
        foreach (var file in scriptFiles)
        {
            scripts.Add(await File.ReadAllTextAsync(file));
        }

        Console.WriteLine($"正在动态编译 {scripts.Count} 个脚本并载入隔离域...");
        try 
        {
            var executors = scriptEngine.LoadScripts(scripts);
            
            foreach (var executor in executors)
            {
                var meta = executor.GetMetadata();
                Console.WriteLine($"\n✅ 脚本载入成功: {meta.Name} (v{meta.Version})");
                
                // 创建追踪上下文
                var trace = new ScriptExecutionContext(meta.Name);
                
                var args = new Dictionary<string, object?> { { "name", "HeMaCupUser" } };
                var result = await executor.ExecuteAsync(args, trace);
                
                Console.WriteLine($"执行结果: {result}");
                
                // 打印追踪信息
                Console.WriteLine("\n--- [脚本执行轨迹 (零侵入注入)] ---");
                PrintStep(trace.RootStep, 0);
            }

            if (executors.Any())
            {
                Console.WriteLine("\n[提示] 现在您可以修改源码并重新调用 LoadScripts，旧域将被自动卸载。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 脚本引擎执行失败: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private static void PrintStep(Admin.NET.Ai.Models.Workflow.ScriptStepInfo step, int indent)
    {
        var prefix = new string(' ', indent * 2) + (indent > 0 ? "└─ " : "");
        var statusIcon = step.Status == Admin.NET.Ai.Models.Workflow.ScriptStepStatus.Completed ? "✅" : (step.Status == Admin.NET.Ai.Models.Workflow.ScriptStepStatus.Failed ? "❌" : "⏳");
        
        Console.WriteLine($"{prefix}{statusIcon} [{step.Name}] 耗时: {step.Duration?.TotalMilliseconds:F2}ms");
        
        var margin = new string(' ', (indent + 1) * 2);
        if (step.Input != null) 
            Console.WriteLine($"{margin} 📥 输入: {System.Text.Json.JsonSerializer.Serialize(step.Input, _jsonOptions)}");
            
        if (step.Output != null)
            Console.WriteLine($"{margin} 📤 输出: {System.Text.Json.JsonSerializer.Serialize(step.Output, _jsonOptions)}");

        if (step.Error != null)
            Console.WriteLine($"{margin} 🔴 错误: {step.Error}");

        foreach (var child in step.Children)
        {
            PrintStep(child, indent + 1);
        }
    }
}
