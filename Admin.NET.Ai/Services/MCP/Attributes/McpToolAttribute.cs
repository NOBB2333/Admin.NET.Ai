using System.Runtime.CompilerServices;

namespace Admin.NET.Ai.Services.MCP.Attributes;

/// <summary>
/// 将 ASP.NET Core Action 或普通方法标记为 MCP 工具。
/// 使用方式:
///   [McpTool("获取天气信息")]                    // 名称默认为方法名
///   [McpTool("get_weather", "获取天气信息")]     // 显式指定名称
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class McpToolAttribute : Attribute
{
    /// <summary>
    /// 工具名称 (null 时默认使用方法名)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 工具分类/标签
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 是否需要审批
    /// </summary>
    public bool RequiresApproval { get; set; } = false;

    /// <summary>
    /// 超时时间 (秒)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 只传描述，名称默认使用方法名
    /// </summary>
    /// <param name="description">工具描述</param>
    public McpToolAttribute(string description)
    {
        Name = null; // 将在注册时使用方法名
        Description = description;
    }

    /// <summary>
    /// 显式指定名称和描述
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <param name="description">工具描述</param>
    public McpToolAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

/// <summary>
/// 标记参数的 MCP 属性 (可选)
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public class McpParameterAttribute : Attribute
{
    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 是否必填 (默认按 C# 参数定义)
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// 示例值
    /// </summary>
    public string? Example { get; set; }

    public McpParameterAttribute(string description)
    {
        Description = description;
    }
}
