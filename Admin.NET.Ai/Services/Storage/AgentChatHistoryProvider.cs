using System.Text.Json;
using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Storage;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Microsoft Agent Framework (MAF) 聊天历史提供者
/// 适配 MAF 1.0.0-preview.260127.1 的 ChatHistoryProvider API
/// 
/// 职责：作为 Agent 与持久化存储之间的桥梁
/// - InvokingAsync: 从 IAgentChatMessageStore 加载历史消息提供给 Agent
/// - InvokedAsync: LLM 调用完成后的回调
/// </summary>
public sealed class AgentChatHistoryProvider : ChatHistoryProvider
{
    private readonly IAgentChatMessageStore _persistenceStore;
    private readonly List<ChatMessage> _messages = new();
    public string ThreadDbKey { get; private set; }

    public AgentChatHistoryProvider(
        IAgentChatMessageStore persistenceStore,
        JsonElement serializedStoreState,
        string threadId,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _persistenceStore = persistenceStore ?? throw new ArgumentNullException(nameof(persistenceStore));
        ThreadDbKey = threadId;

        // 从序列化状态反序列化 threadId（如果有）
        if (serializedStoreState.ValueKind == JsonValueKind.String)
        {
            ThreadDbKey = serializedStoreState.GetString() ?? threadId;
        }
    }

    /// <summary>
    /// 在 Agent 调用 LLM 之前触发 - 从持久化存储加载历史消息
    /// 返回要添加到上下文中的消息列表
    /// </summary>
    public override async ValueTask<IEnumerable<ChatMessage>> InvokingAsync(
        InvokingContext context, 
        CancellationToken cancellationToken)
    {
        // 从数据库加载历史消息
        var data = await _persistenceStore.GetMessagesAsync(ThreadDbKey, cancellationToken);
        _messages.Clear();
        _messages.AddRange(data.Select(x => JsonSerializer.Deserialize<ChatMessage>(x.SerializedMessage!)!));

        // 返回历史消息，框架会自动将它们添加到上下文中
        return _messages;
    }

    /// <summary>
    /// 在 Agent 调用 LLM 完成后触发
    /// TODO: 检查 InvokedContext 的正确 API 来保存新消息
    /// </summary>
    public override ValueTask InvokedAsync(
        InvokedContext context, 
        CancellationToken cancellationToken)
    {
        // NOTE: 新版 API 中 InvokedContext 的结构可能已变化
        // 当前版本只做空实现，消息保存需要在调用端处理
        // 或者通过其他机制（如自定义中间件）来保存消息
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 手动添加消息到持久化存储（用于调用端显式保存）
    /// </summary>
    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        var dto = new ChatHistoryItemDto
        {
            Key = ThreadDbKey + message.MessageId,
            Timestamp = DateTimeOffset.UtcNow,
            ThreadId = ThreadDbKey,
            MessageId = message.MessageId,
            Role = message.Role.Value,
            SerializedMessage = JsonSerializer.Serialize(message),
            MessageText = message.Text
        };

        await _persistenceStore.AddMessagesAsync(new List<ChatHistoryItemDto> { dto }, cancellationToken);
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null) =>
        JsonSerializer.SerializeToElement(ThreadDbKey);
}
