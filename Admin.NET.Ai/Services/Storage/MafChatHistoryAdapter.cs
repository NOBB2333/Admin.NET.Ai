using System.Text.Json;
using Admin.NET.Ai.Abstractions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// Microsoft Agent Framework (MAF) 聊天历史提供者
/// 适配 MAF 1.0.0+ 的 ChatHistoryProvider API
/// 
/// 职责：作为 Agent 与持久化存储之间的桥梁
/// - InvokingAsync: 从 IChatMessageStore 加载历史消息提供给 Agent
/// - InvokedAsync: LLM 调用完成后的回调 (目前空实现)
/// 
/// 注意：MAF 内部也使用 Microsoft.Extensions.AI.ChatMessage，
/// 因此可以直接使用统一的 IChatMessageStore，无需额外转换。
/// </summary>
public sealed class MafChatHistoryAdapter : ChatHistoryProvider
{
    private readonly IChatMessageStore _store;
    private readonly List<ChatMessage> _messages = new();
    
    /// <summary>
    /// 线程/会话 ID (MAF 的 threadId 等同于 sessionId)
    /// </summary>
    public string ThreadId { get; private set; }

    public MafChatHistoryAdapter(
        IChatMessageStore store,
        JsonElement serializedStoreState,
        string threadId,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ThreadId = threadId;

        // 从序列化状态恢复 threadId（如果有）
        if (serializedStoreState.ValueKind == JsonValueKind.String)
        {
            ThreadId = serializedStoreState.GetString() ?? threadId;
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
        // 直接从统一的 IChatMessageStore 加载 (已经是 MEAI ChatMessage)
        var history = await _store.GetHistoryAsync(ThreadId, cancellationToken);
        _messages.Clear();
        _messages.AddRange(history);

        return _messages;
    }

    /// <summary>
    /// 在 Agent 调用 LLM 完成后触发
    /// </summary>
    public override ValueTask InvokedAsync(
        InvokedContext context, 
        CancellationToken cancellationToken)
    {
        // 消息保存由调用端处理或通过自定义中间件
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 手动添加消息到持久化存储（用于调用端显式保存）
    /// </summary>
    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await _store.SaveMessageAsync(ThreadId, message, cancellationToken);
    }

    /// <summary>
    /// 批量添加消息
    /// </summary>
    public async Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        await _store.SaveMessagesAsync(ThreadId, messages, cancellationToken);
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null) =>
        JsonSerializer.SerializeToElement(ThreadId);
}
