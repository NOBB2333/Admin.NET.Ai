using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 场景18: RAG + Agent 智能问答 (轻量级/长下文暴力填充模式)
/// 
/// 📌 企业最常用场景：知识库检索 + Agent 推理回答
/// 
/// 流程:
/// 1. 用户提问 → RAG 检索相关文档
/// 2. 检索结果 + 原问题 → Agent 推理
/// 3. 基于知识库的精准回答
/// 【本方案说明】
/// 这是最基础的“提示词工程”版 RAG (Prompt-Stuffing RAG)。
/// 做法：直接读取本地的几个文档，通过极其简单的关键词 Contains 匹配（甚至如果文档少，就全部无脑读取），
/// 然后一股脑（全部拼接成一长串字符串）塞给大模型的 Prompt 里面，依靠大模型的长上下文能力（Long Context）让模型自己去归纳。
///
/// 【适用/不适用场景】
/// ✅ 适用：知识库非常小（比如只有几万字，几个 md/txt 文件）。无需维护庞大的向量数据库，也不会丢失全局上下文。
/// ❌ 不适用：当文件达到几十个、数百个时，Token 会爆炸，API 费用极高，且模型会彻底遗忘中间内容。
/// </summary>
public static class RagAgentDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("RagAgentDemo");
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        
        var loader = sp.GetRequiredService<Admin.NET.Ai.Services.Rag.LocalTextDocumentLoader>();
        var staticPath = Path.Combine(AppContext.BaseDirectory, "Demos", "Static", "RagFile");
        var rawDocs = await loader.LoadDirectoryAsync(staticPath);

        // 如果没有找到文件，使用一个默认的
        if (rawDocs.Count == 0)
        {
            rawDocs.Add(new RawDocument { 
                SourceName = "无知识库", 
                Content = "未能从 Demos/Static/RagFile 目录中读取到任何知识库文件。请添加文件后再试。" 
            });
        }

        // 构建临时的内存 KnowledgeBase 字典供搜索使用 (简化 RAG)
        var knowledgeBase = rawDocs.ToDictionary(
            d => d.SourceName ?? "Unknown", 
            d => d.Content
        );

        Console.WriteLine("\n=== [18] RAG + Agent 智能问答 (知识库+推理) ===\n");

        // ===== 1. 展示知识库 =====
        Console.WriteLine("--- 1. 企业知识库内容 ---");
        foreach (var doc in knowledgeBase)
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
                var docs = SearchDocuments(knowledgeBase, question);
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

            // ================= 新增：支持用户手动输入问答 =================
            Console.WriteLine("\n--------------------------------------------------");
            Console.WriteLine("✨ 现在你可以试着自己向智能问答助手提问了！");
            Console.WriteLine("请输入你的问题 (输入 'q' 或 'exit' 退出):");
            
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\n🙋 你的问题: ");
                Console.ResetColor();
                
                var userQuestion = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userQuestion)) continue;
                if (userQuestion.Trim().Equals("q", StringComparison.OrdinalIgnoreCase) || 
                    userQuestion.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // 同样进行检索和推理
                var docs = SearchDocuments(knowledgeBase, userQuestion);
                Console.WriteLine($"📚 检索到 {docs.Count} 条相关文档");
                if (docs.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   匹配: {string.Join(", ", docs.Select(d => d.Key))}");
                    Console.ResetColor();
                }

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
                {userQuestion}

                请回答：
                """;

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
        Console.WriteLine("""

                          // 1. 向量检索 (使用 Embedding + 余弦相似度)
                          var embedding = await embeddingGenerator.GenerateEmbeddingAsync(userQuery);
                          var docs = await vectorDb.SearchAsync(embedding, topK: 5, threshold: 0.7);

                          // 2. 构建 RAG Prompt
                          var context = string.Join("\n", docs.Select(d => d.Content));
                          var prompt = $"基于以下内容回答:\n{context}\n\n问题: {userQuery}";

                          // 3. Agent 推理
                          var response = await chatClient.GetStreamingResponseAsync(prompt).WriteToConsoleAsync();

                          """);

        Console.WriteLine("\n========== RAG + Agent 演示结束 ==========");
    }

    /// <summary>
    /// 模拟 RAG 检索 (实际应使用向量相似度)
    /// </summary>
    private static List<KeyValuePair<string, string>> SearchDocuments(Dictionary<string, string> knowledgeBase, string query)
    {
        var results = new List<(KeyValuePair<string, string> Doc, int Score)>();
        
        // 提取关键词
        var keywords = ExtractKeywords(query);
        
        foreach (var doc in knowledgeBase)
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
        
        // 如果严格匹配一无获，且总文档数不多（低于 20），则当作全量上下文返回（简化 Demo 逻辑）
        if (results.Count == 0 && knowledgeBase.Count <= 20)
        {
            return knowledgeBase.Select(k => new KeyValuePair<string, string>(k.Key, k.Value)).ToList();
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
            { ["请假", "假期", "休假", "年假", "病假", "事假"], ["请假", "假"] },
            { ["出差", "住酒店", "酒店", "报销", "差旅", "交通"], ["差旅", "报销", "住宿"] },
            { ["API", "接口", "调用", "用户接口"], ["API", "接口"] },
            { ["MCP", "工具", "协议", "外部工具"], ["MCP", "工具"] },
            { ["功能", "特性", "能力", "支持"], ["功能", "核心"] }
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
