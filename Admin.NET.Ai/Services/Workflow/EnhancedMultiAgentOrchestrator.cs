using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 增强版多 Agent 协作引擎 - 支持多供应商、工具调用、线程隔离。这是多llm提供商 不会有同质化问题
/// </summary>
public class EnhancedMultiAgentOrchestrator
{
    private readonly IAiFactory _aiFactory;
    private readonly List<EnhancedAgentParticipant> _participants = new();
    private readonly EnhancedAgentOptions _options;
    private readonly IServiceProvider? _serviceProvider;

    public EnhancedMultiAgentOrchestrator(
        IAiFactory aiFactory, 
        EnhancedAgentOptions? options = null,
        IServiceProvider? serviceProvider = null)
    {
        _aiFactory = aiFactory;
        _options = options ?? new EnhancedAgentOptions();
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 获取 AiFactory (供子类使用)
    /// </summary>
    protected IAiFactory GetAiFactory() => _aiFactory;

    /// <summary>
    /// 获取所有参与者 (供子类使用)
    /// </summary>
    protected IReadOnlyList<EnhancedAgentParticipant> GetParticipants() => _participants;

    /// <summary>
    /// 注册 Agent - 可指定不同的 LLM 供应商
    /// </summary>
    /// <param name="name">Agent 名称</param>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="provider">LLM 供应商 (null=默认, e.g. "qwen", "deepseek", "gemini", "grok")</param>
    /// <param name="personality">个性描述</param>
    /// <param name="tools">可用工具列表</param>
    public EnhancedMultiAgentOrchestrator AddAgent(
        string name, 
        string systemPrompt, 
        string? provider = null,
        string? personality = null,
        IEnumerable<AgentTool>? tools = null)
    {
        var chatClient = provider != null 
            ? _aiFactory.GetChatClient(provider) 
            : _aiFactory.GetDefaultChatClient();

        _participants.Add(new EnhancedAgentParticipant
        {
            Name = name,
            Provider = provider ?? _aiFactory.DefaultProvider ?? "default",
            SystemPrompt = systemPrompt,
            Personality = personality ?? "",
            ChatClient = chatClient!,
            Tools = tools?.ToList() ?? new List<AgentTool>(),
            ConversationHistory = new List<ChatMessage>()
        });
        return this;
    }

    /// <summary>
    /// 添加搜索工具给 Agent
    /// </summary>
    public EnhancedMultiAgentOrchestrator WithSearchTool(string agentName, Func<string, Task<string>> searchFunc)
    {
        var agent = _participants.FirstOrDefault(p => p.Name == agentName);
        if (agent != null)
        {
            agent.Tools.Add(new AgentTool
            {
                Name = "web_search",
                Description = "搜索互联网获取最新信息",
                ExecuteAsync = searchFunc
            });
        }
        return this;
    }

    /// <summary>
    /// 添加知识库检索工具给 Agent
    /// </summary>
    public EnhancedMultiAgentOrchestrator WithRagTool(string agentName, Func<string, Task<string>> ragFunc)
    {
        var agent = _participants.FirstOrDefault(p => p.Name == agentName);
        if (agent != null)
        {
            agent.Tools.Add(new AgentTool
            {
                Name = "knowledge_base",
                Description = "从知识库中检索相关信息",
                ExecuteAsync = ragFunc
            });
        }
        return this;
    }

    /// <summary>
    /// 添加 MCP 工具给 Agent
    /// </summary>
    public EnhancedMultiAgentOrchestrator WithMcpTool(string agentName, string toolName, Func<string, Task<string>> mcpFunc)
    {
        var agent = _participants.FirstOrDefault(p => p.Name == agentName);
        if (agent != null)
        {
            agent.Tools.Add(new AgentTool
            {
                Name = $"mcp_{toolName}",
                Description = $"调用 MCP 工具: {toolName}",
                ExecuteAsync = mcpFunc
            });
        }
        return this;
    }

    /// <summary>
    /// 运行多供应商讨论 - 流式输出
    /// </summary>
    public async IAsyncEnumerable<EnhancedDiscussionEvent> RunDiscussionAsync(
        string topic,
        int rounds = 3,
        CancellationToken ct = default)
    {
        var sharedContext = new SharedContext { Topic = topic };

        // 显示参与者信息
        var participantInfo = string.Join("\n", _participants.Select(p => 
            $"  - {p.Name} (Provider: {p.Provider}, Tools: {(p.Tools.Any() ? string.Join(", ", p.Tools.Select(t => t.Name)) : "无")})"));

        yield return new EnhancedDiscussionEvent
        {
            Type = DiscussionEventType.Started,
            Content = $"讨论开始: {topic}\n\n参与者:\n{participantInfo}"
        };

        for (int round = 1; round <= rounds; round++)
        {
            yield return new EnhancedDiscussionEvent
            {
                Type = DiscussionEventType.RoundStarted,
                RoundNumber = round,
                Content = $"\n=== 第 {round} 轮 ===\n"
            };

            foreach (var agent in _participants)
            {
                yield return new EnhancedDiscussionEvent
                {
                    Type = DiscussionEventType.AgentSpeaking,
                    AgentName = agent.Name,
                    Provider = agent.Provider,
                    Content = $"【{agent.Name}】({agent.Provider}) 正在思考..."
                };

                // 1. 如果 Agent 有工具，先调用工具获取数据
                string toolResults = "";
                if (agent.Tools.Any() && round == 1) // 第一轮时调用工具收集信息
                {
                    foreach (var tool in agent.Tools)
                    {
                        yield return new EnhancedDiscussionEvent
                        {
                            Type = DiscussionEventType.ToolCalling,
                            AgentName = agent.Name,
                            ToolName = tool.Name,
                            Content = $"[{agent.Name}] 调用工具: {tool.Name}"
                        };

                        string toolResultContent;
                        try
                        {
                            var result = await tool.ExecuteAsync(topic);
                            toolResults += $"\n[{tool.Name}结果]: {result}";
                            toolResultContent = $"[{tool.Name}] 返回: {(result.Length > 100 ? result.Substring(0, 100) + "..." : result)}";
                        }
                        catch (Exception ex)
                        {
                            toolResultContent = $"[{tool.Name}] 错误: {ex.Message}";
                        }
                        
                        yield return new EnhancedDiscussionEvent
                        {
                            Type = DiscussionEventType.ToolResult,
                            AgentName = agent.Name,
                            ToolName = tool.Name,
                            Content = toolResultContent
                        };
                    }
                }


                // 2. 构建上下文
                var messages = BuildAgentContext(agent, sharedContext, round, toolResults);

                // 3. 流式获取响应
                var responseBuilder = new System.Text.StringBuilder();
                
                await foreach (var chunk in agent.ChatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
                {
                    foreach (var textContent in chunk.Contents.OfType<TextContent>())
                    {
                        responseBuilder.Append(textContent.Text);
                        yield return new EnhancedDiscussionEvent
                        {
                            Type = DiscussionEventType.StreamingContent,
                            AgentName = agent.Name,
                            Provider = agent.Provider,
                            Content = textContent.Text ?? ""
                        };
                    }
                }

                var fullResponse = responseBuilder.ToString();

                // 4. 保存到独立历史
                agent.ConversationHistory.Add(new ChatMessage(ChatRole.Assistant, fullResponse));

                // 5. 生成摘要添加到共享上下文
                var summary = ExtractKeyPoints(fullResponse, _options.MaxSummaryLength);
                sharedContext.AddPoint(agent.Name, round, summary);

                yield return new EnhancedDiscussionEvent
                {
                    Type = DiscussionEventType.AgentCompleted,
                    AgentName = agent.Name,
                    Provider = agent.Provider,
                    Content = $"\n[{agent.Name} 发言完毕]"
                };

                await Task.Delay(_options.DelayBetweenAgentsMs, ct);
            }
        }

        // 生成总结 (使用默认客户端)
        yield return new EnhancedDiscussionEvent
        {
            Type = DiscussionEventType.Summarizing,
            Content = "\n=== 讨论总结 ===\n"
        };

        var defaultClient = _aiFactory.GetDefaultChatClient()!;
        var allPoints = string.Join("\n", sharedContext.Points.Select(p => $"[{p.AgentName}]: {p.Summary}"));
        var summaryMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "你是讨论主持人，请总结各方观点，给出平衡的结论。"),
            new(ChatRole.User, $"议题: {topic}\n\n各方观点:\n{allPoints}")
        };

        await foreach (var chunk in defaultClient.GetStreamingResponseAsync(summaryMessages, cancellationToken: ct))
        {
            foreach (var text in chunk.Contents.OfType<TextContent>())
            {
                yield return new EnhancedDiscussionEvent
                {
                    Type = DiscussionEventType.StreamingContent,
                    AgentName = "主持人",
                    Content = text.Text ?? ""
                };
            }
        }

        yield return new EnhancedDiscussionEvent
        {
            Type = DiscussionEventType.Completed,
            Content = "\n--- 讨论结束 ---"
        };
    }

