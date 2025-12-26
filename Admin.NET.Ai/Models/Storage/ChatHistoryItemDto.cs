using System.Text.Json;

namespace Admin.NET.Ai.Models.Storage;

/// <summary>
/// 聊天历史记录传输对象 (用于 Microsoft Agent Framework)
/// </summary>
public class ChatHistoryItemDto
{
    /// <summary>
    /// 唯一键
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// 会话/线程ID
    /// </summary>
    public string ThreadId { get; set; }

    /// <summary>
    /// 消息时间戳
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// 角色 (Assistant/User/System)
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// 序列化后的完整消息对象
    /// </summary>
    public string SerializedMessage { get; set; }

    /// <summary>
    /// 消息文本内容
    /// </summary>
    public string? MessageText { get; set; }

    /// <summary>
    /// 消息ID
    /// </summary>
    public string? MessageId { get; set; }
}
