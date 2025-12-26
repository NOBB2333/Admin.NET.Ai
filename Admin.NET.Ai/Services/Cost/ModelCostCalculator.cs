using Admin.NET.Ai.Abstractions;

namespace Admin.NET.Ai.Services.Cost;

public record ModelPricing
{
    public decimal InputPer1K { get; init; }  // 每1000个输入Token价格
    public decimal OutputPer1K { get; init; } // 每1000个输出Token价格
}

public class ModelCostCalculator : ICostCalculator
{
    private readonly Dictionary<string, ModelPricing> _pricing = new()
    {
        // 示例定价
        ["gpt-4o"] = new ModelPricing { InputPer1K = 0.03m, OutputPer1K = 0.06m }, // RMB
        ["gpt-4o-mini"] = new ModelPricing { InputPer1K = 0.005m, OutputPer1K = 0.015m },
        ["gpt-3.5-turbo"] = new ModelPricing { InputPer1K = 0.005m, OutputPer1K = 0.015m },
        ["unknown-model"] = new ModelPricing { InputPer1K = 0, OutputPer1K = 0 }
    };

    public decimal CalculateCost(TokenUsage usage, string modelName)
    {
        if (!_pricing.TryGetValue(modelName, out var pricing))
        {
            // fallback generic
            pricing = new ModelPricing { InputPer1K = 0.01m, OutputPer1K = 0.01m };
        }

        var inputCost = (usage.PromptTokens / 1000m) * pricing.InputPer1K;
        var outputCost = (usage.CompletionTokens / 1000m) * pricing.OutputPer1K;

        return Math.Round(inputCost + outputCost, 6);
    }
}
