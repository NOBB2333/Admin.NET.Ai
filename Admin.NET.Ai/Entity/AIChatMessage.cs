using Furion.DatabaseAccessor;
using SqlSugar;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Admin.NET.Ai.Entity;

/// <summary>
/// AI聊天记录
/// </summary>
public class AIChatMessage
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Description("创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(Length = 64, IsNullable = false)]
    [Description("会话ID")]
    public string SessionId { get; set; }

    /// <summary>
    /// 角色 (User/Assistant/System)
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    [Description("角色")]
    public string Role { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    [Description("消息内容")]
    public string? Content { get; set; }

    /// <summary>
    /// 附件地址/多模态内容 (JSON List string)
    /// </summary>
    [SugarColumn(IsJson = true, ColumnDataType = "nvarchar(max)", IsNullable = true)]
    [Description("附件")]
    public string? Attachments { get; set; }

    /// <summary>
    /// 元数据/扩展属性 (JSON)
    /// </summary>
    [SugarColumn(IsJson = true, ColumnDataType = "nvarchar(max)", IsNullable = true)]
    [Description("元数据")]
    public string? Metadata { get; set; }
}
