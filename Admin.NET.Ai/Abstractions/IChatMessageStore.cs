using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 会话信息摘要
/// </summary>
public record SessionInfo(string SessionId, DateTime CreatedAt, DateTime LastMessageAt,
    int MessageCount, string? Title = null, Dictionary<string, object>? Metadata = null );

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageIndex, int PageSize )
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex < TotalPages - 1;
}

/// <summary>
/// 对话消息存储接口（五星级企业标准）
/// 支持：批量操作、分页、元数据、会话管理
/// </summary>
public interface IChatMessageStore
{
    #region 基础操作

    /// <summary>
    /// 获取对话历史
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整对话历史</returns>
    Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存单条消息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="message">消息内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除历史
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    #endregion

    #region 批量操作

    /// <summary>
    /// 批量保存消息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="messages">消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveMessagesAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换整个对话历史（用于压缩后更新）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="messages">新的消息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReplaceHistoryAsync(string sessionId, IEnumerable<ChatMessageContent> messages, CancellationToken cancellationToken = default);

    #endregion

    #region 分页与查询

    /// <summary>
    /// 分页获取对话历史
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="pageIndex">页码（从0开始）</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<ChatMessageContent>> GetPagedHistoryAsync(
        string sessionId, 
        int pageIndex = 0, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近N条消息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="count">消息数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<IReadOnlyList<ChatMessageContent>> GetRecentMessagesAsync(
        string sessionId, 
        int count, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取消息数量
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<int> GetMessageCountAsync(string sessionId, CancellationToken cancellationToken = default);

    #endregion

    #region 会话管理

    /// <summary>
    /// 检查会话是否存在
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有会话列表
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话信息列表</returns>
    Task<PagedResult<SessionInfo>> GetSessionsAsync(
        int pageIndex = 0, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话信息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<SessionInfo?> GetSessionInfoAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新会话标题
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="title">新标题</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken cancellationToken = default);

    #endregion
}

