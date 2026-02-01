using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class MyDynamicExecutor : IScriptExecutor
{
    public async Task<object?> ExecuteAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace = null, 
        CancellationToken ct = default)
    {
        string name = args != null && args.TryGetValue("name", out var val) ? val?.ToString() ?? "Guest" : "Guest";
        
        await Task.CompletedTask;
        
        // 模拟调用内部方法，看是否能被自动追踪
        var greeting = GetGreeting(name);
        var score = CalculateScore();
        
        return $"[Natasha脚本执行结果] {greeting}，当前评分: {score}";
    }

    private string GetGreeting(string name)
    {
        return $"你好 {name}";
    }

    private long CalculateScore()
    {
        return DateTime.Now.Ticks % 1000;
    }

    public ScriptMetadata GetMetadata() => new ScriptMetadata("UserCustomScript", "1.0.0");
}
