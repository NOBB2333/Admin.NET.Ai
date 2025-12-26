namespace Admin.NET.Ai.Options;

/// <summary>
/// LLM 语音合成配置
/// </summary>
public sealed class LLMTtsConfig
{
    /// <summary> 默认语音提供商 </summary>
    public string? DefaultProvider { get; set; }

    /// <summary> 语音配置字典 </summary>
    public Dictionary<string, TtsVoiceConfig> Voices { get; set; } = new();

    /// <summary> 语音缓存配置 </summary>
    public TtsCacheConfig Cache { get; set; } = new();
}

/// <summary>
/// TTS 语音配置
/// </summary>
public sealed class TtsVoiceConfig
{
    /// <summary> 使用的语音模型 </summary>
    public string? Model { get; set; }

    /// <summary> 发音人 </summary>
    public string? Voice { get; set; }

    /// <summary> 语言 </summary>
    public string? Language { get; set; }

    /// <summary> 情感/语气 </summary>
    public string? Style { get; set; }

    /// <summary> 采样率 </summary>
    public int SampleRate { get; set; }
}

/// <summary>
/// TTS 缓存配置
/// </summary>
public sealed class TtsCacheConfig
{
    /// <summary> 是否开启语音缓存 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 缓存目录 </summary>
    public string? Path { get; set; }

    /// <summary> 缓存过期时间（小时） </summary>
    public int ExpiryHours { get; set; } = 72;
}

/// <summary>
/// LLM 语音识别配置
/// </summary>
public sealed class LLMAsrConfig
{
    /// <summary> 默认提供商 </summary>
    public string? DefaultProvider { get; set; }

    /// <summary> ASR 配置字典 </summary>
    public Dictionary<string, AsrProviderConfig> Providers { get; set; } = new();

    /// <summary> 语音缓存配置 </summary>
    public AsrCacheConfig Cache { get; set; } = new();
}

/// <summary>
/// ASR 提供商配置
/// </summary>
public sealed class AsrProviderConfig
{
    /// <summary> API 密钥 </summary>
    public string? ApiKey { get; set; }

    /// <summary> 模型名称 </summary>
    public string? Model { get; set; }

    /// <summary> API 基础地址 </summary>
    public string? BaseUrl { get; set; }

    /// <summary> 支持的语言列表 </summary>
    public List<string> SupportedLanguages { get; set; } = new();

    /// <summary> 采样率要求 </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary> 是否支持实时识别 </summary>
    public bool EnableRealtime { get; set; } = false;

    /// <summary> 是否支持标点符号 </summary>
    public bool EnablePunctuation { get; set; } = true;

    /// <summary> 是否支持说话人分离 </summary>
    public bool EnableSpeakerDiarization { get; set; } = false;
}

/// <summary>
/// ASR 缓存配置
/// </summary>
public sealed class AsrCacheConfig
{
    /// <summary> 是否开启识别结果缓存 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 缓存目录 </summary>
    public string? Path { get; set; }

    /// <summary> 缓存过期时间（小时） </summary>
    public int ExpiryHours { get; set; } = 24;
}

/// <summary>
/// LLM 图像生成配置
/// </summary>
public sealed class LLMImageGenConfig
{
    /// <summary> 默认提供商 </summary>
    public string? DefaultProvider { get; set; }

    /// <summary> 图像生成配置字典 </summary>
    public Dictionary<string, ImageGenProviderConfig> Providers { get; set; } = new();

    /// <summary> 默认图像配置 </summary>
    public ImageGenDefaultConfig Defaults { get; set; } = new();

    /// <summary> 图像缓存配置 </summary>
    public ImageGenCacheConfig Cache { get; set; } = new();
}

/// <summary>
/// 图像生成提供商配置
/// </summary>
public sealed class ImageGenProviderConfig
{
    /// <summary> API 密钥 </summary>
    public string? ApiKey { get; set; }

    /// <summary> 模型名称 </summary>
    public string? Model { get; set; }

