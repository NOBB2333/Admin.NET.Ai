using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 场景18: RAG + Agent 智能问答
/// 
/// 📌 企业最常用场景：知识库检索 + Agent 推理回答
/// 
/// 流程:
/// 1. 用户提问 → RAG 检索相关文档
/// 2. 检索结果 + 原问题 → Agent 推理
/// 3. 基于知识库的精准回答
/// </summary>
public static class RagAgentDemo
{
    // 模拟知识库 (实际用向量数据库)
    private static readonly Dictionary<string, string> KnowledgeBase = new()
    {
        ["员工手册-请假规定"] = """
            员工请假规定：
            1. 年假：每年15天，需提前5天申请
            2. 病假：需提供医院证明，当天或次日补办手续
            3. 事假：需提前3天申请，超过3天需部门经理审批
            4. 婚假：法定3天，晚婚可延长至15天
            5. 产假：女员工158天，男员工陪产假15天
            """,
        ["员工手册-差旅报销"] = """
            差旅报销标准：
            1. 交通：火车二等座、飞机经济舱（4小时以上航程）
            2. 住宿：一线城市不超过500元/晚，二线400元/晚
            3. 餐补：100元/天
            4. 流程：填写报销单→附发票→部门审批→财务审核（5个工作日）
            5. 注意：超标需提前申请特批
            """,
        ["技术文档-用户API"] = """
            用户管理 API 文档：
            - GET /api/users - 获取用户列表，支持分页 ?page=1&size=20
            - GET /api/users/{id} - 获取单个用户详情
            - POST /api/users - 创建用户，需要 {name, email, role}
            - PUT /api/users/{id} - 更新用户信息
            - DELETE /api/users/{id} - 删除用户
            认证：所有接口需要 Bearer Token，Header: Authorization: Bearer {token}
            """,
        ["产品手册-Admin.NET.Ai功能"] = """
            Admin.NET.Ai 核心功能：
            1. 多模型工厂 (AiFactory) - 统一管理多个 LLM 提供商
            2. 中间件管道 - 日志、审计、重试、限流、Token监控
            3. RAG 知识检索 - 向量相似度搜索 + 知识图谱
            4. MCP 工具调用 - 支持 Stdio/HTTP 协议连接外部工具
            5. 工作流编排 - 多 Agent 协作、顺序/并行执行
            6. 结构化输出 - JSON Schema 约束生成
            """,
        ["产品手册-MCP集成"] = """
            MCP (Model Context Protocol) 集成说明：
            1. 支持 Stdio 和 HTTP 两种传输协议
            2. 使用 McpToolFactory 加载外部工具
            3. 工具会自动转换为 MEAI 的 AITool 格式
            4. 配合 FunctionInvocation 中间件实现自动工具调用
            5. 配置文件: LLMAgent.Mcp.json
            """
    };

