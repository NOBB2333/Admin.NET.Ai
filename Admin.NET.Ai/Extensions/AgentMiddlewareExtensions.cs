using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Middleware;
using Admin.NET.Ai.Middleware.ChatClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Extensions;

public class ChatClientAgentOptions
{
    public string? Name { get; set; }
    public string? Instructions { get; set; }
}

/// <summary>
/// 也充当 Agent (IChatClient) 的 Agent 构建器
/// </summary>
public class AgentBuilder : IChatClient
{
    private IChatClient _client;
    private readonly IServiceProvider _serviceProvider;

    public AgentBuilder(IChatClient client, IServiceProvider serviceProvider)
    {
        _client = client;
        _serviceProvider = serviceProvider;
    }

    public AgentBuilder UseMiddleware<TMiddleware>(params object[] args) where TMiddleware : IRunMiddleware
    {
        var middleware = ActivatorUtilities.CreateInstance<TMiddleware>(_serviceProvider, args);
        _client = new RunMiddlewareChatClient(_client, middleware, _serviceProvider);
        return this;
    }

    public AgentBuilder UseToolCallingMiddleware<TMiddleware>(params object[] args) where TMiddleware : IToolCallingMiddleware
    {
        var middleware = ActivatorUtilities.CreateInstance<TMiddleware>(_serviceProvider, args);
        _client = new ToolMiddlewareChatClient(_client, new IToolCallingMiddleware[] { middleware }, _serviceProvider);
        return this;
    }

    // 代码执行能力
    // public AgentBuilder UseCodeInterpreter()
    // {
    //     // TODO: 集成 Microsoft.Agents.AI.Tools.HostedCodeInterpreterTool
    //     // 暂时返回 this 不做任何修改
    //     return this;
    // }
    /// <summary>
    /// 启用网络搜索能力
    /// </summary>
    public AgentBuilder UseSearch()
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<Admin.NET.Ai.Middleware.Capabilities.SearchMiddleware>)) 
            as ILogger<Admin.NET.Ai.Middleware.Capabilities.SearchMiddleware>;
        var httpFactory = _serviceProvider.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
        
        if (logger != null)
        {
            _client = new Admin.NET.Ai.Middleware.Capabilities.SearchMiddleware(_client, logger, httpFactory);
        }
        return this;
    }

    /// <summary>
    /// 启用多模态视觉能力
    /// </summary>
    public AgentBuilder UseVision(bool enableImageGeneration = false)
    {
        var logger = _serviceProvider.GetService(typeof(ILogger<Admin.NET.Ai.Middleware.Capabilities.VisionMiddleware>)) 
            as ILogger<Admin.NET.Ai.Middleware.Capabilities.VisionMiddleware>;
        
        if (logger != null)
        {
            _client = new Admin.NET.Ai.Middleware.Capabilities.VisionMiddleware(_client, logger, enableImageGeneration);
        }
        return this;
    }

    public IChatClient Build() => _client;

    // IChatClient 实现 (委托)
    
    public object? GetService(Type serviceType, object? key = null) 
        => _client.GetService(serviceType, key);

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => _client.GetResponseAsync(chatMessages, options, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => _client.GetStreamingResponseAsync(chatMessages, options, cancellationToken);

    public void Dispose()
    {
        _client.Dispose();
    }
}

public static class AgentMiddlewareExtensions
{
    public static AgentBuilder CreateAIAgent(this IChatClient client, IServiceProvider sp, ChatClientAgentOptions? options = null)
    {
        var builder = new AgentBuilder(client, sp);
        
        if (options != null)
        {
             // 如果提供了指令，自动使用 InstructionMiddleware
             if (!string.IsNullOrWhiteSpace(options.Instructions))
             {
                 builder.UseMiddleware<InstructionMiddleware>(options.Instructions);
             }
        }
        
        return builder;
    }

    public static AgentBuilder UseSearch(this AgentBuilder builder) => builder.UseSearch();
    public static AgentBuilder UseVision(this AgentBuilder builder, bool enableImageGeneration = false) => builder.UseVision(enableImageGeneration);
}
