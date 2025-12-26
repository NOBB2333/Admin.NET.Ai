using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 压缩效果监控
/// </summary>
public class CompressionMonitor(ILogger<CompressionMonitor> logger)
{
    private readonly ILogger<CompressionMonitor> _logger = logger;

    public void LogCompressionEffectiveness(
        IEnumerable<ChatMessageContent> original, 
        IEnumerable<ChatMessageContent> compressed,
        TimeSpan compressionTime)
    {
        var originalList = original.ToList();
        var compressedList = compressed.ToList();

        var originalCount = originalList.Count;
        var compressedCount = compressedList.Count;
        
        // 简单的比率计算
        var messageRatio = originalCount > 0 
            ? (double)compressedCount / originalCount 
            : 1.0;
        
        // 估算 Token (这里仅做简单的字符长度估算作为代理，因为精确 Token 需要编码器)
        // 实际项目中可注入 Tokenizer
        var originalEstimatedChars = EstimateChars(originalList);
        var compressedEstimatedChars = EstimateChars(compressedList);
        var charSaving = originalEstimatedChars > 0 
            ? 1.0 - (double)compressedEstimatedChars / originalEstimatedChars 
            : 0.0;
        
        // 记录压缩指标
        _logger.LogInformation(
            "AI上下文压缩报告: 消息数 {Original} -> {Compressed} ({MsgRatio:P1}), " +
            "字符估算 {OriginalChars} -> {CompressedChars} ({CharSaving:P1}节省), " +
            "耗时: {CompressionTime}ms",
            originalCount, compressedCount, messageRatio,
            originalEstimatedChars, compressedEstimatedChars, charSaving,
            compressionTime.TotalMilliseconds);
    }

    private static long EstimateChars(List<ChatMessageContent> messages)
    {
        long count = 0;
        foreach (var msg in messages)
        {
            if (!string.IsNullOrEmpty(msg.Content))
                count += msg.Content.Length;
            
            // 简单估算 Items (如 Function Calls)
            if (msg.Items != null)
                count += msg.Items.Count * 50; 
        }
        return count;
    }
}
