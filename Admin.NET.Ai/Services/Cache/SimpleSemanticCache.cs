using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Cache;

/// <summary>
/// 简单语义缓存实现（基于关键词匹配）
/// 无需向量数据库依赖，使用 Jaccard 相似度 + 编辑距离
/// </summary>
public class SimpleSemanticCache : ISemanticCache
{
    private readonly ILogger<SimpleSemanticCache> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

    private record CacheEntry(
        string Query,
        string Response,
        string[] Keywords,
        DateTime CachedAt,
        DateTime ExpiresAt
    );

    public SimpleSemanticCache(ILogger<SimpleSemanticCache> logger)
    {
        _logger = logger;
    }

    #region 关键词检索实现

    public Task<CachedResponse?> FindSimilarAsync(
        string query, 
        double threshold = 0.85, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var queryKeywords = ExtractKeywords(query);
        CacheEntry? bestMatch = null;
        double bestSimilarity = 0;

        foreach (var entry in _cache.Values)
        {
            // 跳过过期项
            if (entry.ExpiresAt < DateTime.UtcNow)
                continue;

            var similarity = CalculateSimilarity(queryKeywords, entry.Keywords, query, entry.Query);
            
            if (similarity >= threshold && similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = entry;
            }
        }

        if (bestMatch != null)
        {
            _logger.LogInformation("🎯 [SemanticCache] 找到相似缓存，相似度: {Similarity:P2}", bestSimilarity);
            return Task.FromResult<CachedResponse?>(new CachedResponse(
                bestMatch.Query,
                bestMatch.Response,
                bestMatch.Keywords,
                bestMatch.CachedAt,
                bestSimilarity
            ));
        }

        return Task.FromResult<CachedResponse?>(null);
    }

    public Task AddAsync(
        string query, 
        string response, 
        string[]? keywords = null,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var extractedKeywords = keywords ?? ExtractKeywords(query);
        var exp = expiration ?? _defaultExpiration;
        
        var entry = new CacheEntry(
            query,
            response,
            extractedKeywords,
            DateTime.UtcNow,
            DateTime.UtcNow.Add(exp)
        );

        // 使用查询的规范化形式作为键
        var key = NormalizeQuery(query);
        _cache[key] = entry;
        
        _logger.LogDebug("💾 [SemanticCache] 已缓存: {Query} (关键词: {Keywords})", 
            query.Length > 50 ? query[..50] + "..." : query,
            string.Join(", ", extractedKeywords.Take(5)));

        return Task.CompletedTask;
    }

    #endregion

    #region 向量检索实现 (占位 - 需要 Embedding 模型)

    public Task<ChatResponse?> GetSimilarAsync(
        ReadOnlyMemory<float> embedding, 
        double threshold = 0.8, 
        CancellationToken cancellationToken = default)
    {
        // 向量检索需要专门的实现（如 Qdrant、Milvus）
        // 这里返回 null，表示未实现
        _logger.LogDebug("[SemanticCache] 向量检索未实现，请使用专门的向量数据库实现");
        return Task.FromResult<ChatResponse?>(null);
    }

    public Task SetAsync(
        ReadOnlyMemory<float> embedding, 
        ChatResponse response, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default)
    {
        // 向量存储需要专门的实现
        _logger.LogDebug("[SemanticCache] 向量存储未实现，请使用专门的向量数据库实现");
        return Task.CompletedTask;
    }

    #endregion

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        _logger.LogInformation("[SemanticCache] 缓存已清除");
        return Task.CompletedTask;
    }

    #region 相似度计算

    /// <summary>
    /// 综合相似度计算（Jaccard + 编辑距离）
    /// </summary>
    private double CalculateSimilarity(string[] keywords1, string[] keywords2, string text1, string text2)
    {
        // Jaccard 相似度 (关键词)
        var jaccardSim = JaccardSimilarity(keywords1, keywords2);
        
        // 规范化编辑距离 (文本)
        var editSim = 1.0 - NormalizedEditDistance(NormalizeQuery(text1), NormalizeQuery(text2));
        
        // 加权平均：关键词 60%，文本 40%
        return jaccardSim * 0.6 + editSim * 0.4;
    }

    private double JaccardSimilarity(string[] set1, string[] set2)
    {
        if (set1.Length == 0 && set2.Length == 0) return 1.0;
        if (set1.Length == 0 || set2.Length == 0) return 0.0;

        var intersection = set1.Intersect(set2, StringComparer.OrdinalIgnoreCase).Count();
        var union = set1.Union(set2, StringComparer.OrdinalIgnoreCase).Count();
        
        return union > 0 ? (double)intersection / union : 0.0;
    }

    private double NormalizedEditDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 0.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 1.0;

        var maxLen = Math.Max(s1.Length, s2.Length);
        if (maxLen == 0) return 0.0;

        var distance = LevenshteinDistance(s1, s2);
        return (double)distance / maxLen;
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var dp = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++) dp[i, 0] = i;
        for (int j = 0; j <= n; j++) dp[0, j] = j;

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }

        return dp[m, n];
    }

    #endregion

    #region 关键词提取

    /// <summary>
    /// 简单关键词提取
    /// </summary>
    private string[] ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

        // 分词（支持中英文）
        var words = Regex.Split(text, @"[\s,，。！？!?；;：:""''""''()（）\[\]【】]+")
            .Where(w => w.Length >= 2)
            .Select(w => w.ToLowerInvariant())
            .ToList();

        // 移除停用词
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "的", "是", "在", "了", "和", "有", "我", "你", "他", "她", "它",
            "这", "那", "什么", "怎么", "为什么", "如何", "请", "帮", "帮我",
            "the", "a", "an", "is", "are", "was", "were", "be", "been",
            "have", "has", "had", "do", "does", "did", "will", "would",
            "can", "could", "should", "may", "might", "must", "to", "of",
            "in", "on", "at", "for", "with", "about", "by", "from", "as"
        };

        return words.Where(w => !stopWords.Contains(w)).Distinct().ToArray();
    }

    /// <summary>
    /// 规范化查询（用作缓存键）
    /// </summary>
    private string NormalizeQuery(string query)
    {
        return Regex.Replace(query.ToLowerInvariant().Trim(), @"\s+", " ");
    }

    #endregion
}
