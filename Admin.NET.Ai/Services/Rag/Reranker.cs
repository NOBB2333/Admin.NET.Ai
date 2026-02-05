using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.RAG;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Rag;

/// <summary>
/// 重排序器接口
/// </summary>
public interface IReranker
{
    /// <summary>
    /// 对搜索结果进行重排序
    /// </summary>
    Task<List<TextSearchResult>> RerankAsync(string query, List<TextSearchResult> results, RerankOptions? options = null);
}

/// <summary>
/// 重排序选项
/// </summary>
public class RerankOptions
{
    /// <summary>
    /// 返回的最大结果数
    /// </summary>
    public int TopK { get; set; } = 3;
    
    /// <summary>
    /// 最小相关性分数
    /// </summary>
    public double MinScore { get; set; } = 0.5;
    
    /// <summary>
    /// 是否使用模型重排序
    /// </summary>
    public bool UseModelRerank { get; set; } = true;
}

/// <summary>
/// 基于交叉编码器的重排序器
/// </summary>
public class CrossEncoderReranker : IReranker
{
    private readonly ILogger<CrossEncoderReranker> _logger;
    private readonly IChatClient? _rerankClient; // 可选：用于 LLM-based 重排序

    public CrossEncoderReranker(ILogger<CrossEncoderReranker> logger, IChatClient? rerankClient = null)
    {
        _logger = logger;
        _rerankClient = rerankClient;
    }

    public async Task<List<TextSearchResult>> RerankAsync(string query, List<TextSearchResult> results, RerankOptions? options = null)
    {
        options ??= new RerankOptions();
        
        if (results.Count == 0) return results;

        _logger.LogInformation("🔄 [Reranker] 开始重排序 {Count} 条结果", results.Count);

        List<TextSearchResult> rerankedResults;

        if (options.UseModelRerank && _rerankClient != null)
        {
            rerankedResults = await RerankWithLLMAsync(query, results, options);
        }
        else
        {
            rerankedResults = RerankWithHeuristics(query, results, options);
        }

        var finalResults = rerankedResults
            .Where(r => r.Score >= options.MinScore)
            .Take(options.TopK)
            .ToList();

        _logger.LogInformation("✅ [Reranker] 重排序完成, 返回 {Count} 条", finalResults.Count);
        return finalResults;
    }

    /// <summary>
    /// 使用 LLM 进行重排序 (Listwise 方式)
    /// </summary>
    private async Task<List<TextSearchResult>> RerankWithLLMAsync(string query, List<TextSearchResult> results, RerankOptions options)
    {
        if (_rerankClient == null) return results;

        // 构造重排序 Prompt
        var passages = string.Join("\n\n", results.Select((r, i) => $"[{i + 1}] {r.Text}"));
        var prompt = $@"Given the query: ""{query}""

Rank the following passages by relevance to the query. Return only the passage numbers in order of relevance, separated by commas.

Passages:
{passages}

Ranked order (most relevant first):";

        try
        {
            var response = await _rerankClient.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, prompt) });
            var rankingText = response.Messages.LastOrDefault()?.Text ?? "";

            // 解析排名结果
            var ranks = ParseRanking(rankingText, results.Count);
            
            // 按排名重新排序并分配分数
            var reranked = new List<TextSearchResult>();
            for (int i = 0; i < ranks.Count; i++)
            {
                var idx = ranks[i] - 1;
                if (idx >= 0 && idx < results.Count)
                {
                    var result = results[idx];
                    result.Score = 1.0 - (i * 0.1); // 简单的线性分数衰减
                    result.Metadata["RerankPosition"] = i + 1;
                    reranked.Add(result);
                }
            }

            return reranked;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM 重排序失败，回退到启发式方法");
            return RerankWithHeuristics(query, results, options);
        }
    }

    /// <summary>
    /// 使用启发式方法进行重排序
    /// </summary>
    private List<TextSearchResult> RerankWithHeuristics(string query, List<TextSearchResult> results, RerankOptions options)
    {
        var queryTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var result in results)
        {
            var text = result.Text.ToLower();
            
            // 因子1: 原始分数权重
            var baseScore = result.Score * 0.5;
            
            // 因子2: 关键词匹配度
            var matchCount = queryTerms.Count(term => text.Contains(term));
            var termMatchScore = (double)matchCount / queryTerms.Length * 0.3;
            
            // 因子3: 位置权重 (开头匹配更重要)
            var startsWithQuery = queryTerms.Any(t => text.StartsWith(t)) ? 0.1 : 0;
            
            // 因子4: 长度惩罚 (太长的结果可能不够精确)
            var lengthPenalty = Math.Min(500, text.Length) / 500.0 * 0.1;

            result.Score = baseScore + termMatchScore + startsWithQuery + lengthPenalty;
            result.Metadata["RerankMethod"] = "Heuristic";
        }

        return results.OrderByDescending(r => r.Score).ToList();
    }

    /// <summary>
    /// 解析 LLM 返回的排名
    /// </summary>
    private List<int> ParseRanking(string text, int maxCount)
    {
        var ranks = new List<int>();
        var numbers = System.Text.RegularExpressions.Regex.Matches(text, @"\d+");
        
        foreach (System.Text.RegularExpressions.Match m in numbers)
        {
            if (int.TryParse(m.Value, out int n) && n >= 1 && n <= maxCount && !ranks.Contains(n))
            {
                ranks.Add(n);
            }
        }

        return ranks;
    }
}

