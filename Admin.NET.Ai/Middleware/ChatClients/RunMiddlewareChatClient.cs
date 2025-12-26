using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Middleware.ChatClients;

/// <summary>
/// 运行中间件 ChatClient 包装器 via DelegatingChatClient
/// </summary>
public class RunMiddlewareChatClient : DelegatingChatClient
{
    private readonly IRunMiddleware _middleware;
    private readonly IServiceProvider _serviceProvider;

    public RunMiddlewareChatClient(IChatClient innerClient, IRunMiddleware middleware, IServiceProvider serviceProvider) 
        : base(innerClient)
    {
        _middleware = middleware;
        _serviceProvider = serviceProvider;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        // Convert IEnumerable to IList if necessary for Context, but standard interfaces use IEnumerable usually.
        // RunMiddlewareContext expects IList (my definition).
        var messageList = chatMessages.ToList();

        var context = new RunMiddlewareContext
        {
            Messages = messageList,
            Options = options,
            ServiceProvider = _serviceProvider
        };

        // 定义 Next 委托
        NextRunMiddleware next = async (ctx) =>
        {
            // Note: ctx.Messages might be modified, pass it.
            return await base.GetResponseAsync(ctx.Messages, ctx.Options, cancellationToken);
        };

        return await _middleware.InvokeAsync(context, next);
    }

    // 注意：IRunMiddleware 目前设计为返回 ChatResponse，因此不支持流式拦截。
    // 如果调用流式接口，目前绕过中间件，或者需要在中间件层支持流式。
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 简单透传，暂不支持流式中间件拦截
        return base.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
    }
}
