using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Middleware.ChatClients;

public class ToolMiddlewareChatClient : DelegatingChatClient
{
    private readonly IEnumerable<IToolCallingMiddleware> _middlewares;
    private readonly IServiceProvider _serviceProvider;

    public ToolMiddlewareChatClient(IChatClient innerClient, IEnumerable<IToolCallingMiddleware> middlewares, IServiceProvider serviceProvider) 
        : base(innerClient)
    {
        _middlewares = middlewares;
        _serviceProvider = serviceProvider;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var wrappedOptions = WrapOptions(options);
        return await base.GetResponseAsync(chatMessages, wrappedOptions, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var wrappedOptions = WrapOptions(options);
        return base.GetStreamingResponseAsync(chatMessages, wrappedOptions, cancellationToken);
    }

    private ChatOptions? WrapOptions(ChatOptions? options)
    {
        if (options == null || options.Tools == null || options.Tools.Count == 0)
            return options;

        var newOptions = options.Clone();
        var wrappedTools = new List<AITool>();
        foreach (var tool in options.Tools)
        {
            if (tool is AIFunction function)
            {
                 wrappedTools.Add(WrapTool(function));
            }
            else
            {
                 wrappedTools.Add(tool);
            }
        }
        newOptions.Tools = wrappedTools;
        return newOptions;
    }

    private AIFunction WrapTool(AIFunction inner)
    {
        // Use Factory with minimal args. Note: Schema might be lost or generic.
        return AIFunctionFactory.Create(async (IEnumerable<KeyValuePair<string, object?>> args, CancellationToken ct) =>
        {
            var argsDict = args.ToDictionary(k => k.Key, v => v.Value);
            var arguments = new AIFunctionArguments(argsDict);

            NextToolCallingMiddleware pipeline = async (ctx) =>
            {
                var result = await inner.InvokeAsync(arguments, ct);
                return new ToolResponse { Result = result };
            };

            foreach (var middleware in _middlewares.Reverse())
            {
                var next = pipeline;
                pipeline = async (ctx) => await middleware.InvokeAsync(ctx, next);
            }

            var context = new ToolCallingContext
            {
                ToolCall = new FunctionCallContent(Guid.NewGuid().ToString(), inner.Name, argsDict), 
                ServiceProvider = _serviceProvider
            };

            var toolResponse = await pipeline(context);
            return toolResponse.Result;

        }, inner.Name, inner.Description);
    }
}
