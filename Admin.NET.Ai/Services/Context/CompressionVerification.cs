using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Context;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Context;

public class CompressionVerification
{
    public static async Task VerifyAsync(IServiceProvider services)
    {
        Console.WriteLine("\n=== [Compression Strategy Verification] ===\n");

        var config = services.GetRequiredService<IOptions<CompressionConfig>>().Value;
        config.MessageCountThreshold = 5; // 设置低阈值以便测试

        var countingReducer = services.GetRequiredService<MessageCountingReducer>();
        var keywordReducer = services.GetRequiredService<KeywordAwareReducer>();
        var systemProtector = services.GetRequiredService<SystemMessageProtectionReducer>();
        var functionProtector = services.GetRequiredService<FunctionCallPreservationReducer>();

        // 构造测试消息 (MEAI ChatMessage)
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "My order number is #12345."),
            new(ChatRole.Assistant, "Checking your order..."),
            new(ChatRole.User, "Any update on the payment?"),
            new(ChatRole.Assistant, "Payment failed."),
            new(ChatRole.User, "Please retry."),
            new(ChatRole.Assistant, "Retrying..."),
            // Function Call simulation (MEAI FunctionCallContent)
            new(ChatRole.Assistant, [new FunctionCallContent("call_123", "CheckStatus", new Dictionary<string, object?>{ {"id","123"} })]),
            new(ChatRole.Tool, [new FunctionResultContent("call_123", "Status: Shipped")]),
            new(ChatRole.Assistant, "Your order #12345 is shipped.")
        };

        Console.WriteLine($"Original Count: {messages.Count}");
        
        // 1. Counting Reducer (Max 5)
        var res1 = await countingReducer.ReduceAsync(messages);
        Console.WriteLine($"\nCounting (Threshold 5): {res1.Count()} messages");
        PrintMessages(res1);

        // 2. Keyword Reducer (Keywords: order, payment)
        config.CriticalKeywords = ["order", "payment", "shipped"];
        var res2 = await keywordReducer.ReduceAsync(messages);
        Console.WriteLine($"\nKeyword (Protect 'order', 'payment', 'shipped'): {res2.Count()} messages");
        PrintMessages(res2);

        // 3. System Protection (Just splits)
        var res3 = await systemProtector.ReduceAsync(messages);
        Console.WriteLine($"\nSystem Protection (Inner=Counting): {res3.Count()} messages");
        PrintMessages(res3);

        // 4. Function Call Protection
        var res4 = await functionProtector.ReduceAsync(messages);
        Console.WriteLine($"\nFunction Call Protection: {res4.Count()} messages");
        PrintMessages(res4);
    }

    private static void PrintMessages(IEnumerable<ChatMessage> msgs)
    {
        foreach (var m in msgs)
        {
            var content = m.Text;
            if (string.IsNullOrEmpty(content) && m.Contents.Count > 0) content = "[Complex Items]";
            Console.WriteLine($" - [{m.Role}] {content}");
        }
    }
}
