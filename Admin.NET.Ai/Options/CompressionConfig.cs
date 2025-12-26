using Furion.ConfigurableOptions;

namespace Admin.NET.Ai.Options;

/// <summary>
/// 对话上下文压缩配置
/// </summary>
[OptionsSettings("Compression")]
public class CompressionConfig : IConfigurableOptions
{
    /// <summary>
    /// 触发压缩的消息数量阈值 (默认 20)
    /// </summary>
    public int MessageCountThreshold { get; set; } = 20;

    /// <summary>
    /// 触发压缩的 Token 数量阈值 (默认 4000)
    /// </summary>
    public int TokenCountThreshold { get; set; } = 4000;

    /// <summary>
    /// 触发压缩的对话时长阈值 (默认 30分钟)
    /// </summary>
    public TimeSpan TimeDurationThreshold { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 目标压缩比 (默认 0.3，即由原来的大小压缩到 30%)
    /// </summary>
    public double CompressionRatio { get; set; } = 0.3;

    /// <summary>
    /// 关键业务关键词 (用于 KeywordAwareReducer)
    /// </summary>
    public string[] CriticalKeywords { get; set; } = 
    [
        "审批", "支付", "合同", "协议", "订单", 
        "价格", "金额", "截止时间", "重要", "紧急"
    ];

    /// <summary>
    /// 摘要生成的 Prompt 模板
    /// </summary>
    public string SummaryPromptTemplate { get; set; } = "请将以下对话历史总结为简洁的摘要，保留关键信息：";
}
