using System.Text.Json.Serialization;

namespace Admin.NET.Ai.Options;

/// <summary>
/// 内容安全配置选项
/// </summary>
public class ContentSafetyOptions
{
    /// <summary>
    /// 是否启用内容安全检查
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否检查输入内容
    /// </summary>
    public bool CheckInput { get; set; } = true;

    /// <summary>
    /// 是否检查输出内容
    /// </summary>
    public bool CheckOutput { get; set; } = true;

    /// <summary>
    /// 流式输出缓冲区大小 (字符数)
    /// 建议设为最长敏感词长度，默认 10
    /// </summary>
    public int StreamBufferSize { get; set; } = 10;

    /// <summary>
    /// 敏感词替换规则 (精确匹配，支持自定义替换词)
    /// Key: 敏感词, Value: 替换为的内容 (null 或空表示使用默认掩码)
    /// </summary>
    public Dictionary<string, string?> SensitiveWords { get; set; } = new()
    {
        // 示例配置
        // { "敏感词", "和谐词" },   // 自定义替换
        // { "违禁词", null },       // 使用默认掩码 ***
    };

    /// <summary>
    /// 敏感词正则模式 (用于匹配变体，如 "傻-逼"、"傻*逼"、"傻 逼" 等)
    /// Key: 名称, Value: 正则规则
    /// </summary>
    public Dictionary<string, SensitiveWordPattern> SensitiveWordPatterns { get; set; } = new()
    {
        // 示例: 匹配 傻逼 及其变体 (傻-逼、傻*逼、傻 逼、傻　逼 等)
        // { "傻逼变体", new SensitiveWordPattern { Pattern = @"傻[\s\-\*_\.·]*逼", Replacement = "SB" } }
    };

    /// <summary>
    /// 默认掩码字符
    /// </summary>
    public string DefaultMask { get; set; } = "***";

    /// <summary>
    /// PII (个人身份信息) 脱敏规则
    /// Key: 名称, Value: 正则表达式
    /// </summary>
    public Dictionary<string, PiiRule> PiiRules { get; set; } = new()
    {
        { "手机号", new PiiRule { Pattern = @"1[3-9]\d{9}", Replacement = "1**********" } },
        { "身份证", new PiiRule { Pattern = @"\d{17}[\dXx]", Replacement = "****" } },
        { "邮箱", new PiiRule { Pattern = @"[\w.-]+@[\w.-]+\.\w+", Replacement = "****@****.***" } },
        { "银行卡", new PiiRule { Pattern = @"\d{16,19}", Replacement = "****" } }
    };

    /// <summary>
    /// 是否对 PII 进行脱敏
    /// </summary>
    public bool EnablePiiMasking { get; set; } = true;

    /// <summary>
    /// 违规时的处理方式
    /// </summary>
    public ViolationAction ViolationAction { get; set; } = ViolationAction.Replace;

    /// <summary>
    /// 违规拦截时返回的提示消息
    /// </summary>
    public string BlockMessage { get; set; } = "抱歉，该内容包含违规信息，无法显示。";
}

/// <summary>
/// 敏感词正则模式规则
/// </summary>
public class SensitiveWordPattern
{
    /// <summary>
    /// 正则表达式模式
    /// 例如: "傻[\s\-\*_\.·]*逼" 匹配 傻逼、傻-逼、傻*逼、傻 逼 等变体
    /// </summary>
    public string Pattern { get; set; } = "";

    /// <summary>
    /// 替换内容 (null 或空表示使用默认掩码)
    /// </summary>
    public string? Replacement { get; set; }
}

/// <summary>
/// PII 脱敏规则
/// </summary>
public class PiiRule
{
    /// <summary>
    /// 正则表达式模式
    /// </summary>
    public string Pattern { get; set; } = "";

    /// <summary>
    /// 替换内容
    /// </summary>
    public string Replacement { get; set; } = "****";
}

/// <summary>
/// 违规处理方式
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ViolationAction
{
    /// <summary>
    /// 替换敏感内容后继续输出
    /// </summary>
    Replace,

    /// <summary>
    /// 拦截整个响应
    /// </summary>
    Block,

    /// <summary>
    /// 仅记录日志，不做处理
    /// </summary>
    LogOnly
}
