using Admin.NET.Ai.Abstractions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.NET.Ai.Configuration;

/// <summary>
/// DevUI 配置 - MAF 可视化调试界面
/// 仅在开发环境启用
/// 
/// 🔧 使用说明:
/// 1. 服务注册: services.AddMafDevUI()
/// 2. Agent 注册: builder.AddDemoAgents() (可选)
/// 3. 端点映射: app.MapMafDevUI(isDevelopment)
/// 
/// 📌 默认访问地址:
/// - DevUI 界面: http://localhost:5000/devui 或 https://localhost:5001/devui
/// - Responses API: http://localhost:5000/v1/responses
/// - Conversations API: http://localhost:5000/v1/conversations
/// 
/// 💡 端口说明:
/// - 端口取决于 launchSettings.json 或 app.Run() 指定的地址
/// - ASP.NET Core 默认: HTTP 5000, HTTPS 5001
/// - 如果使用 app.Run("https://localhost:50516"), 则访问 https://localhost:50516/devui
/// </summary>
public static class DevUIConfiguration
{
    /// <summary>
    /// 添加 DevUI 相关服务
    /// </summary>
    public static IServiceCollection AddMafDevUI(this IServiceCollection services)
    {
        // 添加 OpenAI 兼容的 Responses 和 Conversations API
        services.AddOpenAIResponses();
        services.AddOpenAIConversations();
        
        return services;
    }

    /// <summary>
    /// 注册 AI Agents（独立于 ServiceCollectionInit）
    /// </summary>
    public static WebApplicationBuilder AddDemoAgents(this WebApplicationBuilder builder)
    {
        // 演示用 Agents - 可根据需要添加更多
        builder.AddAIAgent("assistant", 
            "你是一个有帮助的助手。请简洁准确地回答问题。");
        
        builder.AddAIAgent("coder", 
            "你是一个专业程序员。帮助用户解决编程问题，提供代码示例。");
        
        builder.AddAIAgent("writer", 
            "你是一个专业作家。帮助用户撰写和优化文字内容。");

        return builder;
    }

    /// <summary>
    /// 映射 DevUI 端点
    /// 
    /// 端点列表:
    /// - /devui - 可视化调试界面
    /// - /v1/responses - OpenAI Responses API
    /// - /v1/conversations - Conversations API
    /// </summary>
    /// <param name="app">WebApplication</param>
    /// <param name="isDevelopment">是否为开发环境</param>
    public static WebApplication MapMafDevUI(this WebApplication app, bool isDevelopment = true)
    {
        // OpenAI 兼容 API（总是启用，供其他客户端使用）
        app.MapOpenAIResponses();
        app.MapOpenAIConversations();
        
        // DevUI 界面（仅开发环境）
        if (isDevelopment)
        {
            app.MapDevUI();
            
            // 打印访问地址
            var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";
            Console.WriteLine();
            Console.WriteLine("╭──────────────────────────────────────────╮");
            Console.WriteLine("│         MAF DevUI 已启用                 │");
            Console.WriteLine("├──────────────────────────────────────────┤");
            Console.WriteLine($"│  🖥️  界面: {url}/devui");
            Console.WriteLine($"│  📡 API:  {url}/v1/responses");
            Console.WriteLine("╰──────────────────────────────────────────╯");
            Console.WriteLine();
        }
        
        return app;
    }
}
