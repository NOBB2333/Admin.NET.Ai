using Admin.NET.Ai.Core;
using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Models;
using Microsoft.AspNetCore.Http;

namespace Admin.NET.Ai.Application;

/// <summary>
/// AI 应用服务 (Facade)
/// 提供更易用的 API，封装底层 Pipeline 调用
/// </summary>
public class AiAppService(IAiService aiService)
{
    /// <summary>
    /// 简单对话
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="systemPrompt">系统提示词 (角色设定)</param>
    /// <param name="clientName">指定使用的客户端名称 (可选)</param>
    /// <returns></returns>
    public async Task<string> ChatAsync(string prompt, string? systemPrompt = null, string? clientName = null)
    {
        var options = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(clientName))
        {
            options["ClientName"] = clientName;
        }
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            options["SystemPrompt"] = systemPrompt;
        }

        var result = await aiService.ExecuteAsync<string>(prompt, options);
        return result ?? string.Empty;
    }

    /// <summary>
    /// 多模态对话 (带文件)
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="files">文件列表 (IFormFile)</param>
    /// <param name="clientName">客户端名称</param>
    /// <returns></returns>
    public async Task<string> ChatWithFilesAsync(string prompt, List<IFormFile> files, string? clientName = null)
    {
        var options = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(clientName))
        {
            options["ClientName"] = clientName;
        }

        // 将 IFormFile 转换为 AiAttachment
        var aiFiles = new List<AiAttachment>();
        foreach (var file in files)
        {
            using var stream = file.OpenReadStream();
            // 注意：这里需要复制流，因为 IFormFile 流在请求结束后会释放
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            aiFiles.Add(new AiAttachment(memoryStream, file.FileName, file.ContentType));
        }
        
        options["Attachments"] = aiFiles;

        var result = await aiService.ExecuteAsync<string>(prompt, options);
        return result ?? string.Empty;
    }

    /// <summary>
    /// 生成图片
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>图片 URL 列表</returns>
    public async Task<List<string>> GenerateImageAsync(string prompt)
    {
        var options = new Dictionary<string, object?>
        {
            { "Mode", "ImageGeneration" }
        };

        var result = await aiService.ExecuteAsync<List<string>>(prompt, options);
        return result ?? [];
    }
}
