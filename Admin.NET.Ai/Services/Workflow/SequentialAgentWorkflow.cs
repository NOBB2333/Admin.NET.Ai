using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 顺序执行的代理工作流实现 (层级结构)
/// </summary>
public class SequentialAgentWorkflow : AgentWorkflow
{
    private readonly IEnumerable<string> _agentNames;
    private readonly IServiceProvider _serviceProvider;

    public SequentialAgentWorkflow(string name, IEnumerable<string> agentNames, IServiceProvider serviceProvider)
    {
        this.Name = name;
        this._agentNames = agentNames;
        this._serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 重写流式输出，实现真实的顺序执行逻辑
    /// </summary>
    public async IAsyncEnumerable<AiWorkflowEvent> WatchStreamAsync(string input)
    {
        var aiFactory = _serviceProvider.GetRequiredService<IAiFactory>();
        string currentContent = input;

        foreach (var agentName in _agentNames)
        {
            yield return new AiAgentRunUpdateEvent { AgentName = agentName, Step = "Processing" };

            var client = aiFactory.GetChatClient(agentName);
            if (client == null)
            {
                yield return new AiWorkflowOutputEvent { Output = $"Error: Agent '{agentName}' not found." };
                yield break;
            }

            // 执行当前层
            var response = await client.GetResponseAsync(currentContent);
            currentContent = response.Messages.Count > 0 ? response.Messages[0].Text : string.Empty;

            yield return new AiAgentRunUpdateEvent { AgentName = agentName, Step = "Completed" };
        }

        yield return new AiWorkflowOutputEvent { Output = currentContent };
    }
}
