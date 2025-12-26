using System.Reflection;
using System.Text.Json;
using Admin.NET.Ai.Services.MCP.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.MCP;

/// <summary>
/// MCP 工具自动发现服务 - 扫描 [McpTool] 标记的方法并自动注册
/// </summary>
public class McpToolDiscoveryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<McpToolDiscoveryService> _logger;
    private readonly Dictionary<string, McpToolDefinition> _registeredTools = new();

    public McpToolDiscoveryService(
        IServiceProvider serviceProvider,
        ILogger<McpToolDiscoveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 从指定程序集发现并注册所有 [McpTool] 标记的方法
    /// </summary>
    public void DiscoverFromAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes();
        
        foreach (var type in types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                var attr = method.GetCustomAttribute<McpToolAttribute>();
                if (attr != null)
                {
                    RegisterTool(type, method, attr);
                }
            }
        }

        _logger.LogInformation("Discovered {Count} MCP tools from assembly {Assembly}", 
            _registeredTools.Count, assembly.GetName().Name);
    }

    /// <summary>
    /// 从当前 AppDomain 的所有程序集发现工具
    /// </summary>
    public void DiscoverFromAllAssemblies()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                DiscoverFromAssembly(assembly);
            }
            catch (ReflectionTypeLoadException)
            {
                // 某些程序集无法加载，跳过
            }
        }
    }

    /// <summary>
    /// 注册单个工具
    /// </summary>
    private void RegisterTool(Type declaringType, MethodInfo method, McpToolAttribute attr)
    {
        // 工具名称: 如果未指定，使用方法名 (转换为 snake_case)
        var toolName = attr.Name ?? ToSnakeCase(method.Name);

        // 构建参数 Schema
        var parameters = BuildParameterSchema(method);

        var definition = new McpToolDefinition
        {
            Name = toolName,
            Description = attr.Description,
            Category = attr.Category,
            RequiresApproval = attr.RequiresApproval,
            TimeoutSeconds = attr.TimeoutSeconds,
            DeclaringType = declaringType,
            Method = method,
            ParameterSchema = parameters
        };

        _registeredTools[toolName] = definition;

        _logger.LogDebug("Registered MCP tool: {Name} -> {Type}.{Method}", 
            toolName, declaringType.Name, method.Name);
    }

    /// <summary>
    /// 获取所有已注册的工具定义 (用于 SSE 发送给客户端)
    /// </summary>
    public List<object> GetToolsForMcp()
    {
        return _registeredTools.Values.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            category = t.Category,
            inputSchema = new
            {
                type = "object",
                properties = t.ParameterSchema.ToDictionary(
                    p => p.Name,
                    p => new
                    {
                        type = p.JsonType,
                        description = p.Description
                    }),
                required = t.ParameterSchema
                    .Where(p => p.Required)
                    .Select(p => p.Name)
                    .ToList()
            }
        }).ToList<object>();
    }

    /// <summary>
    /// 执行 MCP 工具调用
    /// </summary>
    public async Task<object?> ExecuteToolAsync(string toolName, Dictionary<string, object?> arguments)
    {
        if (!_registeredTools.TryGetValue(toolName, out var definition))
        {
            throw new ArgumentException($"Tool '{toolName}' not found.");
        }

        _logger.LogInformation("Executing MCP tool: {Name} with args: {Args}", 
            toolName, JsonSerializer.Serialize(arguments));

        // 获取或创建目标实例
        object? target = null;
        if (!definition.Method.IsStatic)
        {
            target = _serviceProvider.GetService(definition.DeclaringType);
            if (target == null)
            {
                // 尝试使用 ActivatorUtilities 创建实例
                target = ActivatorUtilities.CreateInstance(_serviceProvider, definition.DeclaringType);
            }
        }

        // 绑定参数
        var methodParams = definition.Method.GetParameters();
        var invokeArgs = new object?[methodParams.Length];

        for (int i = 0; i < methodParams.Length; i++)
        {
            var param = methodParams[i];
            var paramName = param.Name ?? $"arg{i}";

            if (arguments.TryGetValue(paramName, out var value))
            {
                invokeArgs[i] = ConvertArgument(value, param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                invokeArgs[i] = param.DefaultValue;
            }
            else if (param.ParameterType.IsValueType)
            {
                invokeArgs[i] = Activator.CreateInstance(param.ParameterType);
            }
        }

        // 调用方法
        var result = definition.Method.Invoke(target, invokeArgs);

        // 处理异步方法
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            
            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }
            return null;
        }

        return result;
    }

    /// <summary>
    /// 检查工具是否存在
    /// </summary>
    public bool HasTool(string toolName) => _registeredTools.ContainsKey(toolName);

    /// <summary>
    /// 获取工具定义
    /// </summary>
    public McpToolDefinition? GetTool(string toolName) => 
        _registeredTools.TryGetValue(toolName, out var def) ? def : null;

    #region Private Helpers

    private List<McpParameterDefinition> BuildParameterSchema(MethodInfo method)
    {
        var parameters = new List<McpParameterDefinition>();

        foreach (var param in method.GetParameters())
        {
            var mcpParam = param.GetCustomAttribute<McpParameterAttribute>();

            parameters.Add(new McpParameterDefinition
            {
                Name = param.Name ?? "param",
                Description = mcpParam?.Description ?? param.Name ?? "",
                Required = mcpParam?.Required ?? !param.IsOptional,
                JsonType = GetJsonType(param.ParameterType),
                ClrType = param.ParameterType,
                Example = mcpParam?.Example
            });
        }

        return parameters;
    }

    private static string GetJsonType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "integer";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            return "array";
        return "object";
    }

    private static object? ConvertArgument(object? value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;

        // 处理 JsonElement
        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType);
        }

        // 简单类型转换
        try
        {
            if (targetType == typeof(string)) return value.ToString();
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return JsonSerializer.Deserialize(JsonSerializer.Serialize(value), targetType);
        }
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLower(name[0]));

        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                result.Append('_');
                result.Append(char.ToLower(name[i]));
            }
            else
            {
                result.Append(name[i]);
            }
        }

        return result.ToString();
    }

    #endregion
}

/// <summary>
/// MCP 工具定义
/// </summary>
public class McpToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Category { get; set; }
    public bool RequiresApproval { get; set; }
    public int TimeoutSeconds { get; set; }
    public Type DeclaringType { get; set; } = null!;
    public MethodInfo Method { get; set; } = null!;
    public List<McpParameterDefinition> ParameterSchema { get; set; } = new();
}

/// <summary>
/// MCP 参数定义
/// </summary>
public class McpParameterDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Required { get; set; }
    public string JsonType { get; set; } = "string";
    public Type ClrType { get; set; } = typeof(string);
    public string? Example { get; set; }
}
