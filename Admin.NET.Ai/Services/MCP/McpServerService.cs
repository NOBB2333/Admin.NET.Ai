using System.Reflection;
using Admin.NET.Ai.Services.MCP.Attributes;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.MCP;

public class McpServerService
{
    private readonly ILogger<McpServerService> _logger;
    private readonly Dictionary<string, MethodInfo> _toolRegistry = new();
    private readonly Dictionary<string, object> _toolTargets = new();

    public McpServerService(ILogger<McpServerService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 扫描程序集中的 [McpTool] 属性并将它们注册。
    /// 在真实的应用程序中，您可能会注入 IServiceProvider 来解析控制器。
    /// </summary>
    public void RegisterToolsFromAssembly(Assembly assembly, object? targetInstance = null)
    {
        var methods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttribute<McpToolAttribute>() != null);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<McpToolAttribute>();
            if (attr != null)
            {
                _toolRegistry[attr.Name] = method;
                if (targetInstance != null)
                {
                    _toolTargets[attr.Name] = targetInstance;
                }
                _logger.LogInformation($"Registered MCP Tool: {attr.Name} - {attr.Description}");
            }
        }
    }

    public List<object> GetTools()
    {
        // 返回符合协议的工具定义
        var tools = new List<object>();
        foreach (var kv in _toolRegistry)
        {
            var attr = kv.Value.GetCustomAttribute<McpToolAttribute>();
            var parameters = kv.Value.GetParameters().Select(p => new 
            {
                name = p.Name,
                type = p.ParameterType.Name,
                required = !p.IsOptional
            });

            tools.Add(new 
            {
                name = attr!.Name,
                description = attr.Description,
                inputSchema = new 
                {
                    type = "object",
                    properties = parameters.ToDictionary(p => p.name ?? "param", p => new { type = "string" }) // 简化架构
                }
            });
        }
        return tools;
    }

    public async Task<object?> ExecuteToolAsync(string toolName, Dictionary<string, object> args)
    {
        if (!_toolRegistry.TryGetValue(toolName, out var method))
        {
            throw new ArgumentException($"Tool {toolName} not found.");
        }

        var parameters = method.GetParameters();
        var invokeArgs = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (args.TryGetValue(param.Name ?? "", out var value))
            {
                // 简单类型转换 (模拟)
                invokeArgs[i] = Convert.ChangeType(value, param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                invokeArgs[i] = param.DefaultValue;
            }
        }

        var target = _toolTargets.ContainsKey(toolName) ? _toolTargets[toolName] : null; 
        // 如果目标为空，则假设是静态的或者我们错过了实例注册。
        // 对于控制器操作，我们通常会从 ServiceProvider 解析。
        
        var result = method.Invoke(target, invokeArgs);

        if (result is Task task)
        {
             await task.ConfigureAwait(false);
             var resultProperty = task.GetType().GetProperty("Result");
             return resultProperty?.GetValue(task);
        }

        return result;
    }
}
