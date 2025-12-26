# 上下文压缩 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `IChatReducer.cs` | `Abstractions/` | Reducer 接口 |
| `TruncateReducer.cs` | `Services/Compression/` | 截断策略 |
| `SummarizeReducer.cs` | `Services/Compression/` | 摘要策略 |
| `SelectiveReducer.cs` | `Services/Compression/` | 选择性保留 |
| `CompressionDemo.cs` | `Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 压缩策略

| 策略 | 说明 | 适用场景 |
|------|------|---------|
| Truncate | 简单截断早期消息 | 快速、低精度 |
| Summarize | 摘要早期对话 | 保留语义 |
| Selective | 保留关键消息 | 多轮复杂对话 |
| Sliding Window | 滑动窗口 | 固定长度上下文 |

### 压缩流程

```
[完整对话历史]
    ↓
超过阈值?
    ↓ Yes
[选择 Reducer 策略]
    ↓
[压缩处理]
    ↓
[优化后的上下文]
```

---

## 🔧 核心实现

### 1. Reducer 接口

```csharp
public interface IChatReducer
{
    /// <summary>
    /// 压缩消息列表
    /// </summary>
    Task<List<ChatMessage>> ReduceAsync(
        List<ChatMessage> messages, 
        ReducerOptions options,
        CancellationToken ct = default);
}

public class ReducerOptions
{
    public int MaxTokens { get; set; } = 4000;
    public int MaxMessages { get; set; } = 20;
    public bool PreserveSystemMessage { get; set; } = true;
    public bool PreserveLastN { get; set; } = true;
    public int LastNCount { get; set; } = 4;
}
```

### 2. 截断策略

```csharp
public class TruncateReducer : IChatReducer
{
    public Task<List<ChatMessage>> ReduceAsync(
        List<ChatMessage> messages, 
        ReducerOptions options,
        CancellationToken ct = default)
    {
        if (messages.Count <= options.MaxMessages)
        {
            return Task.FromResult(messages);
        }
        
        var result = new List<ChatMessage>();
        
        // 1. 保留 System 消息
        if (options.PreserveSystemMessage)
        {
            var system = messages.FirstOrDefault(m => m.Role == ChatRole.System);
            if (system != null) result.Add(system);
        }
        
        // 2. 保留最近 N 条
        var recent = messages
            .Where(m => m.Role != ChatRole.System)
            .TakeLast(options.LastNCount);
        
        result.AddRange(recent);
        
        return Task.FromResult(result);
    }
}
```

### 3. 摘要策略

```csharp
public class SummarizeReducer : IChatReducer
{
    private readonly IChatClient _client;
    
    public async Task<List<ChatMessage>> ReduceAsync(
        List<ChatMessage> messages, 
        ReducerOptions options,
        CancellationToken ct = default)
    {
        if (messages.Count <= options.MaxMessages)
        {
            return messages;
        }
        
        var result = new List<ChatMessage>();
        
        // 1. 保留 System
        var system = messages.FirstOrDefault(m => m.Role == ChatRole.System);
        if (system != null && options.PreserveSystemMessage)
        {
            result.Add(system);
        }
        
        // 2. 分离早期和近期消息
        var earlyMessages = messages
            .Where(m => m.Role != ChatRole.System)
            .SkipLast(options.LastNCount)
            .ToList();
        
        var recentMessages = messages
            .Where(m => m.Role != ChatRole.System)
            .TakeLast(options.LastNCount)
            .ToList();
        
        // 3. 摘要早期消息
        if (earlyMessages.Any())
        {
            var summary = await SummarizeMessagesAsync(earlyMessages, ct);
            result.Add(new ChatMessage(ChatRole.System, 
                $"[对话历史摘要]\n{summary}"));
        }
        
        // 4. 添加近期消息
        result.AddRange(recentMessages);
        
        return result;
    }
    
    private async Task<string> SummarizeMessagesAsync(
        List<ChatMessage> messages, 
        CancellationToken ct)
    {
        var transcript = string.Join("\n", messages.Select(m => 
            $"{m.Role}: {m.Text}"));
        
        var response = await _client.GetResponseAsync(
            $"请用 3-5 句话总结以下对话的要点和结论:\n{transcript}",
            cancellationToken: ct);
        
        return response.Text;
    }
}
```

### 4. 选择性保留策略

```csharp
public class SelectiveReducer : IChatReducer
{
    private readonly IChatClient _client;
    
    public async Task<List<ChatMessage>> ReduceAsync(
        List<ChatMessage> messages, 
        ReducerOptions options,
        CancellationToken ct = default)
    {
        if (messages.Count <= options.MaxMessages)
        {
            return messages;
        }
        
        // 1. 评估每条消息的重要性
        var scores = await ScoreMessagesAsync(messages, ct);
        
        // 2. 按重要性排序，保留 Top N
        var important = messages
            .Zip(scores, (m, s) => (Message: m, Score: s))
            .OrderByDescending(x => x.Score)
            .Take(options.MaxMessages)
            .OrderBy(x => messages.IndexOf(x.Message))  // 恢复原顺序
            .Select(x => x.Message)
            .ToList();
        
        return important;
    }
    
    private async Task<List<float>> ScoreMessagesAsync(
        List<ChatMessage> messages, 
        CancellationToken ct)
    {
        // 可以用 LLM 或规则评估重要性
        var scores = new List<float>();
        
        foreach (var msg in messages)
        {
            float score = 0.5f;  // 基础分
            
            // 规则评分
            if (msg.Role == ChatRole.System) score += 0.3f;
            if (msg.Text.Contains("重要", StringComparison.OrdinalIgnoreCase)) score += 0.2f;
            if (msg.Text.Length > 200) score += 0.1f;  // 长消息可能更重要
            
            scores.Add(score);
        }
        
        return scores;
    }
}
```

---

## 📊 Token 计算

```csharp
public class TokenCounter
{
    // 简单估算: ~4 字符 = 1 token (中文约 1.5 字 = 1 token)
    public int EstimateTokens(string text)
    {
        var chineseCount = text.Count(c => c >= 0x4E00 && c <= 0x9FFF);
        var otherCount = text.Length - chineseCount;
        
        return (int)(chineseCount / 1.5 + otherCount / 4);
    }
    
    public int EstimateTokens(IEnumerable<ChatMessage> messages)
    {
        return messages.Sum(m => EstimateTokens(m.Text ?? ""));
    }
}
```

---

## 🚀 使用示例

```csharp
var reducer = sp.GetRequiredService<IChatReducer>();

// 假设有 100 条消息
var longHistory = GetLongConversationHistory();

// 压缩到合适大小
var options = new ReducerOptions
{
    MaxMessages = 20,
    MaxTokens = 4000,
    LastNCount = 5,
    PreserveSystemMessage = true
};

var optimized = await reducer.ReduceAsync(longHistory, options);
// 现在可以安全发送给 LLM
var response = await client.GetResponseAsync(optimized);
```

---

## ⚙️ 配置

```json
{
  "Compression": {
    "Strategy": "Summarize",
    "MaxTokens": 4000,
    "MaxMessages": 20,
    "LastNCount": 5,
    "PreserveSystemMessage": true
  }
}
```
