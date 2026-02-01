using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
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

    public async Task<object?> ExecuteAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace = null, 
        CancellationToken ct = default)
    {
        Console.WriteLine("[ScriptE] Querying Database for Students...");
        
        var students = await _db.Queryable<Student>().ToListAsync(); // Assume ToListAsync exists for SqlSugar
        
        foreach (var s in students)
        {
            Console.WriteLine($"[ScriptE] Found: {s.Id} - {s.Name} ({s.Age})");
        }
        
        return $"Retrieved {students.Count} students";
    }
}
