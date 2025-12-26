using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 通用工作流构建器实现
/// </summary>
public class GenericWorkflowBuilder(string name, IServiceProvider serviceProvider) : IWorkflowBuilder
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly List<AIAgent> _agents = [];

    public IWorkflowBuilder AddAgent(string name, string instructions)
    {
        var aiFactory = _serviceProvider.GetRequiredService<IAiFactory>();
        var agent = aiFactory.GetAgent<ChatClientAgent>(name, instructions);
        if (agent != null) _agents.Add(agent);
        return this;
    }

    public IWorkflowBuilder AddAgent(AIAgent agent)
    {
        if (agent != null) _agents.Add(agent);
        return this;
    }

    public IWorkflowBuilder Sequential(params string[] agentNames)
    {
        var aiFactory = _serviceProvider.GetRequiredService<IAiFactory>();
        foreach (var agentName in agentNames)
        {
            var agent = aiFactory.GetAgent<ChatClientAgent>(agentName);
            if (agent != null) _agents.Add(agent);
        }
        return this;
    }

    public AgentWorkflow Build()
    {
        // 使用 MAF Builder 构建真 Workflow
        var internalWorkflow = AgentWorkflowBuilder.BuildSequential(name, _agents.ToArray());

        return new AgentWorkflow
        {
            Name = name,
            InternalWorkflow = internalWorkflow
        };
    }
}
