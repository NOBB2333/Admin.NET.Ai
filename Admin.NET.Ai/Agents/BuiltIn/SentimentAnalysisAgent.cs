using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.NET.Ai.Agents.BuiltIn;

/// <summary>
/// 情感分析 Agent - 分析文本情感极性和情绪
/// 最佳实践: 结构化输出 + 专业系统指令
/// </summary>
public class SentimentAnalysisAgent
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<SentimentAnalysisAgent> _logger;

    public const string SystemInstruction = @"你是一个专业的情感分析专家。
你的任务是分析输入文本的情感倾向、情绪类型和强度。

分析维度:
1. 情感极性: positive(正面), negative(负面), neutral(中性)
2. 情绪类型: joy(喜悦), anger(愤怒), sadness(悲伤), fear(恐惧), surprise(惊讶), disgust(厌恶)
3. 情感强度: low(低), medium(中), high(高)
4. 关键情感词: 识别触发情感的关键词

请始终返回结构化的JSON格式结果。";

    public SentimentAnalysisAgent(IChatClient chatClient, ILogger<SentimentAnalysisAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    /// <summary>
    /// 分析单条文本情感
    /// </summary>
    [Description("分析文本的情感极性、情绪类型和强度")]
    public async Task<SentimentResult> AnalyzeAsync(
        [Description("待分析的文本内容")] string text,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemInstruction),
            new(ChatRole.User, $"请分析以下文本的情感:\n\n{text}")
        };

        try
        {
            var response = await _chatClient.GetResponseAsync(messages, new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.Json
            }, ct);

            var json = response.Messages.LastOrDefault()?.Text ?? "{}";
            var result = JsonSerializer.Deserialize<SentimentResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new SentimentResult();

            result.OriginalText = text;
            result.AnalyzedAt = DateTime.UtcNow;

            _logger.LogDebug("情感分析完成: {Sentiment} ({Confidence:P0})", result.Sentiment, result.Confidence);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "情感分析失败");
            return new SentimentResult
            {
                OriginalText = text,
                Sentiment = "unknown",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 分析对话情感趋势
    /// </summary>
    [Description("分析整个对话的情感变化趋势")]
    public async Task<ConversationSentimentTrend> AnalyzeConversationAsync(
        [Description("对话消息列表")] IEnumerable<ChatMessage> messages,
        CancellationToken ct = default)
    {
        var userMessages = messages.Where(m => m.Role == ChatRole.User).ToList();
        var sentiments = new List<SentimentResult>();

        foreach (var msg in userMessages)
        {
            var result = await AnalyzeAsync(msg.Text ?? "", ct);
            sentiments.Add(result);
        }

        return new ConversationSentimentTrend
        {
            TotalMessages = userMessages.Count,
            Sentiments = sentiments,
            OverallSentiment = CalculateOverall(sentiments),
            TrendDirection = CalculateTrend(sentiments)
        };
    }

    private string CalculateOverall(List<SentimentResult> sentiments)
    {
        if (!sentiments.Any()) return "neutral";
        var pos = sentiments.Count(s => s.Sentiment == "positive");
        var neg = sentiments.Count(s => s.Sentiment == "negative");
        return pos > neg * 1.5 ? "positive" : neg > pos * 1.5 ? "negative" : "neutral";
    }

    private string CalculateTrend(List<SentimentResult> sentiments)
    {
        if (sentiments.Count < 2) return "stable";
        var first = sentiments.Take(sentiments.Count / 2).ToList();
        var second = sentiments.Skip(sentiments.Count / 2).ToList();
        var score1 = first.Average(s => s.Sentiment == "positive" ? 1 : s.Sentiment == "negative" ? -1 : 0);
        var score2 = second.Average(s => s.Sentiment == "positive" ? 1 : s.Sentiment == "negative" ? -1 : 0);
        return score2 - score1 > 0.3 ? "improving" : score2 - score1 < -0.3 ? "declining" : "stable";
    }
}

#region 情感分析结果模型

public class SentimentResult
{
    [JsonPropertyName("sentiment")]
    public string Sentiment { get; set; } = "neutral";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("emotions")]
    public List<string> Emotions { get; set; } = new();

    [JsonPropertyName("intensity")]
    public string Intensity { get; set; } = "medium";

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    public string OriginalText { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public string? Error { get; set; }
}

public class ConversationSentimentTrend
{
    public int TotalMessages { get; set; }
    public List<SentimentResult> Sentiments { get; set; } = new();
    public string OverallSentiment { get; set; } = "neutral";
    public string TrendDirection { get; set; } = "stable";
}

#endregion
