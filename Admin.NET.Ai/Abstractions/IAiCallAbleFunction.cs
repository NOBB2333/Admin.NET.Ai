using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// AI 工具接口 (用于自动发现和注册 Function Calling)
/// </summary>
public interface IAiCallAbleFunction
{
    /// <summary>
    /// 工具名称 (唯一标识)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 工具描述 (提供给 LLM 理解用途)
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 如果工具逻辑是静态的，或者您希望直接提供 AIFunction 对象，可实现此方法。
    /// 否则，ToolManager 会扫描实现类中的 [Description] 方法。
    /// </summary>
    /// <returns></returns>
    IEnumerable<AIFunction> GetFunctions();
}
