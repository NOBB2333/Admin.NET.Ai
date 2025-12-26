using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Execution;
using System.Collections.Generic;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
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

    // (原 IWorkflowService 方法保持不变 ...)

    /// <summary>
    /// 恢复被挂起的工作流
    /// </summary>
    public async IAsyncEnumerable<AiWorkflowEvent> ResumeWorkflowAsync(string workflowId, string humanInput)
    {
        _logger.LogInformation("🔄 [Workflow] 尝试恢复工作流: {Id}", workflowId);

        // 1. 提交输入，更新状态
        await _humanHandler.ResumeAsync(workflowId, humanInput);
        
        // 2. 加载上下文
        var context = await _stateService.LoadStateAsync(workflowId);
        
        // 3. 这里有一个挑战：原生的 MAF 工作流对象无法直接序列化/反序列化恢复执行栈。
        // 对于这一版实现，我们采用 "Restart with State" 策略：
        // 重新构建工作流，但注入已有的历史记录，并跳过已完成的步骤 (这需要复杂的引擎支持)
        // 
        // 简单实现：将 humanInput 作为新的 User Message 发送给工作流。
        // 这适用于 "Human in the loop" 是作为一个 Response 等待的场景。

        // 假设我们知道这是哪个定义
        // TODO: 在 Context 中保存 WorkflowName
        var workflowName = "Autonomous"; // 暂定，需从 Context 读取
        
    // 4. 继续执行 (模拟)
        // 实际项目需要更复杂的 State Machine
        yield return new AiAgentRunUpdateEvent { AgentName = "System", Step = "Resumed with Input" };
        yield return new AiWorkflowOutputEvent { Output = $"Resumed with input: {humanInput}" };
    }

    public IWorkflowBuilder CreateConcurrentBuilder(string name)
    {
         // 暂时复用 GenericWorkflowBuilder，实际可能需要不同的 Builder 实现
        return new GenericWorkflowBuilder(name, _serviceProvider);
    }

    public IWorkflowBuilder CreateHandoffBuilder(string name)
    {
        return new GenericWorkflowBuilder(name, _serviceProvider);
    }

    public IWorkflowBuilder CreateSequentialBuilder(string name)
    {
        return new GenericWorkflowBuilder(name, _serviceProvider);
    }

    public IWorkflowBuilder CreateGroupChatBuilder(string name)

    {
        return new GenericWorkflowBuilder(name, _serviceProvider);
    }

    /// <summary>
    /// --- 执行方法 (Execution Methods) ---
    /// </summary>
    /// <param name="workflowName"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public async IAsyncEnumerable<AiWorkflowEvent> ExecuteWorkflowAsync(string workflowName, object input)
    {
        _logger.LogInformation("正在执行工作流: {Name}", workflowName);

        var workflowDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "Workflows");
        var filePath = Path.Combine(workflowDir, $"{workflowName}.json");

        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            var definition = JsonSerializer.Deserialize<WorkflowDefinition>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (definition != null)
            {
                var workflow = BuildFromDefinition(definition);
                var messages = new List<ChatMessage> { new(ChatRole.User, input.ToString() ?? string.Empty) };
                var run = await InProcessExecution.StreamAsync(workflow, messages);
                // await foreach (var @event in (IAsyncEnumerable<Microsoft.Agents.AI.Workflows.WorkflowEvent>)run)
                // {
                //     if (@event is AiWorkflowEvent wfEvent) yield return wfEvent;
                // }
                yield break;
            }
        }

        throw new FileNotFoundException($"工作流 '{workflowName}' 未找到。");
    }

    // --- 自主生成逻辑 (Autonomous Logic - 核心需求) ---

    public async IAsyncEnumerable<AiWorkflowEvent> ExecuteAutonomousWorkflowAsync(string requirement)
    {
        _logger.LogInformation("正在为需求生成自主工作流: {Requirement}", requirement);

        // 1. 获取 ChatClient (replaced Kernel)
        var aiFactory = _serviceProvider.GetRequiredService<IAiFactory>();
        var chatClient = aiFactory.GetDefaultChatClient() ?? throw new Exception("Default Chat Client 未配置");

        // 2. Prompt 工程: 生成计划
        var prompt = $@"
            你是一位专家级 AI 架构师。
            请分析以下需求并生成一个 JSON 工作流计划。
            需求: {requirement}

            仅返回 JSON。格式如下:
            {{
              ""type"": ""Sequential"" | ""concurrent"" | ""groupchat"",
              ""steps"": [
                {{ ""name"": ""AgentName"", ""role"": ""RoleDescription"", ""instructions"": ""Detailed Instructions"" }}
              ]
            }}
            ";
            
        // 使用 IChatClient 执行
        var response = await chatClient.GetResponseAsync(new List<ChatMessage> { new(ChatRole.User, prompt) });
        var jsonPlan = CleanJson(response.Messages.Count > 0 ? response.Messages[0].Text : string.Empty);

        _logger.LogInformation("生成的计划: {Plan}", jsonPlan);

        // 3. 解析并构建
        var plan = JsonSerializer.Deserialize<WorkflowDefinition>(jsonPlan,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (plan == null) throw new Exception("生成的计划解析失败。");

        // 4. 构建工作流 (使用真 MAF Builder)
        var agents = plan.Steps.Select(s => 
        {
             // Use CreateDefaultAgent to create a dynamic agent backed by the default LLM.
             // This avoids the issue where "AgentName" (e.g. "SoftwareEngineer") is not a valid Client Config name.
             return aiFactory.CreateDefaultAgent<ChatClientAgent>(s.AgentName ?? "Assistant", s.Content)
                    ?? throw new Exception($"Failed to create dynamic agent: {s.AgentName}");
        }).ToList();

        // 5. 执行
        _logger.LogInformation("正在执行生成的工作流 (Real MAF Execution)...");
        
        // 使用 MAF 原生的顺序流构建
        var workflow = AgentWorkflowBuilder.BuildSequential(plan.Name ?? "Autonomous", agents.Cast<AIAgent>().ToArray());

        // 执行器 (InProcessExecution)
        var messages = new List<ChatMessage> { new(ChatRole.User, requirement) };
        var run = await InProcessExecution.StreamAsync(workflow, messages);
        
        // await foreach (var @event in (IAsyncEnumerable<Microsoft.Agents.AI.Workflows.WorkflowEvent>)run)
        // {
        //     if (@event is AiWorkflowEvent wfEvent)
        //         yield return wfEvent;
        //     else if (@event.ToString().Contains("Output")) // 粗略兼容
        //         yield return new AiWorkflowOutputEvent { Output = @event.ToString() };
        // }
        yield break;
    }

    // --- 辅助方法 (Helpers) ---

    private Microsoft.Agents.AI.Workflows.Workflow BuildFromDefinition(WorkflowDefinition def)
    {
        var aiFactory = _serviceProvider.GetRequiredService<IAiFactory>();
        
        // 1. 创建 Agents
        // 1. 创建 Agents
        var agents = def.Steps.Select(step => 
        {
            // First try to get a specifically configured agent (e.g. "Researcher" with its own API key)
            // If not found, create a dynamic agent using the default client.
            var agent = aiFactory.GetAgent<ChatClientAgent>(step.AgentName ?? "Default", step.Content);
            if (agent == null)
            {
                 // Fallback: Dynamic Agent
                 agent = aiFactory.CreateDefaultAgent<ChatClientAgent>(step.AgentName ?? "Assistant", step.Content);
            }
            return agent ?? throw new InvalidOperationException($"Cannot resolve or create agent '{step.AgentName}'");
        }).ToList();

        // 2. 根据类型选择 Builder
        return def.Type.ToString().ToLower() switch
        {
            "concurrent" => AgentWorkflowBuilder.BuildConcurrent(def.Name, agents.Cast<AIAgent>().ToArray()),
            _ => AgentWorkflowBuilder.BuildSequential(def.Name, agents.Cast<AIAgent>().ToArray())
        };
    }

    private string CleanJson(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "{}";
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start) return text.Substring(start, end - start + 1);
        return text;
    }
}