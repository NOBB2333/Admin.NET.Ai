using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text;

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
    // 这里采用“聚合流式结果 -> 经过同一中间件链 -> 再回放”为统一语义方案。
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        return GetStreamingResponseWithMiddlewareAsync(chatMessages, options, cancellationToken);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseWithMiddlewareAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messageList = chatMessages.ToList();
        var context = new RunMiddlewareContext
        {
            Messages = messageList,
            Options = options,
            ServiceProvider = _serviceProvider
        };

        NextRunMiddleware next = async ctx =>
            await CollectStreamingAsResponseAsync(ctx.Messages, ctx.Options, cancellationToken);

        var response = await _middleware.InvokeAsync(context, next);

        foreach (var update in ConvertToUpdates(response))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }

    private async Task<ChatResponse> CollectStreamingAsResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        var role = ChatRole.Assistant;
        var modelId = options?.ModelId;
        var createdAt = DateTimeOffset.UtcNow;
        UsageDetails? usage = null;
        var sb = new StringBuilder();

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (update.Role.HasValue)
            {
                role = update.Role.Value;
            }

            if (!string.IsNullOrEmpty(update.ModelId))
            {
                modelId = update.ModelId;
            }

            if (update.CreatedAt.HasValue)
            {
                createdAt = update.CreatedAt.Value;
            }

            if (!string.IsNullOrEmpty(update.Text))
            {
                sb.Append(update.Text);
            }

            var usageContent = update.Contents?.OfType<UsageContent>().FirstOrDefault();
            if (usageContent?.Details != null)
            {
                usage = usageContent.Details;
            }
        }

        var response = new ChatResponse(new[] { new ChatMessage(role, sb.ToString()) })
        {
            ModelId = modelId,
            CreatedAt = createdAt
        };

        if (usage != null)
        {
            response.Usage = usage;
        }

        return response;
    }

    private static IEnumerable<ChatResponseUpdate> ConvertToUpdates(ChatResponse response)
    {
        foreach (var message in response.Messages)
        {
            yield return new ChatResponseUpdate(message.Role, message.Text)
            {
                ModelId = response.ModelId,
                CreatedAt = response.CreatedAt
            };
        }

        if (response.Usage != null)
        {
            yield return new ChatResponseUpdate
            {
                ModelId = response.ModelId,
                CreatedAt = response.CreatedAt,
                Contents = new List<AIContent> { new UsageContent(response.Usage) }
            };
        }
    }
}
