using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Entity;
using Admin.NET.Ai.Models.Storage;
using SqlSugar;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// 数据库存储实现 (针对 Microsoft Agent Framework 消息)
/// </summary>
public class DatabaseAgentChatMessageStore : IAgentChatMessageStore
{
    private readonly ISqlSugarClient _db;

    public DatabaseAgentChatMessageStore(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task AddMessagesAsync(List<ChatHistoryItemDto> chatHistoryItems, CancellationToken cancellationToken)
    {
        var entities = chatHistoryItems.Select(t => new TAgentChatMessageStore
        {
            ThreadId = t.ThreadId,
            Timestamp = t.Timestamp,
            Role = t.Role,
            Key = t.Key,
            SerializedMessage = t.SerializedMessage,
            MessageText = t.MessageText,
            MessageId = t.MessageId,
            CreateTime = DateTime.Now,
            IsDelete = false
            // CreateUserId and TenantId can be set here if a context is available
        }).ToList();

        await _db.Insertable(entities).ExecuteCommandAsync();
    }

    public async Task<List<ChatHistoryItemDto>> GetMessagesAsync(string threadId, CancellationToken cancellationToken)
    {
        var entities = await _db.Queryable<TAgentChatMessageStore>()
            .Where(t => t.ThreadId == threadId && t.IsDelete == false)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(cancellationToken);

        return entities.Select(t => new ChatHistoryItemDto
        {
            Key = t.Key,
            ThreadId = t.ThreadId,
            Timestamp = t.Timestamp,
            SerializedMessage = t.SerializedMessage,
            MessageText = t.MessageText,
            Role = t.Role,
            MessageId = t.MessageId
        }).ToList();
    }
}
