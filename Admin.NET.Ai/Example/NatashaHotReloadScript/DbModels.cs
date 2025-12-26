
using SqlSugar;

namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

// Simple Entity
[SugarTable("Student")]
public class Student
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    
    public string? Name { get; set; }
    
    public int Age { get; set; }
}
