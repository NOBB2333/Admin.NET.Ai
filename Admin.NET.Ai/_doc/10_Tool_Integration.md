# 10. 工具调用与集成 (Tool Integration)

## 🎯 设计思维 (Mental Model)
AI 不应只是一个“聊天机器人”，而必须是能够操作系统的“操作员”。
工具集成（Tool/Function Calling）是连接模型智力与现实能力的桥梁。

`Admin.NET.Ai` 的工具系统设计原则是：**“简单声明，严格审批”**。
我们不仅支持自动化的工具发现，还引入了**人工介入（Human-in-the-loop）**机制，确保高风险操作的安全。

---

## 🏗️ 架构设计
### 核心组件
- **`IAiTool` / `AIFunction`**: 工具的统一包装。
- **`ToolManager`**: 工具中心。负责从 DI 容器、反射或 MCP 中收集工具。
- **`ApprovalRequiredAIFunction`**: 装饰器模式，为敏感工具增加“人工点击确认”逻辑。

---

## 🛠️ 技术实现 (Implementation)

### 1. 自动工具生成
你只需要在普通的 C# 方法上加一个 `[Description]` 特性，系统就能自动将其转换为 AI 函数。

```csharp
public class WeatherTool : IAiTool
{
    [Description("查询指定城市的实时天气")]
    public string GetWeather(string city) => $"The weather in {city} is sunny.";
}
```

### 2. 人工审批机制 (`ApprovalRequiredAIFunction.cs`)
对于“删除数据库”、“转账”等高危函数，系统包装了一层 `WaitForResult` 逻辑。
1.  模型发起调用请求。
2.  中间件截获请求，产生一个 **Pending** 任务。
3.  用户在 UI 界面点击“同意”。
4.  函数才真正执行。

### 3. 多样化工具来源
- **Native Tools**: 本地程序集实现的 C# 函数。
- **MCP Tools**: 远程 MCP Server 提供的能力（详见 05. MCP 集成）。
- **Script Tools**: 动态编译的 C# 代码片段（详见 06. 热重载脚本）。

---

## 🚀 代码示例 (Usage Example)

### 注册与发现
```csharp
// 在注入时，系统会自动扫描所有实现了 IAiTool 的类
services.AddAdminNetAi();
```

### 动态调用示例
```csharp
var toolManager = serviceProvider.GetRequiredService<ToolManager>();

// 获取所有可用工具 (包含本地和 MCP)
var allTools = await toolManager.GetAllFunctionsAsync();

// 喂给模型
var response = await chatClient.GetResponseAsync("帮我根据查询的天气写一首诗", new ChatOptions {
    Tools = allTools
});
```

---

## 📊 监控与日志 (`ToolMonitoringMiddleware`)
每一个工具的调用：
- **参数是什么？**
- **返回值是什么？**
- **执行了多久？**
都会被 `ToolMonitoringMiddleware` 记录并展示在 DevUI 的 TimeLine 中，极大方便了调试过程中对“Agent 幻觉”的排查。

---

## 🛡️ 安全限制
- **白名单机制**: 仅限标注了特定接口的类可作为工具。
- **输入校验**: 利用 JSON Schema 校验模型传回的参数类型，防止逻辑执行异常。
- **Timeout 控制**: 每个工具执行都有默认超时时间，防止同步死锁。
