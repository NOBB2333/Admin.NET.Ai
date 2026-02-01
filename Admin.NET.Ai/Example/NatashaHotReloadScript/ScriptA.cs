// 这几个using 不能删除
// 有时候会被编辑器默认优化掉 所以需要加上

using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;

namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

public class ScriptA : IScriptExecutor
{
    private readonly IAiFactory _service;

    public ScriptA(IAiFactory service)
    {
        _service = service;
    }

    public string Name => "Script A";
    public string Version => "1.0.1";

    public ScriptMetadata GetMetadata() => new ScriptMetadata(Name, Version);

    public async Task<object?> ExecuteAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace = null, 
        CancellationToken ct = default)
    {
        Console.WriteLine("[ScriptA] Executing...");
        var name = args != null && args.ContainsKey("user") ? args["user"]?.ToString() : "Unknown";
        // 模拟异步操作
        await Task.CompletedTask;
        return (name ?? "Unknown") + " [From Script A]";
    }
}