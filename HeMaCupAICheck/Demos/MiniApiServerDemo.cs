using Admin.NET.Ai.Services.MCP;
using Admin.NET.Ai.Services.MCP.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using Admin.NET.Ai;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 场景24: MCP Server (MiniAPI)
/// 
/// 📌 演示如何在现有 Web API 项目中使用 MCP
/// - 启动一个内嵌的 ASP.NET Core Web 服务器 (MiniAPI)
/// - 自动把 [McpTool] 标记的方法暴露为 MCP 工具
/// - 通过 HTTP/SSE 端点对外提供服务
/// </summary>
public static class MiniApiServerDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n========== MCP Server (MiniAPI) 演示 ==========\n");
        Console.WriteLine("🌐 正在启动内嵌 Web 服务器...");

        const int Port = 5050;
        var url = $"http://localhost:{Port}";

        try
        {
            var builder = WebApplication.CreateBuilder();

            // 1. 注册日志
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            // 2. 注册 Admin.NET.Ai 核心服务
            // 注意: 这里使用独立的容器，不复用 Console App 的 SP，模拟真实 Web App 环境
            builder.Services.AddAdminNetAi(builder.Configuration);
            
            // 注册业务服务 (关键：将其注册到 DI 中，以便 API 和 MCP 都能使用)
            builder.Services.AddTransient<ExternalHttpTools>();

            // 3. 配置 Kestrel 端口
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, Port);
            });

            var app = builder.Build();

            // 4. 注册外部工具 (从程序集发现，会自动使用 DI 中的服务实例)
            var discoveryService = app.Services.GetRequiredService<McpToolDiscoveryService>();
            discoveryService.DiscoverFromAssembly(typeof(ExternalHttpTools).Assembly);

            // 5.1 映射 MCP 端点
            app.MapMcpEndpoints();

            // 5.2 映射标准 Web API 端点 (混合模式)
            // 这样同一个业务逻辑既可以作为 API 给前端用，也可以作为 MCP 工具给 AI 用
            app.MapGet("/api/weather", (string city, ExternalHttpTools tools) => tools.GetCityWeather(city));
            app.MapGet("/api/sum", (int a, int b, ExternalHttpTools tools) => tools.CalculateSum(a, b));

            // 6. 启动服务器
            var serverTask = app.RunAsync();

            Console.WriteLine($"✅ 服务器已启动: {url}");
            Console.WriteLine($"---------- MCP 模式 ----------");
            Console.WriteLine($"📝 工具列表: {url}/mcp/tools");
            Console.WriteLine($"🔌 SSE 连接: {url}/mcp/sse");
            Console.WriteLine($"---------- API 模式 ----------");
            Console.WriteLine($"🌦️  天气接口: {url}/api/weather?city=Beijing");
            Console.WriteLine($"➕  求和接口: {url}/api/sum?a=10&b=20");
            Console.WriteLine("\n💡 说明:");
            Console.WriteLine("现在演示了 [混合模式]：");
            Console.WriteLine("1. 'ExternalHttpTools' 被注册到 DI 容器中。");
            Console.WriteLine("2. MCP 服务自动发现并调用它 (AI Agent 使用)。");
            Console.WriteLine("3. 标准 Web API 也可以通过依赖注入调用它 (前端/APP 使用)。");
            
            Console.WriteLine("\n按任意键停止服务器并退出演示...");
            Console.ReadKey();

            await app.StopAsync();
            await serverTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 启动失败: {ex.Message}");
            Console.WriteLine("可能是端口 5050 被占用，请尝试关闭占用端口的程序。");
        }

        Console.WriteLine("\n========== 演示结束 ==========");
    }

    /// <summary>
    /// 模拟外部 HTTP 服务包装器
    /// 这里的特性 [McpTool] 会告诉框架将其注册为工具
    /// 同时它也是一个普通的 Service，可以被 API 调用
    /// </summary>
    public class ExternalHttpTools
    {
        private readonly HttpClient _httpClient = new();

        [McpTool("查询指定城市的实时天气 (模拟外部API)")]
        public async Task<string> GetCityWeather(
            [McpParameter("城市名称，如 'Beijing'")] string city)
        {
            // 模拟调用第三方接口
            Console.WriteLine($"[Server] 收到天气查询请求: {city}");
            await Task.Delay(500); // 模拟网络延迟
            
            var temp = new Random().Next(15, 30);
            return $"{city} 天气晴朗，气温 {temp}°C (来自 MiniAPI MCP Server)";
        }

        [McpTool("计算两个数字的和 (本地逻辑)")]
        public int CalculateSum(
            [McpParameter("第一个数字")] int a, 
            [McpParameter("第二个数字")] int b)
        {
            Console.WriteLine($"[Server] 收到计算请求: {a} + {b}");
            return a + b;
        }
    }
}
