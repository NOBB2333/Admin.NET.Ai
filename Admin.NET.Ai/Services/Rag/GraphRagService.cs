using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Admin.NET.Ai.Services.Rag;

/// <summary>
/// Graph RAG 服务实现 (支持 Neo4j 和 多策略)
/// </summary>
public class GraphRagService(ILogger<GraphRagService> logger, IOptions<LLMAgentOptions> options, RagStrategyFactory strategyFactory) : IGraphRagService, IDisposable
{
    private readonly LLMAgentOptions _options = options.Value;
    private IDriver? _driver;

    public async Task<List<string>> SearchAsync(string query, RagSearchOptions? options = null)
    {
        options ??= new RagSearchOptions();
        logger.LogInformation("Searching Graph RAG for: {Query} with Strategy: {Strategy}", query, options.Strategy);

        var neo4jConfig = _options.LLMGraphRag.GraphDatabase;
        if (string.Equals(neo4jConfig.Type, "Neo4j", StringComparison.OrdinalIgnoreCase))
        {
            try 
            {
                var driver = GetDriver(neo4jConfig);
                await using var session = driver.AsyncSession();
                
                // 简单的全文检索模拟 (实际应结合 Vector Search)
                var cypher = "MATCH (n:Document) WHERE toLower(n.content) CONTAINS toLower($query) RETURN n.content AS content LIMIT 5";
                var cursor = await session.RunAsync(cypher, new { query });
                
                var results = await cursor.ToListAsync(record => record["content"].As<string>());
                return results;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to search Neo4j.");
                return [];
            }
        }

        // 回退或其他策略
        var strategy = strategyFactory.GetStrategy(options.Strategy);
        return await strategy.ExecuteSearchAsync(query, options, this);
    }

    public async Task InsertAsync(string text)
    {
        logger.LogInformation("Inserting text into Graph RAG: {Text}...", text[..Math.Min(text.Length, 20)]);
        
        var neo4jConfig = _options.LLMGraphRag.GraphDatabase;
        if (string.Equals(neo4jConfig.Type, "Neo4j", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var driver = GetDriver(neo4jConfig);
                await using var session = driver.AsyncSession();
                
                // 创建节点
                await session.RunAsync("CREATE (n:Document {content: $content, createdAt: datetime()})", new { content = text });
                logger.LogInformation("Inserted into Neo4j.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to insert into Neo4j.");
            }
        }
    }

    private IDriver GetDriver(Admin.NET.Ai.Options.GraphDatabaseConfig config)
    {
        if (_driver != null) return _driver;
        
        var authToken = AuthTokens.Basic(config.Username, config.Password);
        _driver = GraphDatabase.Driver(config.ConnectionString, authToken);
        return _driver;
    }

    public void Dispose()
    {
        _driver?.Dispose();
        GC.SuppressFinalize(this);
    }
}
