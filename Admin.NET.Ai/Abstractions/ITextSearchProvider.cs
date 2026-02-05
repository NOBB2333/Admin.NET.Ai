namespace Admin.NET.Ai.Abstractions;

public interface ITextSearchProvider
{
    Task<SearchResults> SearchAsync(string query, SearchOptions options);
    
    // 可选：索引功能
    Task<IEnumerable<ChunkedDocument>> ChunkAndIndexAsync(IEnumerable<Document> documents);
}

public class SearchOptions
{
    public int MaxResults { get; set; } = 3;
    public double MinScore { get; set; } = 0.5;
    public Dictionary<string, object> Filters { get; set; } = new();
}

public class SearchResults
{
    public List<TextSearchResult> Results { get; set; } = new();

    public SearchResults(List<TextSearchResult> results)
    {
        Results = results;
    }
}

public class TextSearchResult
{
    public string Text { get; set; } = "";
    public double Score { get; set; }
    public string SourceName { get; set; } = "";
    public string SourceLink { get; set; } = "";
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class Document
{
    public string Content { get; set; } = "";
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ChunkedDocument : Document
{
    // Chunk specific properties if any
}
