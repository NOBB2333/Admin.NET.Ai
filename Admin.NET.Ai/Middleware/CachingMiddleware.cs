using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Admin.NET.Ai.Middleware;

/// <summary>
/// 语义缓存中间件 (基于 DelegatingChatClient)
/// 支持语义检索(占位)和流式响应缓存
/// </summary>
public class CachingMiddleware : DelegatingChatClient
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingMiddleware> _logger;
    private readonly ISemanticCache? _semanticCache;
    
    // 简单的 DTO 用于序列化
    private class CachedChatResponse
    {
        public string? Text { get; set; }
        public string? Role { get; set; }
        public string? FinishReason { get; set; }
    }

    public CachingMiddleware(
        IChatClient innerClient,
        IDistributedCache cache, 
        ILogger<CachingMiddleware> logger,
        ISemanticCache? semanticCache = null)
        : base(innerClient)
    {
        _cache = cache;
        _logger = logger;
        _semanticCache = semanticCache;
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(chatMessages, options);
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";
        
        // 1. 尝试精确匹配
        var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedJson))
        {
             _logger.LogInformation("🎯 [Cache] 精确命中: {CacheKey}", cacheKey);
             return DeserializeResponse(cachedJson);
        }

        // 2. 尝试语义缓存匹配 (如果可用)
        if (_semanticCache != null && !string.IsNullOrEmpty(lastUserMessage))
        {
            var semanticHit = await _semanticCache.FindSimilarAsync(lastUserMessage, 0.85, cancellationToken);
            if (semanticHit != null)
            {
                _logger.LogInformation("🧠 [SemanticCache] 语义命中 (相似度: {Similarity:P2})", semanticHit.Similarity);
                var message = new ChatMessage(ChatRole.Assistant, semanticHit.Response);
                return new ChatResponse(new[] { message });
            }
        }

        // 3. 实际调用
        var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
        
        // 4. 写入缓存
        if (response.Messages.Count > 0)
        {
             await CacheResponseAsync(cacheKey, response);
             
             // 5. 同时写入语义缓存
             if (_semanticCache != null && !string.IsNullOrEmpty(lastUserMessage))
             {
                 var responseText = response.Messages.LastOrDefault()?.Text ?? "";
                 if (!string.IsNullOrEmpty(responseText))
                 {
                     await _semanticCache.AddAsync(lastUserMessage, responseText, cancellationToken: cancellationToken);
                 }
             }
        }
        
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(chatMessages, options);
        
        // 1. 尝试精确匹配缓存
        var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            _logger.LogInformation("🎯 [Cache-Stream] 精确命中: {CacheKey}", cacheKey);
            // 模拟流式回放缓存结果
            var cachedResp = DeserializeResponse(cachedJson);
            foreach (var msg in cachedResp.Messages)
            {
                // 模拟逐字吐出效果 (可选)
                yield return new ChatResponseUpdate(msg.Role, msg.Text);
            }
            yield break;
        }

        // 2. 实际流式调用并收集
        var sb = new StringBuilder();
        ChatRole? role = null;
        
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            if (update.Role.HasValue) role = update.Role;
            if (!string.IsNullOrEmpty(update.Text)) sb.Append(update.Text);
            
            yield return update;
        }
        
        // 3. 流结束，写入缓存
        // 注意：这里我们只缓存了合并后的全量文本，但这足以满足下次 GetResponse 或 流式回放的需求
        if (sb.Length > 0)
        {
            var fakeResponse = new ChatResponse(new[] 
            { 
                new ChatMessage(role ?? ChatRole.Assistant, sb.ToString()) 
            });
            await CacheResponseAsync(cacheKey, fakeResponse);
        }
    }
    
    private string GenerateCacheKey(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        // 键包含：模型(来自Options) + 消息哈希 + 参数
        // 注意：ChatOptions.ModelId 可能为空，如果为空则假设是默认模型或不作为Key的一部分（有风险）
        var model = options?.ModelId ?? "default";
        var msgs = string.Join("|", messages.Select(m => $"{m.Role}:{m.Text}"));
        var settings = $"{model}:{options?.Temperature}:{options?.TopP}";
        var rawKey = $"{settings}||{msgs}";
        
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        return $"chat_cache:{BitConverter.ToString(bytes).Replace("-", "")}";
    }

    private ChatResponse DeserializeResponse(string json)
    {
        var dto = JsonSerializer.Deserialize<CachedChatResponse>(json);
        if (dto == null) return new ChatResponse(new[] { new ChatMessage(ChatRole.Assistant, "") });

        var message = new ChatMessage(new ChatRole(dto.Role ?? "assistant"), dto.Text ?? "");
        return new ChatResponse(new[] { message })
        {
            FinishReason = !string.IsNullOrEmpty(dto.FinishReason) 
                ? new ChatFinishReason(dto.FinishReason) 
                : ChatFinishReason.Stop
        };
    }

    private async Task CacheResponseAsync(string key, ChatResponse response)
    {
        var lastMsg = response.Messages.LastOrDefault();
        if (lastMsg == null) return;

        var dto = new CachedChatResponse
        {
            Text = lastMsg.Text,
            Role = lastMsg.Role.Value,
            FinishReason = response.FinishReason?.Value
        };

        var json = JsonSerializer.Serialize(dto);
        var cacheOptions = new DistributedCacheEntryOptions
        {
             AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
        };

        try 
        {
            await _cache.SetStringAsync(key, json, cacheOptions);
            _logger.LogDebug("💾 [Cache] 已缓存: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "虽然请求成功，但缓存写入失败");
        }
    }
}
