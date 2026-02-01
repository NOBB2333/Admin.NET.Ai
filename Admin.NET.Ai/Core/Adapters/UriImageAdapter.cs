using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Core.Adapters;

/// <summary>
/// URI 图片适配器
/// 职责：拦截 ChatMessage 中的 HTTP 图片链接，下载并转换为 DataContent (Base64/Binary)
/// 场景：当 LLM 提供商无法直接访问公网 URL 或必须使用 Base64 输入时使用
/// (原 ImageDownloadingMiddleware，重构为功能适配器)
/// </summary>
public class UriImageAdapter : DelegatingChatClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UriImageAdapter(IChatClient innerClient, IHttpClientFactory httpClientFactory) : base(innerClient)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        // 1. 预处理消息：下载所有 HTTP 图片
        var processedMessages = await ProcessMessagesAsync(chatMessages, cancellationToken);
        
        // 2. 传递给下游
        return await base.GetResponseAsync(processedMessages, options, cancellationToken);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var processedMessages = await ProcessMessagesAsync(chatMessages, cancellationToken);
        
        await foreach (var update in base.GetStreamingResponseAsync(processedMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private async Task<IList<ChatMessage>> ProcessMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken ct)
    {
        var result = new List<ChatMessage>();
        using var httpClient = _httpClientFactory.CreateClient();

        foreach (var message in messages)
        {
            // 如果只有文本，直接复制
            if (message.Contents.All(c => c is TextContent))
            {
                result.Add(message);
                continue;
            }

            // 深拷贝并替换内容
            var newContents = new List<AIContent>();
            foreach (var content in message.Contents)
            {
                if (content is UriContent uriContent && 
                    (uriContent.Uri.Scheme == "http" || uriContent.Uri.Scheme == "https"))
                {
                    try 
                    {
                        // 下载图片
                        var imageBytes = await httpClient.GetByteArrayAsync(uriContent.Uri, ct);
                        var mediaType = DetectMediaType(uriContent.Uri.AbsolutePath) ?? "image/jpeg";
                        
                        // 转换为 DataContent (Binary -> Base64)
                        newContents.Add(new DataContent(imageBytes, mediaType));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[UriImageAdapter] Create DataContent Failed: {ex.Message}. Keeping original URI.");
                        newContents.Add(uriContent);
                    }
                }
                else
                {
                    newContents.Add(content);
                }
            }
            
            result.Add(new ChatMessage(message.Role, newContents) 
            { 
                AuthorName = message.AuthorName,
                AdditionalProperties = message.AdditionalProperties
            });
        }
        return result;
    }

    private string? DetectMediaType(string path)
    {
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return "image/png";
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) return "image/jpeg";
        if (path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) return "image/webp";
        if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) return "image/gif";
        return null;
    }
}
