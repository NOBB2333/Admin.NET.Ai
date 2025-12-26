# 综合场景与存储策略 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `HotColdStorageService.cs` | `Services/Storage/` | 热冷分层存储 |
| `VectorChatMessageStore.cs` | `Services/Storage/` | 向量化消息存储 |
| `StorageDemo.cs` | `Demos/` | 存储演示 |
| `ScenarioDemo.cs` | `Demos/` | 场景演示 |

---

## 🏗️ 存储策略

### 热冷分层

```
                 ┌─────────────┐
                 │   热存储    │   ← 最近/高频访问
                 │   (Redis)   │
                 └──────┬──────┘
                        │ 降温
                 ┌──────▼──────┐
                 │   温存储    │   ← 中等频率
                 │  (Database) │
                 └──────┬──────┘
                        │ 归档
                 ┌──────▼──────┐
                 │   冷存储    │   ← 低频/历史
                 │   (Blob)    │
                 └─────────────┘
```

---

## 🔧 核心实现

### 1. 热冷分层存储

```csharp
public class HotColdStorageService
{
    private readonly IDistributedCache _hotStore;    // Redis
    private readonly IChatMessageStore _warmStore;   // Database
    private readonly IBlobStorage _coldStore;        // Azure Blob / S3
    
    private readonly TimeSpan _hotRetention = TimeSpan.FromHours(24);
    private readonly TimeSpan _warmRetention = TimeSpan.FromDays(30);
    
    public async Task SaveAsync(string threadId, List<ChatMessage> messages)
    {
        // 1. 保存到热存储
        var key = $"chat:hot:{threadId}";
        var json = JsonSerializer.Serialize(messages);
        await _hotStore.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _hotRetention
        });
        
        // 2. 异步写入温存储
        _ = Task.Run(async () =>
        {
            await _warmStore.SaveMessagesAsync(threadId, messages);
        });
    }
    
    public async Task<List<ChatMessage>> GetAsync(string threadId)
    {
        // 1. 尝试热存储
        var hotKey = $"chat:hot:{threadId}";
        var hotData = await _hotStore.GetStringAsync(hotKey);
        if (hotData != null)
        {
            return JsonSerializer.Deserialize<List<ChatMessage>>(hotData)!;
        }
        
        // 2. 尝试温存储
        var warmData = await _warmStore.GetMessagesAsync(threadId);
        if (warmData.Any())
        {
            // 预热到热存储
            await _hotStore.SetStringAsync(hotKey, JsonSerializer.Serialize(warmData), 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _hotRetention });
            return warmData;
        }
        
        // 3. 尝试冷存储
        var coldData = await _coldStore.GetAsync($"chat/{threadId}.json");
        if (coldData != null)
        {
            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(coldData)!;
            // 预热
            await SaveAsync(threadId, messages);
            return messages;
        }
        
        return new List<ChatMessage>();
    }
    
    // 定时任务: 温转冷
    public async Task ArchiveOldDataAsync()
    {
        var oldThreads = await _warmStore.GetOldThreadsAsync(_warmRetention);
        
        foreach (var threadId in oldThreads)
        {
            var messages = await _warmStore.GetMessagesAsync(threadId);
            
            // 写入冷存储
            var json = JsonSerializer.Serialize(messages);
            await _coldStore.UploadAsync($"chat/{threadId}.json", json);
            
            // 从温存储删除
            await _warmStore.DeleteThreadAsync(threadId);
        }
    }
}
```

### 2. 向量化消息存储

```csharp
public class VectorChatMessageStore : IChatMessageStore
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingGenerator _embedder;
    private readonly IChatMessageStore _baseStore;
    
    public async Task SaveMessagesAsync(string threadId, IEnumerable<ChatMessage> messages)
    {
        // 1. 保存原始消息
        await _baseStore.SaveMessagesAsync(threadId, messages);
        
        // 2. 生成向量并索引
        foreach (var msg in messages)
        {
            var embedding = await _embedder.GenerateEmbeddingAsync(msg.Text);
            
            await _vectorStore.UpsertAsync(new VectorRecord
            {
                Id = $"{threadId}_{Guid.NewGuid()}",
                Vector = embedding.Vector,
                Content = msg.Text,
                Metadata = new Dictionary<string, object>
                {
                    ["thread_id"] = threadId,
                    ["role"] = msg.Role.Value,
                    ["timestamp"] = DateTime.UtcNow
                }
            });
        }
    }
    
    // 语义搜索历史消息
    public async Task<List<ChatMessage>> SearchSimilarAsync(string query, int topK = 5)
    {
        var queryEmbedding = await _embedder.GenerateEmbeddingAsync(query);
        var results = await _vectorStore.SearchAsync(queryEmbedding.Vector, topK);
        
        return results.Select(r => new ChatMessage(
            new ChatRole(r.Metadata["role"].ToString()!),
            r.Content)).ToList();
    }
}
```

