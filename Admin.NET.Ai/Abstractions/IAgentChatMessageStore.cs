using Admin.NET.Ai.Models.Storage;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 聊天记录持久化接口 (基于 Microsoft Agent Framework 体系)
/// </summary>
/// <remarks>
/// 注意：此接口专为 Microsoft Agent Framework (MAF) 设计，与基于 Semantic Kernel (SK) 的 <see cref="IChatMessageStore"/> 共存。
/// MAF 方案更侧重于对话线程管理和序列化状态恢复。
/// </remarks>
public interface IAgentChatMessageStore
{
    /// <summary>
    /// 批量添加聊天消息
    /// </summary>
    /// <param name="chatHistoryItems">消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task AddMessagesAsync(List<ChatHistoryItemDto> chatHistoryItems, CancellationToken cancellationToken);

    /// <summary>
    /// 获取指定线程的聊天历史
    /// </summary>
    /// <param name="threadId">线程ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<List<ChatHistoryItemDto>> GetMessagesAsync(string threadId, CancellationToken cancellationToken);
}
