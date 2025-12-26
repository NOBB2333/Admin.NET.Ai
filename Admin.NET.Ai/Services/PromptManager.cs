using System.Collections.Concurrent;
using Admin.NET.Ai.Services.Prompt;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services;

/// <summary>
/// Prompt 管理器 (支持热重载)
/// </summary>
public class PromptManager : IPromptManager, IDisposable
{
    private readonly ILogger<PromptManager> _logger;
    private readonly string _promptDirectory;
    private readonly FileSystemWatcher? _watcher;
    
    // Cache: Name -> Version -> Config
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PromptConfig>> _prompts = new();

    public PromptManager(ILogger<PromptManager> logger)
    {
        _logger = logger;
        _promptDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "Prompts");

        if (!Directory.Exists(_promptDirectory))
        {
            Directory.CreateDirectory(_promptDirectory);
        }

        LoadPrompts();

        try 
        {
            _watcher = new FileSystemWatcher(_promptDirectory, "*.*"); // Watch all files
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize FileSystemWatcher for Prompts.");
        }
    }

    public async Task<PromptConfig?> GetPromptConfigAsync(string name, string version = "latest")
    {
        if (_prompts.TryGetValue(name, out var versions))
        {
            if (version.Equals("latest", StringComparison.OrdinalIgnoreCase))
            {
                // Simple version comparison (assumes semver or alphabetical sort works for now, or last loaded)
                // For robustness, we explicitly track "latest" or sort keys.
                // Here we just pick the lexicographically largest version key.
                var latestKey = versions.Keys.OrderByDescending(k => k).FirstOrDefault();
                return latestKey != null ? versions[latestKey] : null;
            }
            
            if (versions.TryGetValue(version, out var config))
            {
                return config;
            }
        }
        
        _logger.LogWarning("Prompt not found: {Name} (Version: {Version})", name, version);
        return null;
    }

    public Task<string> RenderPromptAsync(string template, Dictionary<string, object> variables)
    {
        if (string.IsNullOrEmpty(template)) return Task.FromResult(string.Empty);
        if (variables == null || variables.Count == 0) return Task.FromResult(template);

        // Simple Template Engine:
        // 1. {{variable}} replacement
        // 2. Simple logic could be added here (e.g. basic if/foreach if needed, using regex or a lightweight parser)
        // For enterprise optimization, we stick to robust {{key}} replacement but handle objects nicely.
        
        var result = template;
        
        // Match {{ key }} or {{key}}
        // Using a callback to handle replacement
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", match =>
        {
            var key = match.Groups[1].Value;
            if (variables.TryGetValue(key, out var val))
            {
                return val?.ToString() ?? "";
            }
            return match.Value; // Keep original if not found (or return empty string?)
        });

        return Task.FromResult(result);
    }

    public async Task<string> GetRenderedPromptAsync(string name, Dictionary<string, object>? variables = null, string version = "latest")
    {
        var config = await GetPromptConfigAsync(name, version);
        if (config == null) return string.Empty;

        // Priority: Multi-message format -> Single Template string
        if (config.Messages != null && config.Messages.Count > 0)
        {
             // For GetRenderedPromptAsync which returns string, we might just join them or return the 'User' part?
             // Usually this method implies getting the text prompt. 
             // If it's a chat prompt, the caller should likely use GetPromptConfigAsync and handle messages.
             // But for compatibility/convenience, let's render the "Template" field if it exists, otherwise join messages.
             
             if (!string.IsNullOrWhiteSpace(config.Template))
             {
                 return await RenderPromptAsync(config.Template, variables ?? new());
             }
             
             // Fallback: Serialize messages to string (not ideal for all LLMs but useful for debugging)
             var sb = new System.Text.StringBuilder();
             foreach(var msg in config.Messages)
             {
                 var content = await RenderPromptAsync(msg.Content, variables ?? new());
                 sb.AppendLine($"{msg.Role}: {content}");
             }
             return sb.ToString();
        }

        return await RenderPromptAsync(config.Template, variables ?? new());
    }

    public async Task RegisterPromptAsync(string name, string template)
    {
        // Legacy support: Register as v1.0 string-only prompt
        var config = new PromptConfig
        {
            Name = name,
            Version = "1.0",
            Template = template,
            Description = "Auto-registered legacy prompt"
        };
        
        UpdateCache(config);

        // Save to file (JSON)
        var filePath = Path.Combine(_promptDirectory, $"{name}.1.0.json");
        await SaveConfigAsync(filePath, config);
    }

    private void LoadPrompts()
    {
        _logger.LogInformation("Loading prompts from {Directory}...", _promptDirectory);
        
        // 1. Load .json (Structured)
        foreach (var file in Directory.GetFiles(_promptDirectory, "*.json", SearchOption.AllDirectories))
        {
            TryLoadConfigFile(file);
        }

        // 2. Load .txt (Legacy/Simple)
        foreach (var file in Directory.GetFiles(_promptDirectory, "*.txt", SearchOption.AllDirectories))
        {
            TryLoadTextFile(file);
        }
    }

    private void TryLoadConfigFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var config = System.Text.Json.JsonSerializer.Deserialize<PromptConfig>(json, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (config != null)
            {
                if (string.IsNullOrWhiteSpace(config.Name)) config.Name = Path.GetFileNameWithoutExtension(filePath).Split('.')[0];
                UpdateCache(config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load prompt config: {File}", filePath);
        }
    }

    private void TryLoadTextFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var name = Path.GetFileNameWithoutExtension(filePath);
            var config = new PromptConfig
            {
                Name = name,
                Version = "1.0", // Default version for txt files
                Template = content,
                Description = "Loaded from text file"
            };
            UpdateCache(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load text prompt: {File}", filePath);
        }
    }

    private void UpdateCache(PromptConfig config)
    {
        var versions = _prompts.GetOrAdd(config.Name, _ => new ConcurrentDictionary<string, PromptConfig>());
        versions[config.Version] = config;
    }
    
    private async Task SaveConfigAsync(string path, PromptConfig config)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce or direct reload
        if (e.FullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
             Task.Delay(100).ContinueWith(_ => TryLoadConfigFile(e.FullPath));
        }
        else if (e.FullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
             Task.Delay(100).ContinueWith(_ => TryLoadTextFile(e.FullPath));
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}
