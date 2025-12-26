using Admin.NET.Ai.Options;
using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Core;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Rag;

/// <summary>
/// RAG 策略工厂
/// </summary>
public class RagStrategyFactory(ILogger<RagStrategyFactory> logger)
{
    public IRagStrategy GetStrategy(RagStrategy strategyType)
    {
        return strategyType switch
        {
            RagStrategy.Naive => new NaiveRagStrategy(logger),
            RagStrategy.Graph => new GraphRagStrategy(logger),
            RagStrategy.Hybrid => new HybridRagStrategy(logger),
            _ => new NaiveRagStrategy(logger) // 默认
        };
    }
}

/// <summary>
/// RAG 策略接口
/// </summary>
public interface IRagStrategy
{
    Task<List<string>> ExecuteSearchAsync(string query, RagSearchOptions options, IGraphRagService service);
}

public class NaiveRagStrategy(ILogger logger) : IRagStrategy
{
    public async Task<List<string>> ExecuteSearchAsync(string query, RagSearchOptions options, IGraphRagService service)
    {
        logger.LogInformation("Executing Naive RAG Strategy...");
        // 模拟向量检索
        return await Task.FromResult(new List<string> { $"[Vector] Result for {query}" });
    }
}

public class GraphRagStrategy(ILogger logger) : IRagStrategy
{
    public async Task<List<string>> ExecuteSearchAsync(string query, RagSearchOptions options, IGraphRagService service)
    {
        logger.LogInformation("Executing Graph RAG Strategy...");
        // 模拟图谱检索
        return await Task.FromResult(new List<string> { $"[Graph] Nodes related to {query}" });
    }
}

public class HybridRagStrategy(ILogger logger) : IRagStrategy
{
    public async Task<List<string>> ExecuteSearchAsync(string query, RagSearchOptions options, IGraphRagService service)
    {
        logger.LogInformation("Executing Hybrid RAG Strategy...");
        var vectorResults = new List<string> { $"[Vector] Result for {query}" };
        var graphResults = new List<string> { $"[Graph] Nodes related to {query}" };
        
        var results = new List<string>();
        results.AddRange(vectorResults);
        results.AddRange(graphResults);
        
        if (options.EnableRerank)
        {
            logger.LogInformation("Reranking results...");
            // TODO: 调用重排序模型
        }

        return await Task.FromResult(results);
    }
}
