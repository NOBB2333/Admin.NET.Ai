namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// AI Agent 基础接口，定义了 Agent 的基本元数据
/// 支持 LLM 自主发现和调度
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

    /// <summary>
    /// Agent 能力描述（LLM 用来决定是否调用此 Agent）
    /// 默认取 Instructions 前 100 字
    /// </summary>
    string Capability => Instructions.Length > 100 ? Instructions[..100] + "..." : Instructions;

    /// <summary>
    /// 此 Agent 允许使用的工具名列表
    /// null 表示全部可用
    /// </summary>
    IReadOnlyList<string>? AllowedTools => null;

    /// <summary>
    /// 最大迭代次数（防止无限循环）
    /// </summary>
    int MaxIterations => 10;
}
