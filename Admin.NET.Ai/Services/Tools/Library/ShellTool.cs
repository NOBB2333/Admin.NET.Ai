using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;

namespace Admin.NET.Ai.Services.Tools.Library;

/// <summary>
/// Shell 命令执行工具 — 借鉴 OpenCowork bash-tool
/// 始终需要审批（Dangerous 级别）
/// </summary>
public class ShellTool : IAiCallableFunction
{
    public string Name => "ShellTool";
    public string Description => "执行 Shell 命令（危险操作，始终需要审批）";
    public ToolExecutionContext? Context { get; set; }

    /// <summary>
    /// Shell 命令始终需要审批
    /// </summary>
    public bool RequiresApproval(IDictionary<string, object?>? arguments = null) => true;

    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(ExecuteShell, "execute_shell", "执行 Shell/终端命令");
    }

    /// <summary>
    /// 执行 Shell 命令
    /// </summary>
    [Description("在终端中执行 Shell 命令并返回输出")]
    private async Task<string> ExecuteShell(
        [Description("要执行的命令")] string command,
        [Description("执行超时（秒，默认30）")] int timeoutSeconds = 30)
    {
        var workDir = Context?.WorkingDirectory ?? Directory.GetCurrentDirectory();
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var shell = isWindows ? "cmd.exe" : "/bin/bash";
        var args = isWindows ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"";

        var psi = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = args,
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
                return "[错误] 无法启动进程";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                return $"[超时] 命令执行超过 {timeoutSeconds} 秒，已终止。";
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCode = process.ExitCode;

            // 截断过长的输出
            const int maxLen = 10000;
            if (stdout.Length > maxLen)
                stdout = stdout[..maxLen] + $"\n... [输出已截断，共 {stdout.Length} 字符]";
            if (stderr.Length > maxLen)
                stderr = stderr[..maxLen] + $"\n... [错误输出已截断，共 {stderr.Length} 字符]";

            var result = $"[Shell] exit_code={exitCode}\n";
            if (!string.IsNullOrWhiteSpace(stdout))
                result += $"[stdout]\n{stdout}\n";
            if (!string.IsNullOrWhiteSpace(stderr))
                result += $"[stderr]\n{stderr}";

            return result.TrimEnd();
        }
        catch (Exception ex)
        {
            return $"[错误] 执行失败: {ex.Message}";
        }
    }
}
