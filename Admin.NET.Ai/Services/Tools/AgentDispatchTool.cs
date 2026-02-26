using System.ComponentModel;
using System.Text;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Tools;

/// <summary>
/// Agent 调度工具 — LLM 看到所有可用 Agent 后自行决定调用哪个
/// 将已注册的 IAiAgent 实例暴露为一个统一的 Function Calling 工具
/// </summary>
public class AgentDispatchTool : IAiCallableFunction
{
    private readonly IAiFactory _aiFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentDispatchTool> _logger;
    private List<IAiAgent>? _agents;

    public string Name => "AgentDispatch";
    public string Description => "调用专业 Agent 完成子任务";
    public ToolExecutionContext? Context { get; set; }

    public AgentDispatchTool(
        IAiFactory aiFactory,
        IServiceProvider serviceProvider,
        ILogger<AgentDispatchTool> logger)
    {
        _aiFactory = aiFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IEnumerable<AIFunction> GetFunctions()
    {
        var agents = GetRegisteredAgents();
        if (agents.Count == 0)
        {
            _logger.LogDebug("No registered agents found, AgentDispatchTool will not expose functions");
            yield break;
        }

        // 构建 Agent 列表描述，让 LLM 看到所有可用 Agent
        var agentDescriptions = string.Join("\n", agents.Select(a =>
            $"  - {a.Name}: {a.Capability}"));

        var description = $"调用专业 Agent 完成子任务。可用 Agent:\n{agentDescriptions}";

        yield return AIFunctionFactory.Create(
            CallAgent,
            "call_agent",
            description);
    }

    /// <summary>
    /// 调用指定 Agent 执行子任务
    /// </summary>
    [Description("调用指定的专业 Agent 完成子任务")]
    private async Task<string> CallAgent(
        [Description("Agent 名称（从可用列表中选择）")] string agentName,
        [Description("详细的任务描述，告诉 Agent 需要完成什么")] string task,
        CancellationToken ct = default)
    {
        var agents = GetRegisteredAgents();
        var agent = agents.FirstOrDefault(a =>
            a.Name.Equals(agentName, StringComparison.OrdinalIgnoreCase));

        if (agent == null)
        {
            var available = string.Join(", ", agents.Select(a => a.Name));
            return $"[错误] 未找到 Agent '{agentName}'。可用的 Agent: {available}";
        }

        _logger.LogInformation("Dispatching task to Agent '{AgentName}': {Task}",
            agentName, task.Length > 100 ? task[..100] + "..." : task);

        try
        {
            // 获取 ChatClient
            var chatClient = _aiFactory.GetDefaultChatClient();
            if (chatClient == null)
                return "[错误] 无法获取 ChatClient";

            // 构建受限的工具集
            var toolManager = new ToolManager(_serviceProvider,
                Microsoft.Extensions.Logging.LoggerFactory.Create(b => { }).CreateLogger<ToolManager>());
            var tools = new List<AIFunction>();

            if (agent.AllowedTools != null)
            {
                // 只提供允许的工具
                foreach (var toolName in agent.AllowedTools)
                {
                    var func = toolManager.GetFunction(toolName);
                    if (func != null) tools.Add(func);
                }
            }

            // 构建消息
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, agent.Instructions),
                new(ChatRole.User, task)
            };

            // 配置选项
            var options = new ChatOptions();
            if (tools.Count > 0)
            {
                options.Tools = tools.Cast<AITool>().ToList();
            }

            // 运行 Agent Loop（带迭代限制）
            var result = new StringBuilder();
            var iterations = 0;
            var maxIterations = agent.MaxIterations;

            while (iterations < maxIterations)
            {
                iterations++;
                ct.ThrowIfCancellationRequested();

                var response = await chatClient.GetResponseAsync(messages, options, ct);

                // 收集文本输出
                var text = response.Text;
                if (!string.IsNullOrWhiteSpace(text))
                    result.AppendLine(text);

                // 检查是否有工具调用
                var toolCalls = response.Messages
                    .Where(m => m.Role == ChatRole.Assistant)
                    .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                    .ToList();

                if (toolCalls.Count == 0)
                    break; // 没有工具调用，Agent 完成

                // 执行工具调用并追加结果
                foreach (var msg in response.Messages)
                    messages.Add(msg);

                foreach (var toolCall in toolCalls)
                {
                    var func = tools.FirstOrDefault(t => t.Name == toolCall.Name);
                    if (func != null)
                    {
                        try
                        {
                            var toolResult = await func.InvokeAsync(
                                new AIFunctionArguments(toolCall.Arguments ?? new Dictionary<string, object?>()), ct);
                            messages.Add(new ChatMessage(ChatRole.Tool,
                                [new FunctionResultContent(toolCall.CallId, toolResult)]));
                        }
                        catch (Exception ex)
                        {
                            messages.Add(new ChatMessage(ChatRole.Tool,
                                [new FunctionResultContent(toolCall.CallId, $"工具执行失败: {ex.Message}")]));
                        }
                    }
                }
            }

            var output = result.ToString().Trim();
            if (string.IsNullOrWhiteSpace(output))
                output = "(Agent 未产生文本输出)";

            _logger.LogInformation("Agent '{AgentName}' completed in {Iterations} iterations",
                agentName, iterations);

            return $"[Agent: {agentName}] (迭代 {iterations}/{maxIterations})\n{output}";
        }
        catch (OperationCanceledException)
        {
            return $"[Agent: {agentName}] 任务已取消";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent '{AgentName}' execution failed", agentName);
            return $"[Agent: {agentName}] 执行失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 获取所有已注册的 Agent 实例
    /// 策略: 先尝试 DI 解析（处理 keyed service 等复杂依赖），失败再用 ActivatorUtilities
    /// </summary>
    private List<IAiAgent> GetRegisteredAgents()
    {
        if (_agents != null) return _agents;

        _agents = new List<IAiAgent>();

        // 扫描所有已加载的程序集中的 IAiAgent 实现
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !a.FullName!.StartsWith("System") && !a.FullName.StartsWith("Microsoft"))
            .ToList();

        foreach (var assembly in assemblies)
        {
            try
            {
                var agentTypes = assembly.GetTypes()
                    .Where(t => typeof(IAiAgent).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

                foreach (var type in agentTypes)
                {
                    try
                    {
                        // 优先通过 DI 解析（支持 keyed service、scoped 等复杂注册）
                        var agent = _serviceProvider.GetService(type) as IAiAgent;
                        
                        if (agent == null)
                        {
                            // 回退到 ActivatorUtilities
                            agent = Microsoft.Extensions.DependencyInjection.ActivatorUtilities
                                .CreateInstance(_serviceProvider, type) as IAiAgent;
                        }

                        if (agent != null)
                        {
                            _agents.Add(agent);
                            _logger.LogDebug("Discovered Agent: {AgentName} ({Type})", agent.Name, type.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not instantiate agent type '{TypeName}', skipping", type.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not scan assembly '{Assembly}' for agents", assembly.FullName);
            }
        }

        _logger.LogInformation("AgentDispatchTool discovered {Count} agents: {Names}",
            _agents.Count, string.Join(", ", _agents.Select(a => a.Name)));

        return _agents;
    }
}
