
using Admin.NET.Ai.Models.Workflow;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// Natasha 脚本热重载接口
/// 不固定入参来兼容多种脚本
/// </summary>
public interface IScriptExecutor
{
    /// <summary>
    /// 获取脚本元数据
    /// </summary>
    ScriptMetadata GetMetadata();
    
    /// <summary>
    /// 执行脚本
    /// </summary>
    /// <param name="args">脚本业务参数 (动态)</param>
    /// <param name="trace">可观测性追踪上下文 (可选)</param>
    /// <param name="ct">取消令牌</param>
    Task<object?> ExecuteAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace = null, CancellationToken ct = default);
    
    #region 可选生命周期钩子 (默认空实现)
    
    /// <summary>
    /// 脚本首次加载到内存后调用 (可选)
    /// 可用于预热缓存、建立连接等
    /// </summary>
    Task OnLoadedAsync(IServiceProvider services) => Task.CompletedTask;
    
    /// <summary>
    /// 脚本即将被卸载前调用 (可选)
    /// 可用于释放资源、保存状态等
    /// </summary>
    Task OnUnloadingAsync() => Task.CompletedTask;
    
    #endregion
}

/// <summary>
/// 脚本元数据
/// </summary>
/// <param name="Name">脚本名称</param>
/// <param name="Version">脚本版本</param>
/// <param name="MaxExecutionTime">最大执行时间 (可选，null=不限制)</param>
/// <param name="Tags">分类标签 (可选)</param>
public record ScriptMetadata(string Name, string Version, TimeSpan? MaxExecutionTime = null, string[]? Tags = null );


