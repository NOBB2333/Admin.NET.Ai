using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Admin.NET.Ai.Configuration;

/// <summary>
/// 支持注释的 JSON 配置源
/// 允许在 JSON 文件中使用 // 和 /* */ 注释
/// </summary>
public class JsonWithCommentsConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new JsonWithCommentsConfigurationProvider(this);
    }
}

/// <summary>
/// 支持注释的 JSON 配置提供程序
/// </summary>
public class JsonWithCommentsConfigurationProvider : FileConfigurationProvider
{
    public JsonWithCommentsConfigurationProvider(JsonWithCommentsConfigurationSource source) 
        : base(source) { }

    public override void Load(Stream stream)
    {
        try
        {
            // 配置 JsonDocumentOptions 以支持注释
            var options = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            
            Data = JsonConfigurationFileParser.Parse(stream, options);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Error parsing JSON configuration: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// JSON 配置文件解析器（支持注释）
/// </summary>
internal static class JsonConfigurationFileParser
{
    public static IDictionary<string, string?> Parse(Stream stream, JsonDocumentOptions options)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        
        using var doc = JsonDocument.Parse(stream, options);
        ProcessElement(doc.RootElement, "", data);
        
        return data;
    }
    
    private static void ProcessElement(JsonElement element, string prefix, IDictionary<string, string?> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) 
                        ? property.Name 
                        : $"{prefix}:{property.Name}";
                    ProcessElement(property.Value, key, data);
                }
                break;
                
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}:{index}";
                    ProcessElement(item, key, data);
                    index++;
                }
                break;
                
            case JsonValueKind.String:
                data[prefix] = element.GetString();
                break;
                
            case JsonValueKind.Number:
                data[prefix] = element.GetRawText();
                break;
                
            case JsonValueKind.True:
            case JsonValueKind.False:
                data[prefix] = element.GetBoolean().ToString().ToLowerInvariant();
                break;
                
            case JsonValueKind.Null:
                data[prefix] = null;
                break;
        }
    }
}

/// <summary>
/// IConfigurationBuilder 扩展方法
/// </summary>
public static class JsonWithCommentsConfigurationExtensions
{
    /// <summary>
    /// 添加支持注释的 JSON 配置文件
    /// </summary>
    /// <param name="builder">配置构建器</param>
    /// <param name="path">文件路径</param>
    /// <param name="optional">是否可选</param>
    /// <param name="reloadOnChange">文件变更时是否重新加载</param>
    public static IConfigurationBuilder AddJsonFileWithComments(
        this IConfigurationBuilder builder, 
        string path, 
        bool optional = false, 
        bool reloadOnChange = false)
    {
        return builder.Add<JsonWithCommentsConfigurationSource>(source =>
        {
            source.Path = path;
            source.Optional = optional;
            source.ReloadOnChange = reloadOnChange;
            source.ResolveFileProvider();
        });
    }
}
