# 热重载脚本引擎 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `NatashaScriptEngine.cs` | `Services/Workflow/` | 脚本引擎核心 |
| `ScriptSourceRewriter.cs` | `Services/Workflow/` | AST 重写器 (追踪) |
| `IScriptContext.cs` | `Abstractions/` | 脚本上下文接口 |
| `ScriptingDemo.cs` | `Demos/` | 演示代码 |

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
    
    public async Task<object?> ExecuteAsync(string scriptCode, IScriptContext context)
    {
        // 1. 计算脚本 Hash (用于缓存)
        var hash = ComputeHash(scriptCode);
        
        // 2. 检查缓存
        if (!_scriptCache.TryGetValue(hash, out var compiled))
        {
            compiled = await CompileAsync(scriptCode);
            _scriptCache[hash] = compiled;
        }
        
        // 3. 执行
        return await compiled.ExecuteAsync(context);
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
        builder.AddReference(typeof(IScriptContext).Assembly);
        
        var assembly = builder.GetAssembly();
        var scriptType = assembly.GetTypes().First(t => t.GetMethod("Execute") != null);
        
        return new CompiledScript(scriptType);
    }
}
```

### 2. 脚本上下文

```csharp
public interface IScriptContext
{
    // 输入参数
    Dictionary<string, object?> Input { get; }
    
    // 输出结果
    Dictionary<string, object?> Output { get; }
    
    // 服务访问
    IServiceProvider Services { get; }
    
    // 追踪记录
    List<TraceEntry> Traces { get; }
    
    // 日志
    void Log(string message);
}

public class ScriptContext : IScriptContext
{
    public Dictionary<string, object?> Input { get; } = new();
    public Dictionary<string, object?> Output { get; } = new();
    public IServiceProvider Services { get; init; } = null!;
    public List<TraceEntry> Traces { get; } = new();
    
    public void Log(string message)
    {
        Traces.Add(new TraceEntry { Time = DateTime.Now, Message = message });
    }
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
public class MyScript
{
    public object Execute(IScriptContext context)
    {
        var name = context.Input["name"]?.ToString() ?? "World";
        var greeting = $"Hello, {name}!";
        
        context.Output["greeting"] = greeting;
        return greeting;
    }
}
```

### Agent 脚本

```csharp
public class AgentScript
{
    public async Task<object> Execute(IScriptContext context)
    {
        var aiFactory = context.Services.GetRequiredService<IAiFactory>();
        var client = aiFactory.GetDefaultChatClient();
        
        var prompt = context.Input["prompt"]?.ToString() ?? "";
        var response = await client.GetResponseAsync(prompt);
        
        context.Output["response"] = response.Text;
        return response.Text;
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
public class Calculator
{
    public object Execute(IScriptContext context)
    {
        var a = (int)context.Input[""a""];
        var b = (int)context.Input[""b""];
        return a + b;
    }
}";

var ctx = new ScriptContext
{
    Input = { ["a"] = 10, ["b"] = 20 },
    Services = sp
};

var result = await engine.ExecuteAsync(script, ctx);
Console.WriteLine($"Result: {result}");  // 30
```

---

## ⚠️ 注意事项

1. **安全性**: 脚本可执行任意代码，需要沙箱隔离
2. **性能**: 首次编译较慢，后续走缓存
3. **依赖**: 需要 `dotnet-isolated-sdk` 或完整 SDK
4. **调试**: 使用 `context.Log()` 追踪执行过程
