using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Configuration;
using Admin.NET.Ai.Extensions;
using Admin.NET.Ai.Middleware;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Chat;
using System.ComponentModel;
// Alias to avoid ambiguity
using ChatRole = Microsoft.Extensions.AI.ChatRole;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Admin.NET.Ai.Example;

/// <summary>
/// AI 服务使用示例 (Phase 3 Verification)
/// </summary>
public class UsageExample
{
    public static async Task RunAsync()
    {
        var builder = Host.CreateApplicationBuilder();

        // 1. 注册 AI 服务
        builder.Services.AddAdminNetAi(builder.Configuration);
        
        // 注册配置 (Mock)
        builder.Services.Configure<Admin.NET.Ai.Options.LLMAgentOptions>(options =>
        {
            options.LLMCostControl.Enabled = true;
            options.LLMClients.DefaultProvider = "DeepSeek";
            options.LLMClients.Clients["DeepSeek"] = new Admin.NET.Ai.Options.LLMClientConfig 
            { 
                 // Mock config
            };
            options.LLMMcp = new Admin.NET.Ai.Options.LLMMcpConfig 
            {
                Servers = new List<Admin.NET.Ai.Options.McpServerConfig>
                {
                    new() { Name = "GitHub-MCP", Enabled = true, Url = "http://localhost:3000/sse" }
                }
            };
        });

        // Register Mocks for testing
        builder.Services.AddTransient<IHttpClientFactory, MockHttpClientFactory>();

        var host = builder.Build();

        // 2. 获取 AI 服务
        using var scope = host.Services.CreateScope();
        var aiFactory = scope.ServiceProvider.GetRequiredService<IAiFactory>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

        try
        {
            // --- Phase 3 Tests ---

            // 16. Test Advanced Prompt Engineering
            Console.WriteLine("\n--- Testing Advanced Prompt Engineering (Phase 3) ---");
            var promptManager = scope.ServiceProvider.GetRequiredService<Admin.NET.Ai.Services.Prompt.IPromptManager>();
            
            // Register a structured prompt dynamically
            var jsonPrompt = @"{
                ""Name"": ""AnalyzeData"",
                ""Version"": ""2.0"",
                ""Template"": ""Analyze this: {{data}}"",
                ""Description"": ""Analysis Prompt""
            }";
            
            // Test Legacy Registration
            await promptManager.RegisterPromptAsync("SimplePrompt", "Hello {{name}}");
            var output = await promptManager.GetRenderedPromptAsync("SimplePrompt", new Dictionary<string, object> { { "name", "Phase3" } });
            Console.WriteLine($"Rendered Prompt: {output}");
            
            // 17. Test Structured Output with Retry
            Console.WriteLine("\n--- Testing Structured Output Retry (Phase 3) ---");
            var structService = scope.ServiceProvider.GetRequiredService<IStructuredOutputService>();
            var brokenJson = "{ \"name\": \"Alice\", \"age\": 30, }"; // Trailing comma
            
            // Mock Fix Callback
            Func<string, Task<string>> mockFixer = async (error) => 
            {
                Console.WriteLine($"[Mock LLM Fix] Fixing error: {error}");
                await Task.Delay(10);
                return "{ \"name\": \"Alice\", \"age\": 30 }"; // Return fixed
            };

            var parsed = await structService.ParseWithRetryAsync<PersonInfo>(brokenJson, mockFixer);
            Console.WriteLine($"Parsed with Retry: {parsed?.Name}");

            // 18. Test Reasoning Service
            Console.WriteLine("\n--- Testing Reasoning Service (CoT) (Phase 3) ---");
            var reasoningService = scope.ServiceProvider.GetRequiredService<Admin.NET.Ai.Services.Thinking.ReasoningService>();
            var modelOut = "<think>\nThinking about life...\n</think>\n42";
            var (thought, ans) = reasoningService.ExtractThinking(modelOut);
            Console.WriteLine($"Thought: {thought}");
            Console.WriteLine($"Answer: {ans}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test Failed: {ex.Message}");
        }
    }
    
    // Mocks
    public class MockOptionsMonitor<T> : Microsoft.Extensions.Options.IOptions<T> where T : class
    {
        public T Value { get; }
        public MockOptionsMonitor(T value) { Value = value; }
    }
    
    public class MockHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}

// Demo Classes
public class PersonInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("age")] 
    public int? Age { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("occupation")]
    public string? Occupation { get; set; }
}
