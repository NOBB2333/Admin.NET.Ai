using System.Reflection;
using System.Text.Json;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Tools;

/// <summary>
/// AI 工具管理器 (Function Calling 注册中心)
/// 自动扫描并管理所有实现 IAiCallableFunction 的工具
/// 支持上下文感知和自管理审批
/// </summary>
public class ToolManager(IServiceProvider serviceProvider, ILogger<ToolManager> logger)
{
    private List<IAiCallableFunction>? _cachedTools;

    /// <summary>
    /// 获取所有已发现的工具实例（带缓存）
    /// </summary>
    public IReadOnlyList<IAiCallableFunction> GetAllTools()
    {
        if (_cachedTools != null) return _cachedTools;

        _cachedTools = new List<IAiCallableFunction>();
        var assembly = Assembly.GetExecutingAssembly();
        var toolTypes = assembly.GetTypes()
            .Where(t => typeof(IAiCallableFunction).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

        foreach (var type in toolTypes)
        {
            try
            {
                var tool = ActivatorUtilities.CreateInstance(serviceProvider, type) as IAiCallableFunction;
                if (tool != null)
                {
                    _cachedTools.Add(tool);
                    logger.LogDebug("Discovered tool: {ToolName} ({Type})", tool.Name, type.Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to instantiate tool '{TypeName}'. Skipping.", type.Name);
            }
        }

        logger.LogInformation("ToolManager discovered {Count} tools", _cachedTools.Count);
        return _cachedTools;
    }

    /// <summary>
    /// 获取所有可用的 AIFunction (原有方法，保持兼容)
    /// </summary>
    public IEnumerable<AIFunction> GetAllAiFunctions()
    {
        foreach (var tool in GetAllTools())
        {
            var functions = tool.GetFunctions();
            if (functions != null)
            {
                foreach (var func in functions)
                {
                    logger.LogDebug("Loaded Function: {FuncName} from Tool: {ToolName}", func.Name, tool.Name);
                    yield return func;
                }
            }
        }
    }

    /// <summary>
    /// 获取所有 AIFunction 并注入执行上下文
    /// 审批逻辑统一由 ToolValidationMiddleware 处理，ToolManager 只负责发现和上下文注入
    /// </summary>
    /// <param name="context">执行上下文</param>
    public IEnumerable<AIFunction> GetAllAiFunctions(ToolExecutionContext context)
    {
        foreach (var tool in GetAllTools())
        {
            // 注入上下文
            tool.Context = context;

            var functions = tool.GetFunctions();
            if (functions == null) continue;

            foreach (var func in functions)
            {
                yield return func;
            }
        }
    }

    /// <summary>
    /// 根据名称获取工具
    /// </summary>
    public AIFunction? GetFunction(string functionName)
    {
        return GetAllAiFunctions().FirstOrDefault(f => f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 刷新工具缓存（新增工具后调用）
    /// </summary>
    public void Refresh() => _cachedTools = null;
}
