using System.Text.Json;
using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Storage;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Microsoft Agent Framework (MAF) 聊天消息存储实现
/// </summary>
public sealed class AgentChatMessageStore : ChatMessageStore
{
    private readonly IAgentChatMessageStore _persistenceStore;
    public string ThreadDbKey { get; private set; }

    public AgentChatMessageStore(
        IAgentChatMessageStore persistenceStore,
        JsonElement serializedStoreState,
        string threadId,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _persistenceStore = persistenceStore ?? throw new ArgumentNullException(nameof(persistenceStore));
        this.ThreadDbKey = threadId;
    }

    public override async Task AddMessagesAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var dtos = messages.Select(x => new ChatHistoryItemDto
        {
            Key = this.ThreadDbKey + x.MessageId,
            Timestamp = DateTimeOffset.UtcNow,
            ThreadId = this.ThreadDbKey,
            MessageId = x.MessageId,
            Role = x.Role.Value,
            SerializedMessage = JsonSerializer.Serialize(x),
            MessageText = x.Text
        }).ToList();

        await _persistenceStore.AddMessagesAsync(dtos, cancellationToken);
    }

    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        CancellationToken cancellationToken)
    {
        var data = await _persistenceStore.GetMessagesAsync(this.ThreadDbKey, cancellationToken);
        var messages = data.ConvertAll(x => JsonSerializer.Deserialize<ChatMessage>(x.SerializedMessage!)!);
        
        // Reverse if stored in descending order, but DatabaseAgentChatMessageStore uses OrderBy(Timestamp)
        // MAF usually expects chronological order.
        return messages;
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null) =>
        JsonSerializer.SerializeToElement(this.ThreadDbKey);
}
