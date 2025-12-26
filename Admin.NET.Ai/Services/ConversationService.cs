using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Conversation;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Admin.NET.Ai.Services;

/// <summary>
/// 会话管理服务（五星级企业标准）
/// 实现线程隔离、压缩集成、完整会话管理
/// </summary>
public class ConversationService(
    IChatMessageStore chatStore,
    IChatReducer? chatReducer = null) : IConversationService
{
    private readonly IChatMessageStore _chatStore = chatStore;
    private readonly IChatReducer? _chatReducer = chatReducer;

    #region 基础操作

    /// <summary>
    /// 获取会话上下文 (加载历史记录)
    /// </summary>
    public async Task<IAiContext> GetContextAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var history = await _chatStore.GetHistoryAsync(sessionId, cancellationToken);
        
        var context = new AiContext("", null!) 
        { 
            SessionId = sessionId 
        };
        
        // 将历史记录注入上下文
        context.Items["History"] = history;
        
        return context; 
    }

    public async Task SaveContextAsync(string sessionId, IAiContext context, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessageContent>();
        
        if (!string.IsNullOrEmpty(context.Prompt))
        {
            messages.Add(new ChatMessageContent(AuthorRole.User, context.Prompt));
        }

        if (context.Result is string response)
        {
            messages.Add(new ChatMessageContent(AuthorRole.Assistant, response));
        }

        if (messages.Any())
        {
            await _chatStore.SaveMessagesAsync(sessionId, messages, cancellationToken);
        }
    }

    public async Task DeleteContextAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _chatStore.ClearHistoryAsync(sessionId, cancellationToken);
    }

    #endregion

    #region 增强功能

    /// <summary>
    /// 获取历史记录
    /// </summary>
    public async Task<IEnumerable<ChatMessageContent>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _chatStore.GetHistoryAsync(sessionId, cancellationToken);
    }

    /// <summary>
    /// 获取并压缩历史记录（如果配置了压缩器）
    /// </summary>
    public async Task<IEnumerable<ChatMessageContent>> GetCompressedHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var history = await _chatStore.GetHistoryAsync(sessionId, cancellationToken);
        
        if (_chatReducer == null)
        {
            return history;
        }

        return await _chatReducer.ReduceAsync(history, cancellationToken);
    }

    /// <summary>
    /// 压缩并持久化历史记录
    /// </summary>
    public async Task CompressAndSaveHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_chatReducer == null) return;

        var history = await _chatStore.GetHistoryAsync(sessionId, cancellationToken);
        var compressed = await _chatReducer.ReduceAsync(history, cancellationToken);
        
        await _chatStore.ReplaceHistoryAsync(sessionId, compressed, cancellationToken);
    }

    /// <summary>
    /// 获取会话信息
    /// </summary>
    public async Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _chatStore.GetSessionInfoAsync(sessionId, cancellationToken);
    }

    /// <summary>
    /// 获取所有会话列表
    /// </summary>
    public async Task<PagedResult<SessionInfo>> GetSessionsAsync(int pageIndex = 0, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _chatStore.GetSessionsAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// 更新会话标题
    /// </summary>
    public async Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default)
    {
        await _chatStore.UpdateSessionTitleAsync(sessionId, title, cancellationToken);
    }

    #endregion
}

