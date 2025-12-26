using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 聊天记录缩减器/压缩器接口
/// </summary>
public interface IChatReducer
{
    /// <summary>
    /// 压缩器名称
    /// </summary>
    string Name => GetType().Name;

    /// <summary>
    /// 压缩器描述
    /// </summary>
    string Description => "聊天记录压缩器";

    /// <summary>
    /// 缩减消息列表 (e.g. 摘要、删除旧消息)
    /// </summary>
    Task<IEnumerable<ChatMessageContent>> ReduceAsync(IEnumerable<ChatMessageContent> messages, CancellationToken ct = default);
}
