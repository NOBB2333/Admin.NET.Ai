using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.RAG;

public class TextSearchProviderOptions
{
    public enum TextSearchBehavior { BeforeAIInvoke, AfterAIInvoke }
    public TextSearchBehavior SearchTime { get; set; } = TextSearchBehavior.BeforeAIInvoke;
    public int RecentMessageMemoryLimit { get; set; } = 6;
}

public class TextSearchProvider : AIContextProvider
{
    private readonly Func<string, CancellationToken, Task<IEnumerable<TextSearchResult>>> _searchFunc;
    private readonly TextSearchProviderOptions _options;
    
    // 我们可能需要状态来跟踪这一轮是否已经搜索过？
    // 如果需要，可以使用上下文中的 SerializedState。

    public TextSearchProvider(
        Func<string, CancellationToken, Task<IEnumerable<TextSearchResult>>> searchFunc,
        string? serializedState = null,
        TextSearchProviderOptions? options = null)
    {
        _searchFunc = searchFunc;
        _options = options ?? new TextSearchProviderOptions();
        // serializedState 逻辑可以在这里
    }

    public override async Task<AIContextItem?> InvokingAsync(AIContextProviderContext context, CancellationToken cancellationToken = default)
    {
        if (_options.SearchTime != TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
            return null;

        var lastMessage = context.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        if (string.IsNullOrWhiteSpace(lastMessage)) return null;

        // 执行搜索
        var results = await _searchFunc(lastMessage, cancellationToken);
        
        if (results == null || !results.Any()) return null;

        // 构建上下文相关字符串
        var contextString = "### Retrieval Context ###\n";
        foreach (var result in results)
        {
            contextString += $"Source: {result.SourceName} ({result.SourceLink})\nContent: {result.Text}\n\n";
        }
        
        return new AIContextItem
        {
             Role = "system",
             Content = $"Relevant Information:\n{contextString}\nPlease use this information to answer the user request."
        };
    }

    public override Task<AIContextItem?> InvokedAsync(AIContextProviderContext context, CancellationToken cancellationToken = default)
    {
        // 可以保存搜索历史或反馈
        return Task.FromResult<AIContextItem?>(null);
    }
}
