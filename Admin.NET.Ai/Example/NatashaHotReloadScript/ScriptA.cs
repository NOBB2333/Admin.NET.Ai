// 这几个using 不能删除
// 有时候会被编辑器默认优化掉 所以需要加上

using Admin.NET.Ai.Abstractions;

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

    public object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null)
    {
        Console.WriteLine("[ScriptA] Executing...");
        var name = input != null && input.ContainsKey("user") ? input["user"]?.ToString() : "Unknown";
        return (name ?? "Unknown") + " [From Script A]";
    }
}