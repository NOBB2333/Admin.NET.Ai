using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OAI = OpenAI.Chat;

namespace Admin.NET.Ai.Core;

/// <summary>
/// OpenAI ChatClient 到 IChatClient 的适配器
/// 用于将 OpenAI SDK 的 ChatClient 转换为 Microsoft.Extensions.AI 的 IChatClient
/// 支持多模态内容（图片等）的智能处理
/// </summary>
public sealed class OpenAIChatClientAdapter : IChatClient
{
    private readonly OAI.ChatClient _client;
    private readonly string? _modelId;

    public OpenAIChatClientAdapter(OAI.ChatClient client, string? modelId = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _modelId = modelId;
    }

    public ChatClientMetadata Metadata => new(nameof(OpenAIChatClientAdapter), null, _modelId);

    public object? GetService(Type serviceType, object? key = null)
    {
        return serviceType.IsInstanceOfType(_client) ? _client : null;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var messages = await ToOpenAIMessagesAsync(chatMessages, cancellationToken);
        var openAIOptions = ToOpenAIOptions(options);
        
        OAI.ChatCompletion completion = await _client.CompleteChatAsync(messages, openAIOptions, cancellationToken);
        
        return ToMEAIResponse(completion);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = await ToOpenAIMessagesAsync(chatMessages, cancellationToken);
        var openAIOptions = ToOpenAIOptions(options);
        
        var updates = _client.CompleteChatStreamingAsync(messages, openAIOptions, cancellationToken);
        
