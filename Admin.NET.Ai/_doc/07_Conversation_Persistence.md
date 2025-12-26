# 07. 对话持久化管理 (Conversation Persistence)

## 🎯 设计思维 (Mental Model)
AI 应用不是一次性的任务执行，而是持续的“对话流”。为了保证用户刷新页面、更换设备或服务器重启后，对话上下文（Context）能够无缝恢复，持久化管理至关重要。

`Admin.NET.Ai` 的 **五星级对话管理** 采用了双层架构：
1.  **通用对话层 (`IChatMessageStore`)**: 面向企业级应用，支持 **会话列表**、**分页查询**、**批量操作** 和 **上下文压缩**。
2.  **Agent 状态层 (`IAgentChatMessageStore`)**: 针对 `Microsoft.Agents` 框架的高级状态隔离与线程管理。

---

## 🏗️ 架构设计
### 核心组件
- **`IChatMessageStore`**: 增强型接口，提供完整的 CRUD 和查询能力。
- **`ChatMessageStoreBase`**: 抽象基类，提供了 80% 的通用逻辑（如分页计算、批量处理默认实现），子类只需实现核心读写方法。
- **`IConversationService`**: 业务服务层，集成 **自动压缩** (`IChatReducer`) 和 **线程隔离**。

### 存储 Provider
系统内置了多种存储适配器，均继承自 `ChatMessageStoreBase`：
- **Database (SqlSugar)**: 推荐用于生产环境，支持分组聚合、事务和复杂查询。
- **Redis**: 适用于分布式的热数据缓存。
- **File (Json)**: 适用于简单本地部署或调试。
- **Vector**: 实验性功能，支持基于语义的历史记录检索。

---

## 🛠️ 技术实现 (Implementation)

### 1. 核心接口 (`Abstractions/IChatMessageStore.cs`)
接口已升级为企业级标准：

```csharp
public interface IChatMessageStore
{
    // === 基础操作 ===
    Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken ct = default);
    Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken ct = default);
    Task ClearHistoryAsync(string sessionId, CancellationToken ct = default);

    // === 增强功能 (New) ===
    Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken ct = default);
    Task<PagedResult<ChatMessageContent>> GetPagedHistoryAsync(string sessionId, int page, int size, CancellationToken ct = default);
    Task<PagedResult<SessionInfo>> GetSessionsAsync(int page, int size, CancellationToken ct = default); // 获取所有会话
    Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken ct = default); // 获取会话元数据
}
```

### 2. 基类简化开发 (`Services/Storage/ChatMessageStoreBase.cs`)
自定义存储只需继承基类并实现 3 个方法，即可自动获得分页、批量等高级功能：

```csharp
public class MyCustomStore : ChatMessageStoreBase
{
    public override async Task<ChatHistory> GetHistoryAsync(...) { /* ... */ }
    public override async Task SaveMessageAsync(...) { /* ... */ }
    public override async Task ClearHistoryAsync(...) { /* ... */ }
}
```

### 3. 会话聚合查询 (`DatabaseChatMessageStore.cs`)
利用 SqlSugar 的分组功能，直接从消息表聚合出会话列表（无需额外的 Session 表）：

```csharp
public async Task<SessionInfo?> GetSessionInfoAsync(string sessionId, ...)
{
    return await _db.Queryable<AIChatMessage>()
        .Where(x => x.SessionId == sessionId)
        .GroupBy(x => x.SessionId)
        .Select(x => new SessionInfo(
             x.SessionId,
             SqlFunc.AggregateMin(x.CreatedTime), // 创建时间
             SqlFunc.AggregateMax(x.CreatedTime), // 最后活跃
             SqlFunc.AggregateCount(x.Id)         // 消息总数
        ))
        .FirstAsync();
}
```

---

## 🚀 代码示例 (Usage Example)

### 获取会话列表 (分页)
```csharp
var conversationService = serviceProvider.GetRequiredService<IConversationService>();

// 获取最近活跃的 20 个会话
var sessions = await conversationService.GetSessionsAsync(pageIndex: 0, pageSize: 20);

foreach (var session in sessions.Items)
{
    Console.WriteLine($"Session: {session.SessionId}, Msgs: {session.MessageCount}, Last: {session.LastMessageAt}");
}
```

### 压缩并保存 (Compress Integration)
结合 `IChatReducer` 自动优化上下文并持久化：

```csharp
// 该方法会自动：
// 1. 获取完整历史
// 2. 调用配置的 Reducer (如 SummarizingReducer) 进行压缩
// 3. 将压缩后的结果替换原有历史
await conversationService.CompressAndSaveHistoryAsync("session_123");
```

---

## ⚙️ 数据表模型

### 1. `AIChatMessage` (通用对话)
| 字段名 | 类型 | 说明 |
| :--- | :--- | :--- |
| Id | long | 主键 |
| SessionId | string | 会话唯一标识 (索引) |
| Role | string | User / Assistant / System |
| Content | string | 消息内容 |
| Metadata | string | 扩展属性 (JSON) |
| CreatedTime | datetime | 创建时间 |

### 2. `TAgentChatMessageStore` (Agent 专用)
| 字段名 | 类型 | 说明 |
| :--- | :--- | :--- |
| Key | string | 存储键 |
| ThreadId | string | Agent 线程 ID |
| MessageText | string | 文本内容 |
| SerializedMessage | string | 完整序列化状态 (Protobuf/Json) |
| Timestamp | long | 时间戳 |

---

## 💡 最佳实践
- **使用基类**: 始终让自定义 Store 继承 `ChatMessageStoreBase`，以确保未来接口升级时的兼容性。
- **定期压缩**: 建议在对话结束后的后台任务中调用 `CompressAndSaveHistoryAsync`，保持数据库轻量化。
- **懒加载会话**: 前端展示会话列表时，使用 `GetSessionsAsync` 分页获取，点击具体会话后再调用 `GetHistoryAsync`，减少流量消耗。
