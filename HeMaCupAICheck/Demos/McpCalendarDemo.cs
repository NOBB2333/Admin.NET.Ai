using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Services.MCP;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 场景19: 智能助手 (天气 + 农历 + 节假日)
/// 
/// 📌 展示工具调用能力 (使用真实公开 API)
/// 
/// 使用的免费API:
/// - 天气: http://t.weather.sojson.com/api/weather/city/{城市代码}
/// - 节假日: https://timor.tech/api/holiday/info/{日期}
/// </summary>
public static class McpCalendarDemo
{
    private static readonly HttpClient _httpClient = new() 
    { 
        Timeout = TimeSpan.FromSeconds(10) 
    };

    // 城市代码映射 (来自 weather.sojson.com)
    private static readonly Dictionary<string, string> CityCodes = new()
    {
        ["北京"] = "101010100",
        ["上海"] = "101020100",
        ["广州"] = "101280101",
        ["深圳"] = "101280601",
        ["杭州"] = "101210101",
        ["成都"] = "101270101",
        ["武汉"] = "101200101",
        ["南京"] = "101190101",
        ["西安"] = "101110101",
        ["天津"] = "101030100"
    };

    public static async Task RunAsync(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("McpCalendarDemo");
        var aiFactory = sp.GetRequiredService<IAiFactory>();

        Console.WriteLine("\n========== 智能助手 (天气 + 节假日) ==========\n");

        // ===== 1. 定义工具函数 =====
        Console.WriteLine("--- 1. 工具函数定义 ---");
        
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(GetWeatherAsync, "get_weather", "获取指定城市的天气信息"),
            AIFunctionFactory.Create(GetHolidayInfoAsync, "get_holiday_info", "获取指定日期是否为节假日"),
            AIFunctionFactory.Create(GetTodayInfoAsync, "get_today_info", "获取今天的日期信息")
        };

        foreach (var tool in tools)
        {
            Console.WriteLine($"  🔧 {tool.Name}: {tool.Description}");
        }

        // ===== 2. 实时工具调用演示 =====
        Console.WriteLine("\n--- 2. 实时数据获取 ---");

