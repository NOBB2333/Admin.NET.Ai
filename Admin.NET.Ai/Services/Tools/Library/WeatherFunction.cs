using System.ComponentModel;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Tools.Library;

/// <summary>
/// 示例工具：天气查询
/// </summary>
public class WeatherAiCallFunction : IAiCallableFunction
{
    public string Name => "WeatherTool";
    public string Description => "提供天气查询服务";

    public IEnumerable<AIFunction> GetFunctions()
    {
        // 使用 MEAI 的 AIFunctionFactory 创建
        yield return AIFunctionFactory.Create(GetWeather, "get_weather", "根据城市名称查询当前天气");
    }

    /// <summary>
    /// 获取天气
    /// </summary>
    /// <param name="city">城市名称</param>
    /// <returns></returns>
    [Description("根据城市名称查询当前天气")]
    private string GetWeather([Description("城市名称")] string city)
    {
        // 模拟返回
        return $"{city} 的天气是 晴朗，25度。";
    }
}
