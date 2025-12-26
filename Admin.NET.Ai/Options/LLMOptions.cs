using Furion.ConfigurableOptions;
using System.Text.Json.Serialization;

namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM Agent 配置选项
/// </summary>
public sealed class LLMAgentOptions : IConfigurableOptions
{
    /// <summary>  LLM 客户端配置 </summary>
    [JsonPropertyName("LLM-Clients")]
    public LLMClientsConfig LLMClients { get; set; } = new();

    /// <summary>  LLM MCP 服务器配置  </summary>
    [JsonPropertyName("LLM-Mcp")]
    public LLMMcpConfig LLMMcp { get; set; } = new();

    /// <summary>  LLM RAG 配置 </summary>
    [JsonPropertyName("LLM-Rag")]
    public LLMRagConfig LLMRag { get; set; } = new();

    /// <summary>  LLM Graph RAG 配置 </summary>
    [JsonPropertyName("LLM-GraphRag")]
    public LLMGraphRagConfig LLMGraphRag { get; set; } = new();

    /// <summary>  LLM 语音合成配置 </summary>
    [JsonPropertyName("LLM-Tts")]
    public LLMTtsConfig LLMTts { get; set; } = new();

    /// <summary>  LLM 语音识别配置 </summary>
    [JsonPropertyName("LLM-Asr")]
    public LLMAsrConfig LLMAsr { get; set; } = new();

    /// <summary>  LLM 图像生成配置 </summary>
    [JsonPropertyName("LLM-ImageGen")]
    public LLMImageGenConfig LLMImageGen { get; set; } = new();

    /// <summary>  LLM 视频生成配置 </summary>
    [JsonPropertyName("LLM-VideoGen")]
    public LLMVideoGenConfig LLMVideoGen { get; set; } = new();

    /// <summary>  LLM Agent 工作流配置 </summary>
    [JsonPropertyName("LLM-AgentWorkflow")]
    public LLMAgentWorkflowConfig LLMAgentWorkflow { get; set; } = new();

    /// <summary>  LLM 文档处理配置 </summary>
    [JsonPropertyName("LLM-Document")]
    public LLMDocumentConfig LLMDocument { get; set; } = new();

    /// <summary>  LLM 成本控制配置 </summary>
    [JsonPropertyName("LLM-CostControl")]
    public LLMCostControlConfig LLMCostControl { get; set; } = new();

    /// <summary>  通用大模型能力扩展配置 </summary>
    [JsonPropertyName("LLM-Capabilities")]
    public LLMCapabilitiesConfig LLMCapabilities { get; set; } = new();

    /// <summary>  AI 持久化存储配置 </summary>
    [JsonPropertyName("LLM-Persistence")]
    public LLMPersistenceConfig LLMPersistence { get; set; } = new();
}

/// <summary>
/// Lucene 配置选项
/// </summary>
public sealed class LuceneOptions : IConfigurableOptions
{
    public string? IndexPath { get; set; }
}