    /// <summary>
    /// 运行任务分配 - 支持工具调用
    /// </summary>
    public async IAsyncEnumerable<TaskExecutionEvent> RunTaskAllocationAsync(
        string requirement,
        CancellationToken ct = default)
    {
        var defaultClient = _aiFactory.GetDefaultChatClient()!;

        yield return new TaskExecutionEvent
        {
            Type = TaskEventType.Analyzing,
            Content = "编排者正在分析需求..."
        };

        // 分析任务
        var taskPlan = await AnalyzeTasksAsync(defaultClient, requirement, ct);

        yield return new TaskExecutionEvent
        {
            Type = TaskEventType.TasksAllocated,
            Content = $"已生成 {taskPlan.Count} 个子任务",
            Tasks = taskPlan.Select(t => new AgentTask
            {
                Id = t.Id,
                AssignedAgent = t.Agent,
                Description = t.Task,
                Instruction = t.Instruction
            }).ToList()
        };

        // 并行执行
        var executions = taskPlan.Select(async task =>
        {
            // 为每个任务选择合适的供应商
            var providers = _aiFactory.GetAvailableClients();
            var provider = providers.Count > 1 
                ? providers[task.Id % providers.Count] 
                : providers.FirstOrDefault();

            var client = provider != null 
                ? _aiFactory.GetChatClient(provider) 
                : defaultClient;

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, $"你是{task.Agent}。{task.Instruction}"),
                new(ChatRole.User, $"请完成任务:\n{task.Task}")
            };

            var response = await client!.GetResponseAsync(messages, cancellationToken: ct);
            return (Task: task, Provider: provider ?? "default", Result: response.Messages.LastOrDefault()?.Text ?? "");
        });

        var taskList = executions.ToList();
        var results = new List<(TaskDefinition Task, string Provider, string Result)>();

        while (taskList.Count > 0)
        {
            var completed = await Task.WhenAny(taskList);
            taskList.Remove(completed);
            var result = await completed;
            results.Add(result);

            yield return new TaskExecutionEvent
            {
                Type = TaskEventType.TaskCompleted,
                AgentName = $"{result.Task.Agent} ({result.Provider})",
                Content = result.Result.Length > 200 ? result.Result.Substring(0, 200) + "..." : result.Result
            };
        }

        // 汇总
        yield return new TaskExecutionEvent
        {
            Type = TaskEventType.Summarizing,
            Content = "\n编排者正在汇总..."
        };

        var summary = string.Join("\n\n", results.Select(r => $"【{r.Task.Agent}】:\n{r.Result}"));
        var summaryMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "你是项目总监，整合所有方案给出执行摘要。"),
            new(ChatRole.User, $"需求: {requirement}\n\n各专家成果:\n{summary}")
        };

        await foreach (var chunk in defaultClient.GetStreamingResponseAsync(summaryMessages, cancellationToken: ct))
        {
            foreach (var text in chunk.Contents.OfType<TextContent>())
            {
                yield return new TaskExecutionEvent
                {
                    Type = TaskEventType.StreamingContent,
                    Content = text.Text ?? ""
                };
            }
        }

        yield return new TaskExecutionEvent
        {
            Type = TaskEventType.Completed,
            Content = "\n--- 任务完成 ---"
        };
    }

    #region Private Methods

    private List<ChatMessage> BuildAgentContext(
        EnhancedAgentParticipant agent, 
        SharedContext shared, 
        int currentRound,
        string toolResults)
    {
        var systemContent = $"{agent.SystemPrompt}\n\n{agent.Personality}";
        
        if (!string.IsNullOrEmpty(toolResults))
        {
            systemContent += $"\n\n你可以参考以下工具调用的结果:\n{toolResults}";
        }
        
        systemContent += $"\n\n请发表你的观点（{_options.MaxResponseLength}字以内）。当前是第{currentRound}轮讨论。";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemContent)
        };

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

    private async Task<List<TaskDefinition>> AnalyzeTasksAsync(
        IChatClient client, 
        string requirement, 
        CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"你是项目编排专家。将需求拆分为3-5个子任务。
返回JSON数组:
[{""id"": 1, ""agent"": ""角色"", ""task"": ""任务"", ""instruction"": ""指令""}]
只返回JSON。"),
            new(ChatRole.User, requirement)
        };

        var response = await client.GetResponseAsync(messages, new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json
        }, ct);

        var json = response.Messages.LastOrDefault()?.Text ?? "[]";
        
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<TaskDefinition>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<TaskDefinition>();
        }
        catch
        {
            return new List<TaskDefinition> 
            { 
                new() { Id = 1, Agent = "默认Agent", Task = requirement } 
            };
        }
    }

    private string ExtractKeyPoints(string response, int maxLength)
    {
        if (response.Length <= maxLength) return response;
        var truncated = response.Substring(0, maxLength);
        var lastPeriod = truncated.LastIndexOfAny(new[] { '。', '！', '？', '.', '!' });
        return lastPeriod > 0 ? truncated.Substring(0, lastPeriod + 1) + "..." : truncated + "...";
    }

    #endregion
}

#region Enhanced Models

/// <summary>
/// 增强版 Agent 选项
/// </summary>
public class EnhancedAgentOptions
{
    public int MaxSummaryLength { get; set; } = 150;
    public int MaxContextPoints { get; set; } = 6;
    public int MaxResponseLength { get; set; } = 150;
    public int DelayBetweenAgentsMs { get; set; } = 300;
}

/// <summary>
/// 增强版 Agent 参与者
/// </summary>
public class EnhancedAgentParticipant
{
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public string Personality { get; set; } = "";
    public IChatClient ChatClient { get; set; } = null!;
    public List<AgentTool> Tools { get; set; } = new();
    public List<ChatMessage> ConversationHistory { get; set; } = new();
}

/// <summary>
/// Agent 工具
/// </summary>
public class AgentTool
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Func<string, Task<string>> ExecuteAsync { get; set; } = _ => Task.FromResult("");
}

/// <summary>
/// 增强版讨论事件
/// </summary>
public class EnhancedDiscussionEvent
{
    public DiscussionEventType Type { get; set; }
    public string? AgentName { get; set; }
    public string? Provider { get; set; }
    public string? ToolName { get; set; }
    public int RoundNumber { get; set; }
    public string Content { get; set; } = "";
}

#endregion

