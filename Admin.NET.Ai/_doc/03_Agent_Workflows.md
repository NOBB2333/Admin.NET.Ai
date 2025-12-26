# 03. 多智能体工作流 (Agent Workflows)

## 🎯 设计思维 (Mental Model)
复杂的业务逻辑无法靠一个庞大的 Prompt 解决。解决复杂问题的工程化方案是：**分而治之**。
`Admin.NET.Ai` 引入了多 Agent 协作模式，将大任务拆解为由多个“专家 Agent”组成的流水线。

设计核心：
1.  **编排化**: 通过 `IWorkflowBuilder` 手动编排。
2.  **自主化**: 使用 Planner Agent 实现自主规划（Autonomous Planning）。
3.  **状态持久化**: 原生支持 Checkpoint 机制，长任务中断后可恢复。

---

## 🏗️ 架构设计
### 核心概念
- **`Agent`**: 一个具备 Instructions (指令) 的最小执行单元。
- **`Workflow`**: 一组 Agent 及其执行顺序的集合。
- **`InProcessExecution`**: 执行器，驱动工作流并在内存中管理 Agent 之间的“剧本传递”。

---

## 🛠️ 技术实现 (Implementation)

### 1. 依赖库
- `Microsoft.Agents.AI`: 微软最新的 Agent 框架，提供多 Agent 协作基础。
- `Microsoft.Agents.AI.WorkFlows`: 提供顺序、并行、移交等工作流算法。

### 2. 构建器模式 (`GenericWorkflowBuilder.cs`)
使用 Fluent API 极简定义流程：

```csharp
var workflow = workflowService.CreateSequentialBuilder("TechWriting")
    .AddAgent("Researcher", "搜索相关技术资料")
    .AddAgent("Writer", "根据资料写博客")
    .AddAgent("Reviewer", "审查错别字")
    .Build();
```

### 3. 自主生成逻辑 (Autonomous Workflow)
这是本项目的亮点。通过大模型对用户原始需求的“翻译”，动态生成工作流。

**逻辑流**:
1.  用户输入: `系统负载太高了，帮我查查日志并给出优化建议。`
2.  **Planner Agent**:
    - 分析结果: 这是一个诊断任务。
    - 生成计划: `[ { Step: "LogReader" }, { Step: "DiagnosisAgent" }, { Step: "Reporter" } ]` (JSON)
3.  **WorkflowService**: 实时解析 JSON，从 `IAiFactory` 获取对应 Agent 实例，组装并执行。

---

## 🚀 代码示例 (Usage Example)

### 手动编排 (从配置加载)
```csharp
// 只需要一行代码，系统自动读取 ExampleSequential.json 并执行
var result = await _workflowService.ExecuteWorkflowAsync("ExampleSequential", "帮我写个关于.NET的诗");
```

### 自主执行 (零配置)
```csharp
// AI 会根据这句话，自动决定需要启动几个 Agent，分别干什么
var result = await _workflowService.ExecuteAutonomousWorkflowAsync("分析一下网站昨天的访问趋势并生成周报");
```

---

## 🔄 状态持久化 (Checkpoint)
工作流集成了 `IAgentChatMessageStore`。在每一个 Agent 执行完毕后，其对话上下文会被自动序列化并存入 `TAgentChatMessageStore` 表。
- **防止宕机**: 若任务执行到一半服务器重启，系统可从最后一步恢复。
- **人工干预**: 可以在 Agent A 执行完后暂停，等待管理人员审核，审核通过后再触发 Agent B。

---

## 📊 可视化调试 (DevUI)
工作流集成了微软官方的 `DevUI`。在本地运行时，打开调试界面，你可以清晰看到：
- 事件流: `AgentStarted` -> `AgentThinking` -> `ToolCalling` -> `AgentCompleted`。
- 多 Agent 之间的“聊天对话框”形式的交互记录。
