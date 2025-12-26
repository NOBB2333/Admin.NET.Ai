// 这几个using 不能删除
// 有时候会被编辑器默认优化掉 所以需要加上

using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

public class ScriptB : IScriptExecutor
{
    public string Name => "Script B";
    public string Version => "0.6-beta";

    public ScriptMetadata GetMetadata() => new ScriptMetadata(Name, Version);

    public object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null)
    {
        Console.WriteLine("[ScriptB] Calculation start...");
        return 100 + " (Answer from Script B - Reloaded)";
    }
}