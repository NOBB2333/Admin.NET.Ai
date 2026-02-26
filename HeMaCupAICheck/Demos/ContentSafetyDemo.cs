using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 内容安全中间件演示
/// 展示敏感词过滤、自定义替换词、PII脱敏等功能
/// </summary>
public static class ContentSafetyDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [22] 内容安全过滤 (敏感词替换+PII脱敏) ===\n");
        Console.WriteLine("功能: 敏感词过滤 | 自定义替换词 | PII脱敏 | 流式输出安全检测\n");
        
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        
        // 创建带内容安全中间件的 Agent
        var client = aiFactory.GetDefaultChatClient()!
            .CreateAIAgent(sp)
            .UseContentSafety()  // 启用内容安全过滤
            .Build();

        // ===== 1. 敏感词自定义替换演示 =====
        Console.WriteLine("--- 1. 敏感词自定义替换 ---");
        Console.WriteLine("配置规则: '废物'→'法王', '测试敏感词'→'已和谐'\n");

        var testCase1 = "请帮我写一个关于测试敏感词的故事";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"输入: {testCase1}");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("输出: ");
        await client.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, testCase1) }).WriteToConsoleAsync();
        Console.ResetColor();
        Console.WriteLine();

        // ===== 2. PII 脱敏演示 =====
        Console.WriteLine("\n--- 2. PII 自动脱敏 ---");
        Console.WriteLine("规则: 手机号/身份证/邮箱/银行卡 自动脱敏\n");

        var testCase2 = "请生成一个包含手机号13812345678和邮箱test@example.com的用户信息";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"输入: {testCase2}");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("输出: ");
        await client.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, testCase2) }).WriteToConsoleAsync();
        Console.ResetColor();
        Console.WriteLine();

        // ===== 3. 非流式调用演示 =====
        Console.WriteLine("\n--- 3. 非流式调用 (完整过滤) ---");
        
        var testCase3 = "用户说'你这个废物'，请帮我翻译成文明用语";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"输入: {testCase3}");
        Console.ResetColor();
        
        var response = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, testCase3) });
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"输出: {response.Text}");
        Console.ResetColor();

        // ===== 配置说明 =====
        Console.WriteLine("\n--- 配置说明 ---");
        Console.WriteLine("配置文件: Admin.NET.Ai/Configuration/LLMAgent.ContentSafety.json");
        Console.WriteLine("支持热更新: 修改配置后自动生效，无需重启应用");
        Console.WriteLine(@"
示例配置:
{
  ""SensitiveWords"": {
    ""废物"": ""法王"",      // 自定义替换词
    ""违禁词"": null        // 使用默认掩码 ***
  }
}");
    }
}
