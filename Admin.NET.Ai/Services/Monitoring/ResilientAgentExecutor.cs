using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Admin.NET.Ai.Services.Monitoring;

public class ResilientAgentExecutor
{
    private readonly ILogger<ResilientAgentExecutor> _logger;
    
    public ResilientAgentExecutor(ILogger<ResilientAgentExecutor> logger)
    {
        _logger = logger;
    }
    
    public async Task<ChatResponse> ExecuteWithRetryAsync(
        IChatClient client, 
        IList<ChatMessage> messages,
        int maxRetries = 3)
    {
        var retryCount = 0;
        
        while (true)
        {
            try
            {
                using var activity = new Activity("Agent.Execution");
                activity?.SetTag("retry.count", retryCount);
                activity?.Start();
                
                var result = await client.GetResponseAsync(messages);
                activity?.Stop();
                return result;
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex, 
                    "Agent Execution Failed, Retrying {RetryCount}/{MaxRetries}",
                    retryCount, maxRetries);
                
                // Exponential Backoff
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
        }
    }
}
