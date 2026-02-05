using Admin.NET.Ai.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 自适应压缩策略 - 实现 MEAI IChatReducer
/// 对应需求: 5.7 压缩触发策略
/// </summary>
public class AdaptiveCompressionReducer(
    IOptions<CompressionConfig> configOptions,
    MessageCountingReducer lightReducer,
    SummarizingReducer mediumReducer, // 假设中等压缩用摘要
    SummarizingReducer heavyReducer   // 假设重度压缩也用摘要但参数不同，或者复用
    ) : IChatReducer
{
    private readonly CompressionConfig _config = configOptions.Value;
    private readonly MessageCountingReducer _lightReducer = lightReducer;
    private readonly SummarizingReducer _mediumReducer = mediumReducer;
    
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        var totalTokens = EstimateTokens(messageList);
        
        bool shouldCompress = CheckCompressionConditions(messageList, totalTokens);
        
        if (!shouldCompress)
            return messageList;
            
        var level = DetermineCompressionLevel(messageList.Count, totalTokens);
        
        return level switch
        {
            CompressionLevel.Light => await _lightReducer.ReduceAsync(messageList, ct),
            CompressionLevel.Medium => await _mediumReducer.ReduceAsync(messageList, ct),
            CompressionLevel.Heavy => await _mediumReducer.ReduceAsync(messageList, ct),
            _ => messageList
        };
    }
    
    private bool CheckCompressionConditions(List<ChatMessage> messages, int totalTokens)
    {
        if (messages.Count > _config.MessageCountThreshold) return true;
        if (totalTokens > _config.TokenCountThreshold) return true;
        return false;
    }

    private CompressionLevel DetermineCompressionLevel(int count, int tokens)
    {
        if (count > _config.MessageCountThreshold * 2 || tokens > _config.TokenCountThreshold * 2)
            return CompressionLevel.Heavy;
            
        if (count > _config.MessageCountThreshold || tokens > _config.TokenCountThreshold)
            return CompressionLevel.Medium;
            
        return CompressionLevel.Light;
    }

    private int EstimateTokens(List<ChatMessage> messages)
    {
        int chars = 0;
        foreach(var m in messages)
        {
            var text = m.Text;
            if (text != null) chars += text.Length;
        }
        return chars / 2;
    }

    private enum CompressionLevel { Light, Medium, Heavy }
}
