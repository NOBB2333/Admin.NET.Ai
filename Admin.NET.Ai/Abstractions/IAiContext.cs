using Admin.NET.Ai.Models;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// AI 请求处理委托
/// </summary>
/// <param name="context">AI 上下文</param>
/// <returns></returns>
public delegate Task AiRequestDelegate(IAiContext context);

/// <summary>
/// AI 请求上下文
/// </summary>
public interface IAiContext
{
    /// <summary>
    /// 请求 ID
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// 会话 ID (用于多轮对话)
    /// </summary>
    string? SessionId { get; set; }

    /// <summary>
    /// 系统提示词 (角色设定)
    /// </summary>
    string? SystemPrompt { get; set; }

    /// <summary>
    /// 用户输入 Prompt
    /// </summary>
    string Prompt { get; set; }

    /// <summary>
    /// 额外的请求参数/选项
    /// </summary>  
    IDictionary<string, object?> Options { get; }

    /// <summary>
    /// 上下文数据字典 (用于中间件传递数据)
    /// </summary>
    IDictionary<string, object?> Items { get; }

    /// <summary>
    /// AI 响应结果
    /// </summary>
    object? Result { get; set; }

    /// <summary>
    /// 服务提供者 (用于解析依赖)
    /// </summary>
    IServiceProvider RequestServices { get; }

    /// <summary>
    /// 附件列表 (多模态输入)
    /// </summary>
    List<AiAttachment> Attachments { get; }
}

/// <summary>
/// 默认 AI 上下文实现
/// </summary>
public class AiContext(string prompt, IServiceProvider serviceProvider) : IAiContext
{
    public string RequestId { get; } = Guid.NewGuid().ToString("N");
    public string? SessionId { get; set; }
    public string? SystemPrompt { get; set; }
    public string Prompt { get; set; } = prompt;
    public IDictionary<string, object?> Options { get; } = new Dictionary<string, object?>();
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
    public object? Result { get; set; }
    public IServiceProvider RequestServices { get; } = serviceProvider;
    public List<AiAttachment> Attachments { get; } = [];
}
