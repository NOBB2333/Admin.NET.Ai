namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// AI 中间件接口
/// </summary>
public interface IAiMiddleware
{
    /// <summary>
    /// 执行中间件逻辑
    /// </summary>
    /// <param name="context">AI 上下文</param>
    /// <param name="next">下一个中间件</param>
    /// <returns></returns>
    Task InvokeAsync(IAiContext context, AiRequestDelegate next);
}
