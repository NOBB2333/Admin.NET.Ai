using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Rag;

/// <summary>
/// 简易本地文本文件加载器
/// 专门用于读取项目目录下的 txt / md 等纯文本文件
/// TODO： 后续要做一个接口出来 支持其他格式和使用第三方的限量化工具。主要重点是第三方的工具，本地的尽量不做
/// </summary>
public class LocalTextDocumentLoader
{
    private readonly ILogger<LocalTextDocumentLoader> _logger;

    public LocalTextDocumentLoader(ILogger<LocalTextDocumentLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 加载指定目录下的所有纯文本文件 (.txt, .md)
    /// </summary>
    /// <param name="directoryPath">本地文件夹绝对路径或相对路径</param>
    /// <returns>解析出的原始文档列表</returns>
    public async Task<List<RawDocument>> LoadDirectoryAsync(string directoryPath)
    {
        var rawDocs = new List<RawDocument>();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("⚠️ 目录不存在: {DirectoryPath}", directoryPath);
            return rawDocs;
        }

        // 获取目录下的 txt 和 md 文件
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                             .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || 
                                         f.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                             .ToList();

        _logger.LogInformation("📂 开始从 [{DirectoryPath}] 加载 {Count} 个文本文件...", directoryPath, files.Count);

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                
                // 去除可能存在的 BOM 头和不可见空字符
                content = content.Trim('\uFEFF', '\u200B').Trim();

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file);
                // 使用文件名（不含扩展名）作为文档的 SourceName
                var sourceName = Path.GetFileNameWithoutExtension(file);

                rawDocs.Add(new RawDocument
                {
                    Content = content,
                    SourceName = sourceName,
                    SourceUri = file,
                    Metadata = new Dictionary<string, object>
                    {
                        { "FileName", fileName },
                        { "Extension", Path.GetExtension(file) },
                        { "LoadTime", DateTime.UtcNow.ToString("O") }
                    }
                });

                _logger.LogDebug("✔️ 成功读取文件: {FileName} ({Length} 字符)", fileName, content.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 读取文件失败: {FilePath}", file);
            }
        }

        return rawDocs;
    }
}
