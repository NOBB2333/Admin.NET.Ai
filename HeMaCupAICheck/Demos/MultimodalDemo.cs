using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Admin.NET.Ai.Extensions;

namespace HeMaCupAICheck.Demos;

public static class MultimodalDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [6] 多模态能力 (Vision & Audio) ===");

        var aiFactory = sp.GetRequiredService<IAiFactory>();
        // 尝试获取支持 Vision 的客户端，或者使用默认的
        // 这里为了演示，我们假设 Default Client 支持 Vision，或者提示用户
        var client = aiFactory.GetDefaultChatClient();
        
        if (client == null)
        {
            Console.WriteLine("未配置默认 ChatClient。");
            return;
        }

        // 1. 网络图片示例
        Console.WriteLine("\n=== [示例 1] 网络图片 ===");
        var imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/Gfp-wisconsin-madison-the-nature-boardwalk.jpg/480px-Gfp-wisconsin-madison-the-nature-boardwalk.jpg";
        imageUrl = "https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png";



        var message = new ChatMessage(ChatRole.User, new List<AIContent> 
        {
            new TextContent($"这张图片里有什么？"),
            new UriContent(new Uri(imageUrl), "image/jpeg") 
        });

        try 
        {
            Console.WriteLine($"发送请求 (网络图片)...");
            await client.GetStreamingResponseAsync(new[] { message }).WriteToConsoleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[注意] 调用失败: {ex.Message}");
        }

        // 2. 本地图片示例
        Console.WriteLine("\n=== [示例 2] 本地图片 (Rust.png) ===");
        var localImagePath = Path.Combine(AppContext.BaseDirectory, "Demos/Static/Image/farm.jpg");
        // var localImagePath = Path.Combine(AppContext.BaseDirectory, "Demos/Static/Image/Rust.png");
        
        if (File.Exists(localImagePath))
        {
             try 
             {
                 // 使用 UriContent 传递本地文件路径 (由 AiFactory 自动处理读取)
                 var localMsg = new ChatMessage(ChatRole.User, new List<AIContent> 
                 {
                     new TextContent("这个图片是什么？请简要介绍一下。"),
                     new UriContent(new Uri(localImagePath), "image/png")
                 });

                 Console.WriteLine($"发送请求 (本地图片: {localImagePath})...");
                 await client.GetStreamingResponseAsync(new[] { localMsg }).WriteToConsoleAsync();
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[注意] 本地图片调用失败: {ex.Message}");
             }
        }
        else
        {
            Console.WriteLine($"[警告] 未找到本地图片: {localImagePath}");
        }
    }
}
