using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HeMaCupAICheck.Agents.BuiltIn;

/// <summary>
/// 知识图谱 Agent - 实体抽取、关系推理、图谱问答
/// 最佳实践: 结构化输出 + 内存图谱 + 路径查询
/// </summary>
public class KnowledgeGraphAgent : IAiAgent
{
    // Public properties for IAiAgent
    public string Name { get; set; } = "KnowledgeGraphAgent";
    public string Instructions { get; set; } = SystemInstruction;

    private readonly IChatClient _chatClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnowledgeGraphAgent> _logger;
    private readonly Dictionary<string, KnowledgeEntity> _entities = new();
    private readonly List<KnowledgeRelation> _relations = new();

    public const string SystemInstruction = @"你是一个知识图谱构建专家。
你的任务是从文本中抽取实体和关系，构建结构化的知识网络。

实体类型:
- Person (人物)
- Organization (组织)
- Location (地点)
- Product (产品)
- Event (事件)
- Concept (概念)

关系示例: 工作于、位于、生产、参与、属于、创立、合作等

请严格按JSON格式返回抽取结果。";

    public KnowledgeGraphAgent(
        IChatClient chatClient, 
        IServiceProvider serviceProvider,
        ILogger<KnowledgeGraphAgent> logger)
    {
        _chatClient = chatClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 从文本中抽取实体和关系
    /// </summary>
    [Description("从文本中提取实体和关系，构建知识图谱")]
    public async Task<ExtractionResult> ExtractAsync(
        [Description("待抽取的文本内容")] string text,
        CancellationToken ct = default)
    {
        try
        {
            // 使用 Builder 模式的结构化输出 API
            var result = await _chatClient
                .Structured()
                .WithSystem(SystemInstruction)
                .RunStructuredAsync<ExtractionResult>(
                    $"请从以下文本中抽取实体和关系:\n\n{text}", 
                    _serviceProvider);
            
            if (result != null)
            {
                // 更新内存图谱
                foreach (var entity in result.Entities)
                {
                    _entities[entity.Name] = entity;
                }
                _relations.AddRange(result.Relations);

                _logger.LogInformation("知识抽取完成: {Entities} 实体, {Relations} 关系",
                    result.Entities.Count, result.Relations.Count);

                return result;
            }
            
            return new ExtractionResult { Error = "解析结果为空" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "知识抽取失败");
            return new ExtractionResult { Error = ex.Message };
        }
    }

    /// <summary>
    /// 基于知识图谱回答问题
    /// </summary>
    [Description("基于已构建的知识图谱回答问题")]
    public async Task<string> QueryAsync(
        [Description("用户问题")] string question,
        CancellationToken ct = default)
    {
        var context = BuildGraphContext();
        var prompt = $@"基于以下知识图谱信息回答问题:

{context}

问题: {question}

请基于图谱中的实体和关系进行回答。如果信息不足，请说明。";

        var response = await _chatClient.GetResponseAsync(
            new List<ChatMessage> { new(ChatRole.User, prompt) }, cancellationToken: ct);

        return response.Messages.LastOrDefault()?.Text ?? "无法回答";
    }

    /// <summary>
    /// 查找两个实体之间的关系路径
    /// </summary>
    [Description("查找两个实体之间的关系路径")]
    public List<List<KnowledgeRelation>> FindPaths(
        [Description("起始实体名")] string source,
        [Description("目标实体名")] string target,
        [Description("最大路径深度")] int maxDepth = 3)
    {
        var paths = new List<List<KnowledgeRelation>>();
        var visited = new HashSet<string>();

        void Dfs(string current, List<KnowledgeRelation> path)
        {
            if (path.Count > maxDepth) return;
            if (current == target && path.Count > 0)
            {
                paths.Add(new List<KnowledgeRelation>(path));
                return;
            }

            visited.Add(current);
            foreach (var r in _relations.Where(r => r.Source == current || r.Target == current))
            {
                var next = r.Source == current ? r.Target : r.Source;
                if (!visited.Contains(next))
                {
                    path.Add(r);
                    Dfs(next, path);
                    path.RemoveAt(path.Count - 1);
                }
            }
            visited.Remove(current);
        }

        Dfs(source, new List<KnowledgeRelation>());
        return paths;
    }

    /// <summary>
    /// 获取实体详情
    /// </summary>
    [Description("获取指定实体的详细信息")]
    public KnowledgeEntity? GetEntity([Description("实体名称")] string name)
        => _entities.TryGetValue(name, out var e) ? e : null;

    /// <summary>
    /// 获取实体的所有关系
    /// </summary>
    [Description("获取指定实体的所有关系")]
    public List<KnowledgeRelation> GetRelations([Description("实体名称")] string entityName)
        => _relations.Where(r => r.Source == entityName || r.Target == entityName).ToList();

    /// <summary>
    /// 获取图谱统计信息
    /// </summary>
    [Description("获取知识图谱的统计信息")]
    public GraphStats GetStats() => new()
    {
        EntityCount = _entities.Count,
        RelationCount = _relations.Count,
        EntityTypes = _entities.Values.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count()),
        RelationTypes = _relations.GroupBy(r => r.Relation).ToDictionary(g => g.Key, g => g.Count())
    };

    /// <summary>
    /// 清空图谱
    /// </summary>
    public void Clear()
    {
        _entities.Clear();
        _relations.Clear();
    }

    private string BuildGraphContext()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【实体】");
        foreach (var e in _entities.Values.Take(30))
            sb.AppendLine($"- {e.Name} ({e.Type})");

        sb.AppendLine("\n【关系】");
        foreach (var r in _relations.Take(30))
            sb.AppendLine($"- {r.Source} --[{r.Relation}]--> {r.Target}");

        return sb.ToString();
    }
}

#region 知识图谱模型

public class ExtractionResult
{
    [JsonPropertyName("entities")]
    public List<KnowledgeEntity> Entities { get; set; } = new();

    [JsonPropertyName("relations")]
    public List<KnowledgeRelation> Relations { get; set; } = new();

    public string? Error { get; set; }
}

public class KnowledgeEntity
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Concept";

    [JsonPropertyName("attributes")]
    public System.Text.Json.JsonElement? Attributes { get; set; }
}

public class KnowledgeRelation
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("relation")]
    public string Relation { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class GraphStats
{
    public int EntityCount { get; set; }
    public int RelationCount { get; set; }
    public Dictionary<string, int> EntityTypes { get; set; } = new();
    public Dictionary<string, int> RelationTypes { get; set; } = new();
}

#endregion
