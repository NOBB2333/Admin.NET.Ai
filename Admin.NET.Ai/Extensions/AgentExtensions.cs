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
        return await RunAsync<T>(client, systemInstruction: null, userPrompt: prompt, serviceProvider, provider);
    }
    
    /// <summary>
    /// 运行 Agent 并返回结构化数据 (带 System 角色)
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="client">ChatClient 实例</param>
    /// <param name="systemInstruction">System 角色指令 (更高权重)</param>
    /// <param name="userPrompt">用户提示</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="provider">模型提供商</param>
    /// <returns>结构化结果</returns>
    public static async Task<T?> RunAsync<T>(
        this IChatClient client, 
        string? systemInstruction, 
        string userPrompt, 
        IServiceProvider serviceProvider, 
        string provider = "Generic")
    {
        var structuredService = serviceProvider.GetRequiredService<IStructuredOutputService>();
        
        // 1. 创建适配该类型的 Options (Schema 或 Prompt Injection)
        var options = structuredService.CreateOptions<T>(provider);
        
        // 2. 构建消息列表
        var messages = new List<ChatMessage>();
        
        // 添加 System 消息 (如果有)
        if (!string.IsNullOrEmpty(systemInstruction))
        {
            messages.Add(new(ChatRole.System, systemInstruction));
        }
        
        // 处理 JSON Schema 注入
        var finalUserPrompt = userPrompt;
        if (options.ResponseFormat == ChatResponseFormat.Json && !provider.ToLower().Contains("openai") && !provider.ToLower().Contains("azure"))
        {
            var schema = structuredService.GenerateJsonSchema(typeof(T));
            finalUserPrompt += $"\n\n请严格按照以下 JSON 格式输出:\n```json\n{schema}\n```\n不要输出任何其他内容。";
        }
        
        messages.Add(new(ChatRole.User, finalUserPrompt));
        
        var response = await client.GetResponseAsync(messages, options);

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
