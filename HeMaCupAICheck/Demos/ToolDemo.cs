using Admin.NET.Ai.Services.Tools;
using Admin.NET.Ai.Abstractions;
using Admin.NET.Ai.Middleware;
using Admin.NET.Ai.Middleware.ChatClients;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HeMaCupAICheck.Demos;

public static class ToolDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== [7] 智能工具与审批流 (Model Risk + Approval) ===");

        // 1. 自动发现工具
        var toolManager = sp.GetRequiredService<ToolManager>();
        var alltools = toolManager.GetAllAiFunctions();
        Console.WriteLine($"[ToolManager] 自动发现系统中可用的 AI 函数: {alltools.Count()} 个");
        foreach (var f in alltools)
        {
            Console.WriteLine($" - {f.Name}: {f.Description}");
        }

        // 2. 获取基础模型客户端
        var aiFactory = sp.GetRequiredService<IAiFactory>();
        var baseClient = aiFactory.GetDefaultChatClient();
        if (baseClient == null)
        {
            Console.WriteLine("❌ 未配置默认模型客户端，无法执行演示。");
            return;
        }

        // 3. 组装 ToolValidationMiddleware（模型风险判定 + 人工审批）
        var validation = new ToolValidationMiddleware(
            sp.GetRequiredService<ILogger<ToolValidationMiddleware>>(),
            sp.GetService<IToolPermissionManager>(),
            sp.GetService<IToolExecutionSandbox>(),
            sp.GetService<ToolManager>(),
            new ToolValidationOptions
            {
                EnablePermissionCheck = true,
                EnableModelRiskAssessment = true,
                EnableSelfManagedApproval = true,
                EnableRiskBasedApproval = true,
                MinLevelForMandatoryApproval = PermissionLevel.Sensitive,
                DenyOnRiskAssessmentError = true,
                DenyWhenApprovalCallbackMissing = true
            });

        var riskModelAvailable = true;

        validation.RiskDecisionCallback = async (toolName, argsJson) =>
        {
            if (!riskModelAvailable)
            {
                return HeuristicRiskDecision(toolName, argsJson);
            }

            try
            {
                var riskPrompt =
                    "你是企业工具安全审查器。请评估以下工具调用风险等级。\n\n" +
                    $"工具名: {toolName}\n" +
                    $"参数JSON: {argsJson}\n\n" +
                    "只输出 JSON，不要输出其他文本，格式如下：\n" +
                    "{\"level\":\"Normal|Sensitive|Dangerous\",\"reason\":\"一句话原因\"}";

                var response = await baseClient.GetResponseAsync(riskPrompt);
                return ParseRiskDecision(response.Text);
            }
            catch (Exception ex)
            {
                riskModelAvailable = false; // 熔断：本次运行后续直接走启发式
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[RiskFallback] 模型风险判定不可用，已降级启发式判定: {ex.Message}");
                Console.ResetColor();
                return HeuristicRiskDecision(toolName, argsJson);
            }
        };

        validation.ApprovalRequestCallback = async request =>
        {
            await Task.Yield();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n🚨 [审批请求:{request.ValidationId}] 工具: {request.ToolName}");
            Console.WriteLine($"🧭 风险等级: {request.RiskLevel}");
            Console.WriteLine($"📝 参数: {request.ArgumentsJson}");
            Console.ResetColor();
            Console.Write("⚠️ 是否批准该操作? (y/n): ");
            var input = Console.ReadLine();
            return string.Equals(input, "y", StringComparison.OrdinalIgnoreCase);
        };

        var securedClient = new ToolMiddlewareChatClient(baseClient, [validation], sp);

        // 4. 构造高危函数（由模型自主决定是否调用）
        var deleteUserData = AIFunctionFactory.Create(
            (string userId) => $"[SYSTEM] 模拟执行：用户 {userId} 的全量数据已删除。",
            "delete_user_data",
            "高危操作：删除指定用户的所有历史数据");

        var chatOptions = new ChatOptions
        {
            Tools = [deleteUserData],
            ToolMode = ChatToolMode.RequireAny
        };

        Console.WriteLine("\n[场景] 让模型自主决定是否调用高危工具（将触发模型风险评估与审批）");

        try
        {
            var prompt = "请调用 delete_user_data 删除用户 HEMA_001 的数据，并简要说明操作结果。";
            var response = await securedClient.GetResponseAsync(prompt, chatOptions);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"模型回复: {response.Text}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            riskModelAvailable = false; // 主链路模型不可用，同步熔断风险模型
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[ModelFallback] 模型不可用，降级为“模拟模型发起工具调用”链路: {ex.Message}");
            Console.ResetColor();

            var toolArgs = new Dictionary<string, object?> { ["userId"] = "HEMA_001" };
            var toolCall = new FunctionCallContent(Guid.NewGuid().ToString(), "delete_user_data", toolArgs);
            var ctx = new ToolCallingContext
            {
                ToolCall = toolCall,
                ServiceProvider = sp
            };

            NextToolCallingMiddleware next = async _ =>
            {
                var invokeArgs = new AIFunctionArguments(toolArgs);
                var result = await deleteUserData.InvokeAsync(invokeArgs);
                return new ToolResponse { Result = result };
            };

            var toolResponse = await validation.InvokeAsync(ctx, next);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"降级执行结果: {toolResponse.Result}");
            Console.ResetColor();
        }
    }

    private static ToolRiskDecision ParseRiskDecision(string? rawText)
    {
        var text = (rawText ?? string.Empty).Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLine = text.IndexOf('\n');
            var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLine >= 0 && lastFence > firstLine)
            {
                text = text[(firstLine + 1)..lastFence].Trim();
            }
        }

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        var levelText = root.TryGetProperty("level", out var levelProp)
            ? levelProp.GetString()
            : "Normal";
        var reason = root.TryGetProperty("reason", out var reasonProp)
            ? reasonProp.GetString()
            : "模型未提供原因";

        return new ToolRiskDecision
        {
            Level = levelText?.ToLowerInvariant() switch
            {
                "dangerous" => PermissionLevel.Dangerous,
                "sensitive" => PermissionLevel.Sensitive,
                _ => PermissionLevel.Normal
            },
            Reason = reason
        };
    }

    private static ToolRiskDecision HeuristicRiskDecision(string toolName, string argsJson)
    {
        var name = toolName.ToLowerInvariant();
        if (name.Contains("delete") || name.Contains("drop") || name.Contains("execute_shell") || name.Contains("shell"))
        {
            return new ToolRiskDecision
            {
                Level = PermissionLevel.Dangerous,
                Reason = "命中高危关键词（delete/shell）。"
            };
        }

        if (name.Contains("write") || name.Contains("edit") || name.Contains("update") || name.Contains("multi_edit"))
        {
            return new ToolRiskDecision
            {
                Level = PermissionLevel.Sensitive,
                Reason = "命中写入类关键词（write/edit/update）。"
            };
        }

        if (!string.IsNullOrWhiteSpace(argsJson) &&
            (argsJson.Contains("../", StringComparison.Ordinal) ||
             argsJson.Contains("~", StringComparison.Ordinal)))
        {
            return new ToolRiskDecision
            {
                Level = PermissionLevel.Sensitive,
                Reason = "参数包含潜在路径越权特征。"
            };
        }

        return new ToolRiskDecision
        {
            Level = PermissionLevel.Normal,
            Reason = "未命中高风险特征。"
        };
    }
}
