# 媒体生成服务 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `IMediaGenerationService.cs` | `Abstractions/` | 服务接口定义 |
| `MediaGenerationService.cs` | `Services/Media/` | 具体实现 |
| `LLMMediaOptions.cs` | `Options/` | 配置模型 |
| `LLMAgent.Media.json` | `Configuration/` | JSON 配置 |
| `MediaDemo.cs` | `HeMaCupAICheck/Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 服务接口

```csharp
public interface IMediaGenerationService
{
    Task<TtsResult> TextToSpeechAsync(TtsRequest request, CancellationToken ct = default);
    Task<AsrResult> SpeechToTextAsync(AsrRequest request, CancellationToken ct = default);
    Task<ImageGenResult> GenerateImageAsync(ImageGenRequest request, CancellationToken ct = default);
    Task<VideoGenResult> GenerateVideoAsync(VideoGenRequest request, CancellationToken ct = default);
}
```

### 多供应商支持

| 功能 | 供应商 | 模型示例 |
|------|--------|---------|
| TTS | Aliyun Bailian | qwen3-tts-flash |
| TTS | Azure OpenAI | gpt-4o-mini-tts |
| ASR | Aliyun Bailian | fun-asr, qwen3-asr |
| ImageGen | Aliyun Wanx | wanx-v1 |
| ImageGen | OpenAI | dall-e-3 |
| VideoGen | Runway | gen-2 |
| VideoGen | Stability AI | stable-video-diffusion |

---

## 📊 请求/响应模型

### TTS (文本转语音)

```csharp
public class TtsRequest
{
    public string Text { get; set; }           // 要转换的文本
    public string? Provider { get; set; }      // 供应商 (null=默认)
    public string? Voice { get; set; }         // 发音人
    public string Language { get; set; } = "zh-CN";
    public string Format { get; set; } = "mp3";
    public int SampleRate { get; set; } = 24000;
}

public class TtsResult
{
    public bool Success { get; set; }
    public byte[]? AudioData { get; set; }     // 音频二进制
    public double DurationSeconds { get; set; }
    public string? Provider { get; set; }
    public string? CachedPath { get; set; }
}
```

### ASR (语音识别)

```csharp
public class AsrRequest
{
    public byte[]? AudioData { get; set; }     // 音频数据
    public string? AudioPath { get; set; }     // 或文件路径
    public string? AudioUrl { get; set; }      // 或 URL
    public string Language { get; set; } = "zh-CN";
    public bool EnablePunctuation { get; set; } = true;
    public bool EnableSpeakerDiarization { get; set; } = false;
}

public class AsrResult
{
    public bool Success { get; set; }
    public string Text { get; set; } = "";             // 识别文本
    public List<AsrSegment> Segments { get; set; }     // 分段结果
}

public class AsrSegment
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; }
    public string? SpeakerId { get; set; }     // 说话人分离
    public double Confidence { get; set; }
}
```

### ImageGen (图像生成)

```csharp
public class ImageGenRequest
{
    public string Prompt { get; set; }                  // 提示词
    public string? NegativePrompt { get; set; }         // 负面提示
    public string? Provider { get; set; }
    public string Size { get; set; } = "1024x1024";
    public int Count { get; set; } = 1;
    public string Quality { get; set; } = "standard";   // standard/hd
    public string Style { get; set; } = "vivid";        // vivid/natural
    public byte[]? ReferenceImage { get; set; }         // img2img
}

public class ImageGenResult
{
    public bool Success { get; set; }
    public List<GeneratedImage> Images { get; set; }
    public long ElapsedMs { get; set; }
}

public class GeneratedImage
{
    public byte[]? Data { get; set; }
    public string? Url { get; set; }
    public string? RevisedPrompt { get; set; }          // AI 优化后的提示词
}
```

### VideoGen (视频生成)

```csharp
public class VideoGenRequest
{
    public string Prompt { get; set; }
    public string Resolution { get; set; } = "1280x720";
    public int DurationSeconds { get; set; } = 5;
    public int FrameRate { get; set; } = 24;
    public byte[]? ReferenceImage { get; set; }         // img2video
}

public class VideoGenResult
{
    public bool Success { get; set; }
    public byte[]? VideoData { get; set; }
    public string? Url { get; set; }
    public string? TaskId { get; set; }                 // 异步任务 ID
    public VideoGenStatus Status { get; set; }
}

