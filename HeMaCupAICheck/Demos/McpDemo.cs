using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.MCP;
using Admin.NET.Ai.Services.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// MCP (Model Context Protocol) 演示 - 连接外部工具服务
/// </summary>
public static class McpDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("McpDemo");

        Console.WriteLine("\n========== MCP 协议演示 ==========\n");

        // ===== 1. MCP 概念介绍 =====
        Console.WriteLine("--- 1. MCP (Model Context Protocol) 概念 ---");
        Console.WriteLine(@"
MCP 是一个标准化协议，用于 LLM 与外部工具/服务的通信。

核心优势:
- 标准化接口: 不同工具遵循统一协议
- 动态工具发现: 工具可在运行时被 Agent 发现
- 安全隔离: 工具在独立进程中运行
- SSE 实时通信: 支持服务端推送事件

Admin.NET.Ai 支持:
- MCP Client: 连接外部 MCP Server
- MCP Server: 将本地方法暴露为 MCP 工具
");

        // ===== 2. MCP Client 使用 =====
        Console.WriteLine("\n--- 2. MCP Client 使用示例 ---");
        Console.WriteLine(@"
// 配置 MCP Server 连接 (appsettings.json 或 LLMAgent.json)
{
  ""LLMMcp"": {
    ""Servers"": [
      {
        ""Name"": ""GitHub-MCP"",
        ""Enabled"": true,
        ""Url"": ""http://localhost:3000/sse""
      },
      {
        ""Name"": ""Database-MCP"",
        ""Enabled"": true,
        ""Url"": ""http://localhost:3001/sse""
      }
    ]
  }
}

// 使用 McpClientService 连接
var mcpClient = sp.GetRequiredService<McpClientService>();
var tools = await mcpClient.ListToolsAsync(""GitHub-MCP"");
foreach (var tool in tools)
{
    Console.WriteLine($""工具: {tool.Name} - {tool.Description}"");
}

// 调用 MCP 工具
var result = await mcpClient.InvokeToolAsync(""GitHub-MCP"", ""search_repos"", new 
{
    query = ""semantic-kernel"",
    language = ""csharp""
});
");

        // ===== 3. MCP Server 暴露本地工具 =====
        Console.WriteLine("\n--- 3. MCP Server 暴露本地工具 ---");
        Console.WriteLine(@"
// 使用 [McpTool] 特性标记方法
public class WeatherService
{
    [McpTool(""GetCurrentWeather"", ""获取指定城市的当前天气"")]
    public async Task<WeatherInfo> GetWeatherAsync(string city)
    {
        // 调用天气 API
        return await _weatherApi.GetAsync(city);
    }
}

// 启动 MCP Server
var mcpServer = sp.GetRequiredService<McpServerService>();
await mcpServer.StartAsync(port: 3000);

// 现在其他 Agent 可以通过 MCP 协议调用这个工具
");

        // ===== 4. MCP 连接池 =====
        Console.WriteLine("\n--- 4. MCP 连接池管理 ---");
        Console.WriteLine(@"
// 获取连接池服务
var connectionPool = sp.GetRequiredService<IMcpConnectionPool>();

// 检查连接状态
var status = connectionPool.GetStatus(""GitHub-MCP"");
Console.WriteLine($""连接状态: {status.IsConnected}"");
Console.WriteLine($""最后心跳: {status.LastHeartbeat}"");

// 连接池自动管理:
// - 连接复用
// - 自动重连
// - 心跳检测
// - SSE 事件处理
");

        // ===== 5. MCP 健康检查 =====
        Console.WriteLine("\n--- 5. MCP 健康检查 ---");
        Console.WriteLine(@"
// 注册 MCP 健康检查 (ASP.NET Core)
services.AddHealthChecks()
    .AddCheck<McpHealthCheck>(""mcp-servers"");

// 访问 /health 端点查看 MCP 服务状态
// {
//   ""status"": ""Healthy"",
//   ""entries"": {
//     ""mcp-servers"": {
//       ""status"": ""Healthy"",
//       ""data"": {
//         ""GitHub-MCP"": ""Connected"",
//         ""Database-MCP"": ""Connected""
//       }
//     }
//   }
// }
");

        // ===== 6. MCP 工具工厂 =====
        Console.WriteLine("\n--- 6. MCP 工具工厂 (自动加载) ---");
        Console.WriteLine(@"
// 使用 McpToolFactory 自动加载配置的 MCP 工具
var toolFactory = sp.GetRequiredService<McpToolFactory>();
var globalTools = await toolFactory.LoadGlobalMcpToolsAsync();

// 这些工具可以直接用于 Agent
var agent = aiFactory.CreateAgent(new AgentOptions
{
    Tools = globalTools // 自动包含所有 MCP 工具
});
");

        Console.WriteLine("\n========== MCP 协议演示结束 ==========");
    }
}