/// <summary>
/// 混合重排序器 (多策略融合)
/// </summary>
public class HybridReranker : IReranker
{
    private readonly ILogger<HybridReranker> _logger;
    private readonly CrossEncoderReranker _crossEncoder;

    public HybridReranker(ILogger<HybridReranker> logger, IChatClient? rerankClient = null)
    {
        _logger = logger;
        _crossEncoder = new CrossEncoderReranker(
            new LoggerFactory().CreateLogger<CrossEncoderReranker>(), 
            rerankClient);
    }

    public async Task<List<TextSearchResult>> RerankAsync(string query, List<TextSearchResult> results, RerankOptions? options = null)
    {
        options ??= new RerankOptions();

        // 第一轮: 使用交叉编码器/LLM
        var firstPass = await _crossEncoder.RerankAsync(query, results, new RerankOptions
        {
            TopK = Math.Min(options.TopK * 2, results.Count), // 保留更多候选
            MinScore = 0.3,
            UseModelRerank = options.UseModelRerank
        });

        // 第二轮: 多样性过滤 (MMR - Maximal Marginal Relevance)
        var finalResults = ApplyMMR(firstPass, options.TopK);

        _logger.LogInformation("🎯 [HybridReranker] 最终返回 {Count} 条结果", finalResults.Count);
        return finalResults;
    }

    /// <summary>
    /// 最大边际相关性算法 - 增加结果多样性
    /// </summary>
    private List<TextSearchResult> ApplyMMR(List<TextSearchResult> results, int topK, double lambda = 0.5)
    {
        if (results.Count <= topK) return results;

        var selected = new List<TextSearchResult>();
        var remaining = results.ToList();

        // 选择第一个 (最相关)
        selected.Add(remaining[0]);
        remaining.RemoveAt(0);

        while (selected.Count < topK && remaining.Count > 0)
        {
            double bestScore = double.MinValue;
            int bestIdx = 0;

            for (int i = 0; i < remaining.Count; i++)
            {
                var candidate = remaining[i];
                
                // 与已选结果的最大相似度
                var maxSim = selected.Max(s => TextSimilarity(candidate.Text, s.Text));
                
                // MMR 分数 = λ * 相关性 - (1-λ) * 最大相似度
                var mmrScore = lambda * candidate.Score - (1 - lambda) * maxSim;

                if (mmrScore > bestScore)
                {
                    bestScore = mmrScore;
                    bestIdx = i;
                }
            }

            selected.Add(remaining[bestIdx]);
            remaining.RemoveAt(bestIdx);
        }

        return selected;
    }

    /// <summary>
    /// 简单的文本相似度 (Jaccard)
    /// </summary>
    private double TextSimilarity(string a, string b)
    {
        var setA = a.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var setB = b.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        
        var intersection = setA.Intersect(setB).Count();
        var union = setA.Union(setB).Count();
        
        return union == 0 ? 0 : (double)intersection / union;
    }
}
