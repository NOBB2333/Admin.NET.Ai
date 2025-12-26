using Microsoft.Extensions.DependencyInjection;
using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Core;

/// <summary>
/// [保留项] AI 管道构建器
/// 注意：当前项目已迁移至 Microsoft.Extensions.AI (MEAI) 中间件流水线。
/// 该类目前处于非活跃状态，保留用于未来可能的自定义扩展或混合架构（Mixed Architecture）方案。
/// </summary>
public class AiPipelineBuilder(IServiceProvider serviceProvider)
{
    private readonly List<Func<AiRequestDelegate, AiRequestDelegate>> _components = [];

    public IServiceProvider ApplicationServices { get; } = serviceProvider;

    /// <summary>
    /// 使用中间件
    /// </summary>
    /// <param name="middleware">中间件委托</param>
    /// <returns></returns>
    public AiPipelineBuilder Use(Func<AiRequestDelegate, AiRequestDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    /// <summary>
    /// 使用中间件类型
    /// </summary>
    /// <typeparam name="TMiddleware"></typeparam>
    /// <returns></returns>
    public AiPipelineBuilder UseMiddleware<TMiddleware>() where TMiddleware : IAiMiddleware
    {
        return Use(next =>
        {
            return async context =>
            {
                // 从 DI 容器中解析中间件实例
                // 注意：这里假设中间件是瞬态或单例注册的，如果是 Scoped 需要注意作用域
                var middleware = context.RequestServices.GetRequiredService<TMiddleware>();
                await middleware.InvokeAsync(context, next);
            };
        });
    }

    /// <summary>
    /// 构建管道
    /// </summary>
    /// <returns></returns>
    public AiRequestDelegate Build()
    {
        AiRequestDelegate app = context =>
        {
            // 管道终点，如果没有中间件处理，或者最后一个中间件调用了 next
            // 可以在这里设置默认行为，或者什么都不做
            return Task.CompletedTask;
        };

        for (var i = _components.Count - 1; i >= 0; i--)
        {
            app = _components[i](app);
        }

        return app;
    }
}
