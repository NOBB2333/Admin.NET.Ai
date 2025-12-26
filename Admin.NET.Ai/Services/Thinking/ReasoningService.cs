using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Admin.NET.Ai.Services.Thinking;

/// <summary>
/// Reasoning Service (Chain of Thought Orchestrator)
/// 负责从模型响应中提取思考过程 (Thinking Process) 并分离最终答案
/// </summary>
public class ReasoningService
{
    private readonly ILogger<ReasoningService> _logger;
    // Regex for typical thought blocks: <thought>...</thought> or <think>...</think>
    // Handles multiline input and lazy matching
    private static readonly Regex ThoughtPattern = new Regex(@"(<thought>|<think>)([\s\S]*?)(</thought>|</think>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ReasoningService(ILogger<ReasoningService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理响应，分离思考过程和最终回答
    /// </summary>
    public (string Thought, string Answer) ExtractThinking(string modelResponse)
    {
        var match = ThoughtPattern.Match(modelResponse);
        if (match.Success)
        {
            var thought = match.Groups[2].Value.Trim();
            var answer = ThoughtPattern.Replace(modelResponse, "").Trim(); // Remove thought block from answer
            
            _logger.LogDebug("🧠 [Reasoning] Thought extracted: {Length} chars", thought.Length);
            return (thought, answer);
        }

        // If no explicit thought block, return empty thought
        return (string.Empty, modelResponse.Trim());
    }

    /// <summary>
    /// 自主推理循环 (ReAct lite)
    /// 这里的 'think' 是指让模型显式输出思考过程，提高复杂任务准确率
    /// </summary>
    public async Task<string> RunWithCoTAsync(IChatClient client, string prompt, int maxSteps = 1)
    {
        // Inject CoT instruction
        var cotPrompt = $"{prompt}\n\nPlease think step by step before answering. Wrap your thinking process in <think>...</think> tags.";
        
        var response = await client.GetResponseAsync(new List<ChatMessage> { new(ChatRole.User, cotPrompt) });
        var text = response.Messages.LastOrDefault()?.Text ?? string.Empty;

        var (thought, answer) = ExtractThinking(text);

        if (!string.IsNullOrEmpty(thought))
        {
            _logger.LogInformation("🧠 Model Thinking Process:\n{Thought}", thought);
        }

        return answer;
    }
}
