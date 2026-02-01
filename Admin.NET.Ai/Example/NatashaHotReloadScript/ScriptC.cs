using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;

namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

public class ScriptC : IScriptExecutor
{
    public ScriptMetadata GetMetadata() => new ScriptMetadata("ScriptC - FlowTestScript", "1.0");

    public async Task<object?> ExecuteAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace = null, 
        CancellationToken ct = default)
    {
        Console.WriteLine("Starting Flow Test...");
        await Task.CompletedTask;
        
        if (args != null)
        {
            Console.WriteLine("Input received");
            if (args.ContainsKey("user"))
            {
                // Branch A
                return "User found";
            }
            else
            {
                // Branch B
                try 
                {
                    Console.WriteLine("Attempting logic...");
                    throw new Exception("Test Error");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Caught: {ex.Message}");
                    return "Error Handled";
                }
                finally
                {
                    Console.WriteLine("Finally block executed");
                }
            }
        }
        
        return "No Input";
    }
}
