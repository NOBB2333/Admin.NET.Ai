using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Services.MCP;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// MCP (Model Context Protocol) 演示
/// 
/// 📌 更新: 2026-01-06
/// - 使用 McpToolFactory (官方 ModelContextProtocol SDK)
/// - 添加内嵌搜索工具演示 (零依赖，可直接运行)
/// - 支持 Stdio 和 HTTP 传输
/// </summary>
public static class McpDemo
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static async Task RunAsync(IServiceProvider sp)
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("McpDemo");
        var aiFactory = sp.GetRequiredService<IAiFactory>();

        Console.WriteLine("\n========== MCP 协议演示 ==========\n");

        // ===== 1. MCP 概念介绍 =====
        Console.WriteLine("--- 1. MCP (Model Context Protocol) 概念 ---");
        Console.WriteLine(@"
MCP 是一个标准化协议，用于 LLM 与外部工具/服务的通信。

核心优势:
- 标准化接口: 不同工具遵循统一协议
- 动态工具发现: 工具可在运行时被 Agent 发现
- 安全隔离: 工具在独立进程中运行
");

        // ===== 2. 内嵌搜索工具演示 (零依赖) =====
        Console.WriteLine("--- 2. 内嵌搜索工具演示 (零依赖) ---");
        Console.WriteLine("💡 以下工具使用 C# 内嵌实现，无需安装任何外部依赖\n");

        // 定义工具
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(BingSearchAsync, "bing_search", "搜索必应获取网页结果"),
            AIFunctionFactory.Create(FetchWebpageAsync, "fetch_webpage", "获取网页内容摘要"),
            AIFunctionFactory.Create(GetTimeAsync, "get_time", "获取当前时间")
        };

        foreach (var tool in tools)
        {
            Console.WriteLine($"  🔧 {tool.Name}: {tool.Description}");
        }

        // ===== 3. 实时工具调用 =====
        Console.WriteLine("\n--- 3. 实时工具调用 ---");

        Console.WriteLine("\n🔍 搜索: 'Admin.NET AI框架'...");
        var searchResult = await BingSearchAsync("Admin.NET AI框架", 3);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(searchResult);
        Console.ResetColor();

        Console.WriteLine("\n🕐 获取当前时间...");
        var timeResult = await GetTimeAsync();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"   {timeResult}");
        Console.ResetColor();

        // ===== 4. Agent + 工具集成 =====
        Console.WriteLine("\n--- 4. Agent + 搜索工具 ---");

        var queries = new[]
        {
            "搜索一下 C# 异步编程最佳实践",
            "现在几点了？"
        };

        try
        {
            var chatClient = aiFactory.GetDefaultChatClient()!
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            var options = new ChatOptions
            {
                Tools = tools,
                ToolMode = ChatToolMode.Auto
            };

            var systemPrompt = """
                你是一个智能助手，可以使用以下工具：
                - bing_search: 搜索网页获取信息
                - fetch_webpage: 获取网页内容
                - get_time: 获取当前时间
                
                当用户需要搜索信息或查询时间时，请使用相应工具。
                根据工具返回的结果回答用户。
                """;

            foreach (var query in queries)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n🙋 用户: {query}");
                Console.ResetColor();

                var messages = new List<ChatMessage>
                {
                    new(ChatRole.System, systemPrompt),
                    new(ChatRole.User, query)
                };

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("🤖 助手: ");
                await chatClient.GetStreamingResponseAsync(messages, options).WriteToConsoleAsync();
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n⚠️ Agent 演示需要配置 LLM: {ex.Message}");
            Console.WriteLine("但工具数据已成功获取，可以看到上面的实时数据！");
        }

        // ===== 5. 外部 MCP Server 配置 =====
        Console.WriteLine("\n--- 5. 外部 MCP Server 配置 ---");
        Console.WriteLine(@"
// 如需连接外部 MCP Server (如 Bing 搜索 MCP)
// 配置 LLMAgent.Mcp.json:

{
  ""LLM-Mcp"": {
    ""Servers"": [
      {
        ""Name"": ""BingCN"",
        ""Enabled"": true,
        ""TransportType"": ""stdio"",
        ""Command"": ""npx"",
        ""Arguments"": [""bing-cn-mcp""]
      }
    ]
  }
}

// 然后使用 McpToolFactory 加载:
var factory = sp.GetRequiredService<McpToolFactory>();
var mcpTools = await factory.LoadAllToolsAsync();
");

        // ===== 6. 代码示例 =====
        Console.WriteLine("--- 6. 代码集成示例 ---");
        Console.WriteLine(@"
// 方式1: 使用内嵌工具 (零依赖)
var tools = new List<AITool>
{
    AIFunctionFactory.Create(BingSearchAsync, ""bing_search"", ""搜索必应""),
    AIFunctionFactory.Create(FetchWebpageAsync, ""fetch_webpage"", ""获取网页"")
};

// 方式2: 使用 MCP Server (需配置)
var factory = sp.GetRequiredService<McpToolFactory>();
var mcpTools = await factory.LoadAllToolsAsync();

// 配合 FunctionInvocation 使用
var chatClient = aiFactory.GetDefaultChatClient()!
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var response = await chatClient.GetStreamingResponseAsync(
    ""搜索一下 .NET 10 新特性"",
    new ChatOptions { Tools = tools }
).WriteToConsoleAsync();
");

        Console.WriteLine("\n========== MCP 协议演示结束 ==========");
    }

    #region 内嵌工具实现 (模拟 MCP 工具)

    /// <summary>
    /// 必应搜索 (直接爬取，无需 API Key)
    /// </summary>
    private static async Task<string> BingSearchAsync(string query, int numResults = 5)
    {
        try
        {
            // 使用必应中国搜索
            var url = $"https://cn.bing.com/search?q={Uri.EscapeDataString(query)}&count={numResults}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
            
            var response = await _httpClient.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();
            
            // 简单解析搜索结果
            var results = ParseBingResults(html, numResults);
            
            if (results.Count == 0)
            {
                return "未找到搜索结果";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🔍 搜索 '{query}' 的结果:");
            for (int i = 0; i < results.Count; i++)
            {
                sb.AppendLine($"  [{i + 1}] {results[i].Title}");
                sb.AppendLine($"      {results[i].Snippet}");
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"搜索失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 获取网页内容摘要
    /// </summary>
    private static async Task<string> FetchWebpageAsync(string url)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            var response = await _httpClient.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();
            
            // 提取正文
            var text = ExtractTextFromHtml(html);
            
            // 返回前 500 字
            if (text.Length > 500)
            {
                text = text[..500] + "...";
            }
            
            return $"📄 网页内容摘要:\n{text}";
        }
        catch (Exception ex)
        {
            return $"获取网页失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 获取当前时间
    /// </summary>
    private static Task<string> GetTimeAsync()
    {
        var now = DateTime.Now;
        var dayOfWeek = now.DayOfWeek switch
        {
            DayOfWeek.Monday => "星期一",
            DayOfWeek.Tuesday => "星期二",
            DayOfWeek.Wednesday => "星期三",
            DayOfWeek.Thursday => "星期四",
            DayOfWeek.Friday => "星期五",
            DayOfWeek.Saturday => "星期六",
            DayOfWeek.Sunday => "星期日",
            _ => ""
        };
        return Task.FromResult($"🕐 当前时间: {now:yyyy年M月d日 HH:mm:ss} {dayOfWeek}");
    }

    #endregion

    #region HTML 解析辅助方法

    private record SearchResult(string Title, string Snippet, string Url);

    private static List<SearchResult> ParseBingResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();
        
        try
        {
            // 简单正则匹配搜索结果
            var titlePattern = new Regex(@"<h2[^>]*><a[^>]*href=""([^""]+)""[^>]*>(.+?)</a></h2>", RegexOptions.Singleline);
            var snippetPattern = new Regex(@"<p[^>]*class=""[^""]*b_algoSlug[^""]*""[^>]*>(.+?)</p>", RegexOptions.Singleline);
            
            var titleMatches = titlePattern.Matches(html);
            var snippetMatches = snippetPattern.Matches(html);
            
            for (int i = 0; i < Math.Min(titleMatches.Count, maxResults); i++)
            {
                var title = StripHtml(titleMatches[i].Groups[2].Value);
                var url = titleMatches[i].Groups[1].Value;
                var snippet = i < snippetMatches.Count ? StripHtml(snippetMatches[i].Groups[1].Value) : "";
                
                if (!string.IsNullOrWhiteSpace(title))
                {
                    results.Add(new SearchResult(title, snippet, url));
                }
            }
        }
        catch
        {
            // 解析失败，返回空列表
        }
        
        return results;
    }

    private static string ExtractTextFromHtml(string html)
    {
        // 移除脚本和样式
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        
        // 移除所有 HTML 标签
        html = StripHtml(html);
        
        // 清理空白
        html = Regex.Replace(html, @"\s+", " ").Trim();
        
        return html;
    }

    private static string StripHtml(string html)
    {
        return Regex.Replace(html, @"<[^>]+>", "")
            .Replace("&nbsp;", " ")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Replace("&quot;", "\"")
            .Trim();
    }

    #endregion
}