        await foreach (var update in updates)
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return new ChatResponseUpdate(ToMEAIRole(update.Role), part.Text);
                }
            }
        }
    }

    public void Dispose() { }

    #region Private Conversion Methods

    private async Task<IEnumerable<OAI.ChatMessage>> ToOpenAIMessagesAsync(
        IEnumerable<ChatMessage> messages, 
        CancellationToken ct)
    {
        var result = new List<OAI.ChatMessage>();
        
        // 使用共享的 HttpClient 来处理网络图片
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        foreach (var m in messages)
        {
            if (m.Role == ChatRole.User)
            {
                if (m.Contents != null && m.Contents.Count > 0)
                {
                    var parts = new List<OAI.ChatMessageContentPart>();
                    
                    foreach (var content in m.Contents)
                    {
                        var part = await ConvertContentPartAsync(content, httpClient, ct);
                        if (part != null)
                        {
                            parts.Add(part);
                        }
                    }
                    
                    result.Add(new OAI.UserChatMessage(parts));
                }
                else
                {
                    result.Add(new OAI.UserChatMessage(m.Text));
                }
            }
            else if (m.Role == ChatRole.System)
            {
                result.Add(new OAI.SystemChatMessage(m.Text));
            }
            else if (m.Role == ChatRole.Assistant)
            {
                result.Add(new OAI.AssistantChatMessage(m.Text));
            }
            else
            {
                // 默认作为用户消息处理
                result.Add(new OAI.UserChatMessage(m.Text));
            }
        }
        
        return result;
    }

    private async Task<OAI.ChatMessageContentPart?> ConvertContentPartAsync(
        AIContent content,
        HttpClient httpClient,
        CancellationToken ct)
    {
        if (content is TextContent tc)
        {
            return OAI.ChatMessageContentPart.CreateTextPart(tc.Text);
        }
        
        if (content is DataContent dc)
        {
            return OAI.ChatMessageContentPart.CreateImagePart(
                BinaryData.FromBytes(dc.Data.ToArray()), 
                dc.MediaType);
        }
        
        if (content is UriContent uc)
        {
            return await ConvertUriContentAsync(uc, httpClient, ct);
        }
        
        return null;
    }

    private async Task<OAI.ChatMessageContentPart> ConvertUriContentAsync(
        UriContent uc,
        HttpClient httpClient,
        CancellationToken ct)
    {
        if (uc.Uri.IsFile)
        {
            // 本地文件：直接读取
            var bytes = await File.ReadAllBytesAsync(uc.Uri.LocalPath, ct);
            var mediaType = GetMediaTypeFromExtension(uc.Uri.LocalPath);
            return OAI.ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), mediaType);
        }
        
        if (uc.Uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            // 网络图片：下载后传递（避免供应商无法访问的问题）
            try
            {
                var bytes = await httpClient.GetByteArrayAsync(uc.Uri, ct);
                var mediaType = GetMediaTypeFromExtension(uc.Uri.AbsoluteUri);
                return OAI.ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), mediaType);
            }
            catch (Exception ex)
            {
                // 下载失败，回退到传递 URL
                Console.WriteLine($"[OpenAIChatClientAdapter] Image download failed: {ex.Message}. Falling back to URL.");
                return OAI.ChatMessageContentPart.CreateImagePart(uc.Uri);
            }
        }
        
        // 其他 URI 方案：直接传递
        return OAI.ChatMessageContentPart.CreateImagePart(uc.Uri);
    }

    private static string GetMediaTypeFromExtension(string path)
    {
        var lower = path.ToLowerInvariant();
        if (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg"))
            return "image/jpeg";
        if (lower.EndsWith(".png"))
            return "image/png";
        if (lower.EndsWith(".gif"))
            return "image/gif";
        if (lower.EndsWith(".webp"))
            return "image/webp";
        
        return "image/jpeg"; // 默认
    }

    private static OAI.ChatCompletionOptions ToOpenAIOptions(ChatOptions? options)
    {
        if (options == null) return new();
        
#pragma warning disable OPENAI001 // Seed is experimental
        var result = new OAI.ChatCompletionOptions
        {
            Temperature = options.Temperature,
            TopP = options.TopP,
            MaxOutputTokenCount = options.MaxOutputTokens,
            Seed = options.Seed
        };
#pragma warning restore OPENAI001
        
        // 停止序列
        if (options.StopSequences?.Count > 0)
        {
            foreach (var seq in options.StopSequences)
            {
                result.StopSequences.Add(seq);
            }
        }
        
        // 频率惩罚
        if (options.FrequencyPenalty.HasValue)
            result.FrequencyPenalty = options.FrequencyPenalty.Value;
        
        // 存在惩罚
        if (options.PresencePenalty.HasValue)
            result.PresencePenalty = options.PresencePenalty.Value;
        
        return result;
    }

    private static ChatResponse ToMEAIResponse(OAI.ChatCompletion completion)
    {
        var content = completion.Content?.Count > 0 ? completion.Content[0].Text : string.Empty;
        
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, content))
        {
            ModelId = completion.Model,
            FinishReason = ToMEAIFinishReason(completion.FinishReason)
        };
        
        // 添加 Usage 信息
        if (completion.Usage != null)
        {
            response.Usage = new UsageDetails
            {
                InputTokenCount = completion.Usage.InputTokenCount,
                OutputTokenCount = completion.Usage.OutputTokenCount,
                TotalTokenCount = completion.Usage.TotalTokenCount
            };
        }
        
        return response;
    }

    private static ChatRole ToMEAIRole(OAI.ChatMessageRole? role)
    {
        if (!role.HasValue) return ChatRole.Assistant;
        if (role == OAI.ChatMessageRole.User) return ChatRole.User;
        if (role == OAI.ChatMessageRole.System) return ChatRole.System;
        return ChatRole.Assistant;
    }

    private static ChatFinishReason? ToMEAIFinishReason(OAI.ChatFinishReason? reason)
    {
        if (!reason.HasValue) return null;
        
        return reason.Value switch
        {
            OAI.ChatFinishReason.Stop => ChatFinishReason.Stop,
            OAI.ChatFinishReason.Length => ChatFinishReason.Length,
            OAI.ChatFinishReason.ContentFilter => ChatFinishReason.ContentFilter,
            OAI.ChatFinishReason.ToolCalls => ChatFinishReason.ToolCalls,
            OAI.ChatFinishReason.FunctionCall => ChatFinishReason.ToolCalls,
            _ => null
        };
    }

    #endregion
}
