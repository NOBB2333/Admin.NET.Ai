# 06. Natasha 动态脚本引擎 (Hot Reload)

## 🎯 设计思维 (Mental Model)
在传统的 .NET 开发中，添加一个功能需要：**写代码 -> 编译 -> 部署 -> 重启**。
但在 AI 飞速迭代的领域，这太慢了。运营人员可能希望今天下午就给 Agent 增加一个新的数据转换工具。

`Admin.NET.Ai` 集成了 **Natasha v9**（国产最强动态编译库），实现了：
1.  **代码即插件**: 直接把 C# 源码字符串注入系统，瞬间生效。
2.  **依赖注入整合**: 动态编译的代码依然可以从主程序的 DI 容器中获取 `ISqlSugarClient` 或 `IAiService`。
3.  **安全隔离与卸载 (Unloading)**: 每个脚本运行在独立的 `AssemblyLoadContext` 中，可以随用随关，不会撑爆内存。

---

## 🏗️ 架构设计
### 核心组件
- **`IScriptExecutor`**: 脚本必须实现的接口，定义了脚本的元数据和执行逻辑。
- **`NatashaScriptEngine`**: 核心编译器。负责代码扫描、引用补全、编译与实例化。
- **`AssemblyLoadContext`**: .NET 底层的隔离机制，确保动态程序集可以被完全回收。

---

## 🛠️ 技术实现 (Implementation)

### 1. 依赖库
- `Natasha.CSharp`: 提供高性能的动态编译能力。
- `System.Runtime.Loader`: 提供程序集隔离支持。

### 2. 核心编译逻辑 (`Services/Workflow/NatashaScriptEngine.cs`)
系统会自动收集宿主程序的常用引用，确保脚本里写普通的 C# 语法（如 LINQ, `JsonSerializer`）都能过。

```csharp
public IEnumerable<IScriptExecutor> LoadScripts(IEnumerable<string> scriptContents)
{
    // 1. 每次加载前，清理旧的域 (Domain)，触发 GC 卸载旧代码
    _currentDomain?.Dispose();
    
    // 2. 创建 Natasha 编译器
    var builder = new AssemblyCSharpBuilder(Guid.NewGuid().ToString());
    
    // 3. 配置 LoadContext，注入主程序引用
    builder.ConfigLoadContext(ctx => {
         ctx.AddReferenceAndUsingCode(typeof(ISqlSugarClient)); // 允许脚本操作数据库
         ctx.AddReferenceAndUsingCode(typeof(IAiService));     // 允许脚本调用 AI
         return ctx;
    });

    // 4. 编译到内存并获取程序集
    var assembly = builder.GetAssembly();
    
    // 5. 查找实现了 IScriptExecutor 的类
    var types = assembly.GetTypes().Where(t => typeof(IScriptExecutor).IsAssignableFrom(t));
    
    // 6. 实例化并支持构造函数注入 (DI)
    return types.Select(t => (IScriptExecutor)ActivatorUtilities.CreateInstance(serviceProvider, t));
}
```

## 👁️ 脚本可观测性 (Observability)

为了解决动态脚本执行过程中的“黑盒”问题，系统内置了基于 Roslyn 的**零侵入式可观测性注入**机制。这使得脚本执行过程具备了类似 **N8N** 或 **Dify** 的可视化追踪能力。

### 1. 核心能力
- **零侵入追踪 (Zero-Invasive)**: 开发者无需在脚本中编写任何日志或添加特性。系统在编译前会自动通过语法树重写注入追踪逻辑。
- **全量 I/O 拦截 (Full I/O Interception)**: 
  - **输入拦截**: 自动捕获每个方法的参数名与参数值，并序列化为 JSON 记录。
  - **输出拦截**: 自动捕获每个方法的返回值（支持 `void` 和异步）。
- **分层执行轨迹 (Hierarchical Trace)**: 自动识别脚本内部方法的调用层次关系，生成树状执行视图。
- **异常自动捕获**: 任何一层的方法报错都会被精准记录在对应的执行步骤中。

### 2. 注入原理 (AOP via Roslyn)
重写器 (`ScriptSourceRewriter`) 会扫描脚本类，并对每个方法进行如下变换：

```csharp
// 原始脚本
private string GetGreeting(string name) {
    return $"你好 {name}";
}

// 自动重写后 (示意)
private string GetGreeting(string name) {
    using (var scope = _scriptContext?.BeginStep("GetGreeting", new { name })) {
        try {
            var result = "你好 " + name; 
            scope?.SetOutput(result); // 自动拦截输出
            return result;
        } catch (Exception ex) {
            scope?.SetError(ex); // 自动捕获错误
            throw;
        }
    }
}
```

---

## 🏗️ 架构设计
### 核心组件
- **`IScriptExecutor`**: 脚本接口。`Execute` 方法接受一个 `IScriptExecutionContext` 可选参数。
- **`ScriptExecutionContext`**: 执行上下文。作为一个容器，自动收集 `ScriptStepInfo` 树及其耗时、状态和数据。
- **`ScriptSourceRewriter`**: 基于 `CSharpSyntaxRewriter` 的核心注入引擎。
- **`NatashaScriptEngine`**: 集成了重写器，确保代码加载前已被自动增强。

---

## 🚀 代码示例 (Usage Example)

### 准备一段普通脚本
```csharp
public class MyDynamicExecutor : IScriptExecutor
{
    public object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null)
    {
        var name = input["name"]?.ToString();
        return SayHello(name);
    }

    private string SayHello(string name) => $"Hello {name}";
}
```

### 带有轨迹捕获的运行方式
```csharp
// 1. 创建执行上下文
var context = new ScriptExecutionContext("MyScriptExecution");

// 2. 加载并运行
var executor = engine.LoadScripts(new[] { code }).First();
executor.Execute(new Dictionary<string, object?> { ["name"] = "Alice" }, context);

// 3. 获取并展示轨迹
var trace = context.RootStep;
Console.WriteLine($"脚本状态: {trace.Status}, 总耗时: {trace.Duration}");
// 递归遍历 trace.Children 即可渲染出完美的 UI 执行视图
```

---

## ⚠️ 注意事项
1.  **引用陷阱**: 脚本中使用的类必须在编译前通过 `AddReferenceAndUsingCode` 告知编译器，否则会报 `CS0246` 找不到类型。
2.  **内存管理**: 虽然 Natasha 支持卸载，但如果有全局静态变量（Static）引用了动态程序集的对象，会导致卸载失效。
3.  **安全性**: 生产环境中建议对上传的脚本代码进行白名单检测（如禁止 `System.IO` 或 `Process.Start`），以防恶意代码攻击。
4.  **性能开销**: 自动拦截会带来极细微的反射和对象创建开销。对于极致性能要求的循环代码，建议提取到非脚本程序集中。
5.  **异步支持**: 当前重写器优化了同步调用。完全自动化的 `async/await` 细粒度断点追踪在复杂异步组合下可能需要进一步精调。
6.  **循环引用**: 在输入参数中包含大型循环对象（如 `context` 自身）会导致轨迹序列化失败。重写器已自动过滤了系统级上下文参数。
