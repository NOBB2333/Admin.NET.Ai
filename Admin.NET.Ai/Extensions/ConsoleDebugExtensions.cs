using Microsoft.Extensions.AI;
using System.Text;

namespace Admin.NET.Ai.Extensions;

public static class ConsoleDebugExtensions
{
    /// <summary>
    /// [Debug] 将流式响应实时打印到控制台，并返回完整内容
    /// </summary>
    public static async Task<string> WriteToConsoleAsync(
        this IAsyncEnumerable<ChatResponseUpdate> updates, 
        string prefix = "\n[AI 响应]: ", 
        bool showUsage = true)
    {
        Console.Write(prefix);
        var fullResponse = new StringBuilder();

        try 
        {
            await foreach (var update in updates)
            {
                if (update.Text != null)
                {
                    Console.Write(update.Text);
                    fullResponse.Append(update.Text);
                }
                
                // 这里还可以处理 update.AdditionalProperties 中的 Token 使用量等元数据
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[Stream Error]: {ex.Message}");
            throw;
        }

        Console.WriteLine(); // 换行
        
        if (showUsage)
        {
             // 如果有 TraceId 或其他上下文，可以在这里扩展打印
             // Console.WriteLine($"[统计信息] Length: {fullResponse.Length}");
        }

        return fullResponse.ToString();
    }

    /// <summary>
    /// [Debug] 将工作流事件流实时打印到控制台
    /// </summary>
    public static async Task WriteToConsoleAsync(
        this IAsyncEnumerable<Admin.NET.Ai.Models.Workflow.AiWorkflowEvent> events)
    {
        await foreach (var @event in events)
        {
            if (@event is Admin.NET.Ai.Models.Workflow.AiAgentRunUpdateEvent update)
                Console.WriteLine($"[Agent {update.AgentName}] {update.Step}...");
            else if (@event is Admin.NET.Ai.Models.Workflow.AiWorkflowOutputEvent output)
                Console.WriteLine($"\n[最终结果]:\n{output.Output}");
            else if (@event is Admin.NET.Ai.Models.Workflow.AiWorkflowErrorEvent error)
                Console.WriteLine($"[错误] {error.ErrorMessage}");
        }
    }
}
