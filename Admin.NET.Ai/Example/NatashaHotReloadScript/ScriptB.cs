using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;

namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

public class ScriptB : IScriptExecutor
{
    public string Name => "Script B";
    public string Version => "2.0.0";

    public ScriptMetadata GetMetadata() => new ScriptMetadata(Name, Version);

    public async Task<object?> ExecuteAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace = null, 
        CancellationToken ct = default)
    {
        Console.WriteLine("[ScriptB] Calculation start...");
        await Task.CompletedTask;
        return "Result from Script B";
    }
}