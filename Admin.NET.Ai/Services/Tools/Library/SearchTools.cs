using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Tools.Library;

/// <summary>
/// 搜索工具集 — 借鉴 OpenCowork search-tool
/// 支持文件名搜索和内容搜索
/// </summary>
public class SearchTools : IAiCallableFunction
{
    public string Name => "SearchTools";
    public string Description => "搜索工具集：按文件名或内容搜索文件";
    public ToolExecutionContext? Context { get; set; }

    // 搜索是只读操作，不需要审批
    public bool RequiresApproval(IDictionary<string, object?>? arguments = null) => false;

    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(GlobSearch, "glob_search", "按文件名模式搜索文件");
        yield return AIFunctionFactory.Create(GrepSearch, "grep_search", "在文件内容中搜索文本或正则表达式");
    }

    /// <summary>
    /// 文件名模式搜索
    /// </summary>
    [Description("按文件名模式搜索文件（支持通配符 * 和 ?）")]
    private Task<string> GlobSearch(
        [Description("搜索根目录")] string directory,
        [Description("文件名模式（如 *.cs, *Controller*）")] string pattern,
        [Description("最大搜索深度（默认5）")] int maxDepth = 5,
        [Description("最大返回数量（默认50）")] int maxResults = 50)
    {
        directory = ResolvePath(directory);
        if (!Directory.Exists(directory))
            return Task.FromResult($"[错误] 目录不存在: {directory}");

        var results = new List<string>();
        SearchDirectoryGlob(directory, pattern, 0, maxDepth, results, maxResults);

        if (results.Count == 0)
            return Task.FromResult($"[GlobSearch] 在 {directory} 中未找到匹配 '{pattern}' 的文件");

        var sb = new StringBuilder();
        sb.AppendLine($"[GlobSearch] 在 {directory} 中找到 {results.Count} 个匹配:");
        foreach (var r in results)
        {
            // 显示相对路径
            var relativePath = Path.GetRelativePath(directory, r);
            var size = new FileInfo(r).Length;
            sb.AppendLine($"  {relativePath} ({FormatSize(size)})");
        }

        if (results.Count >= maxResults)
            sb.AppendLine($"  ... (结果已截断，仅显示前 {maxResults} 个)");

        return Task.FromResult(sb.ToString());
    }

    /// <summary>
    /// 文件内容搜索
    /// </summary>
    [Description("在文件内容中搜索文本或正则表达式")]
    private async Task<string> GrepSearch(
        [Description("搜索根目录")] string directory,
        [Description("搜索文本或正则表达式")] string query,
        [Description("文件名过滤（如 *.cs，可选）")] string? filePattern = null,
        [Description("是否使用正则表达式")] bool isRegex = false,
        [Description("最大结果数量（默认50）")] int maxResults = 50)
    {
        directory = ResolvePath(directory);
        if (!Directory.Exists(directory))
            return $"[错误] 目录不存在: {directory}";

        Regex? regex = null;
        if (isRegex)
        {
            try { regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Compiled); }
            catch (Exception ex) { return $"[错误] 无效的正则表达式: {ex.Message}"; }
        }

        var results = new List<(string File, int Line, string Content)>();
        var searchPattern = filePattern ?? "*.*";

        var files = Directory.EnumerateFiles(directory, searchPattern, new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = 10,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        });

        foreach (var file in files)
        {
            if (results.Count >= maxResults) break;
            if (IsBinaryFile(file)) continue;

            try
            {
                var lines = await File.ReadAllLinesAsync(file);
                for (int i = 0; i < lines.Length && results.Count < maxResults; i++)
                {
                    bool match = isRegex && regex != null
                        ? regex.IsMatch(lines[i])
                        : lines[i].Contains(query, StringComparison.OrdinalIgnoreCase);

                    if (match)
                    {
                        results.Add((file, i + 1, lines[i].Trim()));
                    }
                }
            }
            catch { /* skip unreadable files */ }
        }

        if (results.Count == 0)
            return $"[GrepSearch] 未找到匹配 '{query}' 的内容";

        var sb = new StringBuilder();
        sb.AppendLine($"[GrepSearch] 找到 {results.Count} 个匹配:");
        foreach (var (filePath, line, content) in results)
        {
            var relativePath = Path.GetRelativePath(directory, filePath);
            var truncated = content.Length > 120 ? content[..120] + "..." : content;
            sb.AppendLine($"  {relativePath}:{line}  {truncated}");
        }

        return sb.ToString();
    }

    #region Helpers

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
        var workDir = Context?.WorkingDirectory ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(workDir, path));
    }

    private static void SearchDirectoryGlob(string dir, string pattern, int depth, int maxDepth, List<string> results, int maxResults)
    {
        if (depth >= maxDepth || results.Count >= maxResults) return;

        try
        {
            foreach (var file in Directory.GetFiles(dir, pattern))
            {
                if (results.Count >= maxResults) return;
                if (!Path.GetFileName(file).StartsWith('.'))
                    results.Add(file);
            }

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(subDir);
                if (name.StartsWith('.') || name is "node_modules" or "bin" or "obj" or ".git")
                    continue;
                SearchDirectoryGlob(subDir, pattern, depth + 1, maxDepth, results, maxResults);
            }
        }
        catch (UnauthorizedAccessException) { }
    }

    private static bool IsBinaryFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".dll" or ".exe" or ".bin" or ".png" or ".jpg" or ".gif"
            or ".ico" or ".pdf" or ".zip" or ".tar" or ".gz" or ".7z"
            or ".woff" or ".woff2" or ".ttf" or ".eot" or ".mp3" or ".mp4";
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };

    #endregion
}
