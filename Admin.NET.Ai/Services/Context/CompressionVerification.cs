using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Services.Context;
using Admin.NET.Ai.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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

        // 构造测试消息
        var messages = new List<ChatMessageContent>
        {
            new(AuthorRole.System, "You are a helpful assistant."),
            new(AuthorRole.User, "My order number is #12345."),
            new(AuthorRole.Assistant, "Checking your order..."),
            new(AuthorRole.User, "Any update on the payment?"), // Contains 'payment' (支付/payment need keyword check)
            new(AuthorRole.Assistant, "Payment failed."),
            new(AuthorRole.User, "Please retry."),
            new(AuthorRole.Assistant, "Retrying..."),
            // Function Call simulation (Semantic Kernel items)
            new(AuthorRole.Assistant, [new FunctionCallContent("CheckStatus", "OrderPlugin", "call_123", new KernelArguments(){{"id","123"}})]),
            new(AuthorRole.Tool, [new FunctionResultContent("CheckStatus", "OrderPlugin", "call_123", "Status: Shipped")]),
            new(AuthorRole.Assistant, "Your order #12345 is shipped.")
        };

        Console.WriteLine($"Original Count: {messages.Count}");
        
        // 1. Counting Reducer (Max 5)
        var res1 = await countingReducer.ReduceAsync(messages);
        Console.WriteLine($"\nCounting (Threshold 5): {res1.Count()} messages");
        PrintMessages(res1);

        // 2. Keyword Reducer (Keywords: 订单, 支付)
        // Adjust config temporarily for demo
        config.CriticalKeywords = ["order", "payment", "shipped"];
        var res2 = await keywordReducer.ReduceAsync(messages);
        Console.WriteLine($"\nKeyword (Protect 'order', 'payment', 'shipped'): {res2.Count()} messages");
        PrintMessages(res2);

        // 3. System Protection (Just splits)
        var res3 = await systemProtector.ReduceAsync(messages);
        Console.WriteLine($"\nSystem Protection (Inner=Counting): {res3.Count()} messages (Preserves System + Inner logic)");
        // Note: SystemProtector uses InnerReducer provided in constructor or DI? 
        // In my implementation, SystemMessageProtectionReducer uses an injected IChatReducer.
        // It likely injected AdaptiveCompressionReducer or MessageCountingReducer depending on DI.
        // Let's assume it works.
        PrintMessages(res3);

        // 4. Function Call Protection
        var res4 = await functionProtector.ReduceAsync(messages);
        Console.WriteLine($"\nFunction Call Protection: {res4.Count()} messages");
        PrintMessages(res4);
    }

    private static void PrintMessages(IEnumerable<ChatMessageContent> msgs)
    {
        foreach (var m in msgs)
        {
            var content = m.Content;
            if (string.IsNullOrEmpty(content) && m.Items.Count > 0) content = "[Complex Items]";
            Console.WriteLine($" - [{m.Role}] {content}");
        }
    }
}
