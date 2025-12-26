using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using System.Collections.Generic;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 工作流脚本基类，提供手动记录步骤的辅助方法
/// </summary>
public abstract class BaseWorkflowScript : IScriptExecutor
{
    // 该字段会被 ScriptSourceRewriter 自动注入的值覆盖（如果存在重名的注入，则使用注入的）
    // 为了兼容性，我们定义一个可供子类使用的受保护属性
    protected IScriptExecutionContext? Context => (this as dynamic)._scriptContext;

    public abstract object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null);

    public virtual ScriptMetadata GetMetadata() => new ScriptMetadata(GetType().Name, "1.0.0");

    /// <summary>
    /// 手动记录一个命名步骤
    /// </summary>
    protected T Record<T>(string stepName, Func<T> action, object? input = null)
    {
        using var scope = Context?.BeginStep(stepName, input);
        try
        {
            var result = action();
            scope?.SetOutput(result);
            return result;
        }
        catch (Exception ex)
        {
            scope?.SetError(ex);
            throw;
        }
    }
}
