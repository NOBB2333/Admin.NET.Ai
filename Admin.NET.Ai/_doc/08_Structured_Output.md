# 08. 强类型结构化输出 (Structured Output)

## 🎯 设计思维 (Mental Model)
让 LLM 返回一段自然语言很简单，但让它返回一段 100% 符合业务逻辑代码要求的 **JSON** 很难。
传统的解决方案是 Prompt 里写“请返回 JSON”，然后代码里用 `try-parse` 强转，但这极其不稳定。

`Admin.NET.Ai` 采用了最新的 **Structured Output** 技术方案：
1.  **JSON Schema 约束**: 将 C# 类自动转换为 JSON Schema 随请求发送给模型。
2.  **强制约束 (Constrained Sampling)**: 兼容 OpenAI/DeepSeek 的强约束参数，确保输出格式 0 误差。
3.  **TOON 协议支持**: 特色功能，支持比标准 JSON 更紧凑的序列化方式。

---

## 🏗️ 架构设计
### 核心组件
- **`IStructuredOutputService`**: 负责反射 C# 类型生成 Schema。
- **`RunAsync<T>` 扩展**: 顶层 API，实现“一行代码实现强类型调用”。

---

## 🛠️ 技术实现 (Implementation)

### 1. JSON Schema 生成 (`Services/Data/StructuredOutputService.cs`)
系统利用 `System.Text.Json` 的反射能力，将复杂的 C# 嵌套对象（含枚举、列表）生成为 LLM 可识别的 Schema。

```csharp
public async Task<T> GetStructuredResponseAsync<T>(string prompt, IChatClient client)
{
    // 1. 获取 T 的 Json Schema
    var schema = GenerateSchema(typeof(T));
    
    // 2. 将 Schema 注入 ChatOptions
    var options = new ChatOptions {
        ResponseFormat = ChatResponseFormat.JsonSchema(schema)
    };

    // 3. 调用并反序列化
    var response = await client.GetResponseAsync(prompt, options);
    return JsonSerializer.Deserialize<T>(response.Text);
}
```

### 2. 国产模型兼容
针对 DeepSeek 等模型，若不支持标准的 `json_schema` 字段，服务会自动回退到在 `System Prompt` 中注入格式说明，并在后置处理中利用 `JsonDocument` 进行清洗。

---

## 🚀 代码示例 (Usage Example)

### 定义返回模型
```csharp
public class AnalysisResult
{
    public string Summary { get; set; }
    public List<string> KeyPoints { get; set; }
    public int ConfidenceScore { get; set; }
}
```

### 一行代码调用 (RunAsync 模式)
```csharp
// 系统会自动生成 Schema、配置 Client、调用模型并 Parse
AnalysisResult result = await chatClient.RunAsync<AnalysisResult>("请分析今天的股市", sp);

Console.WriteLine(result.Summary);
```

---

## 💎 特色能力：TOON 协议
在某些高性能场景下，标准 JSON 的冗余字符太长。系统提供了 `ToonCodec` 实验性支持：
- **原理**: 使用更精简的标记符号（类似 Markdown 表格或自定义分隔符）。
- **优势**: 节省约 15-30% 的 Token 消耗，变相降低了 API 成本。

---

## ⚠️ 注意事项
- **必须初始化**: 被序列化的类必须有无参构造函数。
- **注释重要性**: C# 属性上的 `[Description]` 特性会被自动提取到 Schema 的 `description` 字段中，直接决定了模型理解字段的准确度。
- **性能**: 对于极大数据结构的 Schema 生成会有微量反射开销，系统内部已实现缓存机制。
