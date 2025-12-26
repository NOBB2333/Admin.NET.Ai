using System.Collections.Concurrent;

namespace Admin.NET.Ai.Services.CQRS;

// --- 写端 (命令) ---

public class CreateAgentExecutionCommand
{
    public string AgentName { get; set; } = string.Empty;
    public string Request { get; set; } = string.Empty;
}

public class AgentCommandHandler
{
    // 模拟写存储库
    private static readonly ConcurrentDictionary<Guid, AgentExecution> _repository = new();
    
    // 模拟事件总线
    private readonly Action<object> _eventPublisher;

    public AgentCommandHandler(Action<object> eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(CreateAgentExecutionCommand command)
    {
        var id = Guid.NewGuid();
        var execution = new AgentExecution 
        { 
            Id = id, 
            AgentName = command.AgentName, 
            Request = command.Request, 
            Status = "Created",
            CreatedAt = DateTime.UtcNow 
        };

        _repository[id] = execution;
        
        // 模拟异步处理开始
        _eventPublisher(new ExecutionCreatedEvent(id));

        await Task.CompletedTask;
        return id;
    }
    
    public static AgentExecution? GetInternal(Guid id) => _repository.TryGetValue(id, out var v) ? v : null;
}

public record ExecutionCreatedEvent(Guid Id);


// --- 读端 (查询) ---

public class AgentQueryService
{
    // 在真实的 CQRS 中，这将从单独的读取优化存储 (读取模型) 中读取
    // 为简单起见，这里我们只从同一源读取，或者我们可以进行投影。
    
    public async Task<ExecutionView?> GetExecutionStatus(Guid executionId)
    {
        var exec = AgentCommandHandler.GetInternal(executionId);
        if (exec == null) return null;

        await Task.CompletedTask;
        return new ExecutionView 
        { 
            Id = exec.Id, 
            Status = exec.Status, 
            Summary = $"Agent {exec.AgentName} processing '{exec.Request}'" 
        };
    }
}

// --- 模型 ---
public class AgentExecution
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string Request { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ExecutionView
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
