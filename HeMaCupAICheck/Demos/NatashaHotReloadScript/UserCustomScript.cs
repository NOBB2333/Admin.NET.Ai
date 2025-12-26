using Admin.NET.Ai.Abstractions;
using System;
using System.Collections.Generic;

public class MyDynamicExecutor : IScriptExecutor
{
    public object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null)
    {
        string name = input?.GetValueOrDefault("name")?.ToString() ?? "Guest";
        
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
