using Admin.NET.Ai.Models.Workflow;

namespace Admin.NET.Ai.Abstractions;

public interface IScriptExecutor
{
    object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null);
    ScriptMetadata GetMetadata();
}

public record ScriptMetadata(string Name, string Version);
