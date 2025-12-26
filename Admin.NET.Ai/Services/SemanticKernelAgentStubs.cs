using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// 说明：由于当前环境缺少 Microsoft.SemanticKernel.Agents 包，为保证编译通过，
// 提供最小占位类型。后续可替换为官方包并移除此文件。
namespace Microsoft.SemanticKernel.Agents
{
    public abstract class Agent
    {
        public string? Name { get; set; }
        public Kernel? Kernel { get; set; }
    }

    public abstract class TerminationStrategy
    {
        public IReadOnlyList<Agent>? Agents { get; set; }
        public int? MaximumIterations { get; set; }

        protected abstract Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken);
    }
}

namespace Microsoft.SemanticKernel.Agents.Chat
{
    public class ChatCompletionAgent : Agent
    {
        public string? Instructions { get; set; }

        public Task<IAsyncEnumerable<ChatMessageContent>> InvokeAsync(ChatHistory history, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IAsyncEnumerable<ChatMessageContent>>(AsyncEnumerable.Empty<ChatMessageContent>());
        }
    }

    public class ChatExecutionSettings
    {
        public TerminationStrategy? TerminationStrategy { get; set; }
    }

    public class AgentGroupChat
    {
        public ChatExecutionSettings ExecutionSettings { get; set; } = new();

        public AgentGroupChat(params Agent[] agents)
        {
        }

        public void AddChatMessage(ChatMessageContent message)
        {
        }

        public async IAsyncEnumerable<ChatMessageContent> InvokeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield break;
        }
    }
}

