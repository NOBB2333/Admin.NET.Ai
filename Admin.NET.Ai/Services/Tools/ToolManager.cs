using System.Reflection;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Tools;

/// <summary>
/// AI 工具管理器 (Function Calling 注册中心)
/// 自动扫描并管理所有实现 IAiTool 的工具
/// </summary>
public class ToolManager(IServiceProvider serviceProvider, ILogger<ToolManager> logger)
{
    /// <summary>
    /// 获取所有可用的 AIFunction (自动扫描程序集)
    /// </summary>
    /// <returns></returns>
    public IEnumerable<AIFunction> GetAllAiFunctions()
    {
        // 1. 扫描当前程序集 (及引用的相关程序集) 中所有实现 IAiTool 的非抽象类
        // 这里默认扫描 Admin.NET.Ai 所在的程序集，也可以扩展扫描 AppDomain
        var assembly = Assembly.GetExecutingAssembly();
        var toolTypes = assembly.GetTypes()
            .Where(t => typeof(IAiCallAbleFunction).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

        foreach (var type in toolTypes)
        {
            IAiCallAbleFunction? tool = null;
            try
            {
                // 2. 使用 ActivatorUtilities 创建实例，支持依赖注入
                tool = ActivatorUtilities.CreateInstance(serviceProvider, type) as IAiCallAbleFunction;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Failed to instantiate tool '{type.Name}'. Skipping.");
                continue;
            }

            if (tool != null)
            {
                // 3. 加载函数
                var functions = tool.GetFunctions();
                if (functions != null)
                {
                    foreach (var func in functions)
                    {
                        logger.LogDebug($"Loaded Function: {func.Name} from Tool: {tool.Name}");
                        yield return func;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 根据名称获取工具
    /// </summary>
    /// <param name="functionName"></param>
    /// <returns></returns>
    public AIFunction? GetFunction(string functionName)
    {
        return GetAllAiFunctions().FirstOrDefault(f => f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
    }
}
