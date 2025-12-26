# 结构化输出 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `IStructuredOutputService.cs` | `Abstractions/` | 服务接口 |
| `StructuredOutputService.cs` | `Services/Data/` | 核心实现 |
| `SchemaGenerator.cs` | `Services/Data/` | JSON Schema 生成 |
| `StructuredOutputDemo.cs` | `Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 工作流程

```
C# Type (Entity)
    ↓
[SchemaGenerator] → JSON Schema
    ↓
[Prompt + Schema] → LLM
    ↓
[JSON Response] → Deserialize → Entity
```

---

## 🔧 核心实现

### 1. JSON Schema 生成

```csharp
public class SchemaGenerator
{
    public JsonSchema GenerateSchema<T>()
    {
        return GenerateSchema(typeof(T));
    }
    
    public JsonSchema GenerateSchema(Type type)
    {
        var schema = new JsonSchema
        {
            Type = "object",
            Properties = new Dictionary<string, JsonSchemaProperty>(),
            Required = new List<string>()
        };
        
        foreach (var prop in type.GetProperties())
        {
            var propSchema = GetPropertySchema(prop);
            schema.Properties[ToCamelCase(prop.Name)] = propSchema;
            
            // 检查 Required 特性
            if (prop.GetCustomAttribute<RequiredAttribute>() != null)
            {
                schema.Required.Add(ToCamelCase(prop.Name));
            }
        }
        
        return schema;
    }
    
    private JsonSchemaProperty GetPropertySchema(PropertyInfo prop)
    {
        var type = prop.PropertyType;
        
        return type switch
        {
            _ when type == typeof(string) => new() { Type = "string" },
            _ when type == typeof(int) || type == typeof(long) => new() { Type = "integer" },
            _ when type == typeof(float) || type == typeof(double) => new() { Type = "number" },
            _ when type == typeof(bool) => new() { Type = "boolean" },
            _ when type.IsArray || IsCollection(type) => new() 
            { 
                Type = "array", 
                Items = GetItemSchema(type) 
            },
            _ when type.IsClass => GenerateSchema(type),
            _ => new() { Type = "string" }
        };
    }
}
```

### 2. 结构化输出服务

```csharp
public class StructuredOutputService : IStructuredOutputService
{
    private readonly IChatClient _client;
    private readonly SchemaGenerator _schemaGenerator;
    
    public async Task<T> ExtractAsync<T>(string prompt, CancellationToken ct = default)
    {
        // 1. 生成 Schema
        var schema = _schemaGenerator.GenerateSchema<T>();
        var schemaJson = JsonSerializer.Serialize(schema);
        
        // 2. 构建带 Schema 的 Prompt
        var fullPrompt = $@"
{prompt}

请以 JSON 格式返回结果，严格遵循以下 Schema:
```json
{schemaJson}
```

只返回 JSON，不要其他内容。";

        // 3. 调用 LLM
        var response = await _client.GetResponseAsync(fullPrompt, cancellationToken: ct);
        
        // 4. 解析 JSON
        var json = ExtractJsonFromResponse(response.Text);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
    
    private string ExtractJsonFromResponse(string text)
    {
        // 提取 JSON (处理 markdown 代码块)
        var match = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)\s*```");
        if (match.Success)
            return match.Groups[1].Value;
        
        // 尝试直接解析
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text.Substring(start, end - start + 1);
        
        return text;
    }
}
```

### 3. 扩展方法

```csharp
public static class ChatClientStructuredOutputExtensions
{
    public static async Task<T> RunAsync<T>(
        this IChatClient client,
        string prompt,
        IServiceProvider sp,
        CancellationToken ct = default)
    {
        var service = sp.GetRequiredService<IStructuredOutputService>();
        return await service.ExtractAsync<T>(prompt, ct);
    }
}
```

---

## 📊 示例模型

```csharp
public class ProductAnalysis
{
    [Required]
    public string ProductName { get; set; } = "";
    
    public List<string> Strengths { get; set; } = new();
    
    public List<string> Weaknesses { get; set; } = new();
    
    public int MarketScore { get; set; }  // 1-10
    
    public string Recommendation { get; set; } = "";
}
```

生成的 JSON Schema:
```json
{
  "type": "object",
  "properties": {
    "productName": { "type": "string" },
    "strengths": { "type": "array", "items": { "type": "string" } },
    "weaknesses": { "type": "array", "items": { "type": "string" } },
    "marketScore": { "type": "integer" },
    "recommendation": { "type": "string" }
  },
  "required": ["productName"]
}
```

---

## 🚀 使用示例

```csharp
// 方式1: 通过服务
var service = sp.GetRequiredService<IStructuredOutputService>();
var analysis = await service.ExtractAsync<ProductAnalysis>(
    "分析 iPhone 16 的市场竞争力");

Console.WriteLine($"产品: {analysis.ProductName}");
Console.WriteLine($"优势: {string.Join(", ", analysis.Strengths)}");
Console.WriteLine($"评分: {analysis.MarketScore}/10");

// 方式2: 扩展方法
var result = await client.RunAsync<ProductAnalysis>(
    "分析特斯拉 Model 3 的优缺点", sp);
```

---

## ⚠️ 注意事项

1. **模型兼容性**: 部分模型支持 `response_format: json_object`
2. **复杂嵌套**: 深层嵌套可能导致 LLM 输出不准确
3. **验证**: 建议添加 JSON 验证逻辑
4. **重试**: 解析失败时可重试请求
