using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 边界检查中间件 (示例)
/// </summary>
public class BoundaryCheckMiddleware : IRunMiddleware
{
    // Define forbidden keywords or rules
    private static readonly string[] OutOfBoundaryKeywords = new[]
    {
        "投资建议", "法律意见", "医疗诊断", "政治观点", "Stock Advice", "Medical Diagnosis"
    };

    public async Task<ChatResponse> InvokeAsync(RunMiddlewareContext context, NextRunMiddleware next)
    {
        var messages = context.Messages.ToList();
        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;

        if (!string.IsNullOrWhiteSpace(lastUserMessage) && IsOutOfBoundary(lastUserMessage))
        {
            // Return rejection response without calling LLM
            return new ChatResponse(new[] 
            { 
                new ChatMessage(ChatRole.Assistant, "抱歉，这个问题超出了我的专业范围。 (Boundary Check Triggered)") 
            });
        }

        return await next(context);
    }
    
    private bool IsOutOfBoundary(string message)
    {
        return OutOfBoundaryKeywords.Any(keyword => 
            message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
