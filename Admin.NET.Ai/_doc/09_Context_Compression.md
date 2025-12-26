# 09. 上下文管理与压缩 (Context Compression)

## 🎯 设计思维 (Mental Model)
大模型的上下文长度（Context Window）是有限且昂贵的。当对话轮数过多时，不仅会导致 Token 费用飙升，还会因为干扰信息过多导致模型“迷失”。

`Admin.NET.Ai` 采用了多级、多策略的上下文压缩（Reduction）机制。
核心理念是：**“按需保留，智能裁剪”**。

---

## 🏗️ 架构设计
### 核心组件
- **`IChatReducer`**: 上下文缩减逻辑的基础接口。
- **`AdaptiveCompressionReducer`**: 首席裁减官。它会根据当前的 Token 负载自动选择最优的压缩算法。
- **`CompressionMonitor`**: 实时监控当前会话的“饱满度”。

---

## 🛠️ 技术实现 (Implementation)

### 1. 多样化压缩策略 (`Services/Context/`)
本项目实现了 7 种不同的压缩策略，可串联使用：

| 策略名称 | 逻辑说明 | 适用场景 |
| :--- | :--- | :--- |
| **MessageCounting** | 保留最近 N 条消息，其余丢弃。 | 简单聊天 |
| **Summarizing** | 将过期的历史消息让模型自己总结成一段“背景摘要”。 | 长跨度对话 |
| **KeywordAware** | 提取历史关键信息，丢弃修饰语。 | 知识搜索 |
| **SystemMessageProtection** | 无论如何压缩，始终保护 `System Prompt` 不被删除。 | 指令关键场景 |
| **FunctionCallPreservation** | 优先保护未完成的工具调用上下文。 | 复杂 Agent 任务 |
| **LayeredCompression** | 分层处理：前 1/3 总结，中 1/3 抽稀，后 1/3 保留。 | 默认高级推荐 |

### 2. 自适应策略 (`AdaptiveCompressionReducer.cs`)
该组件会监控 `Threshold`（阈值）。当上下文达到 80% 限制时，触发轻量级裁减；达到 95% 时，触发激进的总结压缩。

---

## 🚀 代码示例 (Usage Example)

### 配置自适应压缩
在 `appsettings.json` 中定义触发条件：
```json
{
  "Compression": {
    "Enabled": true,
    "Limit": 10000,
    "Threshold": 0.8,
    "Strategy": "Adaptive"
  }
}
```

### 简单调用
```csharp
// 在创建 Store 时注入 Reducer
var store = new InMemoryChatMessageStore(new AdaptiveCompressionReducer(options));

// 每次 AddMessagesAsync 时，都会自动执行压缩逻辑
await store.AddMessagesAsync(newMessages);
```

---

## 🛡️ 保护机制
为了防止压缩导致业务崩溃，系统支持“关键节点保护”：
- **Anchor Messages**: 标记为关键的消息（如重要的业务 ID）不会被压缩。
- **Token 预测**: 采用 `Tiktoken` 算法进行本地 Token 估算，不等待 API 报错才处理。

---

## 📊 监控界面
在 DevUI 中，你可以直观看到“上下文压缩率”图表。
如果一个 20000 Token 的对话被成功压缩到 2000 Token 且模型回答依然准确，这代表了极高的工程质量。
