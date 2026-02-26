using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Admin.NET.Ai.Extensions;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 场景22: 客服智能分流
/// 
/// 📌 展示意图识别 + 路由 + 多专业 Agent
/// 
/// 流程:
/// 1. 意图识别 Agent 分析用户意图
/// 2. 路由到对应专业 Agent
/// 3. 专业 Agent 处理具体问题
/// </summary>
public static class CustomerServiceDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("CustomerServiceDemo");
        var aiFactory = sp.GetRequiredService<IAiFactory>();

        Console.WriteLine("\n=== [16] 客服智能分流 (意图识别+路由) ===\n");

        // ===== 1. 定义专业 Agent =====
        Console.WriteLine("--- 1. 专业客服 Agent ---");

        var agents = new Dictionary<string, (string Emoji, string Description, string SystemPrompt)>
        {
            ["technical"] = ("🔧", "技术支持", "你是技术支持专家，帮助用户解决技术问题。"),
            ["billing"] = ("💰", "账单咨询", "你是账单专员，处理付款、退款、发票问题。"),
            ["general"] = ("📞", "综合客服", "你是综合客服，处理一般咨询和反馈。"),
            ["sales"] = ("🛒", "销售咨询", "你是销售顾问，介绍产品和报价。")
        };

        foreach (var agent in agents)
        {
            Console.WriteLine($"  {agent.Value.Emoji} {agent.Value.Description} - {agent.Key}");
        }

        // ===== 2. 意图识别函数 =====
        Console.WriteLine("\n--- 2. 意图识别 ---");

        var intentClassifier = new Func<string, (string Intent, double Confidence)>(query =>
        {
            query = query.ToLower();
            
            if (query.Contains("报错") || query.Contains("bug") || query.Contains("无法") || 
                query.Contains("api") || query.Contains("崩溃"))
                return ("technical", 0.9);
            
            if (query.Contains("付款") || query.Contains("退款") || query.Contains("发票") || 
                query.Contains("账单") || query.Contains("价格"))
                return ("billing", 0.85);
            
            if (query.Contains("购买") || query.Contains("优惠") || query.Contains("方案") || 
                query.Contains("报价"))
                return ("sales", 0.8);
            
            return ("general", 0.7);
        });

        // ===== 3. 模拟客服对话 =====
        Console.WriteLine("\n--- 3. 智能分流演示 ---");

        var customerQueries = new[]
        {
            "我的API调用一直报错401，怎么回事？",
            "上个月的发票能补开吗？",  
            "你们企业版有什么优惠活动？",
            "这个产品支持私有化部署吗？"
        };

        try
        {
            var chatClient = aiFactory.GetDefaultChatClient();

            foreach (var query in customerQueries)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n👤 客户: {query}");
                Console.ResetColor();

                // 意图识别
                var (intent, confidence) = intentClassifier(query);
                var agent = agents[intent];
                
                Console.WriteLine($"🎯 意图: {intent} (置信度: {confidence/100:P0}) → 转接 {agent.Description}");

                // 调用对应 Agent (使用流式输出)
                var prompt = $"{agent.SystemPrompt}\n\n客户问题: {query}\n请简洁专业地回答:";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{agent.Emoji} {agent.Description}: ");
                await chatClient!.GetStreamingResponseAsync(prompt).WriteToConsoleAsync();
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            // 模拟输出
            foreach (var query in customerQueries)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n👤 客户: {query}");
                Console.ResetColor();

                var (intent, confidence) = intentClassifier(query);
                var agent = agents[intent];
                
                Console.WriteLine($"🎯 意图: {intent} (置信度: {confidence/100:P0}) → 转接 {agent.Description}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{agent.Emoji} (模拟回复...)");
                Console.ResetColor();
            }
            Console.WriteLine($"\n⚠️ 实际需配置 LLM: {ex.Message}");
        }

        // ===== 4. 高级意图识别 =====
        Console.WriteLine("\n--- 4. LLM 意图识别 (Structured Output) ---");
        Console.WriteLine(@"
// 使用 LLM 进行精准意图识别
var intentPrompt = $""""""
    分析以下客户问题的意图，返回 JSON 格式：
    
    问题: {query}
    
    返回格式:
    {{
        ""intent"": ""technical|billing|sales|general"",
        ""confidence"": 0.0-1.0,
        ""keywords"": [""关键词1"", ""关键词2""],
        ""urgency"": ""low|medium|high""
    }}
    """""";

var result = await chatClient.GetResponseAsync<IntentResult>(intentPrompt);

// 根据意图路由
var handler = intent switch
{
    ""technical"" => technicalAgent,
    ""billing"" => billingAgent,
    ""sales"" => salesAgent,
    _ => generalAgent
};

await handler.HandleAsync(query);
");

        // ===== 5. MAF Autonomous Workflow =====
        Console.WriteLine("--- 5. Autonomous Workflow 实现 ---");
        Console.WriteLine(@"
// 使用 MAF 自主工作流
var workflow = new WorkflowBuilder()
    .AddAgent(""Router"", routerAgent)
    .AddAgent(""Technical"", technicalAgent)
    .AddAgent(""Billing"", billingAgent)
    .AddAgent(""Sales"", salesAgent)
    .AddAgent(""General"", generalAgent)
    // Router 根据意图动态选择下一个 Agent
    .AddConditionalEdge(""Router"", 
        (context) => context.GetVariable<string>(""intent""))
    .Build();

// 执行
await workflow.ExecuteAsync(customerQuery);
");

        Console.WriteLine("\n========== 客服智能分流演示结束 ==========");
    }
}
