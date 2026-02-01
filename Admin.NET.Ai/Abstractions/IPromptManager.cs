using Admin.NET.Ai.Services.Prompt;

namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 提示词管理接口
/// </summary>
public interface IPromptManager
{
    /// <summary>
    /// 获取提示词模板
    /// </summary>

    /// <summary>
    /// 获取提示词配置 (支持版本控制)
    /// </summary>
    /// <param name="name">提示词名称</param>
    /// <param name="version">版本号 (默认为 latest)</param>
    Task<PromptConfig?> GetPromptConfigAsync(string name, string version = "latest");

    /// <summary>
    /// 渲染提示词模版
    /// </summary>
    /// <param name="template">模版字符串</param>
    /// <param name="variables">变量集合</param>
    Task<string> RenderPromptAsync(string template, Dictionary<string, object> variables);
    
    /// <summary>
    /// 获取并渲染 (快捷方法)
    /// </summary>
    Task<string> GetRenderedPromptAsync(string name, Dictionary<string, object>? variables = null, string version = "latest");

    /// <summary>
    /// 注册/更新提示词 (Legacy/Dynamic)
    /// </summary>
    Task RegisterPromptAsync(string name, string template);
}


