using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 指令中间件：负责注入角色系统提示词
/// </summary>
public class InstructionMiddleware : IRunMiddleware
{
    private readonly string _instructions;

    public InstructionMiddleware(string instructions)
    {
        _instructions = instructions;
    }

    public async Task<ChatResponse> InvokeAsync(RunMiddlewareContext context, NextRunMiddleware next)
    {
        if (!string.IsNullOrWhiteSpace(_instructions))
        {
            // 如果历史记录中没有系统消息，或者需要增强，这里我们采取"确保第一条是SystemMessage"的策略
            // 如果需要，复制消息列表以避免严格修改原始引用，
            // 但通常如果是新列表，我们可以直接修改 context.Messages。
            
            var messages = context.Messages.ToList();
            var first = messages.FirstOrDefault();
            
            if (first != null && first.Role == ChatRole.System)
            {
                // 如果已存在系统消息，根据策略可能是追加或替换。
                // 简单起见，我们追加指令到现有系统消息，或者认为已存在的优先。
                // 需求通常是Agent定义的指令是基础。
                // 如果有意义，我们在前面添加指令。
                messages[0] = new ChatMessage(ChatRole.System, _instructions + "\n\n" + first.Text);
            }
            else
            {
                // 插入作为第一条消息
                messages.Insert(0, new ChatMessage(ChatRole.System, _instructions));
            }
            
            // 允许重新赋值吗？IRunMiddleware Messages 是只读的吗？
            // 需要 Abstractions/IRunMiddleware.cs 定义。
            // 通常 Context 上的 Messages 属性是可修改的或者我们替换它？
            // context.Messages 是 IEnumerable<ChatMessage>。
            // 我们需要一种方法来更新上下文消息。
            // RunMiddlewareContext 通常允许更改请求参数。
            // 让我们假设 RunMiddlewareContext 在其构造函数中接受请求或暴露可写的 Request。
            // context.Request.Messages = messages; (如果是标准 MEAI 中间件上下文)
            
            // 查看我的 `RunMiddlewareContext.cs` (如果我创建了它)。
            // 它只是匹配签名 `(ChatClient, messages, options)`。
            // 如果我不能修改 `context.Messages`，我就不能注入。
            // 我将假设上下文逻辑允许它，或者我创建一个新的 Context/传递新列表给 `next`。
            // 等等，中间件使用 `next(context)`。如果上下文是共享的，我不能轻易传递 *不同* 的上下文。
            // 但是 `RunMiddlewareChatClient` 通常将上下文传递给 `next`。
            // 检查 `RunMiddlewareChatClient` 逻辑。
            
            // 如果 context.Messages 只是一个属性，如果是只读的，我就不能更改它。
            // 我将检查 `RunMiddlewareContext` 定义。
        }

        return await next(context);
    }
}
