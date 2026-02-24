using Microsoft.Agents.AI.Workflows;
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
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[Stream Error]: {ex.Message}");
            throw;
        }

        Console.WriteLine();
        return fullResponse.ToString();
    }

    /// <summary>
    /// [Debug] 将 MAF 工作流事件流实时打印到控制台
    /// </summary>
    public static async Task WriteToConsoleAsync(
        this IAsyncEnumerable<WorkflowEvent> events)
    {
        await foreach (var evt in events)
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent agentUpdate:
                    Console.Write(agentUpdate.Update.Text);
                    break;
                    
                case ExecutorCompletedEvent completed:
                    Console.WriteLine($"\n[✓ {completed.ExecutorId}]");
                    break;
                    
                case ExecutorFailedEvent failed:
                    Console.WriteLine($"\n[✗ {failed.ExecutorId}] {failed.Data?.Message}");
                    break;
                    
                case WorkflowOutputEvent output:
                    Console.WriteLine($"\n[最终结果 from {output.ExecutorId}]:\n{output.Data}");
                    break;
            }
        }
    }
}
