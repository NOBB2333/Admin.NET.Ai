using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 会话编排服务接口（MEAI-first 企业标准）
/// 职责：上下文构建、对话保存、历史压缩
/// 
/// 注意：会话/消息查询请直接使用 IChatMessageStore
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// 构建完整对话上下文 (历史 + 压缩 + 用户消息)
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userMessage">用户消息</param>
    /// <param name="compress">是否压缩历史</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整的消息列表，可直接发送给 LLM</returns>
    Task<IList<ChatMessage>> BuildContextAsync(
        string sessionId, 
        ChatMessage userMessage, 
        bool compress = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存一轮对话 (用户消息 + AI响应)
    /// </summary>
    Task SaveTurnAsync(
        string sessionId, 
        ChatMessage userMessage, 
        ChatMessage assistantMessage, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 压缩并持久化历史记录
    /// </summary>
    Task CompressAndSaveAsync(string sessionId, CancellationToken cancellationToken = default);
}
