using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Admin.NET.Ai.Services.Workflow;

//  这会感觉可能还是有问题的。详见我的这个demo看看 https://github.com/NOBB2333/NatashaHotReloadDemo  
public class NatashaScriptEngine(IServiceProvider serviceProvider)
{
    private INatashaDynamicLoadContextBase? _currentDomain;
    private System.WeakReference? _weakDomain;
    private List<IScriptExecutor> _loadedExecutors = new();

    public void Unload()
    {
        // 调用所有已加载脚本的 OnUnloadingAsync 钩子
        foreach (var executor in _loadedExecutors)
        {
            try
            {
                executor.OnUnloadingAsync().GetAwaiter().GetResult();
                Console.WriteLine($"[Natasha引擎] ✅ {executor.GetMetadata().Name} OnUnloadingAsync 完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Natasha引擎] ⚠️ {executor.GetMetadata().Name} OnUnloadingAsync 失败: {ex.Message}");
            }
        }
        _loadedExecutors.Clear();

        if (_currentDomain != null)
        {
            _weakDomain = new System.WeakReference(_currentDomain);
            _currentDomain.Dispose();
            _currentDomain = null;

            // 提示：要使卸载成功，外部持有的 IScriptExecutor 实例也必须被释放
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (_weakDomain.IsAlive)
            {
                Console.WriteLine("[Natasha引擎] ⚠️ 警告: 旧域尚未完全释放，请确保没有外部引用指向脚本实例。");
            }
            else
            {
                Console.WriteLine("[Natasha引擎] ✅ 旧域已成功卸载。");
            }
        }
    }

    public IEnumerable<IScriptExecutor> LoadScripts(IEnumerable<string> scriptContents)
    {
        if (scriptContents == null || !scriptContents.Any()) throw new ArgumentException("脚本内容不能为空");

        try
        {
            #region 1. 清理旧域 (会触发 OnUnloadingAsync)
            Unload();
            #endregion

            #region 2. 创建 Natasha 构建环境
            // 使用构造函数创建独立域
            var domainName = Guid.NewGuid().ToString();
            var builder = new AssemblyCSharpBuilder(domainName);
            
            _currentDomain = builder.Domain;
            
            // 手动配置 LoadContext，添加引用
            builder.ConfigLoadContext(ctx => {
                 // 添加宿主程序集引用，以便脚本能访问 IGreetingService, ILLMService, DbModels 等
                 ctx.AddReferenceAndUsingCode(typeof(Console));
                 ctx.AddReferenceAndUsingCode(typeof(IDisposable));
                 ctx.AddReferenceAndUsingCode(typeof(ISqlSugarClient));
                 ctx.AddReferenceAndUsingCode(typeof(object).Assembly);
                 ctx.AddReferenceAndUsingCode(typeof(Enumerable).Assembly);
                 ctx.AddReferenceAndUsingCode(typeof(IServiceProvider).Assembly);
                  ctx.AddReferenceAndUsingCode(typeof(IScriptExecutor).Assembly);
                  ctx.AddReferenceAndUsingCode(typeof(IScriptExecutionContext)); // 追踪上下文
                  ctx.AddReferenceAndUsingCode(typeof(Dictionary<,>).Assembly);
                 return ctx;
            });
            #endregion
                
            foreach (var content in scriptContents)
            {
                // 核心：在加载前通过 Roslyn 重写源码以注入可观测性代码
                var rewrittenContent = ScriptSourceRewriter.Rewrite(content);
                builder.Add(rewrittenContent);
            }

            #region 3. 编译与获取程序集
            var assembly = builder.GetAssembly();
            if (assembly == null)
            {
                throw new Exception("编译或加载失败");
            }

            // 跟踪域以便后续释放
            _currentDomain = builder.Domain;
            #endregion

            #region 4. 查找实现类型
            var executorTypes = assembly.GetTypes()
                .Where(t => typeof(IScriptExecutor).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (!executorTypes.Any())
            {
                throw new Exception("脚本中未找到 IScriptExecutor 的实现。");
            }
            #endregion

            #region 5. 实例化与依赖注入
            var executors = new List<IScriptExecutor>();
            foreach (var type in executorTypes)
            {
                try
                {
                    // 使用 ActivatorUtilities 支持构造函数注入
                    Console.WriteLine($"[Natasha引擎] 正在使用依赖注入实例化 {type.Name} ...");
                    var executor = (IScriptExecutor)ActivatorUtilities.CreateInstance(serviceProvider, type);
                    executors.Add(executor);
                    
                    var meta = executor.GetMetadata();
                    Console.WriteLine($"[系统] 已加载脚本: {meta.Name} v{meta.Version}");
                    
                    // 显示超时配置
                    if (meta.MaxExecutionTime.HasValue)
                    {
                        Console.WriteLine($"       ⏱️ 最大执行时间: {meta.MaxExecutionTime.Value.TotalSeconds}s");
                    }
                    if (meta.Tags?.Length > 0)
                    {
                        Console.WriteLine($"       🏷️ 标签: [{string.Join(", ", meta.Tags)}]");
                    }
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[错误] 实例化脚本 {type.Name} 失败: {ex.Message}");
                }
            }
            #endregion

            #region 6. 调用生命周期钩子 OnLoadedAsync
            foreach (var executor in executors)
            {
                try
                {
                    executor.OnLoadedAsync(serviceProvider).GetAwaiter().GetResult();
                    Console.WriteLine($"[Natasha引擎] ✅ {executor.GetMetadata().Name} OnLoadedAsync 完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Natasha引擎] ⚠️ {executor.GetMetadata().Name} OnLoadedAsync 失败: {ex.Message}");
                }
            }
            
            // 保存引用以便后续调用 OnUnloadingAsync
            _loadedExecutors = executors;
            #endregion

            return executors;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 编译/加载 失败: {ex.Message}");
            throw;
        }
    }
}