# MCP 工具自动发现 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `McpToolAttribute.cs` | `Services/MCP/Attributes/` | 属性定义 |
| `McpParameterAttribute.cs` | `Services/MCP/Attributes/` | 参数属性定义 |
| `McpToolDiscoveryService.cs` | `Services/MCP/` | 核心发现逻辑 |
| `McpEndpoints.cs` | `Services/MCP/` | HTTP/SSE 端点 |
| `McpToolFactory.cs` | `Services/MCP/` | AITool 转换 |
| `ExampleMcpTools.cs` | `Services/MCP/Examples/` | 使用示例 |

---

## 🔧 McpToolAttribute 设计

### 源码

```csharp
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class McpToolAttribute : Attribute
{
    public string? Name { get; set; }           // null 时使用方法名
    public string Description { get; set; }     // 必填
    public string? Category { get; set; }       // 分类
    public bool RequiresApproval { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 30;

    // 构造函数重载
    public McpToolAttribute(string description) { ... }           // 名称自动
    public McpToolAttribute(string name, string description) { ... }  // 显式名称
}
```

### 设计决策

1. **名称可选**: 大多数情况方法名即工具名，减少冗余
2. **snake_case 转换**: `GetCurrentTime` → `get_current_time`
3. **元数据丰富**: Category 用于分组，RequiresApproval 用于审批流

---

## 🔍 McpToolDiscoveryService 核心逻辑

### 1. 发现流程

```csharp
public void DiscoverFromAssembly(Assembly assembly)
{
    foreach (var type in assembly.GetTypes())
    {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            var attr = method.GetCustomAttribute<McpToolAttribute>();
            if (attr != null)
            {
                RegisterTool(type, method, attr);
            }
        }
    }
}
```

### 2. 工具注册

```csharp
private void RegisterTool(Type declaringType, MethodInfo method, McpToolAttribute attr)
{
    // 工具名称处理
    var toolName = attr.Name ?? ToSnakeCase(method.Name);
    
    // 参数 Schema 构建
    var parameters = BuildParameterSchema(method);
    
    var definition = new McpToolDefinition
    {
        Name = toolName,
        Description = attr.Description,
        DeclaringType = declaringType,
        Method = method,
        ParameterSchema = parameters
    };
    
    _registeredTools[toolName] = definition;
}
```

### 3. 参数 Schema 构建

```csharp
private List<McpParameterDefinition> BuildParameterSchema(MethodInfo method)
{
    var parameters = new List<McpParameterDefinition>();
    
    foreach (var param in method.GetParameters())
    {
        var mcpParam = param.GetCustomAttribute<McpParameterAttribute>();
        
        parameters.Add(new McpParameterDefinition
        {
            Name = param.Name,
            Description = mcpParam?.Description ?? param.Name,
            Required = mcpParam?.Required ?? !param.IsOptional,
            JsonType = GetJsonType(param.ParameterType),  // string/integer/number/boolean/array/object
            ClrType = param.ParameterType
        });
    }
    
    return parameters;
}
```

### 4. 工具执行

```csharp
public async Task<object?> ExecuteToolAsync(string toolName, Dictionary<string, object?> arguments)
{
    var definition = _registeredTools[toolName];
    
    // 1. 通过 DI 获取实例
    object? target = null;
    if (!definition.Method.IsStatic)
    {
        target = _serviceProvider.GetService(definition.DeclaringType)
              ?? ActivatorUtilities.CreateInstance(_serviceProvider, definition.DeclaringType);
    }
    
    // 2. 参数绑定
    var methodParams = definition.Method.GetParameters();
    var invokeArgs = new object?[methodParams.Length];
    for (int i = 0; i < methodParams.Length; i++)
    {
        var param = methodParams[i];
        if (arguments.TryGetValue(param.Name, out var value))
        {
            invokeArgs[i] = ConvertArgument(value, param.ParameterType);
        }
        else if (param.HasDefaultValue)
        {
            invokeArgs[i] = param.DefaultValue;
        }
    }
    
    // 3. 反射调用
    var result = definition.Method.Invoke(target, invokeArgs);
    
    // 4. 处理异步
    if (result is Task task)
    {
        await task;
        if (task.GetType().IsGenericType)
        {
            return task.GetType().GetProperty("Result")?.GetValue(task);
        }
    }
    
    return result;
}
```

