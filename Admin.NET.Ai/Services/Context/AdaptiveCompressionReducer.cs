using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 自适应压缩策略
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
    
    // 注入不同的策略实现
    private readonly MessageCountingReducer _lightReducer = lightReducer;
    private readonly SummarizingReducer _mediumReducer = mediumReducer;
    
    // 在构造时可以区分，或者复用。这里简单起见复用，实际可以注入 HeavyCompressionReducer
    
    public async Task<IEnumerable<ChatMessageContent>> ReduceAsync(IEnumerable<ChatMessageContent> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        var totalTokens = EstimateTokens(messageList); // 估算
        
        bool shouldCompress = CheckCompressionConditions(messageList, totalTokens);
        
        if (!shouldCompress)
            return messageList;
            
        var level = DetermineCompressionLevel(messageList.Count, totalTokens);
        
        return level switch
        {
            CompressionLevel.Light => await _lightReducer.ReduceAsync(messageList, ct),
            CompressionLevel.Medium => await _mediumReducer.ReduceAsync(messageList, ct),
            CompressionLevel.Heavy => await _mediumReducer.ReduceAsync(messageList, ct), // 暂时复用 Medium
            _ => messageList
        };
    }
    
    private bool CheckCompressionConditions(List<ChatMessageContent> messages, int totalTokens)
    {
        if (messages.Count > _config.MessageCountThreshold) return true;
        if (totalTokens > _config.TokenCountThreshold) return true;
        
        // 简单的时间检查 (需要 Metadata 支持，暂时略过或假设第一条是开始时间)
        // var duration = ...
        
        return false;
    }

    private CompressionLevel DetermineCompressionLevel(int count, int tokens)
    {
        // 简单的分级逻辑
        if (count > _config.MessageCountThreshold * 2 || tokens > _config.TokenCountThreshold * 2)
            return CompressionLevel.Heavy;
            
        if (count > _config.MessageCountThreshold || tokens > _config.TokenCountThreshold)
            return CompressionLevel.Medium;
            
        return CompressionLevel.Light;
    }

    // 简单字符估算，1 token ~= 4 chars (英文) or 1 char (中文)
    // 这里简单按 1 char = 1 token 估算中文环境，或 0.5
    private int EstimateTokens(List<ChatMessageContent> messages)
    {
        int chars = 0;
        foreach(var m in messages)
        {
             if (m.Content != null) chars += m.Content.Length;
        }
        return chars / 2; // 粗略估算
    }

    private enum CompressionLevel { Light, Medium, Heavy }
}
