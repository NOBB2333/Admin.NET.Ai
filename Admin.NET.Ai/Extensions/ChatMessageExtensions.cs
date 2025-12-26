using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MEAI = Microsoft.Extensions.AI;
using SKContent = Microsoft.SemanticKernel;

namespace Admin.NET.Ai.Extensions;

/// <summary>
/// SK↔MEAI 类型转换扩展方法
/// 用于在 Semantic Kernel 类型和 Microsoft.Extensions.AI 类型之间转换
/// </summary>
public static class ChatMessageExtensions
{
    #region SK → MEAI 转换

    /// <summary>
    /// 将 SK ChatMessageContent 转换为 MEAI ChatMessage
    /// </summary>
    public static MEAI.ChatMessage ToMeai(this ChatMessageContent skMessage)
    {
        var role = skMessage.Role.ToMeaiRole();
        
        // 处理多模态内容
        if (skMessage.Items?.Count > 0)
        {
            var contents = new List<MEAI.AIContent>();
            foreach (var item in skMessage.Items)
            {
                switch (item)
                {
                    case SKContent.TextContent textContent:
                        contents.Add(new MEAI.TextContent(textContent.Text ?? string.Empty));
                        break;
                    case SKContent.ImageContent imageContent:
                        if (imageContent.Uri != null)
                            contents.Add(new MEAI.UriContent(imageContent.Uri, imageContent.MimeType ?? "image/png"));
                        else if (imageContent.Data != null)
                            contents.Add(new MEAI.DataContent(imageContent.Data.Value, imageContent.MimeType ?? "image/png"));
                        break;
                    case SKContent.FunctionCallContent funcCall:
                        contents.Add(new MEAI.FunctionCallContent(
                            funcCall.Id ?? Guid.NewGuid().ToString(),
                            funcCall.FunctionName,
                            funcCall.Arguments));
                        break;
                    case SKContent.FunctionResultContent funcResult:
                        contents.Add(new MEAI.FunctionResultContent(
                            funcResult.CallId ?? Guid.NewGuid().ToString(),
                            funcResult.Result));
                        break;
                }
            }
            return new MEAI.ChatMessage(role, contents);
        }
        
        // 简单文本消息
        return new MEAI.ChatMessage(role, skMessage.Content ?? string.Empty);
    }

    /// <summary>
    /// 将 SK ChatHistory 转换为 MEAI ChatMessage 列表
    /// </summary>
    public static IEnumerable<MEAI.ChatMessage> ToMeai(this ChatHistory skHistory)
    {
        return skHistory.Select(m => m.ToMeai());
    }

    /// <summary>
    /// 将 SK AuthorRole 转换为 MEAI ChatRole
    /// </summary>
    public static MEAI.ChatRole ToMeaiRole(this AuthorRole skRole)
    {
        return skRole.Label.ToLowerInvariant() switch
        {
            "system" => MEAI.ChatRole.System,
            "user" => MEAI.ChatRole.User,
            "assistant" => MEAI.ChatRole.Assistant,
            "tool" => MEAI.ChatRole.Tool,
            _ => new MEAI.ChatRole(skRole.Label)
        };
    }

    #endregion

    #region MEAI → SK 转换

    /// <summary>
    /// 将 MEAI ChatMessage 转换为 SK ChatMessageContent
    /// </summary>
    public static ChatMessageContent ToSK(this MEAI.ChatMessage meaiMessage)
    {
        var role = meaiMessage.Role.ToSKRole();
        
        // 处理多模态内容
        if (meaiMessage.Contents?.Count > 0)
        {
            var items = new ChatMessageContentItemCollection();
            foreach (var content in meaiMessage.Contents)
            {
                switch (content)
                {
                    case MEAI.TextContent textContent:
                        items.Add(new SKContent.TextContent(textContent.Text));
                        break;
                    case MEAI.UriContent uriContent:
                        items.Add(new SKContent.ImageContent(uriContent.Uri) { MimeType = uriContent.MediaType });
                        break;
                    case MEAI.DataContent dataContent:
                        items.Add(new SKContent.ImageContent(dataContent.Data, dataContent.MediaType));
                        break;
                    case MEAI.FunctionCallContent funcCall:
                        // SK FunctionCallContent(functionName, pluginName, id, arguments) 
                        // arguments 需要是 KernelArguments 类型，这里简化处理仅存储 Name/Id
                        items.Add(new SKContent.FunctionCallContent(funcCall.Name, null, funcCall.CallId));
                        break;
                    case MEAI.FunctionResultContent funcResult:
                        // SK FunctionResultContent(callId, pluginName, result) - result 是 object
                        items.Add(new SKContent.FunctionResultContent(funcResult.CallId, null, funcResult.Result?.ToString()));
                        break;
                }
            }
            return new ChatMessageContent(role, items);
        }
        
        // 简单文本消息
        return new ChatMessageContent(role, meaiMessage.Text ?? string.Empty);
    }

    /// <summary>
    /// 将 MEAI ChatMessage 列表转换为 SK ChatHistory
    /// </summary>
    public static ChatHistory ToSK(this IEnumerable<MEAI.ChatMessage> meaiMessages)
    {
        var history = new ChatHistory();
        foreach (var msg in meaiMessages)
        {
            history.Add(msg.ToSK());
        }
        return history;
    }

    /// <summary>
    /// 将 MEAI ChatRole 转换为 SK AuthorRole
    /// </summary>
    public static AuthorRole ToSKRole(this MEAI.ChatRole meaiRole)
    {
        return meaiRole.Value.ToLowerInvariant() switch
        {
            "system" => AuthorRole.System,
            "user" => AuthorRole.User,
            "assistant" => AuthorRole.Assistant,
            "tool" => AuthorRole.Tool,
            _ => new AuthorRole(meaiRole.Value)
        };
    }

    #endregion
}
