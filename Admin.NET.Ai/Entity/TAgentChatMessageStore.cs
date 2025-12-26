using SqlSugar;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Admin.NET.Ai.Entity;

/// <summary>
/// 专门用于存储 Microsoft Agent Framework (MAF) 聊天记录的表
/// </summary>
[SugarTable("TAgentChatMessageStore")]
[Description("专门用于存储 Microsoft Agent Framework (MAF) 聊天记录的表")]
public class TAgentChatMessageStore
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 唯一标识键
    /// </summary>
    [SugarColumn(Length = 200)]
    public string Key { get; set; }

    /// <summary>
    /// 会话/线程ID
    /// </summary>
    [SugarColumn(Length = 100)]
    public string ThreadId { get; set; }

    /// <summary>
    /// 消息时间戳
    /// </summary>
    [Description("消息的时间戳")]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// 角色标识 (User/Assistant/System)
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Role { get; set; }

    /// <summary>
    /// 序列化的消息内容
    /// </summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)")]
    public string SerializedMessage { get; set; }

    /// <summary>
    /// 消息纯文本
    /// </summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? MessageText { get; set; }

    /// <summary>
    /// 消息原始ID
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? MessageId { get; set; }

    /// <summary>
    /// 创建人ID
    /// </summary>
    public long? CreateUserId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDelete { get; set; } = false;
}
