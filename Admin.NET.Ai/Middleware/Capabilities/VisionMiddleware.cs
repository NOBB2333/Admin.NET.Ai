using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Middleware.Capabilities;


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
