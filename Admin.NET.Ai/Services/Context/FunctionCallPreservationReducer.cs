using Admin.NET.Ai.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 函数调用上下文保护缩减器
/// 对应需求: 5.6 函数调用消息保护
/// 确保 ToolCall 和 ToolResult 成对出现，且尽量保留其上下文
/// </summary>
public class FunctionCallPreservationReducer : IChatReducer
{
    private const int ContextWindow = 2; // 保留前后2条消息作为上下文

    public Task<IEnumerable<ChatMessageContent>> ReduceAsync(
        IEnumerable<ChatMessageContent> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        
        // 识别需要保护的消息集合
        var preservedSet = new HashSet<ChatMessageContent>();
        
        // 1. 系统消息总是保护
        foreach (var sysMsg in messageList.Where(m => m.Role == AuthorRole.System))
        {
            preservedSet.Add(sysMsg);
        }

        // 2. 识别函数调用链
        for (int i = 0; i < messageList.Count; i++)
        {
            var msg = messageList[i];
            bool isToolCall = msg.Items.Any(item => item is FunctionCallContent);
            bool isToolResult = msg.Items.Any(item => item is FunctionResultContent);

            if (isToolCall || isToolResult)
            {
                // 保留自身
                preservedSet.Add(msg);
                
                // 保留上下文 (前 N 后 N)
                // 注意边界检查
                for (int offset = 1; offset <= ContextWindow; offset++)
                {
                    if (i - offset >= 0) preservedSet.Add(messageList[i - offset]);
                    if (i + offset < messageList.Count) preservedSet.Add(messageList[i + offset]);
                }
            }
        }
        
        // 3. 重组消息 (按原始顺序)
        var result = messageList.Where(m => preservedSet.Contains(m)).ToList();
        
        // 如果压缩后消息依然过多，或者需要填充常规消息，通常由组合策略中的其他 Reducer 补充 Recent Messages。
        // 本 Reducer 只负责 Output "Must Have" 的 Function 相关消息。
        // 但为了符合接口契约（返回一个可用的列表），我们如果发现列表太短，可能需要补充一些近期消息？
        // 简便起见，本组件只负责“筛选出必须保留的”。若外部需要追加 Recent，应使用 CompositeReducer。
        // 这里我们默认把最近的 N 条也加上，防止只剩下 Function Call 而没有最新的用户问题。
        
        var recent = messageList.TakeLast(5); // 兜底保留最近5条
        foreach (var r in recent)
        {
            if (!result.Contains(r))
            {
                result.Add(r);
            }
        }

        // 最终排序
        // 重新按 Index 排序比较稳妥
        var finalResult = messageList.Where(m => result.Contains(m)).ToList();

        return Task.FromResult<IEnumerable<ChatMessageContent>>(finalResult);
    }
}
