using Admin.NET.Ai.Services.MCP.Attributes;

namespace Admin.NET.Ai.Services.MCP.Examples;

/// <summary>
/// 示例 MCP 工具服务 - 展示如何使用 [McpTool] 属性
/// 任何带有 [McpTool] 标记的方法都会自动暴露为 MCP 工具
/// </summary>
public class ExampleMcpTools
{
    /// <summary>
    /// 获取当前时间
    /// 使用方式: [McpTool("描述")] - 工具名称自动取自方法名 (get_current_time)
    /// </summary>
    [McpTool("获取当前服务器时间")]
    public DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }

    /// <summary>
    /// 获取天气信息 (模拟)
    /// 使用方式: [McpTool("名称", "描述")] - 显式指定名称
    /// </summary>
    [McpTool("get_weather", "根据城市名称获取天气信息")]
    public WeatherInfo GetWeather(
        [McpParameter("城市名称，如 '北京'")] string city,
        [McpParameter("温度单位，可选 'celsius' 或 'fahrenheit'")] string unit = "celsius")
    {
        // 模拟天气数据
        var temps = new Dictionary<string, int>
        {
            ["北京"] = 15, ["上海"] = 20, ["广州"] = 25, ["深圳"] = 26
        };

        var temp = temps.TryGetValue(city, out var t) ? t : 18;
        if (unit == "fahrenheit")
        {
            temp = (int)(temp * 1.8 + 32);
        }

        return new WeatherInfo
        {
            City = city,
            Temperature = temp,
            Unit = unit,
            Weather = "晴",
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 计算两个数的和
    /// </summary>
    [McpTool("将两个数字相加")]
    public int Add(
        [McpParameter("第一个数字")] int a,
        [McpParameter("第二个数字")] int b)
    {
        return a + b;
    }

    /// <summary>
    /// 文本翻译 (模拟)
    /// </summary>
    [McpTool("translate", "将文本翻译成指定语言")]
    public async Task<TranslateResult> TranslateAsync(
        [McpParameter("要翻译的文本")] string text,
        [McpParameter("目标语言代码，如 'en', 'zh', 'ja'")] string targetLang = "en")
    {
        // 模拟异步翻译
        await Task.Delay(100);

        return new TranslateResult
        {
            Original = text,
            Translated = $"[{targetLang.ToUpper()}] {text}", // 模拟翻译
            TargetLanguage = targetLang
        };
    }

    /// <summary>
    /// 需要审批的敏感操作
    /// </summary>
    [McpTool("delete_file", "删除指定文件 (需要审批)")]
    public string DeleteFile(
        [McpParameter("文件路径")] string path)
    {
        // 实际实现中会检查 RequiresApproval 标记
        return $"模拟删除: {path}";
    }
}

/// <summary>
/// 天气信息
/// </summary>
public class WeatherInfo
{
    public string City { get; set; } = "";
    public int Temperature { get; set; }
    public string Unit { get; set; } = "celsius";
    public string Weather { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 翻译结果
/// </summary>
public class TranslateResult
{
    public string Original { get; set; } = "";
    public string Translated { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
}
