using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Admin.NET.Ai.Services.Data;

/// <summary>
/// 结构化输出服务实现
/// </summary>
public class StructuredOutputService : IStructuredOutputService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public StructuredOutputService()
    {
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public T? Parse<T>(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return default;

        // 1. Clean Markdown (```json ... ```)
        var cleanText = CleanMarkdown(text);

        try
        {
            return JsonSerializer.Deserialize<T>(cleanText, _jsonOptions);
        }
        catch (JsonException)
        {
            // Simple Retry/Repair Logic could go here
            // For now, return default or throw
            // Try to find first '{' and last '}'
            var start = cleanText.IndexOf('{');
            var end = cleanText.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var repair = cleanText.Substring(start, end - start + 1);
                try { return JsonSerializer.Deserialize<T>(repair, _jsonOptions); } catch { }
            }
            throw;
        }
    }

    public async Task<T?> ParseWithRetryAsync<T>(string text, Func<string, Task<string>> fixCallback, int maxRetries = 2)
    {
        for (int i = 0; i <= maxRetries; i++)
        {
            try
            {
                return Parse<T>(text);
            }
            catch (JsonException ex)
            {
                if (i == maxRetries) throw; // Rethrow on last attempt

                var errorMsg = $"JSON Parse Error: {ex.Message}. Please fix the JSON syntax and return ONLY the valid JSON.";
                // Call LLM again to fix
                text = await fixCallback(errorMsg);
            }
        }
        return default;
    }

    public string GenerateJsonSchema(Type type, string description = "")
    {
        // Note: In a real implementation with MIcrosoft.Extensions.AI, use AIJsonUtilities.CreateJsonSchema(type)
        // Since that API is preview/new, we might wrap it. 
        // If not available, we use a simple reflection based generator or return a "JSON Mode" stub.
        
        // Use AIJsonUtilities if available in the referenced MEAI package, otherwise simulate
        try 
        {
            // Assuming MEAI provides AIJsonUtilities.CreateJsonSchema
            // If strictly unavailable, we'll need a fallback custom generator.
            // For this implementation, I will assume basic "JSON Mode" prompt generation if utility is missing.
            
            // Placeholder for AIJsonUtilities or Custom Generator
            // Returning a simplified schema representation for now to trigger JSON Mode in prompts
            return SimpleSchemaGenerator.Generate(type);
        }
        catch
        {
            return "{}";
        }
    }

    public ChatOptions CreateOptions<T>(string provider, string? instructions = null)
    {
        var options = new ChatOptions();
        var schema = GenerateJsonSchema(typeof(T), instructions ?? "");

        // Strategy:
        // OpenAI/Azure -> Use ResponseFormat.ForJsonSchema
        // Others (DeepSeek/Qwen) -> Use ResponseFormat.Json AND Inject Schema into Prompt
        
        if (IsOpenAICompatible(provider))
        {
             // Native Schema Support
             // Note: syntax depends on exact MEAI version APIs. 
             // Using generic JSON Schema format for now.
            options.ResponseFormat = ChatResponseFormat.ForJsonSchema(JsonDocument.Parse(SimpleSchemaGenerator.Generate(typeof(T))).RootElement, schemaName: typeof(T).Name);
        }
        else
        {
            // Fallback: JSON Mode + Prompt Injection
            options.ResponseFormat = ChatResponseFormat.Json;
            // The caller (Agent) needs to append schema to system prompt manually or we utilize a middleware for this.
            // But 'ChatOptions' doesn't hold prompt.
            // So we rely on the Agent Extensions to handle the prompt injection part.
        }

        return options;
    }

    private bool IsOpenAICompatible(string provider)
    {
        var p = provider?.ToLowerInvariant() ?? "";
        return p.Contains("openai") || p.Contains("azure");
    }

    private string CleanMarkdown(string text)
    {
        // Regex to extract listing from ```json ... ```
        var match = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)\s*```");
        return match.Success ? match.Groups[1].Value : text;
    }
}

/// <summary>
/// Simple Reflection-based Schema Generator (Fallback)
/// </summary>
public static class SimpleSchemaGenerator
{
    public static string Generate(Type type, int depth = 0)
    {
        if (depth > 5) return "{}"; // Recursion guard

        // Handle Nullable
        if (Nullable.GetUnderlyingType(type) != null)
        {
            return Generate(Nullable.GetUnderlyingType(type)!, depth);
        }

        // Handle Primitives directly
        if (IsPrimitive(type))
        {
             return GetPrimitiveType(type);
        }
        
        // Handle Lists/Arrays
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
             var itemType = type.IsArray ? type.GetElementType() : type.GetGenericArguments().FirstOrDefault();
             var itemSchema = itemType != null ? Generate(itemType, depth + 1) : "{}";
             return $"[ {itemSchema} ]";
        }

        // Handle Enums
        if (type.IsEnum)
        {
             var names = string.Join("|", Enum.GetNames(type));
             return $"\"enum({names})\"";
        }

        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var isFirst = true;

        foreach (var prop in props)
        {
            // Skip ignored
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

            if (!isFirst) sb.Append(", ");
            
            var name = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
            var desc = prop.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";
            
            sb.Append($"\"{name}\": ");
            
            sb.Append(Generate(prop.PropertyType, depth + 1));
            
            if (!string.IsNullOrEmpty(desc))
            {
                sb.Append($" // {desc}");
            }

            isFirst = false;
        }

        sb.Append("}");
        return sb.ToString();
    }
    
    private static bool IsPrimitive(Type type)
    {
        return type == typeof(string) || type == typeof(int) || type == typeof(double) || type == typeof(bool) || type == typeof(long) || type == typeof(float) || type == typeof(decimal) || type == typeof(DateTime);
    }
    
    private static string GetPrimitiveType(Type type)
    {
        if (type == typeof(string) || type == typeof(DateTime)) return "\"string\"";
        if (type == typeof(int) || type == typeof(double) || type == typeof(long) || type == typeof(float) || type == typeof(decimal)) return "\"number\"";
        if (type == typeof(bool)) return "\"boolean\"";
        return "\"string\"";
    }
}
