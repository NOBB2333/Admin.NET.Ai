using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 系统消息保护器
/// 对应需求: 5.5 系统消息保护
/// 此组件通常和其他 Reducer 组合使用，确保 System 消息不被后续策略误删。
/// 但作为一个独立 Reducer，它实际上是执行“保留 System，压缩其他”的逻辑外壳。
/// 在组合模式下，它可能不是必需的，因为好的 Reducer 都会自觉保留 System。
/// 这里实现为一个过滤器的装饰模式可能更合适，或者作为一个独立的逻辑。
//  这里依照需求文档，实现先分离 System 再压缩其他的逻辑框架。
/// </summary>
public class SystemMessageProtectionReducer(IChatReducer innerReducer) : IChatReducer
{
    private readonly IChatReducer _innerReducer = innerReducer;

    public async Task<IEnumerable<ChatMessageContent>> ReduceAsync(IEnumerable<ChatMessageContent> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        
        // 分离系统消息
        var systemMessages = messageList
            .Where(m => m.Role == AuthorRole.System)
            .ToList();
            
        // 分离非系统消息
        var nonSystemMessages = messageList
            .Where(m => m.Role != AuthorRole.System)
            .ToList();
            
        // 对非系统消息应用内部压缩策略
        var compressedNonSystem = await _innerReducer.ReduceAsync(nonSystemMessages, ct);
        
        // 合并结果：系统消息 + 压缩后的非系统消息
        var result = new List<ChatMessageContent>();
        result.AddRange(systemMessages);
        result.AddRange(compressedNonSystem);
        
        // 注意：这里简单合并，可能通过时间排序更好，但 System 通常在最前。
        return result;
    }
}
