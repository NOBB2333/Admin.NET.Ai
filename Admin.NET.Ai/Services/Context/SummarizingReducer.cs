using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 智能摘要缩减器
/// 对应需求: 5.2 智能摘要压缩器
/// </summary>
/// <summary>
/// 智能摘要缩减器
/// 对应需求: 5.2 智能摘要压缩器
/// </summary>
public class SummarizingReducer(IAiService aiService, Microsoft.Extensions.Options.IOptions<Admin.NET.Ai.Options.CompressionConfig> configOptions) : IChatReducer
{
    private readonly IAiService _aiService = aiService;
    private readonly Admin.NET.Ai.Options.CompressionConfig _config = configOptions.Value;

    public async Task<IEnumerable<ChatMessageContent>> ReduceAsync(IEnumerable<ChatMessageContent> messages, CancellationToken ct = default)
    {
        var msgList = messages.ToList();
        
        // 使用配置阈值
        if (msgList.Count < _config.MessageCountThreshold)
        {
            return msgList;
        }

        // 策略:
        // 1. 保留 System Message
        // 2. split: [Old Messages to Summarize] + [Recent Messages to Keep]
        // 3. Summarize Old Messages -> 1 Summary Message
        // 4. Result = [System] + [Summary of Old] + [Recent]

        var systemMessages = msgList.Where(m => m.Role == AuthorRole.System).ToList();
        var nonSystem = msgList.Where(m => m.Role != AuthorRole.System).ToList();

        // 动态计算保留最近消息的数量 (例如保留阈值的 1/3)
        int keepRecent = Math.Max(2, _config.MessageCountThreshold / 3);
        
        if (nonSystem.Count <= keepRecent) return msgList;

        var toSummarize = nonSystem.Take(nonSystem.Count - keepRecent).ToList();
        var toKeep = nonSystem.TakeLast(keepRecent).ToList();

        // 生成摘要
        var sb = new StringBuilder();
        foreach (var msg in toSummarize)
        {
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        }

        // 注入配置中的 Prompt
        var prompt = $"{_config.SummaryPromptTemplate}\n\n{sb}";
        
        // 调用 AI 服务生成摘要
        // 防止无限递归: IAiService 内部可能调用 Middleware pipeline。
        // 理想情况下应跳过 Compress Middleware，此处通过 Options 传递标志或使用专门的 Client。
        // 这里假设 IAiService 的简单调用不会无限触发 Compress 或者阈值没到。
        // 实战中建议传递一个 "SkipCompression" 的选项给 ExecuteAsync。
        var options = new Dictionary<string, object?> { { "SkipCompression", true } };
        
        string summaryText;
        try 
        {
            summaryText = await _aiService.ExecuteAsync<string>(prompt, options) ?? "Summary generation returned null.";
        }
        catch (Exception ex)
        {
            summaryText = $"Summary generation failed: {ex.Message}";
        }

        var summaryMessage = new ChatMessageContent(AuthorRole.System, $"[Conversation Summary]: {summaryText}");

        var result = new List<ChatMessageContent>();
        result.AddRange(systemMessages);
        result.Add(summaryMessage);
        result.AddRange(toKeep);

        return result;
    }
}
