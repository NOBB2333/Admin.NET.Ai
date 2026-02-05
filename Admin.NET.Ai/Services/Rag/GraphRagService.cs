using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using System.Diagnostics;

namespace Admin.NET.Ai.Services.Rag;

/// <summary>
/// Graph RAG 服务实现 (继承 IRagService，扩展图谱检索)
/// 支持 Neo4j 和多策略
/// </summary>
public class GraphRagService(
    ILogger<GraphRagService> logger, 
    IOptions<LLMAgentOptions> options, 
    RagStrategyFactory strategyFactory) : IGraphRagService, IDisposable
{
    private readonly LLMAgentOptions _options = options.Value;
    private IDriver? _driver;

    #region IRagService (基础向量检索)

    public async Task<RagSearchResult> SearchAsync(
        string query, 
        RagSearchOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        options ??= new RagSearchOptions();
        
        logger.LogInformation("Searching Graph RAG for: {Query}", query);

        var neo4jConfig = _options.LLMGraphRag.GraphDatabase;
        if (string.Equals(neo4jConfig.Type, "Neo4j", StringComparison.OrdinalIgnoreCase))
        {
            try 
            {
                var driver = GetDriver(neo4jConfig);
                await using var session = driver.AsyncSession();
                
                var cypher = "MATCH (n:Document) WHERE toLower(n.content) CONTAINS toLower($query) RETURN n.content AS content LIMIT $limit";
                var cursor = await session.RunAsync(cypher, new { query, limit = options.TopK });
                
                var rawResults = await cursor.ToListAsync();
                var results = rawResults.Select(record => new RagDocument(
                    Content: record["content"].As<string>(),
                    Score: 1.0,
                    Source: "Neo4j"
                )).ToList();
                
                sw.Stop();
                return new RagSearchResult(results, sw.Elapsed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to search Neo4j.");
                sw.Stop();
                return new RagSearchResult([], sw.Elapsed);
            }
        }

        sw.Stop();
        return new RagSearchResult([], sw.Elapsed);
    }

    public async Task IndexAsync(
        IEnumerable<RagDocument> documents, 
        string? collection = null, 
        CancellationToken cancellationToken = default)
    {
        var neo4jConfig = _options.LLMGraphRag.GraphDatabase;
        if (!string.Equals(neo4jConfig.Type, "Neo4j", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var driver = GetDriver(neo4jConfig);
            await using var session = driver.AsyncSession();
            
            foreach (var doc in documents)
            {
                await session.RunAsync(
                    "CREATE (n:Document {content: $content, source: $source, createdAt: datetime()})", 
                    new { content = doc.Content, source = doc.Source ?? "unknown" });
            }
            
            logger.LogInformation("Indexed {Count} documents into Neo4j.", documents.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index into Neo4j.");
        }
    }

    #endregion

    #region IGraphRagService (图谱增强)

    public async Task<RagSearchResult> GraphSearchAsync(
        string query, 
        GraphRagSearchOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        options ??= new GraphRagSearchOptions();
        
        logger.LogInformation("Graph searching for: {Query} with MaxHops: {Hops}", query, options.MaxHops);

        var neo4jConfig = _options.LLMGraphRag.GraphDatabase;
        if (!string.Equals(neo4jConfig.Type, "Neo4j", StringComparison.OrdinalIgnoreCase))
        {
            sw.Stop();
            return new RagSearchResult([], sw.Elapsed);
        }

        try 
        {
            var driver = GetDriver(neo4jConfig);
            await using var session = driver.AsyncSession();
            
            var cypher = @"
                MATCH (n:Document)-[r*1..$maxHops]-(related)
                WHERE toLower(n.content) CONTAINS toLower($query)
                RETURN n.content AS content, collect(DISTINCT related.content) AS relatedContents
                LIMIT $limit";
            
            var cursor = await session.RunAsync(cypher, new { query, maxHops = options.MaxHops, limit = options.TopK });
            
            var results = new List<RagDocument>();
            await foreach (var record in cursor)
            {
                var content = record["content"].As<string>();
                var related = record["relatedContents"].As<List<string>>();
                
                results.Add(new RagDocument(
                    Content: content,
                    Score: 1.0,
                    Source: "Neo4j-Graph",
                    Metadata: options.IncludeRelations 
                        ? new Dictionary<string, object> { { "RelatedContents", related } } 
                        : null
                ));
            }
            
            sw.Stop();
            return new RagSearchResult(results, sw.Elapsed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed graph search in Neo4j.");
            sw.Stop();
            return new RagSearchResult([], sw.Elapsed);
        }
    }

    public async Task BuildGraphAsync(
        IEnumerable<RagDocument> documents, 
        CancellationToken cancellationToken = default)
    {
        await IndexAsync(documents, null, cancellationToken);
        logger.LogInformation("Graph building completed for {Count} documents.", documents.Count());
    }

    #endregion

    private IDriver GetDriver(GraphDatabaseConfig config)
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
