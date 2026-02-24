namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// AI Agent 基础接口，定义了 Agent 的基本元数据
/// </summary>
public interface IAiAgent
{
    /// <summary>
    /// Agent 名称
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Agent 系统指令/提示词
    /// </summary>
    string Instructions { get; set; }
}
