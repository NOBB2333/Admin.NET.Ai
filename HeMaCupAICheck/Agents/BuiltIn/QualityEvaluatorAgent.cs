using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HeMaCupAICheck.Agents.BuiltIn;

/// <summary>
/// 对话质量评估 Agent - 评估AI响应质量和对话效果
/// 最佳实践: 多维度评分 + 结构化反馈 + 快速规则检查
/// </summary>
public class QualityEvaluatorAgent : IAiAgent
{
    // Public properties for IAiAgent
    public string Name { get; set; } = "QualityEvaluatorAgent";
    public string Instructions { get; set; } = SystemInstruction;

    private readonly IChatClient _chatClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QualityEvaluatorAgent> _logger;

    public const string SystemInstruction = @"你是一个专业的AI对话质量评估专家。
你的任务是评估AI响应的质量，从多个维度进行打分和分析。

评估维度 (0-10分):
1. relevance: 相关性 - 响应是否切题
2. accuracy: 准确性 - 信息是否正确
3. completeness: 完整性 - 是否回答了全部问题
4. clarity: 清晰度 - 表达是否清楚易懂
5. helpfulness: 有用性 - 对用户是否有实际帮助
6. tone: 语气 - 是否专业友好
7. overall: 综合评分 - 整体质量评价

同时提供具体问题(issues)和改进建议(suggestions)。请严格按JSON格式返回结果。";