    /// <summary> API 基础地址 </summary>
    public string? BaseUrl { get; set; }

    /// <summary> 支持的图像尺寸列表 </summary>
    public List<string> SupportedSizes { get; set; } = new();

    /// <summary> 支持的图像格式列表 </summary>
    public List<string> SupportedFormats { get; set; } = new();

    /// <summary> 最大生成数量 </summary>
    public int MaxImages { get; set; } = 4;
}

/// <summary>
/// 图像生成默认配置
/// </summary>
public sealed class ImageGenDefaultConfig
{
    /// <summary> 默认图像尺寸 </summary>
    public string DefaultSize { get; set; } = "1024x1024";

    /// <summary> 默认图像格式 </summary>
    public string DefaultFormat { get; set; } = "png";

    /// <summary> 默认生成数量 </summary>
    public int DefaultCount { get; set; } = 1;

    /// <summary> 默认质量 </summary>
    public string DefaultQuality { get; set; } = "standard";

    /// <summary> 默认风格 </summary>
    public string DefaultStyle { get; set; } = "vivid";
}

/// <summary>
/// 图像生成缓存配置
/// </summary>
public sealed class ImageGenCacheConfig
{
    /// <summary> 是否开启图像缓存 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 缓存目录 </summary>
    public string? Path { get; set; }

    /// <summary> 缓存过期时间（小时） </summary>
    public int ExpiryHours { get; set; } = 168; // 7天

    /// <summary> 最大缓存大小（MB） </summary>
    public int MaxCacheSizeMB { get; set; } = 1024;
}

/// <summary>
/// LLM 视频生成配置
/// </summary>
public sealed class LLMVideoGenConfig
{
    /// <summary> 默认提供商 </summary>
    public string? DefaultProvider { get; set; }

    /// <summary> 视频生成配置字典 </summary>
    public Dictionary<string, VideoGenProviderConfig> Providers { get; set; } = new();

    /// <summary> 默认视频配置 </summary>
    public VideoGenDefaultConfig Defaults { get; set; } = new();

    /// <summary> 视频缓存配置 </summary>
    public VideoGenCacheConfig Cache { get; set; } = new();
}

/// <summary>
/// 视频生成提供商配置
/// </summary>
public sealed class VideoGenProviderConfig
{
    /// <summary> API 密钥 </summary>
    public string? ApiKey { get; set; }

    /// <summary> 模型名称 </summary>
    public string? Model { get; set; }

    /// <summary> API 基础地址 </summary>
    public string? BaseUrl { get; set; }

    /// <summary> 支持的分辨率列表 </summary>
    public List<string> SupportedResolutions { get; set; } = new();

    /// <summary> 支持的最大时长（秒） </summary>
    public int MaxDurationSeconds { get; set; } = 60;

    /// <summary> 支持的帧率列表 </summary>
    public List<int> SupportedFrameRates { get; set; } = new();
}

/// <summary>
/// 视频生成默认配置
/// </summary>
public sealed class VideoGenDefaultConfig
{
    /// <summary> 默认分辨率 </summary>
    public string DefaultResolution { get; set; } = "1280x720";

    /// <summary> 默认时长（秒） </summary>
    public int DefaultDurationSeconds { get; set; } = 5;

    /// <summary> 默认帧率 </summary>
    public int DefaultFrameRate { get; set; } = 24;

    /// <summary> 默认格式 </summary>
    public string DefaultFormat { get; set; } = "mp4";
}

/// <summary>
/// 视频生成缓存配置
/// </summary>
public sealed class VideoGenCacheConfig
{
    /// <summary> 是否开启视频缓存 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> 缓存目录 </summary>
    public string? Path { get; set; }

    /// <summary> 缓存过期时间（小时） </summary>
    public int ExpiryHours { get; set; } = 168; // 7天

    /// <summary> 最大缓存大小（GB） </summary>
    public int MaxCacheSizeGB { get; set; } = 10;
}
