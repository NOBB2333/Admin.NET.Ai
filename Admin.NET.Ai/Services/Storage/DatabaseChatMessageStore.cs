using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Entity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SqlSugar;
using System.Text.Json;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// 数据库存储实现（五星级企业标准）
/// 支持：批量操作、分页、会话管理
/// </summary>
public class DatabaseChatMessageStore : IChatMessageStore
{
    private readonly ISqlSugarClient _db; 
    
    public DatabaseChatMessageStore(ISqlSugarClient db)
    {
        _db = db;
    }

    #region 基础操作

    public async Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var entities = await _db.Queryable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedTime)
            .ToListAsync(cancellationToken);

        var history = new ChatHistory();
        foreach (var entity in entities)
        {
            if (Enum.TryParse<AuthorRole>(entity.Role, true, out var role))
            {
                history.AddMessage(role, entity.Content ?? string.Empty);
            }
        }
        return history;
    }

    public async Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default)
    {
        var entity = new AIChatMessage
        {
            SessionId = sessionId,
            Role = message.Role.ToString(),
            Content = message.Content,
            Metadata = message.Metadata != null ? JsonSerializer.Serialize(message.Metadata) : null,
        };
        
        await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
    }

    public async Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _db.Deleteable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .ExecuteCommandAsync(cancellationToken);
    }

    #endregion

    #region 批量操作

    public async Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken cancellationToken = default)
    {
        var entities = messages.Select(m => new AIChatMessage
        {
            SessionId = sessionId,
            Role = m.Role.ToString(),
            Content = m.Content,
            Metadata = m.Metadata != null ? JsonSerializer.Serialize(m.Metadata) : null,
        }).ToList();

        await _db.Insertable(entities).ExecuteCommandAsync(cancellationToken);
    }

    public async Task ReplaceHistoryAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken cancellationToken = default)
    {
        // 使用事务确保原子性
        try
        {
            await _db.Ado.BeginTranAsync();
            
            await _db.Deleteable<AIChatMessage>()
                .Where(x => x.SessionId == sessionId)
                .ExecuteCommandAsync(cancellationToken);

            var entities = messages.Select(m => new AIChatMessage
            {
                SessionId = sessionId,
                Role = m.Role.ToString(),
                Content = m.Content,
                Metadata = m.Metadata != null ? JsonSerializer.Serialize(m.Metadata) : null,
            }).ToList();

            if (entities.Any())
            {
                await _db.Insertable(entities).ExecuteCommandAsync(cancellationToken);
            }

            await _db.Ado.CommitTranAsync();
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    #endregion

    #region 分页与查询

    public async Task<PagedResult<ChatMessageContent>> GetPagedHistoryAsync(
        string sessionId, 
        int pageIndex = 0, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _db.Queryable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .CountAsync(cancellationToken);

        var entities = await _db.Queryable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedTime)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities
            .Where(e => Enum.TryParse<AuthorRole>(e.Role, true, out _))
            .Select(e => new ChatMessageContent(
                Enum.Parse<AuthorRole>(e.Role!, true), 
                e.Content ?? string.Empty))
            .ToList();

        return new PagedResult<ChatMessageContent>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetRecentMessagesAsync(
        string sessionId, 
        int count, 
        CancellationToken cancellationToken = default)
    {
        var entities = await _db.Queryable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CreatedTime)
            .Take(count)
            .ToListAsync(cancellationToken);

        return entities
            .OrderBy(e => e.CreatedTime)
            .Where(e => Enum.TryParse<AuthorRole>(e.Role, true, out _))
            .Select(e => new ChatMessageContent(
                Enum.Parse<AuthorRole>(e.Role!, true), 
                e.Content ?? string.Empty))
            .ToList();
    }

    public async Task<int> GetMessageCountAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .CountAsync(cancellationToken);
    }

    #endregion

    #region 会话管理

    public async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .AnyAsync(cancellationToken);
    }

    public async Task<PagedResult<SessionInfo>> GetSessionsAsync(
        int pageIndex = 0, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        // 使用分组查询获取会话信息
        var query = _db.Queryable<AIChatMessage>()
            .GroupBy(x => x.SessionId)
            .Select(x => new
            {
                SessionId = x.SessionId,
                MessageCount = SqlFunc.AggregateCount(x.Id),
                CreatedAt = SqlFunc.AggregateMin(x.CreatedTime),
                LastMessageAt = SqlFunc.AggregateMax(x.CreatedTime)
            });

        var totalCount = await query.CountAsync(cancellationToken);
        
        var sessionData = await query
            .OrderByDescending(x => x.LastMessageAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = sessionData.Select(s => new SessionInfo(
            s.SessionId!,
            s.CreatedAt,
            s.LastMessageAt,
            s.MessageCount
        )).ToList();

        return new PagedResult<SessionInfo>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var sessionData = await _db.Queryable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .GroupBy(x => x.SessionId)
            .Select(x => new
            {
                SessionId = x.SessionId,
                MessageCount = SqlFunc.AggregateCount(x.Id),
                CreatedAt = SqlFunc.AggregateMin(x.CreatedTime),
                LastMessageAt = SqlFunc.AggregateMax(x.CreatedTime)
            })
            .FirstAsync(cancellationToken);

        if (sessionData == null) return null;

        return new SessionInfo(
            sessionData.SessionId!,
            sessionData.CreatedAt,
            sessionData.LastMessageAt,
            sessionData.MessageCount
        );
    }

    public async Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default)
    {
        // 注意：当前 AIChatMessage 实体可能没有 Title 字段
        // 如果需要此功能，需要添加独立的 Session 表或在消息表中添加字段
        // 这里暂时不做任何操作，仅作为接口占位
        await Task.CompletedTask;
    }

    #endregion
}


