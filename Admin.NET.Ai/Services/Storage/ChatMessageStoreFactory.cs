using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Storage;
using Admin.NET.Ai.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.NET.Ai.Services;

/// <summary>
/// 存储类型枚举
/// </summary>
public enum StorageType
{
    /// <summary>文件存储 (默认)</summary>
    File,
    /// <summary>内存存储</summary>
    InMemory,
    /// <summary>数据库存储 (SqlSugar)</summary>
    Database,
    /// <summary>Redis 存储</summary>
    Redis,
    /// <summary>向量存储</summary>
    Vector,
    /// <summary>CosmosDB 存储</summary>
    CosmosDB,
    /// <summary>混合存储</summary>
    Hybrid
}

/// <summary>
/// ChatMessageStore 工厂 - 动态创建存储实例
/// </summary>
public class ChatMessageStoreFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ChatMessageStoreFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 创建指定类型的存储
    /// </summary>
    public IChatMessageStore Create(StorageType type)
    {
        return type switch
        {
            StorageType.File => _serviceProvider.GetRequiredService<FileChatMessageStore>(),
            StorageType.InMemory => _serviceProvider.GetRequiredService<InMemoryChatMessageStore>(),
            StorageType.Database => _serviceProvider.GetRequiredService<DatabaseChatMessageStore>(),
            StorageType.Redis => _serviceProvider.GetRequiredService<RedisChatMessageStore>(),
            StorageType.Vector => _serviceProvider.GetRequiredService<VectorChatMessageStore>(),
            StorageType.CosmosDB => _serviceProvider.GetRequiredService<CosmosDBChatMessageStore>(),
            StorageType.Hybrid => _serviceProvider.GetRequiredService<HybridChatMessageStore>(),
            _ => throw new ArgumentException($"Unknown storage type: {type}")
        };
    }

    /// <summary>
    /// 获取默认存储 (基于 DI 注册的 IChatMessageStore)
    /// </summary>
    public IChatMessageStore GetDefault() => _serviceProvider.GetRequiredService<IChatMessageStore>();
}
