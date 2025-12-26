# 内置 Agent 与多模态 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `SentimentAnalysisAgent.cs` | `Agents/BuiltIn/` | 情感分析 Agent |
| `KnowledgeGraphAgent.cs` | `Agents/BuiltIn/` | 知识图谱 Agent |
| `QualityAssessmentAgent.cs` | `Agents/BuiltIn/` | 质量评估 Agent |
| `BuiltInAgentDemo.cs` | `Demos/` | 演示代码 |
| `MultimodalDemo.cs` | `Demos/` | 多模态演示 |

---

## 🤖 内置 Agent

### 1. 情感分析 Agent

```csharp
public class SentimentAnalysisAgent
{
    private readonly IChatClient _client;
    
    public async Task<SentimentResult> AnalyzeAsync(string text)
    {
        var prompt = $@"
分析以下文本的情感，返回 JSON 格式:
{{
  ""sentiment"": ""positive/negative/neutral"",
  ""score"": 0.0-1.0,
  ""emotions"": [""joy"", ""sadness"", ...],
  ""keywords"": [""关键词1"", ...]
}}

文本: {text}";

        var response = await _client.GetResponseAsync(prompt);
        return JsonSerializer.Deserialize<SentimentResult>(response.Text)!;
    }
}

public class SentimentResult
{
    public string Sentiment { get; set; } = "neutral";
    public double Score { get; set; }
    public List<string> Emotions { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
}
```

### 2. 知识图谱 Agent

```csharp
public class KnowledgeGraphAgent
{
    private readonly IChatClient _client;
    
    public async Task<List<Triple>> ExtractTriplesAsync(string text)
    {
        var prompt = $@"
从以下文本中提取知识三元组 (主体, 关系, 客体)，返回 JSON 数组:
[
  {{"subject": "...", "relation": "...", "object": "..."}}
]

文本: {text}";

        var response = await _client.GetResponseAsync(prompt);
        return JsonSerializer.Deserialize<List<Triple>>(response.Text) ?? new();
    }
}

public class Triple
{
    public string Subject { get; set; } = "";
    public string Relation { get; set; } = "";
    public string Object { get; set; } = "";
}
```

### 3. 质量评估 Agent

```csharp
public class QualityAssessmentAgent
{
    private readonly IChatClient _client;
    
    public async Task<QualityResult> AssessAsync(string content, string criteria)
    {
        var prompt = $@"
根据以下标准评估内容质量 (1-10分):
评估标准: {criteria}

内容:
{content}

返回 JSON:
{{
  ""overallScore"": 1-10,
  ""dimensions"": {{
    ""clarity"": 1-10,
    ""accuracy"": 1-10,
    ""completeness"": 1-10
  }},
  ""suggestions"": [""改进建议1"", ...]
}}";

        var response = await _client.GetResponseAsync(prompt);
        return JsonSerializer.Deserialize<QualityResult>(response.Text)!;
    }
}
```

---

## 🎨 多模态能力

### Vision (图像理解)

```csharp
public class VisionService
{
    private readonly IChatClient _client;
    
    public async Task<string> DescribeImageAsync(byte[] imageData)
    {
        // 转换为 Base64
        var base64 = Convert.ToBase64String(imageData);
        
        // 构建多模态消息
        var message = new ChatMessage(ChatRole.User, new AIContent[]
        {
            new TextContent("请描述这张图片的内容"),
            new ImageContent(base64, "image/png")
        });
        
        var response = await _client.GetResponseAsync(new[] { message });
        return response.Text;
    }
    
    public async Task<List<string>> ExtractTextFromImageAsync(byte[] imageData)
    {
        var base64 = Convert.ToBase64String(imageData);
        
        var message = new ChatMessage(ChatRole.User, new AIContent[]
        {
            new TextContent("识别图片中的所有文字，每行一个"),
            new ImageContent(base64, "image/png")
        });
        
        var response = await _client.GetResponseAsync(new[] { message });
        return response.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
```

### Audio (音频处理)

```csharp
public class AudioService
{
    private readonly IMediaGenerationService _mediaService;
    
    // 语音转文字
    public async Task<string> TranscribeAsync(byte[] audioData)
    {
        var result = await _mediaService.SpeechToTextAsync(new AsrRequest
        {
            AudioData = audioData,
            Language = "zh-CN",
            EnablePunctuation = true
        });
        
        return result.Text;
    }
    
    // 文字转语音
    public async Task<byte[]> SynthesizeAsync(string text, string voice = "ruoxi")
    {
        var result = await _mediaService.TextToSpeechAsync(new TtsRequest
        {
            Text = text,
            Voice = voice,
            Format = "mp3"
        });
        
        return result.AudioData!;
    }
}
```

---

## 🔗 组合使用

```csharp
public class MultimodalPipeline
{
    private readonly VisionService _vision;
    private readonly AudioService _audio;
    private readonly SentimentAnalysisAgent _sentiment;
    
    public async Task<MultimodalAnalysis> AnalyzeAsync(byte[] imageData)
    {
        // 1. 图像描述
        var description = await _vision.DescribeImageAsync(imageData);
        
        // 2. 情感分析
        var sentiment = await _sentiment.AnalyzeAsync(description);
        
        // 3. 语音播报
        var audioData = await _audio.SynthesizeAsync(description);
        
        return new MultimodalAnalysis
        {
            Description = description,
            Sentiment = sentiment,
            AudioNarration = audioData
        };
    }
}
```

---

## 🚀 使用示例

```csharp
// 情感分析
var sentimentAgent = new SentimentAnalysisAgent(client);
var result = await sentimentAgent.AnalyzeAsync("这个产品真是太棒了！用户体验超好！");
Console.WriteLine($"情感: {result.Sentiment} ({result.Score:P0})");

// 图像描述
var visionService = new VisionService(client);
var imageBytes = await File.ReadAllBytesAsync("photo.jpg");
var description = await visionService.DescribeImageAsync(imageBytes);
Console.WriteLine($"图片内容: {description}");

// 知识图谱提取
var kgAgent = new KnowledgeGraphAgent(client);
var triples = await kgAgent.ExtractTriplesAsync("马云创立了阿里巴巴，总部在杭州");
foreach (var t in triples)
{
    Console.WriteLine($"({t.Subject}, {t.Relation}, {t.Object})");
}
// Output: (马云, 创立, 阿里巴巴), (阿里巴巴, 总部在, 杭州)
```

---

## ⚠️ 注意事项

1. **模型能力**: 多模态需要支持 Vision 的模型 (GPT-4o, Gemini Pro Vision)
2. **图片大小**: 注意 Base64 编码后的大小限制
3. **音频格式**: TTS/ASR 需要兼容的音频格式
4. **成本**: 多模态调用通常比纯文本贵