---

## 🎯 综合场景

### 客服对话场景

```csharp
public class CustomerServiceScenario
{
    private readonly IChatClient _client;
    private readonly IRagService _rag;
    private readonly IChatMessageStore _store;
    
    public async Task<string> HandleQueryAsync(string userId, string query)
    {
        // 1. 加载历史对话
        var history = await _store.GetMessagesAsync($"user_{userId}", limit: 10);
        
        // 2. 检索知识库
        var context = await _rag.RetrieveAsync(query, new RetrievalOptions { TopK = 3 });
        var contextText = string.Join("\n", context.Select(c => c.Content));
        
        // 3. 构建消息
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, $"你是客服助手。参考知识:\n{contextText}")
        };
        messages.AddRange(history);
        messages.Add(new(ChatRole.User, query));
        
        // 4. 获取响应
        var response = await _client.GetResponseAsync(messages);
        
        // 5. 保存对话
        await _store.SaveMessagesAsync($"user_{userId}", new[]
        {
            new ChatMessage(ChatRole.User, query),
            new ChatMessage(ChatRole.Assistant, response.Text)
        });
        
        return response.Text;
    }
}
```

### 文档分析场景

```csharp
public class DocumentAnalysisScenario
{
    private readonly VisionService _vision;
    private readonly KnowledgeGraphAgent _kgAgent;
    private readonly QualityAssessmentAgent _qaAgent;
    
    public async Task<DocumentAnalysisResult> AnalyzeAsync(byte[] documentImage)
    {
        // 1. OCR 提取文字
        var text = await _vision.ExtractTextFromImageAsync(documentImage);
        var fullText = string.Join("\n", text);
        
        // 2. 提取知识图谱
        var triples = await _kgAgent.ExtractTriplesAsync(fullText);
        
        // 3. 质量评估
        var quality = await _qaAgent.AssessAsync(fullText, "专业性、准确性、完整性");
        
        return new DocumentAnalysisResult
        {
            ExtractedText = fullText,
            KnowledgeTriples = triples,
            QualityScore = quality.OverallScore,
            Suggestions = quality.Suggestions
        };
    }
}
```

---

## 📊 性能优化

### 批量处理

```csharp
public class BatchProcessingService
{
    public async Task<List<BatchResult<T>>> ProcessBatchAsync<T>(
        IChatClient client,
        IEnumerable<string> prompts,
        Func<string, Task<T>> processor,
        CancellationToken ct = default)
    {
        var results = new List<BatchResult<T>>();
        var semaphore = new SemaphoreSlim(5);  // 并发限制
        
        var tasks = prompts.Select(async prompt =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await processor(prompt);
                return new BatchResult<T> { Input = prompt, Output = result, Success = true };
            }
            catch (Exception ex)
            {
                return new BatchResult<T> { Input = prompt, Error = ex.Message, Success = false };
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        return (await Task.WhenAll(tasks)).ToList();
    }
}
```

---

## ⚙️ 配置

```json
{
  "Storage": {
    "Hot": {
      "Provider": "Redis",
      "ConnectionString": "localhost:6379",
      "RetentionHours": 24
    },
    "Warm": {
      "Provider": "SqlSugar",
      "RetentionDays": 30
    },
    "Cold": {
      "Provider": "AzureBlob",
      "ConnectionString": "..."
    }
  }
}
```

---

## 🚀 使用示例

```csharp
var storage = sp.GetRequiredService<HotColdStorageService>();

// 保存 (自动分层)
await storage.SaveAsync("thread_123", messages);

// 读取 (自动预热)
var history = await storage.GetAsync("thread_123");

// 向量搜索
var vectorStore = sp.GetRequiredService<VectorChatMessageStore>();
var similar = await vectorStore.SearchSimilarAsync("类似问题的答案", topK: 3);
```
