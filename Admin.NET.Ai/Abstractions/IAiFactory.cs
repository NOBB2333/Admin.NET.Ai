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
/// 职责：客户端创建、连接管理、健康检查、重试降级
/// 注意：角色/场景配置请在调用时通过 ChatOptions.Instructions 传递
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
    /// <returns>默认 IChatClient 实例</returns>
    IChatClient? GetDefaultChatClient();

    /// <summary>
    /// 带重试和降级机制获取 Chat Client
    /// </summary>
    /// <param name="name">首选配置名称</param>
    /// <param name="fallbackNames">降级备选名称列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可用的 IChatClient 实例</returns>
    Task<IChatClient> GetChatClientWithFallbackAsync(string name, IEnumerable<string>? fallbackNames = null, CancellationToken cancellationToken = default);

    #endregion

    #region 客户端发现与管理

    /// <summary>
    /// 获取所有可用的客户端配置名称
    /// </summary>
    /// <returns>配置名称列表</returns>
    IReadOnlyList<string> GetAvailableClients();

    /// <summary>
    /// 获取默认提供商名称
    /// </summary>
    string? DefaultProvider { get; }

    /// <summary>
    /// 刷新指定客户端（清除缓存，下次获取时重新创建）
    /// </summary>
    /// <param name="name">客户端配置名称，为 null 时刷新所有客户端</param>
    void RefreshClient(string? name = null);

    #endregion

    #region 健康检查

    /// <summary>
    /// 检查指定客户端的健康状态
    /// </summary>
    /// <param name="name">客户端配置名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康状态</returns>
    Task<ClientHealthStatus> CheckHealthAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查所有客户端的健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有客户端的健康状态</returns>
    Task<IReadOnlyList<ClientHealthStatus>> CheckAllHealthAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Agent 管理

    /// <summary>
    /// 获取 Agent (封装了 Chat Client)
    /// </summary>
    /// <typeparam name="TAgent">Agent 类型 (e.g. ChatCompletionAgent)</typeparam>
    /// <param name="name">配置名称</param>
    /// <param name="instructions">Agent 预设指令/系统提示词</param>
    /// <returns>Agent 实例</returns>
    TAgent? GetAgent<TAgent>(string name, string? instructions = null) where TAgent : class;

    /// <summary>
    /// 创建 Agent (指定 Client 名称)
    /// </summary>
    /// <param name="clientName">Client 配置名称</param>
    /// <param name="agentName">Agent 名称</param>
    /// <param name="instructions">预设指令</param>
    TAgent? CreateAgent<TAgent>(string clientName, string agentName, string? instructions = null) where TAgent : class;

    /// <summary>
    /// 创建使用默认 Client 的 Agent
    /// </summary>
    TAgent? CreateDefaultAgent<TAgent>(string agentName, string? instructions = null) where TAgent : class;

    /// <summary>
    /// 获取默认 Agent
    /// </summary>
    TAgent? GetDefaultAgent<TAgent>(string? instructions = null) where TAgent : class;

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