public enum VideoGenStatus { Pending, Processing, Completed, Failed }
```

---

## 🔧 供应商实现

### Aliyun Wanx 图像生成

```csharp
private async Task<ImageGenResult> CallAliyunImageGenAsync(
    ImageGenRequest request, 
    ImageGenProviderConfig config, 
    CancellationToken ct)
{
    var payload = new
    {
        model = request.Model ?? config.Model ?? "wanx-v1",
        input = new
        {
            prompt = request.Prompt,
            negative_prompt = request.NegativePrompt ?? ""
        },
        parameters = new
        {
            size = $"{width}*{height}",
            n = request.Count,
            style = request.Style
        }
    };

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, config.BaseUrl)
    {
        Content = JsonContent.Create(payload)
    };
    httpRequest.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

    var response = await _httpClient.SendAsync(httpRequest, ct);
    
    // 解析响应...
}
```

### OpenAI DALL-E 图像生成

```csharp
private async Task<ImageGenResult> CallOpenAiImageGenAsync(
    ImageGenRequest request, 
    ImageGenProviderConfig config, 
    CancellationToken ct)
{
    var payload = new
    {
        model = request.Model ?? "dall-e-3",
        prompt = request.Prompt,
        n = request.Count,
        size = request.Size,
        quality = request.Quality,
        style = request.Style,
        response_format = "url"
    };

    var httpRequest = new HttpRequestMessage(
        HttpMethod.Post, 
        $"{config.BaseUrl}/images/generations")
    {
        Content = JsonContent.Create(payload)
    };
    httpRequest.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

    // ...
}
```

---

## ⚙️ 配置结构

### LLMAgent.Media.json

```json
{
    "LLM-Tts": {
        "DefaultProvider": "AliyunBailian-qwen3-tts-flash",
        "Providers": {
            "AliyunBailian-qwen3-tts-flash": {
                "ApiKey": "sk-xxx",
                "Model": "qwen3-tts-flash",
                "Voice": "Cherry",
                "Stream": true,
                "SampleRate": 24000,
                "OutPrice": 0.00022
            }
        }
    },
    "LLM-ImageGen": {
        "DefaultProvider": "AliyunBailian",
        "Providers": {
            "AliyunBailian": {
                "ApiKey": "sk-xxx",
                "Model": "wanx-v1",
                "BaseUrl": "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-to-image",
                "SupportedSizes": ["1024x1024", "720x1280"],
                "MaxImages": 4
            },
            "OpenAI": {
                "ApiKey": "sk-xxx",
                "Model": "dall-e-3",
                "BaseUrl": "https://api.openai.com/v1"
            }
        }
    }
}
```

### Options 类

```csharp
public sealed class LLMImageGenConfig
{
    public string? DefaultProvider { get; set; }
    public Dictionary<string, ImageGenProviderConfig> Providers { get; set; } = new();
    public ImageGenDefaultConfig Defaults { get; set; } = new();
    public ImageGenCacheConfig Cache { get; set; } = new();
}

public sealed class ImageGenProviderConfig
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string? BaseUrl { get; set; }
    public List<string> SupportedSizes { get; set; } = new();
    public List<string> SupportedFormats { get; set; } = new();
    public int MaxImages { get; set; } = 4;
}
```

---

## 🚀 使用示例

```csharp
var mediaService = sp.GetRequiredService<IMediaGenerationService>();

// 图像生成
var imageResult = await mediaService.GenerateImageAsync(new ImageGenRequest
{
    Prompt = "一只可爱的机器猫，像素艺术风格",
    Provider = "AliyunBailian",
    Size = "1024x1024",
    Count = 1
});

if (imageResult.Success)
{
    foreach (var img in imageResult.Images)
    {
        Console.WriteLine($"URL: {img.Url}");
    }
}

// 完整创作流程
var text = "在遥远的未来...";
var tts = await mediaService.TextToSpeechAsync(new TtsRequest { Text = text });
var image = await mediaService.GenerateImageAsync(new ImageGenRequest { Prompt = text });
var video = await mediaService.GenerateVideoAsync(new VideoGenRequest { Prompt = text });
```

---

## ⚠️ 注意事项

1. **异步任务**: 视频生成通常是异步的，需要轮询 `TaskId`
2. **缓存**: 配置 `Cache.Enabled` 可以缓存结果，节省重复调用
3. **价格**: 注意配置价格字段用于成本计算
4. **流式 TTS**: 阿里云 TTS 支持流式返回 base64 音频
