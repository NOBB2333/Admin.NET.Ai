using Admin.NET.Ai.Models.Workflow;
using Microsoft.Extensions.Logging;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 人工介入步骤处理器
/// </summary>
public class HumanInputStepHandler
{
    private readonly ILogger<HumanInputStepHandler> _logger;
    private readonly WorkflowStateService _stateService;

    public HumanInputStepHandler(
        ILogger<HumanInputStepHandler> logger,
        WorkflowStateService stateService)
    {
        _logger = logger;
        _stateService = stateService;
    }

    /// <summary>
    /// 请求人工输入
    /// </summary>
    public async Task<AiWorkflowHumanInputEvent> RequestInputAsync(string workflowId, string stepName, string prompt)
    {
        _logger.LogInformation("✋ [HumanInput] 工作流 {WorkflowId} 在步骤 {Step} 请求输入: {Prompt}", 
            workflowId, stepName, prompt);

        // 1. 获取当前状态
        var context = await _stateService.LoadStateAsync(workflowId);
        if (context == null)
        {
            // 如果不存在，创建新上下文 (对于新启动的工作流)
            context = new WorkflowContext { WorkflowId = workflowId };
        }

        // 2. 更新状态为挂起 (Suspended)
        context.Status = "Suspended";
        context.CurrentStep = stepName;
        context.Variables["HumanPrompt"] = prompt;
        context.LastUpdated = DateTime.UtcNow;

        await _stateService.SaveStateAsync(workflowId, context);

        // 3. 返回事件通知 UI
        return new AiWorkflowHumanInputEvent
        {
            WorkflowId = workflowId,
            StepName = stepName,
            Prompt = prompt
        };
    }

    /// <summary>
    /// 提交人工输入并恢复执行
    /// </summary>
    public async Task ResumeAsync(string workflowId, string input)
    {
         _logger.LogInformation("⏩ [HumanInput] 工作流 {WorkflowId} 收到输入，准备恢复", workflowId);

         var context = await _stateService.LoadStateAsync(workflowId);
         if (context == null || context.Status != "Suspended")
         {
             throw new InvalidOperationException($"工作流 {workflowId} 未处于挂起等待输入状态");
         }

         // 更新状态
         context.Status = "Running";
         context.Variables["HumanResult"] = input;
         context.LastUpdated = DateTime.UtcNow;

         await _stateService.SaveStateAsync(workflowId, context);
         
         // 注意：实际恢复执行逻辑需要在 WorkflowService 中触发
         // 这里只是更新状态
    }
}
