using System;
using System.Collections.Generic;

namespace Admin.NET.Ai.Models.Workflow;

/// <summary>
/// 脚本执行步骤信息
/// </summary>
public class ScriptStepInfo
{
    public string Name { get; set; } = default!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public object? Input { get; set; }
    public object? Output { get; set; }
    public string? Error { get; set; }
    public ScriptStepStatus Status { get; set; }
    public List<ScriptStepInfo> Children { get; set; } = new();

    public TimeSpan? Duration => EndTime - StartTime;
}

public enum ScriptStepStatus
{
    Running,
    Completed,
    Failed
}

/// <summary>
/// 脚本执行上下文接口
/// </summary>
public interface IScriptExecutionContext
{
    ScriptStepInfo RootStep { get; }
    IScriptStepScope BeginStep(string name, object? input = null);
    void SetOutput(object? output);
    void SetError(Exception ex);
}

/// <summary>
/// 步骤作用域接口，支持 using 语法
/// </summary>
public interface IScriptStepScope : IDisposable
{
    void SetOutput(object? output);
    void SetError(Exception ex);
}

/// <summary>
/// 脚本执行最终结果汇总
/// </summary>
public class ScriptExecutionTrace
{
    public ScriptStepInfo Root { get; set; } = default!;
    public object? FinalResult { get; set; }
    public bool Success => Root.Status == ScriptStepStatus.Completed;
}
