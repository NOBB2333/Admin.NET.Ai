using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public object? Execute(Dictionary<string, object?>? input, IScriptExecutionContext? context = null)
    {
        Console.WriteLine("[ScriptD] Starting AI Chat...");
        
        var prompt = "Explain Quantum Physics";
        if (input != null && input.TryGetValue("prompt", out var p))
        {
            prompt = p?.ToString() ?? prompt;
        }

        Console.WriteLine($"[ScriptD] Sending prompt: {prompt}");
        
        // 修复参数传递：包含 prompt, serviceProvider 和 provider ("DeepSeek")
        return _llm.RunAsync(prompt, _serviceProvider, "DeepSeek");
    }
}
