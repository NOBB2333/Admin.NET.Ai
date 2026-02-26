using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 媒体生成功能演示 - TTS, ASR, ImageGen, VideoGen
/// </summary>
public static class MediaDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [27] 媒体生成 (TTS/ASR/图像/视频) ===\n");

        var mediaService = sp.GetRequiredService<IMediaGenerationService>();

        Console.WriteLine(@"
多模态媒体生成服务:
    1. 文本转语音 (TTS) - Aliyun Qwen-TTS / Azure OpenAI
    2. 语音转文本 (ASR) - Aliyun Paraformer / Whisper
    3. 图像生成 (ImageGen) - Aliyun Wanx / DALL-E 3
    4. 视频生成 (VideoGen) - Runway Gen-2 / Stable Video
    5. 完整创作流程 - 文本→图像→视频
");
        Console.Write("请选择 (1-5): ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1": await DemoTtsAsync(mediaService); break;
            case "2": await DemoAsrAsync(mediaService); break;
            case "3": await DemoImageGenAsync(mediaService); break;
            case "4": await DemoVideoGenAsync(mediaService); break;
            case "5": await DemoFullCreationAsync(mediaService); break;
            default: Console.WriteLine("无效选择"); break;
        }
    }

    /// <summary>
    /// TTS 演示
    /// </summary>
    private static async Task DemoTtsAsync(IMediaGenerationService mediaService)
    {
        Console.WriteLine("\n=== 文本转语音 (TTS) ===\n");

        Console.Write("请输入要转换的文本 (或回车使用默认): ");
        var text = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "欢迎使用 Admin.NET.Ai 框架，这是一个企业级的 AI 中间件解决方案，支持多供应商、多模态能力。";
        }

        Console.WriteLine($"\n转换文本: {text}");
        Console.WriteLine("正在生成语音...\n");

        var result = await mediaService.TextToSpeechAsync(new TtsRequest
        {
            Text = text,
            Voice = "ruoxi",
            Language = "zh-CN",
            Format = "mp3"
        });

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ TTS 生成成功!");
            Console.WriteLine($"  供应商: {result.Provider}");
            Console.WriteLine($"  时长: {result.DurationSeconds:F1} 秒");
            Console.WriteLine($"  数据大小: {result.AudioData?.Length ?? 0} bytes");
            if (!string.IsNullOrEmpty(result.CachedPath))
            {
                Console.WriteLine($"  缓存路径: {result.CachedPath}");
            }
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ TTS 失败: {result.ErrorMessage}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// ASR 演示
    /// </summary>
    private static async Task DemoAsrAsync(IMediaGenerationService mediaService)
    {
        Console.WriteLine("\n=== 语音转文本 (ASR) ===\n");
        Console.WriteLine("注意: 这是模拟演示，实际使用需要提供音频文件\n");

        Console.WriteLine("模拟识别音频文件...\n");

        var result = await mediaService.SpeechToTextAsync(new AsrRequest
        {
            Language = "zh-CN",
            EnablePunctuation = true,
            AudioData = new byte[100] // 模拟音频数据
        });

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ ASR 识别成功!");
            Console.WriteLine($"  供应商: {result.Provider}");
            Console.WriteLine($"  识别结果: {result.Text}");
            Console.WriteLine($"\n  分段详情:");
            foreach (var seg in result.Segments)
            {
                Console.WriteLine($"    [{seg.StartTime:F1}s - {seg.EndTime:F1}s] {seg.Text} (置信度: {seg.Confidence:P0})");
            }
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ ASR 失败: {result.ErrorMessage}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 图像生成演示
    /// </summary>
    private static async Task DemoImageGenAsync(IMediaGenerationService mediaService)
    {
        Console.WriteLine("\n=== 图像生成 (ImageGen) ===\n");

        Console.Write("请输入图像生成提示词 (或回车使用默认): ");
        var prompt = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            prompt = "一只可爱的机器猫坐在星空下的草地上，仰望银河，像素艺术风格，高清细节";
        }

        Console.WriteLine($"\n提示词: {prompt}");
        Console.WriteLine("正在生成图像...\n");

        var result = await mediaService.GenerateImageAsync(new ImageGenRequest
        {
            Prompt = prompt,
            Size = "1024x1024",
            Quality = "standard",
            Style = "vivid",
            Count = 1
        });

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ 图像生成成功!");
            Console.WriteLine($"  供应商: {result.Provider}");
            Console.WriteLine($"  耗时: {result.ElapsedMs}ms");
            Console.WriteLine($"  生成数量: {result.Images.Count}");
            
            foreach (var (img, i) in result.Images.Select((x, i) => (x, i)))
            {
                Console.WriteLine($"\n  图像 {i + 1}:");
                if (!string.IsNullOrEmpty(img.Url))
                {
                    Console.WriteLine($"    URL: {img.Url}");
                }
                if (!string.IsNullOrEmpty(img.RevisedPrompt))
                {
                    Console.WriteLine($"    优化后提示词: {img.RevisedPrompt}");
                }
                if (!string.IsNullOrEmpty(img.CachedPath))
                {
                    Console.WriteLine($"    缓存路径: {img.CachedPath}");
                }
            }
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ 图像生成失败: {result.ErrorMessage}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 视频生成演示
    /// </summary>
    private static async Task DemoVideoGenAsync(IMediaGenerationService mediaService)
    {
        Console.WriteLine("\n=== 视频生成 (VideoGen) ===\n");

        Console.Write("请输入视频生成提示词 (或回车使用默认): ");
        var prompt = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            prompt = "一朵花从种子缓缓生长绽放，延时摄影风格，柔和的阳光";
        }

        Console.WriteLine($"\n提示词: {prompt}");
        Console.WriteLine("正在生成视频 (模拟异步任务)...\n");

        var result = await mediaService.GenerateVideoAsync(new VideoGenRequest
        {
            Prompt = prompt,
            Resolution = "1280x720",
            DurationSeconds = 5,
            FrameRate = 24,
            Format = "mp4"
        });

        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ 视频生成任务已提交!");
            Console.WriteLine($"  供应商: {result.Provider}");
            Console.WriteLine($"  任务ID: {result.TaskId}");
            Console.WriteLine($"  状态: {result.Status}");
            Console.WriteLine($"  耗时: {result.ElapsedMs}ms");
            
            if (!string.IsNullOrEmpty(result.Url))
            {
                Console.WriteLine($"  预览URL: {result.Url}");
            }
            Console.ResetColor();

            Console.WriteLine("\n注意: 视频生成通常是异步任务，需要轮询任务状态获取最终结果。");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ 视频生成失败: {result.ErrorMessage}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 完整创作流程演示
    /// </summary>
    private static async Task DemoFullCreationAsync(IMediaGenerationService mediaService)
    {
        Console.WriteLine("\n=== 完整创作流程 ===\n");
        Console.WriteLine("流程: 文本 → TTS语音 → 图像 → 视频\n");

        // Step 1: 准备文本
        var storyText = "在遥远的未来，人类与AI共同创造了一个和谐的世界。";
        Console.WriteLine($"1. 故事文本: {storyText}\n");

        // Step 2: TTS
        Console.WriteLine("2. 生成旁白语音...");
        var ttsResult = await mediaService.TextToSpeechAsync(new TtsRequest
        {
            Text = storyText,
            Voice = "ruoxi"
        });
        Console.WriteLine($"   ✓ TTS 完成 - {ttsResult.Provider}, 时长: {ttsResult.DurationSeconds:F1}s\n");

        // Step 3: 图像生成
        Console.WriteLine("3. 生成配图...");
        var imagePrompt = "未来城市，人类和机器人和谐相处，阳光明媚，科幻风格";
        var imageResult = await mediaService.GenerateImageAsync(new ImageGenRequest
        {
            Prompt = imagePrompt,
            Size = "1280x720"
        });
        Console.WriteLine($"   ✓ 图像生成完成 - {imageResult.Provider}");
        if (imageResult.Images.Any())
        {
            Console.WriteLine($"   URL: {imageResult.Images[0].Url}\n");
        }

        // Step 4: 视频生成
        Console.WriteLine("4. 生成动态视频...");
        var videoResult = await mediaService.GenerateVideoAsync(new VideoGenRequest
        {
            Prompt = imagePrompt + ", 镜头缓慢推进",
            Resolution = "1280x720",
            DurationSeconds = 5
        });
        Console.WriteLine($"   ✓ 视频任务提交 - {videoResult.Provider}");
        Console.WriteLine($"   任务ID: {videoResult.TaskId}");
        Console.WriteLine($"   状态: {videoResult.Status}\n");

        // Summary
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== 创作完成 ===");
        Console.WriteLine($"  文本: {storyText.Length} 字");
        Console.WriteLine($"  语音: {ttsResult.DurationSeconds:F1} 秒");
        Console.WriteLine($"  图像: {imageResult.Images.Count} 张");
        Console.WriteLine($"  视频: {videoResult.Status}");
        Console.ResetColor();

        Console.WriteLine("\n这个流程展示了如何使用 Admin.NET.Ai 的多模态能力进行完整的内容创作。");
    }
}
