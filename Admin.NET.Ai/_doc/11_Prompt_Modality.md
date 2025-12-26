# 11. 提示工程优化与多模态 (Prompt & Modality)

## 🎯 设计思维 (Mental Model)
AI 的表现 50% 取决于模型能力，另 50% 取决于 **Prompts (提示词)**。
提示工程不应是写死在代码里的字符串，而应当是**可管理、可版本化、可编排**的资源。

同时，现代 AI 已进入**多模态 (Multimodal)** 时代。`Admin.NET.Ai` 统一了文本、图像、文件的处理接口，让开发者能像处理字符串一样处理图片。

---

## 🏗️ 架构设计
### 核心组件
- **`IPromptManager`**: 提示词仓库。支持从文件、数据库或配置中加载 Prompt。
- **`YPrompt` 语法**: 项目内置的增强型 Prompt 模板语法（支持占位符和动态注入）。
- **`MultimodalItem`**: 标准化多模态数据容器。

---

## 🛠️ 技术实现 (Implementation)

### 1. 提示词管理 (`Services/PromptManager.cs`)
支持类似 `Handlebars` 的动态模板：

```csharp
// 从模板库获取提示词
string prompt = await promptManager.GetRenderedPromptAsync("TechnicalWriter", new {
    Topic = "C# 13 New Features",
    Tone = "Professional"
});
```

### 2. 多模态输入封装
系统抹平了不同 Provider 对图片的处理差异：

```csharp
var messages = new List<ChatMessage> {
    new ChatMessage(ChatRole.User, "请解释这张图里的代码") {
        Contents = { 
            new ImageContent(new Uri("https://example.com/code_screenshot.png")),
            new FileContent("logs.txt", "text/plain")
        }
    }
};
```

---

## 🚀 最佳实践 (Prompt Engineering)

我们在项目中深度践行了以下提示词优化策略：

### A. 角色指令 (Role-based Instructions)
通过 `SystemMessage` 强行固化 Agent 身份。
> "你是一个严谨的.NET高级架构师，只使用最新的语法糖回答问题..."

### B. 思维链 (Chain of Thought)
通过内置模板引导模型：
> "请先进行逻辑推理，列出步骤，最后再输出代码..."

### C. 输出格式强制 (Few-shot)
在 Prompt 中内置 2-3 个正向示例，配合 `Structured Output` (详见 08. 强类型输出) 达到 100% 格式稳定性。

---

## ⚙️ 动态配置 (`LLMAgent.Media.json`)
```json
{
  "LLMMedia": {
    "Image": {
      "MaxSize": 2048,
      "AllowedTypes": ["jpg", "png", "webp"]
    },
    "Audio": {
      "Provider": "Whisper",
      "Model": "whisper-large-v3"
    }
  }
}
```

---

## 💎 特色功能：提示词热更新
得益于 `Admin.NET.Ai` 的配置热重载能力，你可以在不重启服务的情况下，通过管理后台修改 `PromptTemplates.json`，下一次 Agent 启动时将自动加载最新的业务话术。这是企业级 Agent 能够快速迭代的关键。

---

## 📈 性能建议
- **图片压缩**: 建议在中间件中先压缩图片再上传，最高可节省 80% 的图像 Token。
- **缓存**: `IPromptManager` 内部实现了已渲染模板的二级缓存，百万次调用无鸭力。
