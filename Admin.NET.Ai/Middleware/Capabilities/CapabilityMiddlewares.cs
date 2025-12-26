using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Middleware.Capabilities;

/// <summary>
/// 搜索能力中间件
/// 为 Agent 添加网络搜索能力
/// </summary>
public class SearchMiddleware : DelegatingChatClient
{
    private readonly ILogger<SearchMiddleware> _logger;
    private readonly HttpClient? _httpClient;

    public SearchMiddleware(
        IChatClient innerClient,
        ILogger<SearchMiddleware> logger,
        IHttpClientFactory? httpClientFactory = null)
        : base(innerClient)
    {
        _logger = logger;
        _httpClient = httpClientFactory?.CreateClient("search");
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var enhancedOptions = AddSearchTool(options);
        return await base.GetResponseAsync(chatMessages, enhancedOptions, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var enhancedOptions = AddSearchTool(options);
        return base.GetStreamingResponseAsync(chatMessages, enhancedOptions, cancellationToken);
    }

    private ChatOptions AddSearchTool(ChatOptions? options)
    {
        var newOptions = options?.Clone() ?? new ChatOptions();
        var tools = newOptions.Tools?.ToList() ?? new List<AITool>();

        // 网络搜索工具
        var webSearch = AIFunctionFactory.Create(
            async (string query, CancellationToken ct) =>
            {
                _logger.LogInformation("🔍 [Search] 搜索: {Query}", query);
                return await SearchWebAsync(query, ct);
            },
            "search_web",
            "Search the web for current information. Use this when you need up-to-date information or facts you don't know."
        );

        tools.Add(webSearch);
        newOptions.Tools = tools;
        return newOptions;
    }

    private async Task<string> SearchWebAsync(string query, CancellationToken ct)
    {
        // 这里可以集成:
        // 1. Bing Search API
        // 2. Google Search API  
        // 3. SerpAPI
        // 4. MCP 搜索服务
        
        if (_httpClient == null)
        {
            return "[Simulated Search Results]\n" +
                   $"Query: {query}\n" +
                   "Note: Real web search requires HttpClient configuration and API keys.\n" +
                   "Configure IHttpClientFactory with 'search' client to enable real searches.";
        }

        try
        {
            // 示例：调用 Bing Search API
            var apiKey = Environment.GetEnvironmentVariable("BING_SEARCH_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                return "[Error] BING_SEARCH_API_KEY environment variable not set.";
            }

            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            var response = await _httpClient.GetStringAsync(
                $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}", ct);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "搜索失败");
            return $"[Error] Search failed: {ex.Message}";
        }
    }
}

/// <summary>
/// 多模态视觉能力中间件
/// 为 Agent 添加图像理解能力
/// </summary>
public class VisionMiddleware : DelegatingChatClient
{
    private readonly ILogger<VisionMiddleware> _logger;
    private readonly bool _enableImageGeneration;

    public VisionMiddleware(
        IChatClient innerClient,
        ILogger<VisionMiddleware> logger,
        bool enableImageGeneration = false)
        : base(innerClient)
    {
        _logger = logger;
        _enableImageGeneration = enableImageGeneration;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var enhancedOptions = AddVisionTools(options);
        return await base.GetResponseAsync(chatMessages, enhancedOptions, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var enhancedOptions = AddVisionTools(options);
        return base.GetStreamingResponseAsync(chatMessages, enhancedOptions, cancellationToken);
    }

    private ChatOptions AddVisionTools(ChatOptions? options)
    {
        var newOptions = options?.Clone() ?? new ChatOptions();
        var tools = newOptions.Tools?.ToList() ?? new List<AITool>();

        // 图像分析工具
        var analyzeImage = AIFunctionFactory.Create(
            (string imageUrl, string prompt) =>
            {
                _logger.LogInformation("👁️ [Vision] 分析图像: {Url}", imageUrl);
                return $"[Vision Analysis]\nImage: {imageUrl}\nPrompt: {prompt}\n" +
                       "Note: Actual vision analysis requires a multimodal model (GPT-4V, Claude 3, etc.).\n" +
                       "The image content should be passed as ImageContent in the chat messages.";
            },
            "analyze_image",
            "Analyze an image and describe its contents. Provide the image URL and an optional prompt for specific analysis."
        );

        tools.Add(analyzeImage);

        // 图像生成工具 (可选)
        if (_enableImageGeneration)
        {
            var generateImage = AIFunctionFactory.Create(
                (string prompt) =>
                {
                    _logger.LogInformation("🎨 [Vision] 生成图像: {Prompt}", prompt);
                    return $"[Image Generation]\nPrompt: {prompt}\n" +
                           "Note: Image generation requires DALL-E or similar API integration.";
                },
                "generate_image",
                "Generate an image based on a text prompt using AI image generation."
            );
            tools.Add(generateImage);
        }

        newOptions.Tools = tools;
        return newOptions;
    }
}