    public static async Task RunAsync(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("RagAgentDemo");
        var aiFactory = sp.GetRequiredService<IAiFactory>();

        Console.WriteLine("\n========== RAG + Agent 智能问答 ==========\n");

        // ===== 1. 展示知识库 =====
        Console.WriteLine("--- 1. 企业知识库内容 ---");
        foreach (var doc in KnowledgeBase)
        {
            Console.WriteLine($"  📄 {doc.Key}");
        }

        // ===== 2. RAG 检索演示 =====
        Console.WriteLine("\n--- 2. 智能检索 + Agent 问答 ---");
        
        var questions = new[]
        {
            "请假需要提前多久申请？",
            "出差住酒店有什么标准？",
            "Admin.NET.Ai 有哪些功能？",
            "如何调用用户API？",
            "MCP是什么？"
        };

        try
        {
            var chatClient = aiFactory.GetDefaultChatClient();

            foreach (var question in questions)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n🙋 用户: {question}");
                Console.ResetColor();

                // RAG 检索 (模拟向量相似度搜索)
                var docs = SearchDocuments(question);
                Console.WriteLine($"📚 检索到 {docs.Count} 条相关文档");
                
                if (docs.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   匹配: {string.Join(", ", docs.Select(d => d.Key))}");
                    Console.ResetColor();
                }

                // 构建 RAG 增强 Prompt
                var context = docs.Any() 
                    ? string.Join("\n\n", docs.Select(d => $"【{d.Key}】\n{d.Value}")) 
                    : "未找到相关文档";

                var enhancedPrompt = $"""
                你是一个企业知识库助手。请基于以下知识库内容回答用户问题。
                如果知识库中没有相关信息，请明确说明"知识库中未找到相关信息"。
                回答要简洁准确，可以适当总结要点。

                === 知识库内容 ===
                {context}

                === 用户问题 ===
                {question}

                请回答：
                """;

                // Agent 推理 (流式输出)
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("🤖 助手: ");
                await chatClient!.GetStreamingResponseAsync(enhancedPrompt).WriteToConsoleAsync();
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 需要配置 LLM: {ex.Message}");
        }

        // ===== 3. 代码示例 =====
        Console.WriteLine("\n--- 3. 完整代码示例 ---");
        Console.WriteLine(@"
// 1. 向量检索 (使用 Embedding + 余弦相似度)
var embedding = await embeddingGenerator.GenerateEmbeddingAsync(userQuery);
var docs = await vectorDb.SearchAsync(embedding, topK: 5, threshold: 0.7);

// 2. 构建 RAG Prompt
var context = string.Join(""\n"", docs.Select(d => d.Content));
var prompt = $""基于以下内容回答:\n{context}\n\n问题: {userQuery}"";

// 3. Agent 推理
var response = await chatClient.GetStreamingResponseAsync(prompt).WriteToConsoleAsync();
");

        Console.WriteLine("\n========== RAG + Agent 演示结束 ==========");
    }

    /// <summary>
    /// 模拟 RAG 检索 (实际应使用向量相似度)
    /// </summary>
    private static List<KeyValuePair<string, string>> SearchDocuments(string query)
    {
        var results = new List<(KeyValuePair<string, string> Doc, int Score)>();
        
        // 提取关键词
        var keywords = ExtractKeywords(query);
        
        foreach (var doc in KnowledgeBase)
        {
            var score = 0;
            var docText = doc.Key + " " + doc.Value;
            
            foreach (var keyword in keywords)
            {
                if (doc.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    score += 5; // 标题匹配权重最高
                if (doc.Value.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    score += 2; // 内容匹配
            }
            
            // 同义词/相关词匹配
            score += GetSynonymScore(query, doc.Key, doc.Value);
            
            if (score > 0)
            {
                results.Add((doc, score));
            }
        }
        
        return results
            .OrderByDescending(r => r.Score)
            .Take(3)
            .Select(r => r.Doc)
            .ToList();
    }

    /// <summary>
    /// 同义词/相关词匹配加分
    /// </summary>
    private static int GetSynonymScore(string query, string docKey, string docValue)
    {
        var synonymGroups = new Dictionary<string[], string[]>
        {
            // 查询词 -> 文档可能包含的词
            { new[] { "请假", "假期", "休假", "年假", "病假", "事假" }, new[] { "请假", "假" } },
            { new[] { "出差", "住酒店", "酒店", "报销", "差旅", "交通" }, new[] { "差旅", "报销", "住宿" } },
            { new[] { "API", "接口", "调用", "用户接口" }, new[] { "API", "接口" } },
            { new[] { "MCP", "工具", "协议", "外部工具" }, new[] { "MCP", "工具" } },
            { new[] { "功能", "特性", "能力", "支持" }, new[] { "功能", "核心" } }
        };

        var score = 0;
        var queryLower = query.ToLower();
        var docTextLower = (docKey + " " + docValue).ToLower();

        foreach (var group in synonymGroups)
        {
            var queryWords = group.Key;
            var docWords = group.Value;

            var queryMatch = queryWords.Any(w => queryLower.Contains(w.ToLower()));
            var docMatch = docWords.Any(w => docTextLower.Contains(w.ToLower()));

            if (queryMatch && docMatch)
            {
                score += 3;
            }
        }

        return score;
    }

    private static string[] ExtractKeywords(string query)
    {
        // 停用词
        var stopWords = new HashSet<string> 
        { 
            "的", "是", "有", "什么", "怎么", "如何", "哪些", "需要", "可以", "吗", 
            "？", "。", "要", "能", "会", "在", "了", "呢", "啊", "吧"
        };
        
        // 先按常见分隔符分词
        var words = query
            .Split(new[] { ' ', '，', ',', '、', '？', '?', '。', '!', '：', ':' }, 
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 1 && !stopWords.Contains(w))
            .ToList();

        // 添加滑动窗口提取的词组 (2-4字)
        for (int len = 2; len <= Math.Min(4, query.Length); len++)
        {
            for (int i = 0; i <= query.Length - len; i++)
            {
                var substr = query.Substring(i, len);
                if (!stopWords.Contains(substr) && !substr.Any(c => "？?。，,、：:".Contains(c)))
                {
                    words.Add(substr);
                }
            }
        }

        return words.Distinct().ToArray();
    }
}
