using System.Diagnostics;

namespace Admin.NET.Ai.Diagnostics;

/// <summary>
/// AI 模块 Activity Source (用于 OpenTelemetry 追踪)
/// </summary>
public static class AiDiagnostics
{
    public const string Name = "Admin.NET.Ai";
    
    public static readonly ActivitySource Source = new(Name);
}
