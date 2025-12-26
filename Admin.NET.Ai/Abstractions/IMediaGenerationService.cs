namespace Admin.NET.Ai.Abstractions;

/// <summary>
/// 媒体生成服务接口 - 统一的多模态生成入口
/// </summary>
public interface IMediaGenerationService
{
    /// <summary>
    /// 文本转语音
    /// </summary>
    Task<TtsResult> TextToSpeechAsync(TtsRequest request, CancellationToken ct = default);

    /// <summary>
    /// 语音转文本
    /// </summary>
    Task<AsrResult> SpeechToTextAsync(AsrRequest request, CancellationToken ct = default);

    /// <summary>
    /// 文本生成图像
    /// </summary>
    Task<ImageGenResult> GenerateImageAsync(ImageGenRequest request, CancellationToken ct = default);

    /// <summary>
    /// 文本/图像生成视频
    /// </summary>
    Task<VideoGenResult> GenerateVideoAsync(VideoGenRequest request, CancellationToken ct = default);
}

#region TTS Models

/// <summary>
/// TTS 请求
/// </summary>
public class TtsRequest
{
    /// <summary>
    /// 要转换的文本
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// 供应商 (null=使用默认)
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 语音模型
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 发音人
    /// </summary>
    public string? Voice { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 情感/风格
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// 采样率
    /// </summary>
    public int SampleRate { get; set; } = 24000;

    /// <summary>
    /// 输出格式 (mp3, wav, pcm)
    /// </summary>
    public string Format { get; set; } = "mp3";
}

/// <summary>
/// TTS 结果
/// </summary>
public class TtsResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 音频数据
    /// </summary>
    public byte[]? AudioData { get; set; }

    /// <summary>
    /// 音频时长（秒）
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// 使用的供应商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 缓存路径（如果开启缓存）
    /// </summary>
    public string? CachedPath { get; set; }
}

#endregion

#region ASR Models

/// <summary>
/// ASR 请求
/// </summary>
public class AsrRequest
{
    /// <summary>
    /// 音频数据
    /// </summary>
    public byte[]? AudioData { get; set; }

    /// <summary>
    /// 音频文件路径（二选一）
    /// </summary>
    public string? AudioPath { get; set; }

    /// <summary>
    /// 音频 URL（三选一）
    /// </summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// 供应商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 是否开启标点
    /// </summary>
    public bool EnablePunctuation { get; set; } = true;

    /// <summary>
    /// 是否开启说话人分离
    /// </summary>
    public bool EnableSpeakerDiarization { get; set; } = false;
}

/// <summary>
/// ASR 结果
/// </summary>
public class AsrResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 识别的文本
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// 分段结果
    /// </summary>
    public List<AsrSegment> Segments { get; set; } = new();

    /// <summary>
    /// 使用的供应商
    /// </summary>
    public string? Provider { get; set; }
}

/// <summary>
/// ASR 分段
/// </summary>
public class AsrSegment
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; } = "";
    public string? SpeakerId { get; set; }
    public double Confidence { get; set; }
}

#endregion

#region ImageGen Models

/// <summary>
/// 图像生成请求
/// </summary>
public class ImageGenRequest
{
    /// <summary>
    /// 提示词
    /// </summary>
    public string Prompt { get; set; } = "";

    /// <summary>
    /// 负面提示词
    /// </summary>
    public string? NegativePrompt { get; set; }

    /// <summary>
    /// 供应商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 模型
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 图像尺寸 (如 "1024x1024")
    /// </summary>
    public string Size { get; set; } = "1024x1024";

    /// <summary>
    /// 生成数量
    /// </summary>
    public int Count { get; set; } = 1;

    /// <summary>
    /// 质量 (standard, hd)
    /// </summary>
    public string Quality { get; set; } = "standard";

    /// <summary>
    /// 风格 (vivid, natural)
    /// </summary>
    public string Style { get; set; } = "vivid";

    /// <summary>
    /// 输出格式 (png, jpg)
    /// </summary>
    public string Format { get; set; } = "png";

    /// <summary>
    /// 参考图像（用于 img2img）
    /// </summary>
    public byte[]? ReferenceImage { get; set; }
}

/// <summary>
/// 图像生成结果
/// </summary>
public class ImageGenResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 生成的图像列表
    /// </summary>
    public List<GeneratedImage> Images { get; set; } = new();

    /// <summary>
    /// 使用的供应商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 生成耗时（毫秒）
    /// </summary>
    public long ElapsedMs { get; set; }
}

/// <summary>
/// 生成的图像
/// </summary>
public class GeneratedImage
{
    /// <summary>
    /// 图像数据
    /// </summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// 图像 URL（如果是 URL 返回）
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 本地缓存路径
    /// </summary>
    public string? CachedPath { get; set; }

    /// <summary>
    /// 修改后的提示词（AI 可能会优化）
    /// </summary>
    public string? RevisedPrompt { get; set; }
}

#endregion

#region VideoGen Models

/// <summary>
/// 视频生成请求
/// </summary>
public class VideoGenRequest
{
    /// <summary>
    /// 提示词
    /// </summary>
    public string Prompt { get; set; } = "";

    /// <summary>
    /// 负面提示词
    /// </summary>
    public string? NegativePrompt { get; set; }

    /// <summary>
    /// 供应商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 模型
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 分辨率 (如 "1280x720")
    /// </summary>
    public string Resolution { get; set; } = "1280x720";

    /// <summary>
    /// 时长（秒）
    /// </summary>
    public int DurationSeconds { get; set; } = 5;

    /// <summary>
    /// 帧率
    /// </summary>
    public int FrameRate { get; set; } = 24;

    /// <summary>
    /// 输出格式 (mp4, webm)
    /// </summary>
    public string Format { get; set; } = "mp4";

    /// <summary>
    /// 参考图像（用于 img2video）
    /// </summary>
    public byte[]? ReferenceImage { get; set; }

    /// <summary>
    /// 参考视频（用于 video2video）
    /// </summary>
    public byte[]? ReferenceVideo { get; set; }
}

/// <summary>
/// 视频生成结果
/// </summary>
public class VideoGenResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 视频数据
    /// </summary>
    public byte[]? VideoData { get; set; }

    /// <summary>
    /// 视频 URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 本地缓存路径
    /// </summary>
    public string? CachedPath { get; set; }

    /// <summary>
    /// 使用的供应商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 生成耗时（毫秒）
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// 任务 ID（异步任务时返回）
    /// </summary>
    public string? TaskId { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public VideoGenStatus Status { get; set; }
}

/// <summary>
/// 视频生成状态
/// </summary>
public enum VideoGenStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

#endregion
