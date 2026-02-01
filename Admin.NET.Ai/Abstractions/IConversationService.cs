using Microsoft.SemanticKernel;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 会话管理服务接口（五星级企业标准）
/// 支持：线程隔离、压缩集成、完整会话管理
/// </summary>
public interface IConversationService
{
    #region 基础操作

    /// <summary>
    /// 获取或创建会话上下文
    /// </summary>
    Task<IAiContext> GetContextAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 保存会话
    /// </summary>
    Task SaveContextAsync(string sessionId, IAiContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除会话
    /// </summary>
    Task DeleteContextAsync(string sessionId, CancellationToken cancellationToken = default);

    #endregion

    #region 增强功能

    /// <summary>
    /// 获取历史记录
    /// </summary>
    Task<IEnumerable<ChatMessageContent>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取并压缩历史记录（如果配置了压缩器）
    /// </summary>
    Task<IEnumerable<ChatMessageContent>> GetCompressedHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 压缩并持久化历史记录
    /// </summary>
    Task CompressAndSaveHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话信息
    /// </summary>
    Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有会话列表
    /// </summary>
    Task<PagedResult<SessionInfo>> GetSessionsAsync(int pageIndex = 0, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新会话标题
    /// </summary>
    Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default);

    #endregion
}

