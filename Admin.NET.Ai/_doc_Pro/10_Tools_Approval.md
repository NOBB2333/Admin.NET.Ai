# 工具调用与审批流 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `ToolManager.cs` | `Services/Tools/` | 工具管理器 |
| `IToolPermissionManager.cs` | `Abstractions/` | 权限管理接口 |
| `ToolPermissionManager.cs` | `Services/Tools/` | 权限实现 |
| `IToolExecutionSandbox.cs` | `Abstractions/` | 沙箱接口 |
| `ToolExecutionSandbox.cs` | `Services/Tools/` | 沙箱实现 |
| `ToolMonitoringMiddleware.cs` | `Middleware/` | 工具监控 |
| `ToolValidationMiddleware.cs` | `Middleware/` | 工具验证 |
| `ToolDemo.cs` | `Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 工具执行流程

```
LLM 请求调用工具
    ↓
[ToolValidationMiddleware] → 参数验证
    ↓
[ToolPermissionManager] → 权限检查
    ↓
需要审批? ──Yes──→ [等待人工审批]
    │                    ↓
    No                 审批通过?
    ↓                    ↓
[ToolExecutionSandbox] ← ─┘
    ↓
执行工具
    ↓
[ToolMonitoringMiddleware] → 记录日志
    ↓
返回结果
```

---

## 🔧 核心实现

### 1. 工具管理器

```csharp
public class ToolManager
{
    private readonly Dictionary<string, AITool> _tools = new();
    private readonly McpToolFactory _mcpFactory;
    
    // 注册本地工具
    public void RegisterTool(AITool tool)
    {
        _tools[tool.Name] = tool;
    }
    
    // 注册函数作为工具
    public void RegisterFunction<T>(
        string name, 
        string description, 
        Func<T, Task<object?>> handler)
    {
        var tool = AIFunctionFactory.Create(handler, name, description);
        _tools[name] = tool;
    }
    
    // 获取所有工具 (本地 + MCP)
    public async Task<List<AITool>> GetAllToolsAsync()
    {
        var tools = _tools.Values.ToList();
        
        // 加载 MCP 工具
        var mcpTools = await _mcpFactory.LoadGlobalMcpToolsAsync();
        tools.AddRange(mcpTools);
        
        return tools;
    }
}
```

### 2. 权限管理

```csharp
public interface IToolPermissionManager
{
    Task<PermissionResult> CheckPermissionAsync(string toolName, string userId, Dictionary<string, object?> args);
    Task<bool> RequiresApprovalAsync(string toolName, Dictionary<string, object?> args);
}

public class ToolPermissionManager : IToolPermissionManager
{
    private readonly Dictionary<string, ToolPermissionConfig> _permissions;
    
    public async Task<PermissionResult> CheckPermissionAsync(
        string toolName, 
        string userId, 
        Dictionary<string, object?> args)
    {
        // 1. 检查工具是否存在
        if (!_permissions.TryGetValue(toolName, out var config))
        {
            return PermissionResult.Allowed();  // 默认允许
        }
        
        // 2. 检查用户角色
        var userRoles = await GetUserRolesAsync(userId);
        if (!config.AllowedRoles.Intersect(userRoles).Any())
        {
            return PermissionResult.Denied("用户无权限调用此工具");
        }
        
        // 3. 检查是否需要审批
        if (config.RequiresApproval)
        {
            return PermissionResult.RequiresApproval(config.ApprovalRoles);
        }
        
        return PermissionResult.Allowed();
    }
    
    public async Task<bool> RequiresApprovalAsync(string toolName, Dictionary<string, object?> args)
    {
        // 敏感操作检测
        if (toolName.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
            toolName.Contains("execute", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return _permissions.TryGetValue(toolName, out var config) && config.RequiresApproval;
    }
}
```

### 3. 沙箱执行

```csharp
public class ToolExecutionSandbox : IToolExecutionSandbox
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    
    public async Task<ToolExecutionResult> ExecuteAsync(
        AITool tool, 
        Dictionary<string, object?> args,
        CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);
        
        var sw = Stopwatch.StartNew();
        
        try
        {
            // 资源限制 (示例)
            // 实际实现可能需要进程隔离
            
            var result = await tool.InvokeAsync(args, cts.Token);
            
            sw.Stop();
            return new ToolExecutionResult
            {
                Success = true,
                Result = result,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"工具执行超时 ({_timeout.TotalSeconds}s)"
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
```

### 4. 审批流

```csharp
public class ApprovalService
{
    private readonly ConcurrentDictionary<string, ApprovalRequest> _pending = new();
    
    public async Task<string> RequestApprovalAsync(
        string toolName, 
        Dictionary<string, object?> args,
        string requestedBy)
    {
        var request = new ApprovalRequest
        {
            Id = Guid.NewGuid().ToString(),
            ToolName = toolName,
            Arguments = args,
            RequestedBy = requestedBy,
            RequestedAt = DateTime.UtcNow,
            Status = ApprovalStatus.Pending
        };
        
        _pending[request.Id] = request;
        
        // 通知审批人 (可通过 SignalR、邮件等)
        await NotifyApproversAsync(request);
        
        return request.Id;
    }
    
    public async Task<ApprovalResult> WaitForApprovalAsync(
        string requestId, 
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < deadline)
        {
            if (_pending.TryGetValue(requestId, out var request))
            {
                if (request.Status != ApprovalStatus.Pending)
                {
                    return new ApprovalResult
                    {
                        Approved = request.Status == ApprovalStatus.Approved,
                        ApprovedBy = request.ApprovedBy,
                        Comments = request.Comments
                    };
                }
            }
            
            await Task.Delay(1000);
        }
        
        return new ApprovalResult { Approved = false, Comments = "审批超时" };
    }
}
```

---

## ⚙️ 配置

```json
{
  "LLM-Tools": {
    "Permissions": {
      "delete_file": {
        "RequiresApproval": true,
        "ApprovalRoles": ["admin", "manager"],
        "AllowedRoles": ["developer", "admin"]
      },
      "execute_sql": {
        "RequiresApproval": true,
        "AllowedRoles": ["dba"]
      }
    },
    "Sandbox": {
      "TimeoutSeconds": 30,
      "MaxMemoryMB": 100
    }
  }
}
```

---

## 🚀 使用示例

```csharp
var toolManager = sp.GetRequiredService<ToolManager>();

// 注册工具
toolManager.RegisterFunction<SearchQuery>(
    "web_search",
    "搜索网页信息",
    async query => await SearchAsync(query.Query));

// 带权限检查的调用
var permission = sp.GetRequiredService<IToolPermissionManager>();
var result = await permission.CheckPermissionAsync("delete_file", userId, args);

if (result.Status == PermissionStatus.RequiresApproval)
{
    var approvalId = await approvalService.RequestApprovalAsync("delete_file", args, userId);
    var approval = await approvalService.WaitForApprovalAsync(approvalId, TimeSpan.FromMinutes(5));
    
    if (!approval.Approved)
    {
        Console.WriteLine($"审批被拒绝: {approval.Comments}");
        return;
    }
}

// 执行工具
var sandbox = sp.GetRequiredService<IToolExecutionSandbox>();
var execResult = await sandbox.ExecuteAsync(tool, args);
```
