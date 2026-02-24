using Admin.NET.Ai.Abstractions;
using HeMaCupAICheck.Agents.BuiltIn;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 内置 Agent 演示 - 情感分析、知识图谱、质量评估
/// </summary>
public static class BuiltInAgentDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        Console.WriteLine("\n========== 内置 Agent 演示 ==========\n");

        // 获取默认 ChatClient
        var chatClient = aiFactory.GetDefaultChatClient();

        // ===== 1. 情感分析 Agent =====
        Console.WriteLine("--- 1. 情感分析 Agent ---");
        var sentimentAgent = new SentimentAnalysisAgent(
            chatClient!,
            sp,
            loggerFactory.CreateLogger<SentimentAnalysisAgent>());

        var texts = new[]
        {
            "这个产品太棒了！我非常满意，强烈推荐！",
            "服务态度很差，等了半小时都没人理我。",
            "价格还可以，质量一般般吧。"
        };

        foreach (var text in texts)
        {
            Console.WriteLine($"\n输入: {text}");
            var result = await sentimentAgent.AnalyzeAsync(text);
            Console.WriteLine($"情感: {result.Sentiment} | 置信度: {result.Confidence/100:P0} | 强度: {result.Intensity}");
            if (result.Emotions.Any())
                Console.WriteLine($"情绪: {string.Join(", ", result.Emotions)}");
        }

        // ===== 2. 知识图谱 Agent =====
        Console.WriteLine("\n\n--- 2. 知识图谱 Agent ---");
        var kgAgent = new KnowledgeGraphAgent(
            chatClient!,
            sp,
            loggerFactory.CreateLogger<KnowledgeGraphAgent>());

        var document = @"
张三是北京ABC科技公司的CEO，该公司成立于2020年。
李四是公司的CTO，负责技术研发。
ABC科技公司的主要产品是智能客服系统。
该公司位于北京中关村，获得了红杉资本的A轮投资。";

        Console.WriteLine($"输入文档:\n{document}\n");

        var extraction = await kgAgent.ExtractAsync(document);
        Console.WriteLine($"抽取实体: {extraction.Entities.Count} 个");
        foreach (var entity in extraction.Entities)
        {
            Console.WriteLine($"  - {entity.Name} ({entity.Type})");
        }

        Console.WriteLine($"\n抽取关系: {extraction.Relations.Count} 个");
        foreach (var relation in extraction.Relations)
        {
            Console.WriteLine($"  - {relation.Source} --[{relation.Relation}]--> {relation.Target}");
        }

        // 图谱问答
        Console.WriteLine("\n--- 知识图谱问答 ---");
        var question = "张三在哪家公司工作？担任什么职位？";
        Console.WriteLine($"问题: {question}");
        var answer = await kgAgent.QueryAsync(question);
        Console.WriteLine($"回答: {answer}");

        // 路径查询
        Console.WriteLine("\n--- 实体路径查询 ---");
        var paths = kgAgent.FindPaths("张三", "红杉资本");
        Console.WriteLine($"从'张三'到'红杉资本'找到 {paths.Count} 条路径");

        // ===== 3. 质量评估 Agent =====
        Console.WriteLine("\n\n--- 3. 质量评估 Agent ---");
        var qualityAgent = new QualityEvaluatorAgent(
            chatClient!,
            sp,
            loggerFactory.CreateLogger<QualityEvaluatorAgent>());

        var userQuery = "如何学习C#编程？";
        var aiResponse = @"学习C#编程可以按以下步骤进行：

1. **基础语法**: 学习变量、数据类型、控制流程
2. **面向对象**: 掌握类、继承、多态、接口
3. **进阶特性**: async/await、LINQ、泛型
4. **实践项目**: 做一个小型控制台应用或Web API

推荐资源：Microsoft Learn、B站教程、《C# in Depth》";


        Console.WriteLine($"用户问题: {userQuery}");
        Console.WriteLine($"AI响应:\n{aiResponse}\n");

        var score = await qualityAgent.EvaluateResponseAsync(userQuery, aiResponse);
        Console.WriteLine($"质量评分:");
        Console.WriteLine($"  相关性: {score.Relevance}/10");
        Console.WriteLine($"  准确性: {score.Accuracy}/10");
        Console.WriteLine($"  清晰度: {score.Clarity}/10");
        Console.WriteLine($"  综合分: {score.Overall}/10");

        // 快速检查 (不调用LLM)
        Console.WriteLine("\n--- 快速规则检查 ---");
        var quickCheck = qualityAgent.QuickCheck(aiResponse);
        Console.WriteLine($"长度: {quickCheck.Length} 字符");
        Console.WriteLine($"结构良好: {quickCheck.IsWellStructured}");
        Console.WriteLine($"专业语气: {quickCheck.HasProfessionalTone}");
        Console.WriteLine($"通过基础检查: {quickCheck.PassesBasicCheck}");

        Console.WriteLine("\n========== 内置 Agent 演示结束 ==========");
    }
}
