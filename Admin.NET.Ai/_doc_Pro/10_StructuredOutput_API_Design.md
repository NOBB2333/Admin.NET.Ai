# 结构化输出 API 设计方案

## 方案对比

### 方案一：字符串参数 (原始)
```csharp
var result = await client.RunAsync<T>(
    systemInstruction: "你是专家...",
    userPrompt: "请分析...",
    serviceProvider);
```
- ❌ 参数多时混淆
- ❌ 不够结构化

### 方案二：Builder 模式 ⭐ 推荐
```csharp
var result = await client
    .Structured()
    .WithSystem("你是专家...")
    .WithContext(ragDocs)  // 可选
    .RunStructuredAsync<T>("请分析...", sp);
```
- ✅ 流式 API，优雅易读
- ✅ 可扩展 (WithHistory, WithTools...)
- ✅ 企业级设计

### 方案三：消息列表
```csharp
var messages = new[] {
    new ChatMessage(ChatRole.System, "你是专家..."),
    new ChatMessage(ChatRole.User, "请分析...")
};
var result = await client.RunAsync<T>(messages, sp);
```
- ✅ 完全控制消息
- ❌ 代码啰嗦

## 实现文件

- `Extensions/StructuredOutputBuilder.cs` - Builder 实现
- `Extensions/AgentExtensions.cs` - 扩展方法入口
