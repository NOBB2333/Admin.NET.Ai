namespace Admin.NET.Ai.Abstractions;

using Microsoft.Extensions.AI;

/// <summary>
/// AI 客户端健康状态
/// </summary>
public record ClientHealthStatus(
    string ClientName,
    bool IsHealthy,
    TimeSpan? ResponseTime = null,
    string? ErrorMessage = null,
    DateTime CheckedAt = default
)
{
    public DateTime CheckedAt { get; init; } = CheckedAt == default ? DateTime.UtcNow : CheckedAt;
}

/// <summary>
/// AI 工厂接口 (MEAI-first, 企业级标准)
/// 职责：客户端创建、Agent创建、连接管理、健康检查、重试降级
/// </summary>
public interface IAiFactory : IDisposable, IAsyncDisposable
{
    #region 核心客户端获取

    /// <summary>
    /// 获取 Chat Client (带 Pipeline 管道)
    /// </summary>
    /// <param name="name">配置名称 (e.g. "DeepSeek")</param>
    /// <returns>IChatClient 实例，如果配置不存在则返回 null</returns>
    IChatClient? GetChatClient(string name);

    /// <summary>
    /// 获取默认 Chat Client
    /// </summary>
    IChatClient? GetDefaultChatClient();

    /// <summary>
    /// 带重试和降级机制获取 Chat Client
    /// </summary>
    Task<IChatClient> GetChatClientWithFallbackAsync(string name, IEnumerable<string>? fallbackNames = null, CancellationToken cancellationToken = default);

    #endregion

    #region 客户端发现与管理

    /// <summary>
    /// 获取所有可用的客户端配置名称
    /// </summary>
    IReadOnlyList<string> GetAvailableClients();

    /// <summary>
    /// 获取默认提供商名称
    /// </summary>
    string? DefaultProvider { get; }

    /// <summary>
    /// 刷新指定客户端（清除缓存，下次获取时重新创建）
    /// </summary>
    void RefreshClient(string? name = null);

    #endregion

    #region 健康检查

    /// <summary>
    /// 检查指定客户端的健康状态
    /// </summary>
    Task<ClientHealthStatus> CheckHealthAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查所有客户端的健康状态
    /// </summary>
    Task<IReadOnlyList<ClientHealthStatus>> CheckAllHealthAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Agent 创建 (精简为2个重载)

    /// <summary>
    /// 创建 Agent (指定 Client)
    /// </summary>
    /// <typeparam name="TAgent">Agent 类型 (e.g. ChatCompletionAgent)</typeparam>
    /// <param name="clientName">Client 配置名称</param>
    /// <param name="agentName">Agent 名称 (可选，默认使用 clientName)</param>
    /// <param name="instructions">Agent 预设指令/系统提示词</param>
    TAgent? CreateAgent<TAgent>(string clientName, string? agentName = null, string? instructions = null) where TAgent : class;

    /// <summary>
    /// 创建 Agent (使用默认 Client)
    /// </summary>
    /// <typeparam name="TAgent">Agent 类型 (e.g. ChatCompletionAgent)</typeparam>
    /// <param name="agentName">Agent 名称 (可选)</param>
    /// <param name="instructions">Agent 预设指令/系统提示词</param>
    TAgent? CreateDefaultAgent<TAgent>(string? agentName = null, string? instructions = null) where TAgent : class;

    #endregion

    #region 泛型客户端获取

    /// <summary>
    /// 获取指定类型的 Client (e.g. Kernel)
    /// </summary>
    T? GetClient<T>(string name) where T : class;

    /// <summary>
    /// 获取默认的指定类型 Client
    /// </summary>
    T? GetDefaultClient<T>() where T : class;

    #endregion
}
