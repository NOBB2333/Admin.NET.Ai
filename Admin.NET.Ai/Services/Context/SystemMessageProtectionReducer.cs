using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 系统消息保护器 - 实现 MEAI IChatReducer
/// 对应需求: 5.5 系统消息保护
/// 此组件通常和其他 Reducer 组合使用，确保 System 消息不被后续策略误删。
/// </summary>
public class SystemMessageProtectionReducer(IChatReducer innerReducer) : IChatReducer
{
    private readonly IChatReducer _innerReducer = innerReducer;

    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        
        // 分离系统消息
        var systemMessages = messageList.Where(m => m.Role == ChatRole.System).ToList();
        var nonSystemMessages = messageList.Where(m => m.Role != ChatRole.System).ToList();
            
        // 对非系统消息应用内部压缩策略
        var compressedNonSystem = await _innerReducer.ReduceAsync(nonSystemMessages, ct);
        
        // 合并结果：系统消息 + 压缩后的非系统消息
        var result = new List<ChatMessage>();
        result.AddRange(systemMessages);
        result.AddRange(compressedNonSystem);
        
        return result;
    }
}
