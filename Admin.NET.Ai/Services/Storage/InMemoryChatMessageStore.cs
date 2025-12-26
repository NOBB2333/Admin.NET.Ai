using System.Collections.Concurrent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// 内存对话存储（五星级企业标准实现）
/// 支持：会话管理、批量操作、分页查询
/// </summary>
public class InMemoryChatMessageStore : IChatMessageStore
{
    private readonly ConcurrentDictionary<string, ChatHistory> _store = new();
    private readonly ConcurrentDictionary<string, SessionMetadata> _sessionMetadata = new();

    // 内部会话元数据
    private record SessionMetadata(DateTime CreatedAt, string? Title = null)
    {
        public DateTime LastMessageAt { get; set; } = CreatedAt;
    }

    #region 基础操作

    public Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.GetOrAdd(sessionId, _ =>
        {
            _sessionMetadata.TryAdd(sessionId, new SessionMetadata(DateTime.UtcNow));
            return new ChatHistory();
        }));
    }

    public Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var history = _store.GetOrAdd(sessionId, _ =>
        {
            _sessionMetadata.TryAdd(sessionId, new SessionMetadata(DateTime.UtcNow));
            return new ChatHistory();
        });
        history.Add(message);
        
        // 更新最后消息时间
        if (_sessionMetadata.TryGetValue(sessionId, out var meta))
        {
            meta.LastMessageAt = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }

    public Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryRemove(sessionId, out _);
        _sessionMetadata.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    #endregion

    #region 批量操作

    public Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var history = _store.GetOrAdd(sessionId, _ =>
        {
            _sessionMetadata.TryAdd(sessionId, new SessionMetadata(DateTime.UtcNow));
            return new ChatHistory();
        });
        
        foreach (var message in messages)
        {
            history.Add(message);
        }
        
        if (_sessionMetadata.TryGetValue(sessionId, out var meta))
        {
            meta.LastMessageAt = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }

    public Task ReplaceHistoryAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newHistory = new ChatHistory();
        foreach (var message in messages)
        {
            newHistory.Add(message);
        }
        _store[sessionId] = newHistory;
        
        if (_sessionMetadata.TryGetValue(sessionId, out var meta))
        {
            meta.LastMessageAt = DateTime.UtcNow;
        }
        
        return Task.CompletedTask;
    }

    #endregion

    #region 分页与查询

    public Task<PagedResult<ChatMessageContent>> GetPagedHistoryAsync(
        string sessionId, 
        int pageIndex = 0, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!_store.TryGetValue(sessionId, out var history))
        {
            return Task.FromResult(new PagedResult<ChatMessageContent>(
                Array.Empty<ChatMessageContent>(), 0, pageIndex, pageSize));
        }

        var totalCount = history.Count;
        var items = history.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        
        return Task.FromResult(new PagedResult<ChatMessageContent>(items, totalCount, pageIndex, pageSize));
    }

    public Task<IReadOnlyList<ChatMessageContent>> GetRecentMessagesAsync(
        string sessionId, 
        int count, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!_store.TryGetValue(sessionId, out var history))
        {
            return Task.FromResult<IReadOnlyList<ChatMessageContent>>(Array.Empty<ChatMessageContent>());
        }

        var messages = history.TakeLast(count).ToList();
        return Task.FromResult<IReadOnlyList<ChatMessageContent>>(messages);
    }

    public Task<int> GetMessageCountAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.TryGetValue(sessionId, out var history) ? history.Count : 0);
    }

    #endregion

    #region 会话管理

    public Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.ContainsKey(sessionId));
    }

    public Task<PagedResult<SessionInfo>> GetSessionsAsync(
        int pageIndex = 0, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var allSessions = _sessionMetadata
            .Select(kvp => new SessionInfo(
                kvp.Key,
                kvp.Value.CreatedAt,
                kvp.Value.LastMessageAt,
                _store.TryGetValue(kvp.Key, out var h) ? h.Count : 0,
                kvp.Value.Title))
            .OrderByDescending(s => s.LastMessageAt)
            .ToList();

        var totalCount = allSessions.Count;
        var items = allSessions.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        
        return Task.FromResult(new PagedResult<SessionInfo>(items, totalCount, pageIndex, pageSize));
    }

    public Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!_sessionMetadata.TryGetValue(sessionId, out var meta))
        {
            return Task.FromResult<SessionInfo?>(null);
        }

        var messageCount = _store.TryGetValue(sessionId, out var h) ? h.Count : 0;
        return Task.FromResult<SessionInfo?>(new SessionInfo(
            sessionId, meta.CreatedAt, meta.LastMessageAt, messageCount, meta.Title));
    }

    public Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        _sessionMetadata.AddOrUpdate(
            sessionId,
            _ => new SessionMetadata(DateTime.UtcNow, title),
            (_, existing) => existing with { Title = title });
        
        return Task.CompletedTask;
    }

    #endregion
}

