using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// AI上下文注入中间件
/// 负责调用 AIContextProvider 的生命周期钩子
/// </summary>
public class ContextInjectionMiddleware : IRunMiddleware
{
    private readonly IEnumerable<AIContextProvider> _providers;

    public ContextInjectionMiddleware(IEnumerable<AIContextProvider> providers)
    {
        _providers = providers;
    }

    public async Task<ChatResponse> InvokeAsync(
        RunMiddlewareContext context, 
        NextRunMiddleware next)
    {
        var providerContext = new AIContextProviderContext
        {
            Messages = context.Messages,
            ServiceProvider = context.ServiceProvider
        };

        // 1. InvokingAsync 钩子 (注入上下文)
        // 1. InvokingAsync 钩子 (注入上下文)
        foreach (var provider in _providers)
        {
            var injection = await provider.InvokingAsync(providerContext);
            if (injection != null && !string.IsNullOrEmpty(injection.Content))
            {
                // 注入为系统、用户或工具消息？
                // 通常是系统指令或用户上下文。
                var role = injection.Role?.ToLower() == "user" ? ChatRole.User : ChatRole.System;
                // Add to messages list (Effective for current turn)
                // 注意：修改上下文中的列表会影响传递给 Next 的 Request。
                // 假设 Next 使用 context.Messages。
                context.Messages.Add(new ChatMessage(role, injection.Content));
            }
        }

        // 2. 执行 Next
        var response = await next(context);
        
        // 为 InvokedAsync 更新带有响应的上下文
        providerContext.Response = response;

        // 3. InvokedAsync 钩子 (保存状态/记忆)
        foreach (var provider in _providers)
        {
            await provider.InvokedAsync(providerContext);
        }

        return response;
    }
}
