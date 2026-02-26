using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 对话存储演示 - 多种存储策略
/// </summary>
public static class StorageDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        Console.WriteLine("\n=== [24] 存储策略 (Hot/Cold/Vector) ===\n");

        // ===== 1. 内存存储 =====
        Console.WriteLine("--- 1. InMemoryChatMessageStore (默认) ---");
        Console.WriteLine(@"
最简单的存储方式，适合开发和测试。

var store = new InMemoryChatMessageStore();
await store.AddMessagesAsync(messages);
var history = await store.GetMessagesAsync();
");

        // ===== 2. 文件存储 =====
        Console.WriteLine("\n--- 2. FileChatMessageStore (JSON文件) ---");
        Console.WriteLine(@"
适合单机应用的轻量持久化。

var store = new FileChatMessageStore(
    filePath: ""chat-history.json"",
    reducer: new MessageCountingChatReducer(20) // 可选压缩器
);

特点:
- 线程安全 (SemaphoreSlim)
- 自动压缩 (配合 IChatReducer)
- JSON 序列化
");

        // ===== 3. 数据库存储 =====
        Console.WriteLine("\n--- 3. DatabaseChatMessageStore (SqlSugar) ---");
        Console.WriteLine(@"
企业级数据库存储，支持 SqlSugar ORM。

var store = new DatabaseChatMessageStore(
    db: sqlSugarClient,
    threadId: ""user-123-thread-456""
);

// 或使用工厂模式
services.AddScoped<IChatMessageStore>(sp => 
{
    var db = sp.GetRequiredService<ISqlSugarClient>();
    var threadId = sp.GetRequiredService<ICurrentUser>().ThreadId;
    return new DatabaseChatMessageStore(db, threadId);
});
");

        // ===== 4. Redis 分布式存储 =====
        Console.WriteLine("\n--- 4. RedisChatMessageStore (分布式) ---");
        Console.WriteLine(@"
适合多实例部署的分布式场景。

var store = new RedisChatMessageStore(
    redisCache: distributedCache,
    threadId: ""user-123"",
    expiration: TimeSpan.FromDays(7)
);

特点:
- 自动过期
- 多实例共享
- 高性能读写
");

        // ===== 5. 向量存储 =====
        Console.WriteLine("\n--- 5. VectorChatMessageStore (语义检索) ---");
        Console.WriteLine(@"
支持语义搜索的向量存储。

var store = new VectorChatMessageStore(
    textSearchProvider: vectorSearchProvider,
    logger: logger
);

// 语义搜索历史消息
var similar = await store.SearchSimilarAsync(
    query: ""关于机器学习的讨论"",
    topK: 5
);

用途:
- 对话回忆 (Recall)
- 上下文增强
- 历史知识检索
");

        // ===== 6. 混合存储 =====
        Console.WriteLine("\n--- 6. HybridChatMessageStore (热冷分离) ---");
        Console.WriteLine(@"
结合 Redis 和数据库的混合策略。

var store = new HybridChatMessageStore(
    redisCache: redisCache,
    database: sqlSugarClient,
    hotDataExpiration: TimeSpan.FromHours(24)
);

策略:
- 热数据 (24h内): Redis 快速读写
- 冷数据 (24h后): 数据库持久存储
- 自动迁移: 热数据过期后自动归档
");

        // ===== 7. 对话摘要 =====
        Console.WriteLine("\n--- 7. ConversationSummarizer (上下文压缩) ---");
        Console.WriteLine(@"
使用 LLM 自动压缩对话历史。

var summarizer = sp.GetRequiredService<ConversationSummarizer>();

// 生成摘要
var summary = await summarizer.SummarizeAsync(messages);

// 压缩历史 (保留最近N条 + 摘要)
var compressed = await summarizer.CompressHistoryAsync(
    messages,
    keepRecent: 5
);

// 压缩后的结构:
// [System: 你是一个助手]
// [System: 对话摘要: 用户询问了关于C#的问题...]
// [最近的5条消息...]
");

        // ===== 8. 存储配置示例 =====
        Console.WriteLine("\n--- 8. 存储策略配置 (ServiceCollectionInit) ---");
        Console.WriteLine(@"
// 注册存储服务
services.TryAddScoped<InMemoryChatMessageStore>();
services.TryAddScoped<FileChatMessageStore>();
services.TryAddScoped<DatabaseChatMessageStore>();
services.TryAddScoped<RedisChatMessageStore>();
services.TryAddScoped<VectorChatMessageStore>();
services.TryAddScoped<HybridChatMessageStore>();
services.TryAddScoped<ConversationSummarizer>();

// 配置默认存储
services.TryAddScoped<IChatMessageStore, HybridChatMessageStore>();
");

        Console.WriteLine("\n========== 对话存储演示结束 ==========");
    }
}
