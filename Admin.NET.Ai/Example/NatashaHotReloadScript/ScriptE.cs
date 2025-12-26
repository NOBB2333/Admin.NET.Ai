using Admin.NET.Ai.Abstractions;
using SqlSugar;

namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

public class ScriptE : IScriptExecutor
{
    private readonly ISqlSugarClient _db;

    // SqlSugar Client injected by host
    public ScriptE(ISqlSugarClient db)
    {
        _db = db;
    }

    public ScriptMetadata GetMetadata() => new ScriptMetadata("ScriptE - Database Script", "1.0");

    public object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null)
    {
        Console.WriteLine("[ScriptE] Querying Database for Students...");
        
        var students = _db.Queryable<Student>().ToList();
        
        foreach (var s in students)
        {
            Console.WriteLine($"[ScriptE] Found: {s.Id} - {s.Name} ({s.Age})");
        }
        
        return $"Retrieved {students.Count} students";
    }
}
