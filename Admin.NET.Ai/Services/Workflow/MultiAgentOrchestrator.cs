using Microsoft.Extensions.AI;


namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 真正的多 Agent 协作引擎 - 支持线程隔离、轮流发言、Token 优化。这是单一llm提供商 有同质化问题
/// </summary>
public class MultiAgentOrchestrator
{
    private readonly IChatClient _chatClient;
    private readonly List<AgentParticipant> _participants = new();
    private readonly MultiAgentOptions _options;

    public MultiAgentOrchestrator(IChatClient chatClient, MultiAgentOptions? options = null)
    {
        _chatClient = chatClient;
        _options = options ?? new MultiAgentOptions();
    }

    /// <summary>
    /// 获取 ChatClient (供子类使用)
    /// </summary>
    protected IChatClient GetChatClient() => _chatClient;

    /// <summary>
    /// 获取所有参与者 (供子类使用)
    /// </summary>
    protected IReadOnlyList<AgentParticipant> GetParticipants() => _participants;



    /// <summary>
    /// 注册参与者 Agent (独立线程上下文)
    /// </summary>
    public MultiAgentOrchestrator AddAgent(string name, string systemPrompt, string? personality = null)
    {
        _participants.Add(new AgentParticipant
        {
            Name = name,
            SystemPrompt = systemPrompt,
            Personality = personality ?? "",
            // 每个 Agent 独立的对话历史 (Thread Isolation)
            ConversationHistory = new List<ChatMessage>()
        });
        return this;
    }