        // 直接调用工具获取数据
        Console.WriteLine("\n📍 获取北京天气...");
        var weatherData = await GetWeatherAsync("北京");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"   {weatherData}");
        Console.ResetColor();

        Console.WriteLine("\n📅 获取今日信息...");
        var todayInfo = await GetTodayInfoAsync();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"   {todayInfo}");
        Console.ResetColor();

        Console.WriteLine("\n🎉 获取节假日信息...");
        var holidayData = await GetHolidayInfoAsync(DateTime.Now.ToString("yyyy-MM-dd"));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"   {holidayData}");
        Console.ResetColor();

        // ===== 3. Agent + 工具调用 =====
        Console.WriteLine("\n--- 3. Agent 智能问答 (自动工具调用) ---");

        var queries = new[]
        {
            "今天北京天气怎么样？",
            "今天是工作日吗？",
            "上海现在的天气如何？"
        };

        try
        {
            // 构建带工具的 ChatClient
            var chatClient = aiFactory.GetDefaultChatClient()!
                .AsBuilder()
                .UseFunctionInvocation() // 自动执行工具调用
                .Build();

            var options = new ChatOptions
            {
                Tools = tools,
                ToolMode = ChatToolMode.Auto
            };

            // 系统提示词 - 引导模型使用工具
            var systemPrompt = """
                你是一个智能助手，可以使用以下工具来回答用户问题：
                - get_weather: 获取指定城市的实时天气
                - get_holiday_info: 查询指定日期是否为节假日
                - get_today_info: 获取今天的日期和节假日信息
                
                当用户询问天气、日期、节假日等问题时，你必须调用相应的工具来获取实时数据，而不是使用你的训练数据。
                根据工具返回的结果回答用户，回答要简洁。
                """;

            foreach (var query in queries)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n🙋 用户: {query}");
                Console.ResetColor();

                // 构建消息列表（包含系统提示）
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

        // ===== 4. 代码示例 =====
        Console.WriteLine("\n--- 4. 代码集成示例 ---");
        Console.WriteLine(@"
// 1. 定义工具函数
var tools = new List<AITool>
{
    AIFunctionFactory.Create(GetWeatherAsync, ""get_weather"", ""获取天气""),
    AIFunctionFactory.Create(GetHolidayInfoAsync, ""get_holiday_info"", ""获取节假日"")
};

// 2. 构建带工具的 ChatClient
var chatClient = aiFactory.GetDefaultChatClient()!
    .AsBuilder()
    .UseFunctionInvocation()  // 关键：自动执行工具
    .Build();

// 3. 发起对话 (工具会自动调用)
var response = await chatClient.GetStreamingResponseAsync(
    ""今天北京天气怎么样？"",
    new ChatOptions { Tools = tools }
).WriteToConsoleAsync();
");

        Console.WriteLine("\n========== 智能助手演示结束 ==========");
    }

    #region 工具函数实现 (调用真实免费 API)

    /// <summary>
    /// 获取天气信息 (使用 sojson 免费天气 API)
    /// </summary>
    private static async Task<string> GetWeatherAsync(string city)
    {
        try
        {
            // 获取城市代码
            if (!CityCodes.TryGetValue(city, out var cityCode))
            {
                cityCode = "101010100"; // 默认北京
            }

            // 调用 sojson 天气 API
            var url = $"http://t.weather.sojson.com/api/weather/city/{cityCode}";
            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            
            var status = json.RootElement.GetProperty("status").GetInt32();
            if (status != 200)
            {
                return $"🌡️ {city}天气: 数据获取失败";
            }

            var data = json.RootElement.GetProperty("data");
            var wendu = data.GetProperty("wendu").GetString();
            var shidu = data.GetProperty("shidu").GetString();
            var quality = data.GetProperty("quality").GetString();
            
            var forecast = data.GetProperty("forecast")[0];
            var high = forecast.GetProperty("high").GetString();
            var low = forecast.GetProperty("low").GetString();
            var type = forecast.GetProperty("type").GetString();
            
            return $"🌡️ {city}天气: {type}，当前 {wendu}°C，{low} ~ {high}，湿度 {shidu}，空气质量 {quality}";
        }
        catch (Exception ex)
        {
            return $"🌡️ {city}天气: 晴，温度 5°C (模拟数据，API暂不可用: {ex.Message})";
        }
    }

    /// <summary>
    /// 获取今日信息
    /// </summary>
    private static async Task<string> GetTodayInfoAsync()
    {
        var today = DateTime.Now;
        var dayOfWeek = today.DayOfWeek switch
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
        
        var holidayInfo = await GetHolidayInfoAsync(today.ToString("yyyy-MM-dd"));
        return $"📅 今天是 {today:yyyy年M月d日} {dayOfWeek}，{holidayInfo}";
    }

    /// <summary>
    /// 获取节假日信息 (使用 timor.tech 免费 API)
    /// </summary>
    private static async Task<string> GetHolidayInfoAsync(string date)
    {
        try
        {
            // 调用 timor.tech 节假日 API
            var url = $"https://timor.tech/api/holiday/info/{date}";
            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            
            var code = json.RootElement.GetProperty("code").GetInt32();
            if (code != 0)
            {
                return GetFallbackHolidayInfo(date);
            }

            var type = json.RootElement.GetProperty("type");
            var typeCode = type.GetProperty("type").GetInt32();
            var typeName = type.GetProperty("name").GetString();
            
            // 检查是否有节日名称
            var holidayName = "";
            if (json.RootElement.TryGetProperty("holiday", out var holiday) && 
                holiday.ValueKind == JsonValueKind.Object)
            {
                holidayName = holiday.GetProperty("name").GetString();
            }
            
            var emoji = typeCode switch
            {
                0 => "💼", // 工作日
                1 => "🎉", // 节假日
                2 => "🛋️", // 周末
                3 => "💼", // 调休工作日
                _ => "📅"
            };
            
            var result = $"{emoji} {typeName}";
            if (!string.IsNullOrEmpty(holidayName))
            {
                result += $" ({holidayName})";
            }
            
            return result;
        }
        catch
        {
            return GetFallbackHolidayInfo(date);
        }
    }

    private static string GetFallbackHolidayInfo(string date)
    {
        if (DateTime.TryParse(date, out var dateTime))
        {
            var isWeekend = dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            return $"{(isWeekend ? "🛋️ 周末休息日" : "💼 工作日")}";
        }
        return "📅 日期信息";
    }

    #endregion
}
