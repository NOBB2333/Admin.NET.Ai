using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Context;

/// <summary>
/// 函数调用上下文保护缩减器 - 实现 MEAI IChatReducer
/// 对应需求: 5.6 函数调用消息保护
/// 确保 ToolCall 和 ToolResult 成对出现，且尽量保留其上下文
/// </summary>
public class FunctionCallPreservationReducer : IChatReducer
{
    private const int ContextWindow = 2;

    public Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        var preservedSet = new HashSet<ChatMessage>();
        
        // 1. 系统消息总是保护
        foreach (var sysMsg in messageList.Where(m => m.Role == ChatRole.System))
        {
            preservedSet.Add(sysMsg);
        }

        // 2. 识别函数调用链
        for (int i = 0; i < messageList.Count; i++)
        {
            var msg = messageList[i];
            bool isToolCall = msg.Contents.Any(item => item is FunctionCallContent);
            bool isToolResult = msg.Contents.Any(item => item is FunctionResultContent);

            if (isToolCall || isToolResult)
            {
                // 保留自身
                preservedSet.Add(msg);
                
                // 保留上下文 (前 N 后 N)
                for (int offset = 1; offset <= ContextWindow; offset++)
                {
                    if (i - offset >= 0) preservedSet.Add(messageList[i - offset]);
                    if (i + offset < messageList.Count) preservedSet.Add(messageList[i + offset]);
                }
            }
        }
        
        // 3. 重组消息 (按原始顺序)
        var result = messageList.Where(m => preservedSet.Contains(m)).ToList();
        
        // 兜底保留最近5条
        var recent = messageList.TakeLast(5);
        foreach (var r in recent)
        {
            if (!result.Contains(r))
            {
                result.Add(r);
            }
        }

        // 最终排序
        var finalResult = messageList.Where(m => result.Contains(m)).ToList();

        return Task.FromResult<IEnumerable<ChatMessage>>(finalResult);
    }
}