    /// <summary>
    /// 运行讨论 - 流式输出，Agent 轮流发言
    /// </summary>
    public async IAsyncEnumerable<DiscussionEvent> RunDiscussionAsync(
        string topic,
        int rounds = 3,
        CancellationToken ct = default)
    {
        // 共享上下文 (精简版，只保留要点)
        var sharedContext = new SharedContext { Topic = topic };

        yield return new DiscussionEvent
        {
            Type = DiscussionEventType.Started,
            Content = $"讨论开始: {topic}\n参与者: {string.Join(", ", _participants.Select(p => p.Name))}"
        };

        for (int round = 1; round <= rounds; round++)
        {
            yield return new DiscussionEvent
            {
                Type = DiscussionEventType.RoundStarted,
                RoundNumber = round,
                Content = $"\n=== 第 {round} 轮 ===\n"
            };

            foreach (var agent in _participants)
            {
                // 1. 构建此 Agent 的上下文 (Token 优化: 只传递摘要而非全量历史)
                var messages = BuildAgentContext(agent, sharedContext, round);

                yield return new DiscussionEvent
                {
                    Type = DiscussionEventType.AgentSpeaking,
                    AgentName = agent.Name,
                    Content = $"【{agent.Name}】正在思考..."
                };

                // 2. 流式获取响应
                var responseBuilder = new System.Text.StringBuilder();
                
                await foreach (var chunk in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
                {
                    foreach (var textContent in chunk.Contents.OfType<TextContent>())
                    {
                        responseBuilder.Append(textContent.Text);
                        yield return new DiscussionEvent
                        {
                            Type = DiscussionEventType.StreamingContent,
                            AgentName = agent.Name,
                            Content = textContent.Text ?? ""
                        };
                    }
                }

                var fullResponse = responseBuilder.ToString();

                // 3. 保存到该 Agent 的独立历史 (Thread Isolation)
                agent.ConversationHistory.Add(new ChatMessage(ChatRole.Assistant, fullResponse));

                // 4. 生成摘要添加到共享上下文 (Token 优化)
                var summary = ExtractKeyPoints(fullResponse, _options.MaxSummaryLength);
                sharedContext.AddPoint(agent.Name, round, summary);

                yield return new DiscussionEvent
                {
                    Type = DiscussionEventType.AgentCompleted,
                    AgentName = agent.Name,
                    Content = $"\n[{agent.Name} 发言完毕]\n"
                };

                // 5. 等待其他 Agent "听完" (模拟真实讨论节奏)
                await Task.Delay(_options.DelayBetweenAgentsMs, ct);
            }
        }

        // 生成总结
        yield return new DiscussionEvent
        {
            Type = DiscussionEventType.Summarizing,
            Content = "\n=== 讨论总结 ===\n"
        };

        await foreach (var chunk in GenerateSummaryAsync(sharedContext, ct))
        {
            yield return new DiscussionEvent
            {
                Type = DiscussionEventType.StreamingContent,
                AgentName = "主持人",
                Content = chunk
            };
        }

        yield return new DiscussionEvent
        {
            Type = DiscussionEventType.Completed,
            Content = "\n--- 讨论结束 ---"
        };
    }

    /// <summary>
    /// 运行任务分配模式 - 编排者分配任务，各 Agent 独立执行
    /// </summary>
    public async IAsyncEnumerable<TaskExecutionEvent> RunTaskAllocationAsync(
        string requirement,
        CancellationToken ct = default)
    {
        // Step 1: 编排者分析并拆分任务
        yield return new TaskExecutionEvent
        {
            Type = TaskEventType.Analyzing,
            Content = "编排者正在分析需求..."
        };

        var taskPlan = await AnalyzeAndSplitTasksAsync(requirement, ct);

        yield return new TaskExecutionEvent
        {
            Type = TaskEventType.TasksAllocated,
            Content = $"已生成 {taskPlan.Count} 个子任务",
            Tasks = taskPlan
        };

        // Step 2: 并行执行所有任务 (真正的多线程/多任务)
        var executions = taskPlan.Select(async task =>
        {
            var agent = _participants.FirstOrDefault(p => p.Name == task.AssignedAgent);
            if (agent == null)
            {
                // 动态创建 Agent
                agent = new AgentParticipant
                {
                    Name = task.AssignedAgent,
                    SystemPrompt = $"你是一位{task.AssignedAgent}。{task.Instruction}",
                    ConversationHistory = new List<ChatMessage>()
                };
            }

            var result = await ExecuteAgentTaskAsync(agent, task, ct);
            return (Task: task, Result: result);
        });

        // 收集结果 (使用 Channel 实时推送)
        var completedTasks = new List<(AgentTask Task, string Result)>();
        await foreach (var result in RunConcurrentlyAsync(executions, ct))
        {
            completedTasks.Add(result);
            yield return new TaskExecutionEvent
            {
                Type = TaskEventType.TaskCompleted,
                AgentName = result.Task.AssignedAgent,
                Content = result.Result.Length > 200 
                    ? result.Result.Substring(0, 200) + "..." 
                    : result.Result
            };
        }

        // Step 3: 汇总结果
        yield return new TaskExecutionEvent
        {
            Type = TaskEventType.Summarizing,
            Content = "\n编排者正在汇总所有结果..."
        };

        await foreach (var chunk in GenerateTaskSummaryAsync(requirement, completedTasks, ct))
        {
            yield return new TaskExecutionEvent
            {
                Type = TaskEventType.StreamingContent,
                Content = chunk
            };
        }

        yield return new TaskExecutionEvent
        {
            Type = TaskEventType.Completed,
            Content = "\n--- 任务执行完成 ---"
        };
    }

    #region Private Methods

    private List<ChatMessage> BuildAgentContext(AgentParticipant agent, SharedContext shared, int currentRound)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, $"{agent.SystemPrompt}\n\n{agent.Personality}\n\n请根据议题和之前的讨论发表你的观点（100字以内）。当前是第{currentRound}轮讨论。")
        };

        // Token 优化: 只添加精简的共享上下文摘要，而不是全量历史
        if (shared.Points.Any())
        {
            var contextSummary = string.Join("\n", 
                shared.Points.TakeLast(_options.MaxContextPoints)
                    .Select(p => $"[{p.AgentName} R{p.Round}]: {p.Summary}"));
            
            messages.Add(new ChatMessage(ChatRole.User, $"讨论背景:\n{contextSummary}\n\n议题: {shared.Topic}"));
        }
        else
        {
            messages.Add(new ChatMessage(ChatRole.User, $"议题: {shared.Topic}"));
        }

        return messages;
    }

    private async Task<List<AgentTask>> AnalyzeAndSplitTasksAsync(string requirement, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"你是项目编排专家。将用户需求拆分为3-5个可独立执行的子任务。
返回JSON数组格式:
[
    {""agent"": ""角色名"", ""task"": ""任务描述"", ""instruction"": ""执行指令""}
]
只返回JSON，不要其他内容。"),
            new(ChatRole.User, requirement)
        };

        var response = await _chatClient.GetResponseAsync(messages, new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json
        }, ct);

        var json = response.Messages.LastOrDefault()?.Text ?? "[]";
        
        try
        {
            var tasks = System.Text.Json.JsonSerializer.Deserialize<List<TaskDefinition>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<TaskDefinition>();

            return tasks.Select((t, i) => new AgentTask
            {
                Id = i + 1,
                AssignedAgent = t.Agent ?? $"Agent-{i}",
                Description = t.Task ?? "",
                Instruction = t.Instruction ?? ""
            }).ToList();
        }
        catch
        {
            return new List<AgentTask> { new() { Id = 1, AssignedAgent = "默认Agent", Description = requirement } };
        }
    }

    private async Task<string> ExecuteAgentTaskAsync(AgentParticipant agent, AgentTask task, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, agent.SystemPrompt),
            new(ChatRole.User, $"请完成以下任务:\n{task.Description}\n\n{task.Instruction}")
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Messages.LastOrDefault()?.Text ?? "";
    }

    private async IAsyncEnumerable<(AgentTask Task, string Result)> RunConcurrentlyAsync(
        IEnumerable<Task<(AgentTask, string)>> tasks,
        CancellationToken ct)
    {
        var taskList = tasks.ToList();
        while (taskList.Count > 0)
        {
            var completed = await Task.WhenAny(taskList);
            taskList.Remove(completed);
            yield return await completed;
        }
    }

    private async IAsyncEnumerable<string> GenerateSummaryAsync(SharedContext context, CancellationToken ct)
    {
        var allPoints = string.Join("\n", context.Points.Select(p => $"[{p.AgentName}]: {p.Summary}"));
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "你是讨论主持人，请总结各方观点，给出平衡的结论。"),
            new(ChatRole.User, $"议题: {context.Topic}\n\n各方观点:\n{allPoints}")
        };

        await foreach (var chunk in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            foreach (var text in chunk.Contents.OfType<TextContent>())
            {
                yield return text.Text ?? "";
            }
        }
    }

    private async IAsyncEnumerable<string> GenerateTaskSummaryAsync(
        string requirement, 
        List<(AgentTask Task, string Result)> results, 
        CancellationToken ct)
    {
        var summary = string.Join("\n\n", results.Select(r => $"【{r.Task.AssignedAgent}】:\n{r.Result}"));
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "你是项目总监，请整合所有专家的方案，给出完整的执行摘要。"),
            new(ChatRole.User, $"原始需求: {requirement}\n\n各专家成果:\n{summary}")
        };

        await foreach (var chunk in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            foreach (var text in chunk.Contents.OfType<TextContent>())
            {
                yield return text.Text ?? "";
            }
        }
    }

    private string ExtractKeyPoints(string response, int maxLength)
    {
        // 简单截取要点 (实际可用 LLM 生成摘要)
        if (response.Length <= maxLength) return response;
        
        // 尝试在句号处截断
        var truncated = response.Substring(0, maxLength);
        var lastPeriod = truncated.LastIndexOfAny(new[] { '。', '！', '？', '.', '!' });
        return lastPeriod > 0 ? truncated.Substring(0, lastPeriod + 1) + "..." : truncated + "...";
    }

    #endregion
}

