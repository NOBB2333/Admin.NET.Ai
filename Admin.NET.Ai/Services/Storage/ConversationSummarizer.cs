using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace Admin.NET.Ai.Services.Storage;

/// <summary>
/// 对话摘要服务
/// </summary>
public class ConversationSummarizer
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ConversationSummarizer> _logger;

    public ConversationSummarizer(IChatClient chatClient, ILogger<ConversationSummarizer> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<string> SummarizeAsync(IEnumerable<ChatMessageContent> history, int maxWords = 100)
    {
        if (!history.Any()) return string.Empty;

        var sb = new StringBuilder();
        foreach (var msg in history)
        {
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        }

        var prompt = $@"
请将以下对话总结为简短的摘要（不超过 {maxWords} 字）。
摘要应包含关键信息、用户意图和最终结果。

对话内容:
{sb}

摘要:";

        var response = await _chatClient.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, prompt) });
        var summary = response.Messages.Last().Text;

        _logger.LogInformation("📝 [Summarizer] 生成摘要: {Summary}", summary);
        return summary;
    }

    /// <summary>
    /// 压缩对话历史 (保留最近 N 条 + 摘要)
    /// </summary>
    public async Task<ChatHistory> CompressHistoryAsync(ChatHistory history, int keepLastN = 10)
    {
        if (history.Count <= keepLastN) return history;

        var messagesToSummarize = history.Take(history.Count - keepLastN).ToList();
        var recentMessages = history.Skip(history.Count - keepLastN).ToList();

        // 转换 SK ChatMessageContent 到 MEAI ChatMessageContent (如果需要的话，这里假设兼容)
        // 注意：SK 和 MEAI 类型不同，这里只是演示逻辑
        
        var summary = await SummarizeAsync(messagesToSummarize);

        var newHistory = new ChatHistory();
        newHistory.AddSystemMessage($"之前的对话摘要: {summary}");
        newHistory.AddRange(recentMessages);

        _logger.LogInformation("🗜️ [Summarizer] 历史记录已压缩: {OldCount} -> {NewCount}", history.Count, newHistory.Count);
        return newHistory;
    }
}
