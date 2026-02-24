using Admin.NET.Ai.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 内容安全中间件 - 敏感词过滤和 PII 脱敏
/// 支持流式输出的滑动窗口缓冲检测
/// </summary>
public class ContentSafetyMiddleware : DelegatingChatClient
{
    private readonly ContentSafetyOptions _options;
    private readonly ILogger<ContentSafetyMiddleware> _logger;
    private readonly List<(Regex Pattern, string Replacement)> _sensitivePatterns;
    private readonly List<(Regex Pattern, string Replacement, string Name)> _piiPatterns;
    private readonly List<(Regex Pattern, string Replacement, string Name)> _regexPatterns;

    public ContentSafetyMiddleware(
        IChatClient innerClient,
        IOptions<ContentSafetyOptions> options,
        ILogger<ContentSafetyMiddleware> logger) : base(innerClient)
    {
        _options = options.Value;
        _logger = logger;

        // 预编译敏感词正则 (精确匹配，支持自定义替换)
        _sensitivePatterns = _options.SensitiveWords
            .Where(kv => !string.IsNullOrEmpty(kv.Key))
            .Select(kv => (
                Pattern: new Regex(Regex.Escape(kv.Key), RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Replacement: kv.Value ?? _options.DefaultMask
            ))
            .ToList();

        // 预编译自定义正则模式 (用于匹配变体如 A-B A*B 等)
        _regexPatterns = _options.SensitiveWordPatterns
            .Where(kv => !string.IsNullOrEmpty(kv.Value.Pattern))
            .Select(kv => (
                Pattern: new Regex(kv.Value.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Replacement: kv.Value.Replacement ?? _options.DefaultMask,
                Name: kv.Key
            ))
            .ToList();

        // 预编译 PII 正则
        _piiPatterns = _options.EnablePiiMasking
            ? _options.PiiRules
                .Where(kv => !string.IsNullOrEmpty(kv.Value.Pattern))
                .Select(kv => (
                    Pattern: new Regex(kv.Value.Pattern, RegexOptions.Compiled),
                    Replacement: kv.Value.Replacement,
                    Name: kv.Key
                ))
                .ToList()
            : new();

        _logger.LogDebug("ContentSafetyMiddleware 初始化: {Sensitive} 精确, {Regex} 正则, {Pii} PII",
            _sensitivePatterns.Count, _regexPatterns.Count, _piiPatterns.Count);
    }

    /// <summary>
    /// 非流式响应 - 完整过滤
    /// </summary>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return await base.GetResponseAsync(messages, options, cancellationToken);

        // 输入过滤
        var filteredMessages = _options.CheckInput
            ? messages.Select(FilterMessage).ToList()
            : messages.ToList();

        var response = await base.GetResponseAsync(filteredMessages, options, cancellationToken);

        // 输出过滤
        if (_options.CheckOutput && response.Messages != null)
        {
            var filteredResponseMessages = response.Messages.Select(FilterMessage).ToList();

            // 检查是否需要拦截
            if (_options.ViolationAction == ViolationAction.Block && HasViolation(response))
            {
                return CreateBlockedResponse();
            }

            return new ChatResponse(filteredResponseMessages)
            {
                Usage = response.Usage,
                FinishReason = response.FinishReason,
                ModelId = response.ModelId,
                CreatedAt = response.CreatedAt
            };
        }

        return response;
    }

    /// <summary>
    /// 流式响应 - 滑动窗口缓冲过滤
    /// </summary>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
                yield return update;
            yield break;
        }

        // 输入过滤
        var filteredMessages = _options.CheckInput
            ? messages.Select(FilterMessage).ToList()
            : messages.ToList();

        if (!_options.CheckOutput)
        {
            await foreach (var update in base.GetStreamingResponseAsync(filteredMessages, options, cancellationToken))
                yield return update;
            yield break;
        }

        // 滑动窗口缓冲过滤
        var buffer = new StringBuilder();
        var bufferSize = _options.StreamBufferSize;
        var hasViolation = false;

        await foreach (var update in base.GetStreamingResponseAsync(filteredMessages, options, cancellationToken))
        {
            if (string.IsNullOrEmpty(update.Text))
            {
                yield return update;
                continue;
            }

            buffer.Append(update.Text);

            // 当缓冲区足够大时，输出安全的前缀部分
            while (buffer.Length > bufferSize)
            {
                var safeLength = buffer.Length - bufferSize;
                var safeText = buffer.ToString(0, safeLength);

                // 过滤并输出
                var (filtered, violated) = FilterText(safeText);
                
                if (violated) hasViolation = true;

                if (_options.ViolationAction == ViolationAction.Block && hasViolation)
                {
                    yield return CreateTextUpdate(_options.BlockMessage);
                    yield break;
                }

                yield return CreateTextUpdate(filtered, update.Role, update.ModelId, update.CreatedAt);
                buffer.Remove(0, safeLength);
            }
        }

        if (buffer.Length > 0)
        {
            var (filtered, violated) = FilterText(buffer.ToString());
            if (violated) hasViolation = true;

            if (_options.ViolationAction == ViolationAction.Block && hasViolation)
            {
                yield return CreateTextUpdate(_options.BlockMessage);
                yield break;
            }

            yield return CreateTextUpdate(filtered);
        }

        if (hasViolation && _options.ViolationAction == ViolationAction.LogOnly)
        {
            _logger.LogWarning("⚠️ 检测到敏感内容 (仅记录)");
        }
    }

    #region Private Helpers

    private ChatMessage FilterMessage(ChatMessage message)
    {
        if (string.IsNullOrEmpty(message.Text))
            return message;

        var (filtered, violated) = FilterText(message.Text);
        if (violated)
            _logger.LogDebug("🔒 消息已过滤");

        return new ChatMessage(message.Role, filtered);
    }

    private (string Filtered, bool HasViolation) FilterText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return (text, false);

        var result = text;
        var hasViolation = false;

        // 1. 精确敏感词过滤
        foreach (var (pattern, replacement) in _sensitivePatterns)
        {
            if (pattern.IsMatch(result))
            {
                hasViolation = true;
                result = pattern.Replace(result, replacement);
            }
        }

        // 2. 正则敏感词过滤
        foreach (var (pattern, replacement, _) in _regexPatterns)
        {
            if (pattern.IsMatch(result))
            {
                hasViolation = true;
                result = pattern.Replace(result, replacement);
            }
        }

        // 3. PII 脱敏
        foreach (var (pattern, replacement, _) in _piiPatterns)
        {
            if (pattern.IsMatch(result))
            {
                result = pattern.Replace(result, replacement);
            }
        }

        return (result, hasViolation);
    }

    private bool HasViolation(ChatResponse response)
    {
        return response.Messages?.Any(m =>
            !string.IsNullOrEmpty(m.Text) && FilterText(m.Text).HasViolation
        ) ?? false;
    }

    private ChatResponse CreateBlockedResponse()
    {
        _logger.LogWarning("⛔ 响应被拦截");
        return new ChatResponse(new[] { new ChatMessage(ChatRole.Assistant, _options.BlockMessage) });
    }

    private static ChatResponseUpdate CreateTextUpdate(
        string text,
        ChatRole? role = null,
        string? modelId = null,
        DateTimeOffset? createdAt = null)
    {
        return new ChatResponseUpdate
        {
            Role = role ?? ChatRole.Assistant,
            Contents = new List<AIContent> { new TextContent(text) },
            ModelId = modelId,
            CreatedAt = createdAt
        };
    }

    #endregion
}
