using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.NET.Ai.Extensions;

/// <summary>
/// Agent 扩展方法
/// </summary>
public static class AgentExtensions
{
    /// <summary>
    /// 运行 Agent 并返回结构化数据
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="client">ChatClient 实例</param>
    /// <param name="prompt">提示词</param>
    /// <param name="serviceProvider">服务提供者 (用于解析 IStructuredOutputService)</param>
    /// <param name="provider">模型提供商 (用于适配策略)</param>
    /// <returns>结构化结果</returns>
    public static async Task<T?> RunAsync<T>(this IChatClient client, string prompt, IServiceProvider serviceProvider, string provider = "Generic")
    {
        var structuredService = serviceProvider.GetRequiredService<IStructuredOutputService>();
        
        // 1. 创建适配该类型的 Options (Schema 或 Prompt Injection)
        var options = structuredService.CreateOptions<T>(provider);
        
        // 2. 发送请求
        // 注意：如果是 DeepSeek/Qwen 等不支持原生 Schema 的模型，CreateOptions 可能会依赖调用方手动拼接 Schema 到 Prompt
        // 这里我们做个简单的增强：如果 Options 是 JSON Mode 且没有原生 Schema 支持，我们手动把 Schema 拼接到 User Prompt 后面
        // (注：StructuredOutputService 里的 CreateOptions 逻辑目前比较简单，我们在 Extensions 里做这个补充)
        
        // 检查是否需要 Prompt 注入 (简单判断: JSON Mode 且不是 OpenAI)
        if (options.ResponseFormat == ChatResponseFormat.Json && !provider.ToLower().Contains("openai") && !provider.ToLower().Contains("azure"))
        {
            var schema = structuredService.GenerateJsonSchema(typeof(T));
            prompt += $"\n\n请严格按照以下 JSON 格式输出:\n```json\n{schema}\n```\n不要输出任何其他内容。";
        }

        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
        var response = await client.GetResponseAsync(messages, options);

        // 3. 解析结果
        // 3. 解析结果
        var lastMessage = response.Messages?.LastOrDefault();
        if (lastMessage?.Text is null) return default;
        return structuredService.Parse<T>(lastMessage.Text);
    }
    /// <summary>
    /// 运行 Agent 并返回文本结果 (简化版)
    /// </summary>
    public static async Task<ChatResponse?> RunAsync(this IChatClient client, string prompt, IServiceProvider serviceProvider, string provider = "Generic")
    {
         var messages = new List<Microsoft.Extensions.AI.ChatMessage> { new(Microsoft.Extensions.AI.ChatRole.User, prompt) };
         var options = new ChatOptions(); 
         return await client.GetResponseAsync(messages, options);
    }
}
