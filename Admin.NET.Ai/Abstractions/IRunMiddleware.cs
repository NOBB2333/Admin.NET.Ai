using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 运行中间件上下文
/// </summary>
public class RunMiddlewareContext
{
    public ChatOptions? Options { get; set; }
    // 请求信息通常来自 ChatRequest 或隐式参数。
    // 在 MEAI 中，我们通常有 IList<ChatMessage> 和 ChatOptions。
    // 我们将在这里把它们包装起来。
    public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    
    // 帮助模拟需求中的 "Request" 对象
    public ChatRequest Request => new ChatRequest { Messages = Messages, Model = Options?.ModelId };

    public IServiceProvider ServiceProvider { get; set; } = null!;

    public string? TraceId { get; set; }
    public string? ActionName { get; set; }

    public string? GetUserId() 
    {
        // 从 Options 或 ServiceProvider (HttpContext) 获取
        // 这里简单模拟
        return "user_generic";
    }

    public string? GetClientIp() => "127.0.0.1";

    public string? GetActionName() => ActionName;
}

// 模拟 ChatRequest 用于兼容代码
public class ChatRequest
{
    public IList<ChatMessage> Messages { get; set; }
    public string? Model { get; set; }
}

public delegate Task<ChatResponse> NextRunMiddleware(RunMiddlewareContext context);

/// <summary>
/// 对话执行拦截中间件接口
/// </summary>
public interface IRunMiddleware
{
    Task<ChatResponse> InvokeAsync(RunMiddlewareContext context, NextRunMiddleware next);
}
