using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Storage;

/// <summary>
/// 文件对话存储（继承基类，实现3个核心方法）
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

    public override async Task<ChatHistory> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(sessionId);
        if (!File.Exists(filePath))
        {
            return new ChatHistory();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var messages = JsonSerializer.Deserialize<List<ChatMessageContent>>(json);
            var history = new ChatHistory();
            if (messages != null)
            {
                history.AddRange(messages);
            }
            return history;
        }
        catch
        {
            return new ChatHistory();
        }
    }

    public override async Task SaveMessageAsync(string sessionId, ChatMessageContent message, CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var history = await GetHistoryAsync(sessionId, cancellationToken);
            history.Add(message);

            var filePath = GetFilePath(sessionId);
            var json = JsonSerializer.Serialize(history.ToList(), new JsonSerializerOptions { WriteIndented = true });
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
}

