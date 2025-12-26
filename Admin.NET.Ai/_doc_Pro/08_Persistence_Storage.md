# 对话持久化 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `IChatMessageStore.cs` | `Abstractions/` | 消息存储接口 |
| `InMemoryChatMessageStore.cs` | `Services/Storage/` | 内存存储 |
| `DatabaseChatMessageStore.cs` | `Services/Storage/` | 数据库存储 (SqlSugar) |
| `RedisChatMessageStore.cs` | `Services/Storage/` | Redis 存储 |
| `VectorChatMessageStore.cs` | `Services/Storage/` | 向量化存储 |
| `ConversationSummarizer.cs` | `Services/Storage/` | 对话摘要 |
| `PersistenceDemo.cs` | `Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 存储接口

```csharp
public interface IChatMessageStore
{
    Task SaveMessagesAsync(string threadId, IEnumerable<ChatMessage> messages);
    Task<List<ChatMessage>> GetMessagesAsync(string threadId, int? limit = null);
    Task<List<string>> GetThreadIdsAsync(string? userId = null);
    Task DeleteThreadAsync(string threadId);
}
```

### 存储层次

```
          ┌─────────────────┐
          │   Application   │
          └────────┬────────┘
                   │
     ┌─────────────┼─────────────┐
     ▼             ▼             ▼
┌─────────┐  ┌──────────┐  ┌─────────┐
│ InMemory │  │ Database │  │  Redis  │
│ (Debug)  │  │ (持久化) │  │ (分布式)│
└─────────┘  └──────────┘  └─────────┘
```

---

## 🔧 核心实现

### 1. 内存存储 (开发/测试用)

```csharp
public class InMemoryChatMessageStore : IChatMessageStore
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _threads = new();
    
    public Task SaveMessagesAsync(string threadId, IEnumerable<ChatMessage> messages)
    {
        var list = _threads.GetOrAdd(threadId, _ => new List<ChatMessage>());
        lock (list)
        {
            list.AddRange(messages);
        }
        return Task.CompletedTask;
    }
    
    public Task<List<ChatMessage>> GetMessagesAsync(string threadId, int? limit = null)
    {
        if (_threads.TryGetValue(threadId, out var messages))
        {
            var result = limit.HasValue 
                ? messages.TakeLast(limit.Value).ToList() 
                : messages.ToList();
            return Task.FromResult(result);
        }
        return Task.FromResult(new List<ChatMessage>());
    }
}
```

### 2. 数据库存储 (SqlSugar)

```csharp
public class DatabaseChatMessageStore : IChatMessageStore
{
    private readonly ISqlSugarClient _db;
    
    public async Task SaveMessagesAsync(string threadId, IEnumerable<ChatMessage> messages)
    {
        var entities = messages.Select((m, i) => new ChatMessageEntity
        {
            Id = Guid.NewGuid(),
            ThreadId = threadId,
            Role = m.Role.Value,
            Content = SerializeContent(m),
            Sequence = i,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        
        await _db.Insertable(entities).ExecuteCommandAsync();
    }
    
    public async Task<List<ChatMessage>> GetMessagesAsync(string threadId, int? limit = null)
    {
        var query = _db.Queryable<ChatMessageEntity>()
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.Sequence);
        
        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }
        
        var entities = await query.ToListAsync();
        return entities.Select(DeserializeMessage).ToList();
    }
    
    private string SerializeContent(ChatMessage message)
    {
        // 处理多模态内容
        return JsonSerializer.Serialize(message.Contents);
    }
    
    private ChatMessage DeserializeMessage(ChatMessageEntity entity)
    {
        var contents = JsonSerializer.Deserialize<List<AIContent>>(entity.Content);
        return new ChatMessage(new ChatRole(entity.Role), contents);
    }
}
```

### 3. Redis 存储 (分布式)

```csharp
public class RedisChatMessageStore : IChatMessageStore
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _expiry = TimeSpan.FromDays(7);
    
    public async Task SaveMessagesAsync(string threadId, IEnumerable<ChatMessage> messages)
    {
        var key = $"chat:thread:{threadId}";
        var existing = await GetMessagesAsync(threadId);
        existing.AddRange(messages);
        
        var json = JsonSerializer.Serialize(existing);
        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiry
        });
    }
    
    public async Task<List<ChatMessage>> GetMessagesAsync(string threadId, int? limit = null)
    {
        var key = $"chat:thread:{threadId}";
        var json = await _cache.GetStringAsync(key);
        
        if (string.IsNullOrEmpty(json))
            return new List<ChatMessage>();
        
        var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new();
        return limit.HasValue ? messages.TakeLast(limit.Value).ToList() : messages;
    }
}
```

### 4. 对话摘要 (长对话优化)

```csharp
public class ConversationSummarizer
{
    private readonly IChatClient _client;
    private readonly int _summarizeThreshold = 20;  // 超过 20 条时开始摘要
    
    public async Task<List<ChatMessage>> OptimizeContextAsync(
        List<ChatMessage> messages, 
        int maxMessages = 10)
    {
        if (messages.Count <= _summarizeThreshold)
            return messages;
        
        // 1. 保留最近的消息
        var recentMessages = messages.TakeLast(maxMessages).ToList();
        
        // 2. 摘要早期消息
        var earlyMessages = messages.Take(messages.Count - maxMessages).ToList();
        var summary = await SummarizeAsync(earlyMessages);
        
        // 3. 组合: [摘要] + [最近消息]
        var result = new List<ChatMessage>
        {
            new(ChatRole.System, $"[对话历史摘要]: {summary}")
        };
        result.AddRange(recentMessages);
        
        return result;
    }
    
    private async Task<string> SummarizeAsync(List<ChatMessage> messages)
    {
        var content = string.Join("\n", messages.Select(m => $"{m.Role}: {m.Text}"));
        var response = await _client.GetResponseAsync(
            $"请用 2-3 句话总结以下对话的要点:\n{content}");
        return response.Text;
    }
}
```

---

## 📊 数据库表结构

```sql
CREATE TABLE ChatMessages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ThreadId NVARCHAR(100) NOT NULL,
    UserId NVARCHAR(100),
    Role NVARCHAR(20) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Sequence INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    Metadata NVARCHAR(MAX)
);

CREATE INDEX IX_ChatMessages_ThreadId ON ChatMessages(ThreadId);
CREATE INDEX IX_ChatMessages_UserId ON ChatMessages(UserId);
```

---

## ⚙️ 配置

```json
{
  "LLM-Persistence": {
    "Provider": "database",
    "ConnectionString": "...",
    "Redis": {
      "ConnectionString": "localhost:6379",
      "ExpiryDays": 7
    },
    "Summarization": {
      "Enabled": true,
      "Threshold": 20,
      "MaxMessages": 10
    }
  }
}
```

---

## 🚀 使用示例

```csharp
var store = sp.GetRequiredService<IChatMessageStore>();

// 保存消息
await store.SaveMessagesAsync("thread_123", new[]
{
    new ChatMessage(ChatRole.User, "你好"),
    new ChatMessage(ChatRole.Assistant, "你好！有什么可以帮助你的？")
});

// 读取历史
var history = await store.GetMessagesAsync("thread_123");

// 续聊
history.Add(new ChatMessage(ChatRole.User, "继续之前的话题"));
var response = await client.GetResponseAsync(history);
```
