using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Admin.NET.Ai.Services.RAG;

public enum RetrievalStrategy { DirectAnswering, MultiStepReasoning, CodeRetrieval }
public class QueryAnalysis
{
    public string Query { get; set; } = "";
    public RetrievalStrategy Strategy { get; set; }
    public double Complexity { get; set; }
}

public class RAGPlan
{
    public RetrievalStrategy Strategy { get; set; }
    public string RewrittenQuery { get; set; } = "";
    public List<string> Steps { get; set; } = new();
}

public class RAGResult
{
    public string Answer { get; set; } = "";
    public List<TextSearchResult> Sources { get; set; } = new();
    public bool IsRefined { get; set; }
}

public class AgenticRAGStrategy
{
    private readonly ITextSearchProvider _searchProvider;
    private readonly IChatClient? _refinerClient; // 用于规划/细化的 LLM

    public AgenticRAGStrategy(ITextSearchProvider searchProvider, IChatClient? refinerClient = null)
    {
        _searchProvider = searchProvider;
        _refinerClient = refinerClient; // 演示时可以为空
    }

    public async Task<RAGPlan> CreateExecutionPlanAsync(string query)
    {
        // 模拟分析：基于关键字检测
        var analysis = new QueryAnalysis { Query = query, Complexity = 0.5 };
        if (query.Contains("CEO") && query.Contains("妻子")) // 多跳示例
        {
            analysis.Complexity = 0.9;
            analysis.Strategy = RetrievalStrategy.MultiStepReasoning;
        }
        else if (query.Contains("code") || query.Contains("API"))
        {
             analysis.Strategy = RetrievalStrategy.CodeRetrieval;
        }
        else
        {
             analysis.Strategy = RetrievalStrategy.DirectAnswering;
        }
        
        // 规划
        var plan = new RAGPlan
        {
            Strategy = analysis.Strategy,
            RewrittenQuery = query // 简化：无重写模拟
        };

        if (plan.Strategy == RetrievalStrategy.MultiStepReasoning)
        {
             plan.Steps = new List<string> { "Identify CEO", "Identify CEO's Wife", "Find Wife's Education" };
        }

        await Task.CompletedTask;
        return plan;
    }

    public async Task<RAGResult> ExecuteWithIterativeRefinementAsync(RAGPlan plan)
    {
        // 1. 第一遍
        var results = await _searchProvider.SearchAsync(plan.RewrittenQuery, new SearchOptions { MaxResults = 3 });
        
        // 2. 模拟迭代细化
        // 假设第一个结果对演示来说足够好，除非为空
        if (results.Results.Count == 0 && plan.Strategy != RetrievalStrategy.CodeRetrieval)
        {
             // 细化查询模拟
             plan.RewrittenQuery += " refined";
             results = await _searchProvider.SearchAsync(plan.RewrittenQuery, new SearchOptions { MaxResults = 5 });
             return new RAGResult { Answer = "Refined Result found.", Sources = results.Results, IsRefined = true };
        }

        return new RAGResult { Answer = "Direct Result found.", Sources = results.Results, IsRefined = false };
    }
}
