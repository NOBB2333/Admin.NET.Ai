using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// 文件对话存储（MEAI-first，继承基类）
/// </summary>
public class FileChatMessageStore : ChatMessageStoreBase
{
    private readonly string _basePath;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public FileChatMessageStore()
    {
        _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "ChatHistory");
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public override async Task<IList<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(sessionId);
        if (!File.Exists(filePath))
        {
            return new List<ChatMessage>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var dtos = JsonSerializer.Deserialize<List<ChatMessageDto>>(json);
            if (dtos != null)
            {
                return dtos.Select(d => new ChatMessage(ParseChatRole(d.Role), d.Content ?? "")).ToList();
            }
            return new List<ChatMessage>();
        }
        catch
        {
            return new List<ChatMessage>();
        }
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var history = await GetHistoryAsync(sessionId, cancellationToken);
            history.Add(message);

            var filePath = GetFilePath(sessionId);
            var dtos = history.Select(m => new ChatMessageDto { Role = m.Role.Value, Content = m.Text }).ToList();
            var json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public override Task ClearHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = GetFilePath(sessionId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    private string GetFilePath(string sessionId)
    {
        var safeId = string.Join("_", sessionId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_basePath, $"{safeId}.json");
    }

    private static ChatRole ParseChatRole(string? role)
    {
        return role?.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User
        };
    }

    // 用于序列化的 DTO
    private class ChatMessageDto
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }
}
