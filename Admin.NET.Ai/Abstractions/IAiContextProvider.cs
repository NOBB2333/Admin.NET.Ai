using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;


/// <summary>
/// AI 上下文提供者基类
/// 支持 InvokingAsync (注入上下文) 和 InvokedAsync (保存状态)
/// </summary>
public interface IAiContextProvider
{
    /// <summary>
    /// 在 Agent 处理请求前调用 (注入上下文)
    /// </summary>
    public Task<AiContextItem?> InvokingAsync(AiContextProviderContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AiContextItem?>(null);
    }

    /// <summary>
    /// 在 Agent 生成响应后调用 (后处理/保存)
    /// </summary>
    public Task<AiContextItem?> InvokedAsync(AiContextProviderContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AiContextItem?>(null);
    }
}


public abstract class AiContextProviderContext
{
    public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ChatResponse? Response { get; set; }
    public IServiceProvider ServiceProvider { get; set; } = null!;
}

public class AiContextItem
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}
