using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Extensions;

/// <summary>
/// 结构化输出请求构建器 - 流式 API 设计
/// </summary>
public class StructuredOutputBuilder
{
    private readonly IChatClient _client;
    private readonly List<ChatMessage> _messages = new();
    private ChatOptions? _options;
    private string? _provider;  // null = 自动检测

    internal StructuredOutputBuilder(IChatClient client)
    {
        _client = client;
        // 尝试从 Client Metadata 自动检测 Provider
        _provider = DetectProvider(client);
    }
    
    /// <summary>
    /// 从 IChatClient 元数据自动检测 Provider
    /// </summary>
    private static string? DetectProvider(IChatClient client)
    {
        var modelId = client.GetService<ChatClientMetadata>()?.DefaultModelId ?? "";
        var providerUri = client.GetService<ChatClientMetadata>()?.ProviderUri?.Host ?? "";
        
        // 检测 OpenAI 兼容
        if (providerUri.Contains("openai") || providerUri.Contains("azure") ||
            modelId.StartsWith("gpt-") || modelId.StartsWith("o1") || modelId.StartsWith("o3"))
        {
            return "OpenAI";
        }
        
        // 检测 DeepSeek
        if (providerUri.Contains("deepseek") || modelId.Contains("deepseek"))
        {
            return "DeepSeek";
        }
        
        // 检测 Qwen
        if (providerUri.Contains("dashscope") || modelId.Contains("qwen"))
        {
            return "Qwen";
        }
        
        return "Generic";  // 默认回退到 prompt 注入模式
    }

    /// <summary>
    /// 设置 System 角色消息 (更高权重的指令约束)
    /// </summary>
    public StructuredOutputBuilder WithSystem(string instruction)
    {
        // System 消息始终在最前面
        _messages.Insert(0, new ChatMessage(ChatRole.System, instruction));
        return this;
    }

    /// <summary>
    /// 添加上下文信息 (用于 RAG 等场景)
    /// </summary>
    public StructuredOutputBuilder WithContext(string context)
    {
        _messages.Add(new ChatMessage(ChatRole.System, $"参考信息:\n{context}"));
        return this;
    }

    /// <summary>
    /// 添加历史消息 (用于多轮对话)
    /// </summary>
    public StructuredOutputBuilder WithHistory(IEnumerable<ChatMessage> history)
    {
        _messages.AddRange(history);
        return this;
    }

    /// <summary>
    /// 设置模型提供商 (用于 Schema 策略适配)
    /// </summary>
    public StructuredOutputBuilder WithProvider(string provider)
    {
        _provider = provider;
        return this;
    }

    /// <summary>
    /// 设置自定义 ChatOptions
    /// </summary>
    public StructuredOutputBuilder WithOptions(ChatOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// 执行结构化输出请求
    /// </summary>
    /// <typeparam name="T">目标结构类型</typeparam>
    /// <param name="userPrompt">用户提示</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>解析后的结构化结果</returns>
    public async Task<T?> RunStructuredAsync<T>(string userPrompt, IServiceProvider serviceProvider)
    {
        var structuredService = serviceProvider.GetRequiredService<IStructuredOutputService>();
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<StructuredOutputBuilder>();
        var provider = _provider ?? "Generic";
        var options = _options ?? structuredService.CreateOptions<T>(provider);
        
        // JSON Schema 注入 (仅非 OpenAI/Azure 模型需要)
        // OpenAI/Azure 使用原生 ForJsonSchema，无需 prompt 注入
        var finalPrompt = userPrompt;
        var providerLower = provider.ToLower();
        if (options.ResponseFormat == ChatResponseFormat.Json && 
            !providerLower.Contains("openai") && 
            !providerLower.Contains("azure"))
        {
            var schema = structuredService.GenerateJsonSchema(typeof(T));
            finalPrompt += $"\n\n请严格按照以下 JSON 格式输出:\n```json\n{schema}\n```\n不要输出任何其他内容。";
            
            // 提醒：当前模型不支持原生 JSON Schema，使用 Prompt 注入
            logger?.LogDebug("📋 [{Provider}] 不支持原生 JSON Schema，使用 Prompt 注入方式 (Type: {Type})", provider, typeof(T).Name);
        }
        else if (providerLower.Contains("openai") || providerLower.Contains("azure"))
        {
            // OpenAI/Azure 使用原生 Schema
            logger?.LogDebug("✅ [{Provider}] 使用原生 JSON Schema 约束 (Type: {Type})", provider, typeof(T).Name);
        }
        
        // 添加用户消息
        _messages.Add(new ChatMessage(ChatRole.User, finalPrompt));
        
        // 执行请求
        var response = await _client.GetResponseAsync(_messages, options);
        
        // 解析结果
        var text = response.Messages?.LastOrDefault()?.Text;
        return text != null ? structuredService.Parse<T>(text) : default;
    }
}

/// <summary>
/// IChatClient 结构化输出扩展方法
/// </summary>
public static class ChatClientStructuredExtensions
{
    /// <summary>
    /// 开始构建结构化输出请求 (Builder 模式入口)
    /// </summary>
    /// <example>
    /// var result = await client
    ///     .Structured()
    ///     .WithSystem("你是专家...")
    ///     .RunStructuredAsync&lt;MyResult&gt;("请分析...", sp);
    /// </example>
    public static StructuredOutputBuilder Structured(this IChatClient client)
        => new StructuredOutputBuilder(client);
}
