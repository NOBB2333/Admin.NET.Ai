using System.ComponentModel;
using System.Text;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Tools.Library;

/// <summary>
/// 文件系统工具集 — 借鉴 OpenCowork fs-tool
/// 支持读取、写入、编辑、多点编辑、目录列表
/// 写操作自带路径审批逻辑
/// </summary>
public class FileSystemTools : IAiCallableFunction
{
    public string Name => "FileSystemTools";
    public string Description => "文件系统操作工具集：读取、写入、编辑文件和列出目录";
    public ToolExecutionContext? Context { get; set; }

    /// <summary>
    /// 写操作在工作目录外时需要审批
    /// </summary>
    public bool RequiresApproval(IDictionary<string, object?>? arguments = null)
    {
        if (arguments == null || Context?.WorkingDirectory == null) return false;

        // 检查路径参数是否在工作目录内
        if (arguments.TryGetValue("filePath", out var pathObj) && pathObj is string filePath)
        {
            var fullPath = Path.GetFullPath(filePath);
            var workDir = Path.GetFullPath(Context.WorkingDirectory);
            return !fullPath.StartsWith(workDir, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(ReadFile, "read_file", "读取文件内容（支持行范围）");
        yield return AIFunctionFactory.Create(WriteFile, "write_file", "写入内容到文件（创建或覆盖）");
        yield return AIFunctionFactory.Create(EditFile, "edit_file", "精确替换文件中的指定文本");
        yield return AIFunctionFactory.Create(MultiEdit, "multi_edit", "在同一文件中执行多个非连续替换");
        yield return AIFunctionFactory.Create(ListDirectory, "list_directory", "列出目录中的文件和子目录");
    }

    /// <summary>
    /// 读取文件内容
    /// </summary>
    [Description("读取指定文件的内容，支持按行范围读取")]
    private async Task<string> ReadFile(
        [Description("文件路径")] string filePath,
        [Description("起始行号（从1开始，可选）")] int? startLine = null,
        [Description("结束行号（包含，可选）")] int? endLine = null)
    {
        filePath = ResolvePath(filePath);
        if (!File.Exists(filePath))
            return $"[错误] 文件不存在: {filePath}";

        var lines = await File.ReadAllLinesAsync(filePath);
        var totalLines = lines.Length;

        if (startLine.HasValue || endLine.HasValue)
        {
            var start = Math.Max(0, (startLine ?? 1) - 1);
            var end = Math.Min(totalLines, endLine ?? totalLines);
            lines = lines.Skip(start).Take(end - start).ToArray();
            return $"[文件: {filePath}] (行 {start + 1}-{start + lines.Length} / 共 {totalLines} 行)\n{string.Join('\n', lines)}";
        }

        // 大文件截断提示
        if (totalLines > 500)
        {
            var preview = string.Join('\n', lines.Take(200));
            return $"[文件: {filePath}] (共 {totalLines} 行，仅显示前 200 行)\n{preview}\n\n... [截断，请使用 startLine/endLine 参数查看更多]";
        }

        return $"[文件: {filePath}] (共 {totalLines} 行)\n{string.Join('\n', lines)}";
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    [Description("将内容写入指定文件（创建新文件或覆盖已有文件）")]
    private async Task<string> WriteFile(
        [Description("文件路径")] string filePath,
        [Description("要写入的内容")] string content)
    {
        filePath = ResolvePath(filePath);

        // 确保目录存在
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(filePath, content);
        var lineCount = content.Split('\n').Length;
        return $"[成功] 已写入 {filePath} ({lineCount} 行, {content.Length} 字符)";
    }

    /// <summary>
    /// 精确编辑文件
    /// </summary>
    [Description("在文件中精确替换指定的文本内容")]
    private async Task<string> EditFile(
        [Description("文件路径")] string filePath,
        [Description("要被替换的原始文本（必须精确匹配）")] string oldText,
        [Description("替换后的新文本")] string newText)
    {
        filePath = ResolvePath(filePath);
        if (!File.Exists(filePath))
            return $"[错误] 文件不存在: {filePath}";

        var content = await File.ReadAllTextAsync(filePath);

        var count = CountOccurrences(content, oldText);
        if (count == 0)
            return $"[错误] 未找到要替换的文本。请确认 oldText 是否精确匹配文件内容。";
        if (count > 1)
            return $"[错误] 找到 {count} 处匹配。请提供更精确的上下文以避免歧义，或使用 multi_edit。";

        var newContent = content.Replace(oldText, newText);
        await File.WriteAllTextAsync(filePath, newContent);

        return $"[成功] 已替换 {filePath} 中的 {oldText.Split('\n').Length} 行 → {newText.Split('\n').Length} 行";
    }

    /// <summary>
    /// 多点编辑
    /// </summary>
    [Description("在同一文件中执行多个非连续的文本替换")]
    private async Task<string> MultiEdit(
        [Description("文件路径")] string filePath,
        [Description("替换列表，每项包含 oldText 和 newText")] IEnumerable<EditOperation> edits)
    {
        filePath = ResolvePath(filePath);
        if (!File.Exists(filePath))
            return $"[错误] 文件不存在: {filePath}";

        var content = await File.ReadAllTextAsync(filePath);
        var editList = edits.ToList();
        var results = new List<string>();

        // 按出现位置从后向前替换，避免偏移
        var sortedEdits = editList
            .Select((e, i) => (Edit: e, Index: i, Position: content.IndexOf(e.OldText, StringComparison.Ordinal)))
            .OrderByDescending(x => x.Position)
            .ToList();

        foreach (var item in sortedEdits)
        {
            if (item.Position < 0)
            {
                results.Add($"  [{item.Index + 1}] ❌ 未找到: \"{Truncate(item.Edit.OldText, 50)}\"");
                continue;
            }

            content = content.Remove(item.Position, item.Edit.OldText.Length)
                             .Insert(item.Position, item.Edit.NewText);
            results.Add($"  [{item.Index + 1}] ✅ 已替换");
        }

        await File.WriteAllTextAsync(filePath, content);
        return $"[MultiEdit] {filePath}\n{string.Join('\n', results)}";
    }

    /// <summary>
    /// 列出目录
    /// </summary>
    [Description("列出指定目录中的文件和子目录")]
    private Task<string> ListDirectory(
        [Description("目录路径")] string dirPath,
        [Description("最大深度（默认1层）")] int maxDepth = 1)
    {
        dirPath = ResolvePath(dirPath);
        if (!Directory.Exists(dirPath))
            return Task.FromResult($"[错误] 目录不存在: {dirPath}");

        var sb = new StringBuilder();
        sb.AppendLine($"[目录: {dirPath}]");
        ListDirectoryRecursive(sb, dirPath, "", 0, maxDepth);
        return Task.FromResult(sb.ToString());
    }

    #region Helpers

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
        var workDir = Context?.WorkingDirectory ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(workDir, path));
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "...";

    private static void ListDirectoryRecursive(StringBuilder sb, string path, string indent, int depth, int maxDepth)
    {
        if (depth >= maxDepth) return;

        try
        {
            foreach (var dir in Directory.GetDirectories(path).OrderBy(d => d))
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith('.')) continue; // 跳过隐藏目录
                var childCount = Directory.GetFileSystemEntries(dir).Length;
                sb.AppendLine($"{indent}📁 {name}/ ({childCount} items)");
                ListDirectoryRecursive(sb, dir, indent + "  ", depth + 1, maxDepth);
            }

            foreach (var file in Directory.GetFiles(path).OrderBy(f => f))
            {
                var name = Path.GetFileName(file);
                if (name.StartsWith('.')) continue;
                var size = new FileInfo(file).Length;
                sb.AppendLine($"{indent}📄 {name} ({FormatSize(size)})");
            }
        }
        catch (UnauthorizedAccessException)
        {
            sb.AppendLine($"{indent}⚠️ [权限不足]");
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };

    #endregion
}

/// <summary>
/// 编辑操作（用于 MultiEdit）
/// </summary>
public class EditOperation
{
    [Description("要被替换的原始文本")]
    public string OldText { get; set; } = "";

    [Description("替换后的新文本")]
    public string NewText { get; set; } = "";
}
