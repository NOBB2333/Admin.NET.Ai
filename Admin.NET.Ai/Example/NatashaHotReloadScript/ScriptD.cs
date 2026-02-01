using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

public class ScriptD : IScriptExecutor
{
    private readonly IChatClient _llm;
    private readonly IServiceProvider _serviceProvider;

    // 根据项目设定，注入 IAiFactory 和 IServiceProvider
    public ScriptD(IAiFactory aiFactory, IServiceProvider serviceProvider)
    {
        _llm = aiFactory.GetChatClient("DeepSeek") ?? throw new InvalidOperationException("DeepSeek AI client is not configured.");
        _serviceProvider = serviceProvider;
    }

    public ScriptMetadata GetMetadata() => new ScriptMetadata("ScriptD - AI Chat Script", "1.0");

    public async Task<object?> ExecuteAsync(
        IDictionary<string, object?> args, 
        IScriptExecutionContext? trace = null, 
        CancellationToken ct = default)
    {
        Console.WriteLine("[ScriptD] Starting AI Chat...");
        
        var prompt = "Explain Quantum Physics";
        if (args != null && args.TryGetValue("prompt", out var p))
        {
            prompt = p?.ToString() ?? prompt;
        }

        Console.WriteLine($"[ScriptD] Sending prompt: {prompt}");
        
        // 修复参数传递：包含 prompt, serviceProvider 和 provider ("DeepSeek")
        return await _llm.RunAsync(prompt, _serviceProvider, "DeepSeek");
    }
}
