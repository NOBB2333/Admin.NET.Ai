using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// Function Calling 与 Tool Reduction 演示
/// 展示 MEAI 的工具调用和工具精简最佳实践
/// </summary>
public static class FunctionCallingDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n========== Function Calling 演示 ==========\n");
        
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var client = aiFactory.GetChatClient("DeepSeek");
        
        if (client == null)
        {
            Console.WriteLine("❌ 无法获取 ChatClient");
            return;
        }
        
        // ========== 1. 基础 Function Calling ==========
        Console.WriteLine("--- 1. 基础 Function Calling ---\n");
        
        // 定义工具
        var weatherTool = AIFunctionFactory.Create(GetWeather, "get_weather", "获取指定城市的当前天气");
        var timeTool = AIFunctionFactory.Create(() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "get_current_time", "获取当前时间");
        var calcTool = AIFunctionFactory.Create(Calculate, "calculate", "执行数学计算");
        
        var tools = new List<AITool> { weatherTool, timeTool, calcTool };
        Console.WriteLine($"已注册 {tools.Count} 个工具:");
        foreach (var tool in tools)
        {
            Console.WriteLine($"  - {tool.Name}: {tool.Description}");
        }
        
        // 配置 ChatOptions
        var options = new ChatOptions
        {
            Tools = tools,
            ToolMode = ChatToolMode.Auto  // 自动决定是否调用工具
        };
        
        Console.WriteLine("\n用户: 北京今天天气怎么样？");
        Console.WriteLine("AI: (正在思考并调用工具...)");
        
        try
        {
            var response = await client.GetResponseAsync("北京今天天气怎么样？", options);
            Console.WriteLine($"\n回答: {response.Text}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
        }
        
        // ========== 2. 多工具调用 ==========
        Console.WriteLine("\n\n--- 2. 多工具调用 ---\n");
        
        var multiToolOptions = new ChatOptions
        {
            Tools = tools,
            ToolMode = ChatToolMode.Auto
        };
        
        Console.WriteLine("用户: 现在几点了？顺便告诉我上海的天气");
        
        try
        {
            var response = await client.GetResponseAsync(
                "现在几点了？顺便告诉我上海的天气", 
                multiToolOptions);
            Console.WriteLine($"回答: {response.Text}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
        }
        
        // ========== 3. 工具调用模式 ==========
        Console.WriteLine("\n\n--- 3. 工具调用模式 ---\n");
        
        Console.WriteLine("ToolMode 选项:");
        Console.WriteLine("  - Auto: 自动决定是否调用工具（默认）");
        Console.WriteLine("  - RequireAny: 必须调用至少一个工具");
        Console.WriteLine("  - RequireSpecific(name): 必须调用指定工具");
        Console.WriteLine("  - None: 禁用工具调用");
        
        // 强制调用工具
        var forceToolOptions = new ChatOptions
        {
            Tools = tools,
            ToolMode = ChatToolMode.RequireAny
        };
        
        Console.WriteLine("\n使用 RequireAny 模式：");
        Console.WriteLine("用户: 你好");
        
        try
        {
            var response = await client.GetResponseAsync("你好", forceToolOptions);
            Console.WriteLine($"回答: {response.Text}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
        }
        
        // ========== 4. Tool Reduction 概念 ==========
        Console.WriteLine("\n\n--- 4. Tool Reduction（工具精简）概念 ---\n");
        
        Console.WriteLine(@"
当工具数量较多时（> 10 个），Tool Reduction 可以：
✅ 自动筛选出与用户请求最相关的工具
✅ 减少 Token 消耗（工具描述占用大量上下文）
✅ 提升模型的工具选择准确率

使用方式（需要 Embedding 服务）:
```csharp
var strategy = new EmbeddingToolReductionStrategy(embeddingGenerator, toolLimit: 5);

var client = chatClient.AsBuilder()
    .UseToolReduction(strategy)  // 在 FunctionInvocation 之前
    .UseFunctionInvocation()
    .Build();

// 配置中启用
var options = new ChatOptions
{
    Tools = allTools  // 100 个工具
};

// 实际发送给模型: 只有 5 个最相关的工具
await client.GetResponseAsync(""查询天气"", options);
```

配置文件中的设置:
```json
{
  ""Pipeline"": {
    ""EnableToolReduction"": true,
    ""ToolLimit"": 5,
    ""RequiredToolPrefixes"": [""Core"", ""System""]
  }
}
```
");
        
        Console.WriteLine("\n========== Function Calling 演示结束 ==========");
    }
    
    #region 工具函数
    
    /// <summary>
    /// 模拟获取天气
    /// </summary>
    [Description("获取指定城市的当前天气")]
    private static string GetWeather([Description("城市名称")] string city)
    {
        var weathers = new Dictionary<string, string>
        {
            ["北京"] = "晴朗，温度 25°C，空气质量优",
            ["上海"] = "多云，温度 28°C，有轻微雾霾",
            ["深圳"] = "阴天，温度 30°C，可能有雷阵雨",
            ["广州"] = "晴，温度 32°C，紫外线较强"
        };
        
        return weathers.TryGetValue(city, out var weather) 
            ? $"{city}天气: {weather}" 
            : $"{city}天气数据暂不可用";
    }
    
    /// <summary>
    /// 计算器
    /// </summary>
    [Description("执行数学计算")]
    private static double Calculate(
        [Description("第一个数")] double a, 
        [Description("第二个数")] double b, 
        [Description("运算符: +, -, *, /")] string op)
    {
        return op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" => b != 0 ? a / b : double.NaN,
            _ => throw new ArgumentException($"不支持的运算符: {op}")
        };
    }
    
    #endregion
}