    public QualityEvaluatorAgent(
        IChatClient chatClient, 
        IServiceProvider serviceProvider,
        ILogger<QualityEvaluatorAgent> logger)
    {
        _chatClient = chatClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 评估单个响应质量
    /// </summary>
    [Description("评估AI响应的质量，返回多维度评分")]
    public async Task<QualityScore> EvaluateResponseAsync(
        [Description("用户问题")] string userQuery,
        [Description("AI响应")] string aiResponse,
        [Description("对话上下文(可选)")] string? context = null,
        CancellationToken ct = default)
    {
        try
        {
            // 使用 Builder 模式的结构化输出 API
            var score = await _chatClient
                .Structured()
                .WithSystem(SystemInstruction)
                .RunStructuredAsync<QualityScore>(
                    $@"请评估以下AI响应的质量:

用户问题: {userQuery}
{(context != null ? $"上下文: {context}" : "")}
AI响应: {aiResponse}", 
                    _serviceProvider);
            
            if (score != null)
            {
                score.UserQuery = userQuery;
                score.AiResponse = aiResponse;
                score.EvaluatedAt = DateTime.UtcNow;
                _logger.LogInformation("质量评估完成: 综合分 {Overall}/10", score.Overall);
                return score;
            }
            
            return new QualityScore { UserQuery = userQuery, AiResponse = aiResponse, Error = "解析结果为空" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "质量评估失败");
            return new QualityScore { UserQuery = userQuery, AiResponse = aiResponse, Error = ex.Message };
        }
    }

    /// <summary>
    /// 评估整个对话质量
    /// </summary>
    [Description("评估整个对话的质量，生成综合报告")]
    public async Task<ConversationReport> EvaluateConversationAsync(
        [Description("对话消息列表")] IEnumerable<ChatMessage> messages,
        CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        var scores = new List<QualityScore>();

        for (int i = 0; i < messageList.Count - 1; i++)
        {
            if (messageList[i].Role == ChatRole.User && messageList[i + 1].Role == ChatRole.Assistant)
            {
                var score = await EvaluateResponseAsync(
                    messageList[i].Text ?? "",
                    messageList[i + 1].Text ?? "",
                    ct: ct);
                scores.Add(score);
            }
        }

        return new ConversationReport
        {
            TotalTurns = scores.Count,
            Scores = scores,
            AverageRelevance = scores.Any() ? scores.Average(s => s.Relevance) : 0,
            AverageAccuracy = scores.Any() ? scores.Average(s => s.Accuracy) : 0,
            AverageClarity = scores.Any() ? scores.Average(s => s.Clarity) : 0,
            OverallScore = scores.Any() ? scores.Average(s => s.Overall) : 0,
            CommonIssues = scores.SelectMany(s => s.Issues).GroupBy(i => i)
                .Where(g => g.Count() > 1).Select(g => g.Key).Take(5).ToList(),
            Recommendations = GenerateRecommendations(scores)
        };
    }

    /// <summary>
    /// 快速规则检查 (不调用LLM)
    /// </summary>
    [Description("基于规则的快速质量检查，不调用LLM")]
    public QuickCheck QuickCheck([Description("AI响应内容")] string response)
    {
        var check = new QuickCheck
        {
            Length = response.Length,
            IsEmpty = string.IsNullOrWhiteSpace(response),
            HasErrorPatterns = ContainsErrorPatterns(response),
            HasProfessionalTone = !ContainsUnprofessionalPatterns(response),
            IsWellStructured = IsStructured(response),
            PassesBasicCheck = true
        };

        check.PassesBasicCheck = !check.IsEmpty && !check.HasErrorPatterns && check.Length >= 10;
        return check;
    }

    /// <summary>
    /// 对比多个响应选择最佳
    /// </summary>
    [Description("对比多个候选响应，选择质量最高的")]
    public async Task<(string BestResponse, QualityScore Score)> SelectBestAsync(
        [Description("用户问题")] string query,
        [Description("候选响应列表")] IEnumerable<string> candidates,
        CancellationToken ct = default)
    {
        var results = new List<(string Response, QualityScore Score)>();
        foreach (var candidate in candidates)
        {
            var score = await EvaluateResponseAsync(query, candidate, ct: ct);
            results.Add((candidate, score));
        }

        var best = results.OrderByDescending(r => r.Score.Overall).First();
        _logger.LogInformation("从 {Count} 个候选中选择最佳, 评分: {Score}/10", results.Count, best.Score.Overall);
        return (best.Response, best.Score);
    }

    private List<string> GenerateRecommendations(List<QualityScore> scores)
    {
        var recs = new List<string>();
        if (scores.Average(s => s.Relevance) < 7) recs.Add("提高响应与问题的相关性");
        if (scores.Average(s => s.Clarity) < 7) recs.Add("使用更清晰的语言表达");
        if (scores.Average(s => s.Completeness) < 7) recs.Add("确保回答完整覆盖问题");
        recs.AddRange(scores.SelectMany(s => s.Suggestions).Distinct().Take(3));
        return recs;
    }

    private bool ContainsErrorPatterns(string text) =>
        new[] { "我无法", "抱歉，我不能", "发生错误", "I cannot", "error" }
            .Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    private bool ContainsUnprofessionalPatterns(string text) =>
        new[] { "额", "嗯...", "这个嘛", "我觉得吧" }.Any(p => text.Contains(p));

    private bool IsStructured(string text) =>
        text.Split("\n\n").Length > 1 || text.Contains("- ") || text.Contains("1. ") || text.Length < 200;
}

#region 质量评估模型

public class QualityScore
{
    [JsonPropertyName("relevance")]
    public double Relevance { get; set; }

    [JsonPropertyName("accuracy")]
    public double Accuracy { get; set; }

    [JsonPropertyName("completeness")]
    public double Completeness { get; set; }

    [JsonPropertyName("clarity")]
    public double Clarity { get; set; }

    [JsonPropertyName("helpfulness")]
    public double Helpfulness { get; set; }

    [JsonPropertyName("tone")]
    public double Tone { get; set; }

    [JsonPropertyName("overall")]
    public double Overall { get; set; }

    [JsonPropertyName("issues")]
    public List<string> Issues { get; set; } = new();

    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();

    public string UserQuery { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
    public string? Error { get; set; }
}

public class ConversationReport
{
    public int TotalTurns { get; set; }
    public List<QualityScore> Scores { get; set; } = new();
    public double AverageRelevance { get; set; }
    public double AverageAccuracy { get; set; }
    public double AverageClarity { get; set; }
    public double OverallScore { get; set; }
    public List<string> CommonIssues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class QuickCheck
{
    public int Length { get; set; }
    public bool IsEmpty { get; set; }
    public bool HasErrorPatterns { get; set; }
    public bool HasProfessionalTone { get; set; }
    public bool IsWellStructured { get; set; }
    public bool PassesBasicCheck { get; set; }
}

#endregion
