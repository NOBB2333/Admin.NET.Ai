using Admin.NET.Ai.Services.Context;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Admin.NET.Ai.Services;

/// <summary>
/// Reducer 类型枚举
/// </summary>
public enum ReducerType
{
    /// <summary>自适应压缩 (默认)</summary>
    Adaptive,
    /// <summary>消息计数 (保留最近 N 条)</summary>
    MessageCounting,
    /// <summary>摘要压缩 (使用 LLM 生成摘要)</summary>
    Summarizing,
    /// <summary>关键词优先</summary>
    KeywordAware,
    /// <summary>分层压缩</summary>
    Layered,
    /// <summary>系统消息保护</summary>
    SystemMessageProtection,
    /// <summary>函数调用保留</summary>
    FunctionCallPreservation
}

/// <summary>
/// ChatReducer 工厂 - 动态创建 Reducer 实例
/// </summary>
public class ChatReducerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ChatReducerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 创建指定类型的 Reducer
    /// </summary>
    public IChatReducer Create(ReducerType type)
    {
        return type switch
        {
            ReducerType.Adaptive => _serviceProvider.GetRequiredService<AdaptiveCompressionReducer>(),
            ReducerType.MessageCounting => _serviceProvider.GetRequiredService<MessageCountingReducer>(),
            ReducerType.Summarizing => _serviceProvider.GetRequiredService<SummarizingReducer>(),
            ReducerType.KeywordAware => _serviceProvider.GetRequiredService<KeywordAwareReducer>(),
            ReducerType.Layered => _serviceProvider.GetRequiredService<LayeredCompressionReducer>(),
            ReducerType.SystemMessageProtection => _serviceProvider.GetRequiredService<SystemMessageProtectionReducer>(),
            ReducerType.FunctionCallPreservation => _serviceProvider.GetRequiredService<FunctionCallPreservationReducer>(),
            _ => throw new ArgumentException($"Unknown reducer type: {type}")
        };
    }

    /// <summary>
    /// 获取默认 Reducer (Adaptive)
    /// </summary>
    public IChatReducer GetDefault() => Create(ReducerType.Adaptive);
}
