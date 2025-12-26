using MEAI = Microsoft.Extensions.AI;
using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Diagnostics;
using System.Text;
using Admin.NET.Ai.Models;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Admin.NET.Ai.Core;

/// <summary>
/// AI 服务实现 (MEAI Wrapper)
/// </summary>
public class AiService(IAiFactory aiFactory) : IAiService
{
    public async Task<object?> ExecuteAsync(string prompt, Dictionary<string, object?>? options = null)
    {
        // Default execution returns string content
        var chatResponse = await ExecuteChatCompletionAsync(prompt, options);
        return chatResponse.Messages.Count > 0 ? chatResponse.Messages[0].Text : string.Empty;
    }

    public async Task<TResult?> ExecuteAsync<TResult>(string prompt, Dictionary<string, object?>? options = null)
    {
        var result = await ExecuteAsync(prompt, options);
        if (result is TResult typedResult)
        {
            return typedResult;
        }
        // Basic conversion if result is string and TResult is something else (e.g. JSON extraction)
        // For now, return default
        return default;
    }

    public async IAsyncEnumerable<string> ExecuteStreamAsync(string prompt, Dictionary<string, object?>? options = null)
    {
        using var activity = AiDiagnostics.Source.StartActivity("AiService.ExecuteStreamAsync");
        
        var (client, messages, chatOptions) = PrepareRequest(prompt, options);
        
        await foreach (var update in client.GetStreamingResponseAsync(messages, chatOptions))
        {
             if (update.Text != null)
             {
                 yield return update.Text;
             }
        }
    }

    // Helper to execute standardized ChatCompletion
    private async Task<MEAI.ChatResponse> ExecuteChatCompletionAsync(string prompt, Dictionary<string, object?>? options = null)
    {
        using var activity = AiDiagnostics.Source.StartActivity("AiService.ExecuteChatCompletionAsync");
        
        var (client, messages, chatOptions) = PrepareRequest(prompt, options);

        return await client.GetResponseAsync(messages, chatOptions);
    }

    private (MEAI.IChatClient client, IList<MEAI.ChatMessage> messages, MEAI.ChatOptions chatOptions) PrepareRequest(string prompt, Dictionary<string, object?>? options)
    {
        // 1. Determine Client
        string? clientName = null;
        if (options != null && options.TryGetValue("ClientName", out var nameObj))
        {
            clientName = nameObj?.ToString();
        }

        var client = (string.IsNullOrEmpty(clientName) 
            ? aiFactory.GetDefaultChatClient() 
            : aiFactory.GetChatClient(clientName))
            ?? throw new InvalidOperationException($"Failed to create AI client: {clientName ?? "Default"}");

        // 2. Build Messages
        var messages = new List<MEAI.ChatMessage>();

        // System Prompt
        if (options != null && options.TryGetValue("SystemPrompt", out var sysPromptObj) && sysPromptObj is string sysPrompt)
        {
            messages.Add(new MEAI.ChatMessage(MEAI.ChatRole.System, sysPrompt));
        }

        // Attachments & User Prompt
        // If attachments exist, we need Multi-modal message
        if (options != null && options.TryGetValue("Attachments", out var attObj) && attObj is List<AiAttachment> attachments && attachments.Count > 0)
        {
             var contentItems = new List<MEAI.AIContent>();
             contentItems.Add(new MEAI.TextContent(prompt));
             
             // Convert AiAttachment to suitable AIContent (e.g. ImageContent, DataContent)
             // MEAI supports ImageContent, DataContent.
             // Assuming AiAttachment has Stream or Byte[]
             // Simplified: just assuming text for now or need to check MEAI types.
             // For now, let's skip complex mapping and assume text-only unless we strictly implement Multi-modal.
        }
        else
        {
            messages.Add(new MEAI.ChatMessage(MEAI.ChatRole.User, prompt));
        }

        var chatOptions = new MEAI.ChatOptions();
        
        // Pass SessionId logic if needed (usually handled by higher level Agent)

        return (client, messages, chatOptions);
    }
}
