# 12. 媒体生成 (Media Generation)

## 🎯 设计思维 (Mental Model)
现代 AI 应用不仅限于文字对话，还需要 **多模态内容创作** 能力：语音合成、语音识别、图像生成、视频生成。

`IMediaGenerationService` 提供统一接口，支持多供应商切换：
- **TTS**: 将文本转换为语音
- **ASR**: 将语音转换为文本
- **ImageGen**: 文字生成图像
- **VideoGen**: 文字/图像生成视频

---

## 🏗️ 架构设计

### 核心接口

```csharp
public interface IMediaGenerationService
{
    Task<TtsResult> TextToSpeechAsync(TtsRequest request, CancellationToken ct = default);
    Task<AsrResult> SpeechToTextAsync(AsrRequest request, CancellationToken ct = default);
    Task<ImageGenResult> GenerateImageAsync(ImageGenRequest request, CancellationToken ct = default);
    Task<VideoGenResult> GenerateVideoAsync(VideoGenRequest request, CancellationToken ct = default);
}
```

### 支持的供应商

| 功能 | 供应商 | 模型 |
|------|--------|------|
| TTS | 阿里云百炼 | qwen3-tts-flash |
| TTS | Azure OpenAI | gpt-4o-mini-tts |
| ASR | 阿里云百炼 | fun-asr, qwen3-asr |
| ImageGen | 阿里云万象 | wanx-v1 |
| ImageGen | OpenAI | dall-e-3 |
| VideoGen | Runway | gen-2 |
| VideoGen | Stability AI | stable-video-diffusion |

---

## 🚀 代码示例

### 文本转语音

```csharp
var result = await mediaService.TextToSpeechAsync(new TtsRequest
{
    Text = "欢迎使用 Admin.NET.Ai 框架",
    Voice = "ruoxi",
    Language = "zh-CN",
    Format = "mp3"
});

if (result.Success)
{
    File.WriteAllBytes("output.mp3", result.AudioData!);
}
```

### 图像生成

```csharp
var result = await mediaService.GenerateImageAsync(new ImageGenRequest
{
    Prompt = "一只可爱的机器猫，像素艺术风格",
    Provider = "AliyunBailian",  // 或 "OpenAI"
    Size = "1024x1024",
    Count = 1
});

foreach (var image in result.Images)
{
    Console.WriteLine($"Image URL: {image.Url}");
}
```

### 完整创作流程

```csharp
// 文本 → 语音 → 图像 → 视频
var text = "在遥远的未来，人类与AI共同创造了和谐的世界。";

var tts = await mediaService.TextToSpeechAsync(new TtsRequest { Text = text });
var image = await mediaService.GenerateImageAsync(new ImageGenRequest { Prompt = text });
var video = await mediaService.GenerateVideoAsync(new VideoGenRequest { Prompt = text });
```

---

## ⚙️ 配置

在 `LLMAgent.Media.json` 中配置供应商：

```json
{
  "LLM-ImageGen": {
    "DefaultProvider": "AliyunBailian",
    "Providers": {
      "AliyunBailian": {
        "ApiKey": "sk-xxx",
        "Model": "wanx-v1",
        "BaseUrl": "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-to-image"
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

---

## 📖 更多技术细节

详见 `_doc_Pro/03_Media_Generation.md`