#region Models

/// <summary>
/// 多 Agent 配置选项
/// </summary>
public class MultiAgentOptions
{
    /// <summary>
    /// 摘要最大长度 (Token 优化)
    /// </summary>
    public int MaxSummaryLength { get; set; } = 150;

    /// <summary>
    /// 上下文保留的观点数量
    /// </summary>
    public int MaxContextPoints { get; set; } = 6;

    /// <summary>
    /// Agent 发言间隔 (ms)
    /// </summary>
    public int DelayBetweenAgentsMs { get; set; } = 500;
}

/// <summary>
/// 参与讨论的 Agent
/// </summary>
public class AgentParticipant
{
    public string Name { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public string Personality { get; set; } = "";
    
    /// <summary>
    /// 独立的对话历史 (Thread Isolation)
    /// </summary>
    public List<ChatMessage> ConversationHistory { get; set; } = new();
}

/// <summary>
/// 共享上下文 (精简版)
/// </summary>
public class SharedContext
{
    public string Topic { get; set; } = "";
    public List<ContextPoint> Points { get; } = new();

    public void AddPoint(string agentName, int round, string summary)
    {
        Points.Add(new ContextPoint { AgentName = agentName, Round = round, Summary = summary });
    }
}

public class ContextPoint
{
    public string AgentName { get; set; } = "";
    public int Round { get; set; }
    public string Summary { get; set; } = "";
}

/// <summary>
/// 讨论事件
/// </summary>
public class DiscussionEvent
{
    public DiscussionEventType Type { get; set; }
    public string? AgentName { get; set; }
    public int RoundNumber { get; set; }
    public string Content { get; set; } = "";
}

public enum DiscussionEventType
{
    Started,
    RoundStarted,
    AgentSpeaking,
    ToolCalling,
    ToolResult,
    StreamingContent,
    AgentCompleted,
    Summarizing,
    Completed
}


/// <summary>
/// 任务执行事件
/// </summary>
public class TaskExecutionEvent
{
    public TaskEventType Type { get; set; }
    public string? AgentName { get; set; }
    public string Content { get; set; } = "";
    public List<AgentTask>? Tasks { get; set; }
}

public enum TaskEventType
{
    Analyzing,
    TasksAllocated,
    TaskCompleted,
    Summarizing,
    StreamingContent,
    Completed
}

public class AgentTask
{
    public int Id { get; set; }
    public string AssignedAgent { get; set; } = "";
    public string Description { get; set; } = "";
    public string Instruction { get; set; } = "";
}

public class TaskDefinition
{
    public int Id { get; set; }
    public string? Agent { get; set; }
    public string? Task { get; set; }
    public string? Instruction { get; set; }
}


#endregion