---

## 🌐 McpEndpoints HTTP API

### SSE 连接 (`/mcp/sse`)

```csharp
app.MapGet("/mcp/sse", async (HttpContext context) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    
    // 发送工具列表
    var tools = discoveryService.GetToolsForMcp();
    await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "tools_list", tools })}\n\n");
    
    // 保持连接
    while (!context.RequestAborted.IsCancellationRequested)
    {
        await Task.Delay(10000);
        await context.Response.WriteAsync($": keep-alive\n\n");
    }
});
```

### 工具调用 (`/mcp/call`)

```csharp
app.MapPost("/mcp/call", async (HttpContext context) =>
{
    var request = await JsonSerializer.DeserializeAsync<McpCallRequest>(context.Request.Body);
    
    if (!discoveryService.HasTool(request.Tool))
    {
        return Results.Json(new { error = $"Tool '{request.Tool}' not found" });
    }
    
    var result = await discoveryService.ExecuteToolAsync(request.Tool, request.Arguments);
    return Results.Json(new { success = true, result });
});
```

---

## 📊 数据模型

### McpToolDefinition

```csharp
public class McpToolDefinition
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string? Category { get; set; }
    public bool RequiresApproval { get; set; }
    public int TimeoutSeconds { get; set; }
    public Type DeclaringType { get; set; }
    public MethodInfo Method { get; set; }
    public List<McpParameterDefinition> ParameterSchema { get; set; }
}
```

### McpParameterDefinition

```csharp
public class McpParameterDefinition
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public string JsonType { get; set; }  // string/integer/number/boolean/array/object
    public Type ClrType { get; set; }
    public string? Example { get; set; }
}
```

---

## 🔄 类型转换

### CLR → JSON Schema 类型映射

```csharp
private static string GetJsonType(Type type)
{
    if (type == typeof(string)) return "string";
    if (type == typeof(int) || type == typeof(long)) return "integer";
    if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
    if (type == typeof(bool)) return "boolean";
    if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type)) return "array";
    return "object";
}
```

### 参数值转换

```csharp
private static object? ConvertArgument(object? value, Type targetType)
{
    if (value == null) return null;
    if (targetType.IsInstanceOfType(value)) return value;
    
    // JsonElement 处理
    if (value is JsonElement jsonElement)
    {
        return JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType);
    }
    
    // 简单类型
    return Convert.ChangeType(value, targetType);
}
```

---

## 🧪 测试用例

```csharp
// 1. 无参方法
[McpTool("获取当前时间")]
public DateTime GetCurrentTime() => DateTime.Now;

// 2. 带参数方法
[McpTool("加法计算")]
public int Add([McpParameter("数字A")] int a, [McpParameter("数字B")] int b) => a + b;

// 3. 异步方法
[McpTool("translate", "翻译文本")]
public async Task<string> TranslateAsync(string text, string lang = "en")
{
    await Task.Delay(100);
    return $"[{lang}] {text}";
}

// 4. 复杂返回类型
[McpTool("获取天气")]
public WeatherInfo GetWeather(string city) => new WeatherInfo { City = city, Temp = 20 };
```

---

## ⚠️ 注意事项

1. **DI 解析**: 如果类未注册到 DI，会使用 `ActivatorUtilities.CreateInstance`
2. **静态方法**: 支持静态方法，不需要实例
3. **异步支持**: 自动检测并等待 `Task` 和 `Task<T>`
4. **默认值**: 支持 C# 参数默认值
5. **snake_case**: 方法名自动转换 (`GetWeather` → `get_weather`)
