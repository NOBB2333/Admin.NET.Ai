using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Admin.NET.Ai.Middleware;

public class ToolMonitoringMiddleware : IToolCallingMiddleware
{
    private readonly ILogger<ToolMonitoringMiddleware> _logger;

    public ToolMonitoringMiddleware(ILogger<ToolMonitoringMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<ToolResponse> InvokeAsync(
        ToolCallingContext context, 
        NextToolCallingMiddleware next)
    {
        var startTime = DateTime.UtcNow;
        var toolName = context.ToolCall.Name;
        var parameters = context.ToolCall.Arguments;
        
        try
        {
            _logger.LogInformation($"开始调用工具: {toolName}, 参数: {JsonSerializer.Serialize(parameters)}");
            
            var result = await next(context);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation($"工具调用完成: {toolName}, 耗时: {duration.TotalMilliseconds}ms");
            
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError($"工具调用失败: {toolName}, 错误: {ex.Message}");
            throw;
        }
    }
}
