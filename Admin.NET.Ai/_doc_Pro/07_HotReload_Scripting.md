# 热重载脚本引擎 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `NatashaScriptEngine.cs` | `Services/Workflow/` | 脚本引擎核心 |
| `ScriptSourceRewriter.cs` | `Services/Workflow/` | AST 重写器 (追踪) |
| `IScriptExecutor.cs` | `Abstractions/` | 脚本执行接口与上下文 |
| `ScriptContext` | `Abstractions/` | 脚本执行环境记录 |

---

## 🏗️ 架构设计

### 技术栈

- **Natasha**: 基于 Roslyn 的动态编译库
- **Roslyn**: C# 语法分析和代码生成

### 工作流程

```
C# 脚本文本
    ↓
[Roslyn 解析] → AST
    ↓
[ScriptSourceRewriter] → 注入追踪代码
    ↓
[Natasha 编译] → Assembly
    ↓
[反射执行] → 结果
```

---

## 🔧 核心实现

### 1. NatashaScriptEngine

```csharp
public class NatashaScriptEngine
{
    private readonly ConcurrentDictionary<string, CompiledScript> _scriptCache = new();
    
    public async Task<object?> ExecuteAsync(string scriptCode, IDictionary<string, object?> args, ScriptContext context)
    {
        // ... 省略缓存编译逻辑 ...
        
        // 3. 执行
        return await compiled.ExecuteAsync(args, context);
    }
    
    private async Task<CompiledScript> CompileAsync(string scriptCode)
    {
        // 使用 Natasha 编译
        var builder = new AssemblyCSharpBuilder
        {
            AssemblyName = $"Script_{Guid.NewGuid():N}"
        };
        
        // 注入追踪代码
        var rewrittenCode = RewriteForTracing(scriptCode);
        
        builder.Add(rewrittenCode);
        builder.Add(rewrittenCode);
        builder.AddReference(typeof(ScriptContext).Assembly);
        
        var assembly = builder.GetAssembly();
        var scriptType = assembly.GetTypes().First(t => t.GetMethod("Execute") != null);
        
        return new CompiledScript(scriptType);
    }
}
```

### 2. 脚本上下文

```csharp
/// <summary>
/// 脚本执行上下文 (环境)
/// </summary>
public record ScriptContext(
    IServiceProvider Services, 
    CancellationToken CancellationToken = default
)
{
    public string? CorrelationId { get; init; } 
    
    // 可观测性/追踪上下文 (可选)
    public IScriptExecutionContext? ExecutionContext { get; init; }
}

/// <summary>
/// 追踪上下文 (包含执行树)
/// </summary>
public interface IScriptExecutionContext
{
    ScriptStepInfo RootStep { get; }
    IScriptStepScope BeginStep(string name, object? input = null);
    void SetOutput(object? output);
    void SetError(Exception ex);
}
```

### 3. AST 重写器 (追踪注入)

```csharp
public class ScriptSourceRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // 在每个方法开头注入追踪代码
        var tracingStatement = SyntaxFactory.ParseStatement(
            $"context.Log(\"Entering {node.Identifier}\");");
        
        var newBody = node.Body?.WithStatements(
            node.Body.Statements.Insert(0, tracingStatement));
        
        return node.WithBody(newBody);
    }
    
    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
    {
        // 记录返回值
        if (node.Expression != null)
        {
            var logging = SyntaxFactory.ParseStatement(
                $"var __result = {node.Expression}; context.Log($\"Return: {{__result}}\"); return __result;");
            return logging;
        }
        return base.VisitReturnStatement(node);
    }
}
```

---

## 📝 脚本模板

### 基础脚本

```csharp
public class MyScript : IScriptExecutor
{
    public ScriptMetadata GetMetadata() => new ScriptMetadata("Basic", "1.0");

    public async Task<object?> ExecuteAsync(IDictionary<string, object?> input, ScriptContext context)
    {
        var name = input["name"]?.ToString() ?? "World";
        var greeting = $"Hello, {name}!";
        return greeting;
    }
}
```

### Agent 脚本

```csharp
public class AgentScript : IScriptExecutor
{
    public ScriptMetadata GetMetadata() => new ScriptMetadata("AgentScript", "1.0");

    public async Task<object?> ExecuteAsync(IDictionary<string, object?> input, ScriptContext context)
    {
        var aiFactory = context.Services.GetRequiredService<IAiFactory>();
        var client = aiFactory.GetDefaultChatClient();
        
        var prompt = input["prompt"]?.ToString() ?? "";
        // var response = await client.RunAsync(prompt); // 假设有扩展方法
        
        return "AI Response Placeholder";
    }
}
```

---

## 🔄 热重载机制

```csharp
public class ScriptWatcher
{
    private readonly FileSystemWatcher _watcher;
    private readonly NatashaScriptEngine _engine;
    
    public ScriptWatcher(string scriptsPath, NatashaScriptEngine engine)
    {
        _engine = engine;
        _watcher = new FileSystemWatcher(scriptsPath, "*.cs");
        _watcher.Changed += OnScriptChanged;
        _watcher.EnableRaisingEvents = true;
    }
    
    private async void OnScriptChanged(object sender, FileSystemEventArgs e)
    {
        // 脚本文件变化时，清除缓存并重新编译
        var scriptCode = await File.ReadAllTextAsync(e.FullPath);
        var hash = ComputeHash(scriptCode);
        
        // 强制重新编译
        _engine.InvalidateCache(hash);
        
        Console.WriteLine($"[HotReload] Script updated: {e.Name}");
    }
}
```

---

## 🚀 使用示例

```csharp
var engine = sp.GetRequiredService<NatashaScriptEngine>();

var script = @"
using Admin.NET.Ai.Abstractions;
public class Calculator : IScriptExecutor
{
    public ScriptMetadata GetMetadata() => new ScriptMetadata(""Calc"", ""1.0"");
    public async Task<object?> ExecuteAsync(IDictionary<string, object?> input, ScriptContext context)
    {
        var a = (int)input[""a""];
        var b = (int)input[""b""];
        return a + b;
    }
}";

// 1. 加载
var executors = engine.LoadScripts(new[] { script });

// 2. 准备上下文
var traceContext = new ScriptExecutionContext("CalcRun");
var ctx = new ScriptContext(sp) { ExecutionContext = traceContext };
var args = new Dictionary<string, object?> { ["a"] = 10, ["b"] = 20 };

// 3. 执行
var result = await executors.First().ExecuteAsync(args, ctx);
Console.WriteLine($"Result: {result}");  // 30
```

---

## ⚠️ 注意事项

1. **安全性**: 脚本可执行任意代码，需要沙箱隔离
2. **性能**: 首次编译较慢，后续走缓存
3. **依赖**: 需要 `dotnet-isolated-sdk` 或完整 SDK
4. **调试**: 使用 `context.Log()` 追踪执行过程
