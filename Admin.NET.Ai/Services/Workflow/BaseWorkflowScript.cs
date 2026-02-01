using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 工作流脚本基类，提供手动记录步骤的辅助方法
/// </summary>
public abstract class BaseWorkflowScript : IScriptExecutor
{
    /// <summary>
    /// 追踪上下文，在 ExecuteAsync 调用时被设置
    /// </summary>
    protected IScriptExecutionContext? Trace { get; private set; }

    public virtual Task<object?> ExecuteAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace = null, 
        CancellationToken ct = default)
    {
        Trace = trace;
        return ExecuteInternalAsync(args, trace, ct);
    }

    /// <summary>
    /// 子类需实现的实际执行逻辑
    /// </summary>
    protected abstract Task<object?> ExecuteInternalAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace, 
        CancellationToken ct);

    public virtual ScriptMetadata GetMetadata() => new ScriptMetadata(GetType().Name, "1.0.0");

    /// <summary>
    /// 手动记录一个命名步骤
    /// </summary>
    protected T Record<T>(string stepName, Func<T> action, object? input = null)
    {
        using var scope = Trace?.BeginStep(stepName, input);
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

    /// <summary>
    /// 手动记录一个异步命名步骤
    /// </summary>
    protected async Task<T> RecordAsync<T>(string stepName, Func<Task<T>> action, object? input = null)
    {
        using var scope = Trace?.BeginStep(stepName, input);
        try
        {
            var result = await action();
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
