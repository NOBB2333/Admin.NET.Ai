using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Entity;
using Microsoft.Extensions.AI;
using SqlSugar;
using System.Text.Json;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// 数据库存储实现（MEAI-first 企业标准）
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

    public async Task<IList<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var entities = await _db.Queryable<AIChatMessage>()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedTime)
            .ToListAsync(cancellationToken);

        var history = new List<ChatMessage>();
        foreach (var entity in entities)
        {
            var role = ParseChatRole(entity.Role);
            history.Add(new ChatMessage(role, entity.Content ?? string.Empty));
        }
        return history;
    }

    public async Task SaveMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        var entity = new AIChatMessage
        {
            SessionId = sessionId,
            Role = message.Role.Value,
            Content = message.Text,
            Metadata = message.AdditionalProperties != null ? JsonSerializer.Serialize(message.AdditionalProperties) : null,
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

    public async Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        var entities = messages.Select(m => new AIChatMessage
        {
            SessionId = sessionId,
            Role = m.Role.Value,
            Content = m.Text,
            Metadata = m.AdditionalProperties != null ? JsonSerializer.Serialize(m.AdditionalProperties) : null,
        }).ToList();

        await _db.Insertable(entities).ExecuteCommandAsync(cancellationToken);
    }

    public async Task ReplaceHistoryAsync(string sessionId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.Ado.BeginTranAsync();
            
            await _db.Deleteable<AIChatMessage>()
                .Where(x => x.SessionId == sessionId)
                .ExecuteCommandAsync(cancellationToken);

            var entities = messages.Select(m => new AIChatMessage
            {
                SessionId = sessionId,
                Role = m.Role.Value,
                Content = m.Text,
                Metadata = m.AdditionalProperties != null ? JsonSerializer.Serialize(m.AdditionalProperties) : null,
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

    public async Task<PagedResult<ChatMessage>> GetPagedHistoryAsync(
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
            .Select(e => new ChatMessage(ParseChatRole(e.Role), e.Content ?? string.Empty))
            .ToList();

        return new PagedResult<ChatMessage>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(
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
            .Select(e => new ChatMessage(ParseChatRole(e.Role), e.Content ?? string.Empty))
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
        await Task.CompletedTask;
    }

    #endregion

    #region Helper

    private static ChatRole ParseChatRole(string? role)
    {
        return role?.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User
        };
    }

    #endregion
}
