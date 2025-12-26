using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// 对话存储抽象基类
/// 提供新增接口方法的默认实现，简化子类开发
/// </summary>
public abstract class ChatMessageStoreBase : IChatMessageStore
{
    #region 抽象方法（子类必须实现）

    public abstract Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default);
    public abstract Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default);
    public abstract Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    #endregion

    #region 批量操作（默认实现）

    public virtual async Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            await SaveMessageAsync(sessionId, message, cancellationToken);
        }
    }

    public virtual async Task ReplaceHistoryAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken cancellationToken = default)
    {
        await ClearHistoryAsync(sessionId, cancellationToken);
        await SaveMessagesAsync(sessionId, messages, cancellationToken);
    }

    #endregion

    #region 分页与查询（默认实现）

    public virtual async Task<PagedResult<ChatMessageContent>> GetPagedHistoryAsync(
        string sessionId, 
        int pageIndex = 0, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(sessionId, cancellationToken);
        var totalCount = history.Count;
        var items = history.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return new PagedResult<ChatMessageContent>(items, totalCount, pageIndex, pageSize);
    }

    public virtual async Task<IReadOnlyList<ChatMessageContent>> GetRecentMessagesAsync(
        string sessionId, 
        int count, 
        CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(sessionId, cancellationToken);
        return history.TakeLast(count).ToList();
    }

    public virtual async Task<int> GetMessageCountAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(sessionId, cancellationToken);
        return history.Count;
    }

    #endregion

    #region 会话管理（默认实现）

    public virtual async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryAsync(sessionId, cancellationToken);
        return history.Count > 0;
    }

    public virtual Task<PagedResult<SessionInfo>> GetSessionsAsync(
        int pageIndex = 0, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        // 默认返回空列表，子类可重写提供实际实现
        return Task.FromResult(new PagedResult<SessionInfo>(
            Array.Empty<SessionInfo>(), 0, pageIndex, pageSize));
    }

    public virtual Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // 默认返回 null，子类可重写提供实际实现
        return Task.FromResult<SessionInfo?>(null);
    }

    public virtual Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default)
    {
        // 默认不做操作，子类可重写提供实际实现
        return Task.CompletedTask;
    }

    #endregion
}
