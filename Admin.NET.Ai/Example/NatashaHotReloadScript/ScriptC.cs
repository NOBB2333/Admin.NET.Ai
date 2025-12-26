using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

public class ScriptC : IScriptExecutor
{
    public ScriptMetadata GetMetadata() => new ScriptMetadata("ScriptC - FlowTestScript", "1.0");

    public object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null)
    {
        Console.WriteLine("Starting Flow Test...");
        
        if (input != null)
        {
            Console.WriteLine("Input received");
            if (input.ContainsKey("user"))
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
