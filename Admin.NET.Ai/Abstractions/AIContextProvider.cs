using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;

public class AIContextProviderContext
{
    public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ChatResponse? Response { get; set; }
    public IServiceProvider ServiceProvider { get; set; } = null!;
}

public class AIContextItem
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

/// <summary>
/// AI 上下文提供者基类
/// 支持 InvokingAsync (注入上下文) 和 InvokedAsync (保存状态)
/// </summary>
public abstract class AIContextProvider
{
    /// <summary>
    /// 在 Agent 处理请求前调用 (注入上下文)
    /// </summary>
    public virtual Task<AIContextItem?> InvokingAsync(AIContextProviderContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AIContextItem?>(null);
    }

    /// <summary>
    /// 在 Agent 生成响应后调用 (后处理/保存)
    /// </summary>
    public virtual Task<AIContextItem?> InvokedAsync(AIContextProviderContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AIContextItem?>(null);
    }
}
