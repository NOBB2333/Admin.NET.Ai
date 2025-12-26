using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;

namespace Admin.NET.Ai.Tools;

/// <summary>
/// 需要审批的 AI 函数包装器
/// </summary>
public static class ApprovalRequiredAIFunction
{
    public static AIFunction Create(AIFunction innerFunction, Func<string, string, Task<bool>> approvalCallback)
    {
        // Use a more explicit delegate to avoid CS4010 and fix the IDictionary to AIFunctionArguments conversion
        return AIFunctionFactory.Create(async (IDictionary<string, object?> args, CancellationToken ct) =>
        {
            var argsJson = JsonSerializer.Serialize(args);
            bool isApproved = await approvalCallback(innerFunction.Name, argsJson);

            if (!isApproved) return (object?)$"[Tool Call Rejected] User denied function '{innerFunction.Name}'.";

            return await innerFunction.InvokeAsync(new AIFunctionArguments(args), ct);
        }, 
        innerFunction.Name, 
        innerFunction.Description);
    }
}
