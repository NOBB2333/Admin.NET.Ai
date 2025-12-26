using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;

public class ToolCallingContext
{
    public FunctionCallContent ToolCall { get; set; } = null!;
    public IServiceProvider ServiceProvider { get; set; } = null!;
}

// 工具调用的结果通常是 object? 或 string?。在 MEAI 中可能是 FunctionResultContent?
// 对于中间件，我们要拦截 *结果* 的生成。
// 然而，需求说 "Task<ToolResponse>"。我们将定义 ToolResponse。
public class ToolResponse
{
    public object? Result { get; set; }
}

public delegate Task<ToolResponse> NextToolCallingMiddleware(ToolCallingContext context);

/// <summary>
/// 工具调用拦截中间件
/// </summary>
public interface IToolCallingMiddleware
{
    Task<ToolResponse> InvokeAsync(ToolCallingContext context, NextToolCallingMiddleware next);
}
