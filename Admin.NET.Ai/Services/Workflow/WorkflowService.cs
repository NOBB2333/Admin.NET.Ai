using Admin.NET.Ai.Abstractions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Execution;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MafWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 工作流服务 - 基于 MAF (Microsoft Agent Framework)
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly ILogger<WorkflowService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkflowStateService _stateService;
    private readonly HumanInputStepHandler _humanHandler;

    public WorkflowService(
        ILogger<WorkflowService> logger,
        IServiceProvider serviceProvider,
        WorkflowStateService stateService,
        HumanInputStepHandler humanHandler)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _stateService = stateService;
        _humanHandler = humanHandler;
    }

    #region 工厂方法

    /// <summary>
    /// 创建顺序工作流
    /// </summary>
    public MafWorkflow CreateSequential(string name, params AIAgent[] agents)
    {
        if (agents.Length == 0)
            throw new ArgumentException("至少需要一个 Agent", nameof(agents));

        _logger.LogInformation("创建顺序工作流: {Name}, Agents: {Count}", name, agents.Length);

        var builder = new WorkflowBuilder(agents[0])
            .WithName(name);

        // 链式连接所有 Agent
        for (int i = 0; i < agents.Length - 1; i++)
        {
            builder.AddEdge(agents[i], agents[i + 1]);
        }

        return builder
            .WithOutputFrom(agents[^1])
            .Build();
    }

    /// <summary>
    /// 创建并发工作流 (简化版：顺序执行后汇总)
    /// 注意：MAF FanOut/FanIn API 需要特定签名，这里简化处理
    /// </summary>
    public MafWorkflow CreateParallel(string name, AIAgent[] workers, AIAgent aggregator)
    {
        if (workers.Length == 0)
            throw new ArgumentException("至少需要一个 Worker Agent", nameof(workers));

        _logger.LogInformation("创建并发工作流: {Name}, Workers: {Count}", name, workers.Length);

        // 简化实现：将所有 workers 串联，最后接 aggregator
        var allAgents = workers.Append(aggregator).ToArray();
        return CreateSequential(name, allAgents);
    }

    #endregion

    #region 执行方法

    /// <summary>
    /// 执行工作流（ChatMessage 输入）
    /// </summary>
    public async IAsyncEnumerable<WorkflowEvent> ExecuteAsync(MafWorkflow workflow, ChatMessage input)
    {
        _logger.LogInformation("执行工作流: {Name}", workflow.Name);

        await using var run = await InProcessExecution.RunStreamingAsync(workflow, input);
        
        // 发送 TurnToken 触发 Agent 执行
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (var evt in run.WatchStreamAsync())
        {
            yield return evt;
        }
    }

    /// <summary>
    /// 执行工作流（字符串输入）
    /// </summary>
    public IAsyncEnumerable<WorkflowEvent> ExecuteAsync(MafWorkflow workflow, string input)
    {
        return ExecuteAsync(workflow, new ChatMessage(ChatRole.User, input));
    }

    #endregion

    #region 自主工作流

    /// <summary>
    /// 生成并执行自主工作流
    /// </summary>
    public async IAsyncEnumerable<WorkflowEvent> ExecuteAutonomousAsync(string requirement)
    {
        _logger.LogInformation("生成自主工作流: {Requirement}", requirement);

        var aiFactory = _serviceProvider.GetRequiredService<IAiFactory>();
        var chatClient = aiFactory.GetDefaultChatClient()
            ?? throw new InvalidOperationException("Default Chat Client 未配置");

        // 1. 让 LLM 规划工作流
        var planPrompt = $@"
你是 AI 架构师。请分析以下需求并生成 JSON 工作流计划。
需求: {requirement}

仅返回 JSON，格式如下:
{{
  ""name"": ""WorkflowName"",
  ""steps"": [
    {{ ""name"": ""AgentName"", ""instructions"": ""详细指令"" }}
  ]
}}";

        var response = await chatClient.GetResponseAsync(planPrompt);
        var planJson = CleanJson(response.Text);

        _logger.LogDebug("生成的计划: {Plan}", planJson);

        // 2. 解析计划
        var plan = JsonSerializer.Deserialize<WorkflowPlan>(planJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (plan?.Steps == null || plan.Steps.Count == 0)
        {
            yield return new WorkflowErrorEvent(new Exception("计划解析失败"));
            yield break;
        }

        // 3. 创建 Agents
        var agents = plan.Steps.Select(step =>
            aiFactory.CreateDefaultAgent<ChatClientAgent>(step.Name ?? "Assistant", step.Instructions ?? "")
            ?? throw new InvalidOperationException($"创建 Agent '{step.Name}' 失败")
        ).Cast<AIAgent>().ToArray();

        // 4. 构建并执行工作流
        var workflow = CreateSequential(plan.Name ?? "Autonomous", agents);

        await foreach (var evt in ExecuteAsync(workflow, requirement))
        {
            yield return evt;
        }
    }

    #endregion

    #region Human-in-the-loop

    /// <summary>
    /// 恢复被挂起的工作流
    /// </summary>
    public async IAsyncEnumerable<WorkflowEvent> ResumeAsync(string workflowId, string humanInput)
    {
        _logger.LogInformation("恢复工作流: {Id}", workflowId);

        // 提交人工输入
        await _humanHandler.ResumeAsync(workflowId, humanInput);

        // 加载上下文
        var context = await _stateService.LoadStateAsync(workflowId);

        // 简化实现：返回恢复确认
        yield return new ExecutorCompletedEvent("System", $"工作流已恢复，输入: {humanInput}");
    }

    #endregion

    #region Helpers

    private static string CleanJson(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "{}";
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start) return text.Substring(start, end - start + 1);
        return text;
    }

    #endregion
}

#region Internal Models

internal class WorkflowPlan
{
    public string? Name { get; set; }
    public List<WorkflowStep> Steps { get; set; } = new();
}

internal class WorkflowStep
{
    public string? Name { get; set; }
    public string? Instructions { get; set; }
}

#endregion