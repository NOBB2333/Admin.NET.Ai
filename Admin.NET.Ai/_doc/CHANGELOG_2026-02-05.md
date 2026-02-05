# 变更日志 - 2026-02-05

## 接口重构摘要

本次更新主要涉及 RAG、成本管理和 CancellationToken 命名规范化。

---

## 1. CancellationToken 命名规范化

所有接口方法的 `ct` 参数已统一更名为 `cancellationToken`，提高可读性：

```diff
- Task<T> MethodAsync(string param, CancellationToken ct = default);
+ Task<T> MethodAsync(string param, CancellationToken cancellationToken = default);
```

**受影响接口：**
- `IAiFactory`
- `IRagService`
- `IGraphRagService`
- `IConversationService`
- `ITokenUsageStore`
- `IBudgetManager`
- `IBudgetStore`

---

## 2. 成本接口合并

`ICostStore` 和 `ICostCalculator` 已合并到 `ITokenUsageStore`：

| 删除的接口 | 新位置 |
|-----------|--------|
| `ICostStore.SaveCostAsync()` | `ITokenUsageStore.SaveCostAsync()` |
| `ICostCalculator.CalculateCost()` | `ITokenUsageStore.CalculateCost()` |

**新 ITokenUsageStore 接口：**
```csharp
public interface ITokenUsageStore
{
    // 使用记录
    Task RecordStartAsync(TokenUsageRecord record, CancellationToken cancellationToken = default);
    Task RecordCompletionAsync(TokenUsageRecord record, CancellationToken cancellationToken = default);
    Task<List<TokenUsageRecord>> GetUserUsageAsync(string userId, DateTime? start, DateTime? end, CancellationToken cancellationToken = default);
    
    // 成本计算与存储 (从 ICostCalculator/ICostStore 合并)
    decimal CalculateCost(TokenUsage usage, string modelName);
    Task SaveCostAsync(string requestId, int inputTokens, int outputTokens, string model, IDictionary<string, object?>? additionalData = null, CancellationToken cancellationToken = default);
}
```

---

## 3. RAG 接口更新

### IRagService

```csharp
public interface IRagService
{
    Task<RagSearchResult> SearchAsync(
        string query, 
        RagSearchOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    Task IndexAsync(
        IEnumerable<RagDocument> documents, 
        string? collection = null, 
        CancellationToken cancellationToken = default);
}
```

### IGraphRagService (继承 IRagService)

```csharp
public interface IGraphRagService : IRagService
{
    Task<RagSearchResult> GraphSearchAsync(
        string query, 
        GraphRagSearchOptions? options = null, 
        CancellationToken cancellationToken = default);

    Task BuildGraphAsync(
        IEnumerable<RagDocument> documents, 
        CancellationToken cancellationToken = default);
}
```

### 新数据类型

```csharp
public record RagSearchResult(
    IReadOnlyList<RagDocument> Documents,
    TimeSpan ElapsedTime
);

public record RagDocument(
    string Content,
    double Score = 0,
    string? Source = null,
    IDictionary<string, object>? Metadata = null
);
```

---

## 4. GraphRagSearchOptions 移动

`GraphRagSearchOptions` 已从 `IGraphRagService.cs` 移动到 `Options/RagOptions.cs`：

```csharp
// Options/RagOptions.cs
public class GraphRagSearchOptions : RagSearchOptions
{
    public int MaxHops { get; set; } = 2;
    public bool IncludeRelations { get; set; } = true;
    public bool HybridFusion { get; set; } = true;
}
```

---

## 5. 删除的文件

- `Abstractions/ICostStore.cs` → 合并到 ITokenUsageStore
- `Abstractions/ICostCalculator.cs` → 合并到 ITokenUsageStore
- `Services/Cost/ModelCostCalculator.cs` → 功能合并到 InMemoryTokenUsageStore

---

## 迁移指南

```csharp
// Before
var calculator = sp.GetRequiredService<ICostCalculator>();
var cost = calculator.CalculateCost(usage, "modelName");

// After
var tokenStore = sp.GetRequiredService<ITokenUsageStore>();
var cost = tokenStore.CalculateCost(usage, "modelName");
```
