using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services;

/// <summary>
/// 会话编排服务（MEAI-first 企业标准）
/// 实现线程隔离、压缩集成、对话管理
/// </summary>
public class ConversationService(
    IChatMessageStore chatStore,
    IChatReducer? chatReducer = null) : IConversationService
{
    private readonly IChatMessageStore _chatStore = chatStore;
    private readonly IChatReducer? _chatReducer = chatReducer;

    /// <summary>
    /// 构建完整对话上下文
    /// </summary>
    public async Task<IList<ChatMessage>> BuildContextAsync(
        string sessionId, 
        ChatMessage userMessage,
        bool compress = true,
        CancellationToken cancellationToken = default)
    {
        // 1. 获取历史记录
        var history = await _chatStore.GetHistoryAsync(sessionId, cancellationToken);
        
        // 2. 可选：压缩历史
        IEnumerable<ChatMessage> processedHistory = history;
        if (compress && _chatReducer != null && history.Count > 0)
        {
            processedHistory = await _chatReducer.ReduceAsync(history, cancellationToken);
        }
        
        // 3. 构建完整上下文
        var context = new List<ChatMessage>(processedHistory);
        context.Add(userMessage);
        
        return context;
    }

    /// <summary>
    /// 保存一轮对话
    /// </summary>
    public async Task SaveTurnAsync(
        string sessionId, 
        ChatMessage userMessage, 
        ChatMessage assistantMessage, 
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage> { userMessage, assistantMessage };
        await _chatStore.SaveMessagesAsync(sessionId, messages, cancellationToken);
    }

    /// <summary>
    /// 压缩并持久化历史记录
    /// </summary>
    public async Task CompressAndSaveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_chatReducer == null) return;

        var history = await _chatStore.GetHistoryAsync(sessionId, cancellationToken);
        if (history.Count == 0) return;
        
        var compressed = await _chatReducer.ReduceAsync(history, cancellationToken);
        await _chatStore.ReplaceHistoryAsync(sessionId, compressed, cancellationToken);
    }
}
