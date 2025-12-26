using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Admin.NET.Ai.Services.MCP;

public static class McpEndpoints
{
    /// <summary>
    /// 配置 MCP 服务端端点
    /// </summary>
    public static void MapMcpEndpoints(this WebApplication app)
    {
        var discoveryService = app.Services.GetRequiredService<McpToolDiscoveryService>();

        // 1. SSE 连接端点 - 发送工具列表
        app.MapGet("/mcp/sse", async (HttpContext context) =>
        {
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            // 发送初始连接事件
            var connectEvent = new { type = "connection", message = "Connected to Admin.NET MCP Server" };
            await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(connectEvent)}\n\n");
            await context.Response.Body.FlushAsync();

            // 发送工具列表
            var tools = discoveryService.GetToolsForMcp();
            var toolsEvent = new { type = "tools_list", tools = tools };
            await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(toolsEvent)}\n\n");
            await context.Response.Body.FlushAsync();

            // 保持活跃循环
            while (!context.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(10000);
                await context.Response.WriteAsync($": keep-alive\n\n");
                await context.Response.Body.FlushAsync();
            }
        });

        // 2. 工具列表端点 (REST)
        app.MapGet("/mcp/tools", () =>
        {
            var tools = discoveryService.GetToolsForMcp();
            return Results.Json(new { tools = tools });
        });

        // 3. 工具调用端点 (POST)
        app.MapPost("/mcp/call", async (HttpContext context) =>
        {
            var request = await JsonSerializer.DeserializeAsync<McpCallRequest>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null || string.IsNullOrEmpty(request.Tool))
            {
                context.Response.StatusCode = 400;
                return Results.Json(new { error = "Invalid request: missing 'tool' field" });
            }

            if (!discoveryService.HasTool(request.Tool))
            {
                context.Response.StatusCode = 404;
                return Results.Json(new { error = $"Tool '{request.Tool}' not found" });
            }

            try
            {
                var result = await discoveryService.ExecuteToolAsync(request.Tool, request.Arguments ?? new());
                return Results.Json(new { success = true, result = result });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                return Results.Json(new { success = false, error = ex.Message });
            }
        });

        // 4. 消息端点 (兼容标准 MCP 协议)
        app.MapPost("/mcp/messages", async (HttpContext context) =>
        {
            var request = await JsonSerializer.DeserializeAsync<McpMessageRequest>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                context.Response.StatusCode = 400;
                return Results.Json(new { error = "Invalid request" });
            }

            if (request.Type == "call_tool" || request.Type == "tools/call")
            {
                try
                {
                    var args = request.Arguments ?? new Dictionary<string, object?>();
                    var result = await discoveryService.ExecuteToolAsync(request.ToolName, args);
                    return Results.Json(new { status = "success", result = result });
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    return Results.Json(new { status = "error", message = ex.Message });
                }
            }
            else if (request.Type == "list_tools" || request.Type == "tools/list")
            {
                var tools = discoveryService.GetToolsForMcp();
                return Results.Json(new { tools = tools });
            }
            else
            {
                return Results.Json(new { status = "ignored", message = $"Unknown message type: {request.Type}" });
            }
        });
    }

    public class McpCallRequest
    {
        public string Tool { get; set; } = "";
        public Dictionary<string, object?> Arguments { get; set; } = new();
    }

    public class McpMessageRequest
    {
        public string Type { get; set; } = "";
        public string ToolName { get; set; } = "";
        public Dictionary<string, object?> Arguments { get; set; } = new();
    }
}
