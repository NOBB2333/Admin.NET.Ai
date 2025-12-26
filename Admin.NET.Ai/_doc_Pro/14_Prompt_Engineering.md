# 提示词工程 - 技术实现详解

## 📁 相关文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `IPromptManager.cs` | `Abstractions/` | 提示词管理接口 |
| `PromptManager.cs` | `Services/Prompt/` | 提示词管理实现 |
| `PromptTemplate.cs` | `Models/` | 模板模型 |
| `PromptDemo.cs` | `Demos/` | 演示代码 |

---

## 🏗️ 架构设计

### 模板系统

```
[Prompt Template]
    ↓
[变量替换] ← Input Variables
    ↓
[条件渲染] ← Context
    ↓
[Final Prompt]
```

---

## 🔧 核心实现

### 1. 提示词模板

```csharp
public class PromptTemplate
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Template { get; set; } = "";
    public List<PromptVariable> Variables { get; set; } = new();
    public string? Category { get; set; }
    public Dictionary<string, object>? Defaults { get; set; }
}

public class PromptVariable
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Required { get; set; } = true;
    public string? DefaultValue { get; set; }
    public VariableType Type { get; set; } = VariableType.String;
}

public enum VariableType { String, Number, Boolean, List, Object }
```

### 2. 提示词管理器

```csharp
public class PromptManager : IPromptManager
{
    private readonly Dictionary<string, PromptTemplate> _templates = new();
    private readonly string _templatesPath;
    
    public PromptManager(IOptions<PromptOptions> options)
    {
        _templatesPath = options.Value.TemplatesPath;
        LoadTemplates();
    }
    
    private void LoadTemplates()
    {
        var files = Directory.GetFiles(_templatesPath, "*.json");
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var template = JsonSerializer.Deserialize<PromptTemplate>(json);
            if (template != null)
            {
                _templates[template.Name] = template;
            }
        }
    }
    
    public string Render(string templateName, Dictionary<string, object> variables)
    {
        if (!_templates.TryGetValue(templateName, out var template))
        {
            throw new ArgumentException($"Template '{templateName}' not found");
        }
        
        return RenderTemplate(template.Template, variables, template.Defaults);
    }
    
    private string RenderTemplate(
        string template, 
        Dictionary<string, object> variables,
        Dictionary<string, object>? defaults)
    {
        var result = template;
        
        // 合并默认值
        var mergedVars = new Dictionary<string, object>(defaults ?? new());
        foreach (var (key, value) in variables)
        {
            mergedVars[key] = value;
        }
        
        // 简单变量替换: {{variable}}
        foreach (var (key, value) in mergedVars)
        {
            result = result.Replace($"{{{{{key}}}}}", value?.ToString() ?? "");
        }
        
        // 条件渲染: {{#if condition}}...{{/if}}
        result = ProcessConditionals(result, mergedVars);
        
        // 循环渲染: {{#each items}}...{{/each}}
        result = ProcessLoops(result, mergedVars);
        
        return result;
    }
    
    private string ProcessConditionals(string template, Dictionary<string, object> vars)
    {
        var pattern = @"\{\{#if\s+(\w+)\}\}(.*?)\{\{/if\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var varName = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            
            if (vars.TryGetValue(varName, out var value) && IsTruthy(value))
            {
                return content;
            }
            return "";
        }, RegexOptions.Singleline);
    }
    
    private string ProcessLoops(string template, Dictionary<string, object> vars)
    {
        var pattern = @"\{\{#each\s+(\w+)\}\}(.*?)\{\{/each\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var varName = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            
            if (vars.TryGetValue(varName, out var value) && value is IEnumerable<object> items)
            {
                var sb = new StringBuilder();
                foreach (var item in items)
                {
                    var itemContent = content.Replace("{{this}}", item?.ToString() ?? "");
                    sb.AppendLine(itemContent);
                }
                return sb.ToString();
            }
            return "";
        }, RegexOptions.Singleline);
    }
}
```

---

## 📝 模板示例

### 分析报告模板

```json
{
  "name": "analysis_report",
  "description": "生成分析报告的提示词模板",
  "template": "你是一位专业的{{role}}分析师。\n\n请分析以下{{subject}}:\n{{content}}\n\n{{#if constraints}}分析约束:\n{{#each constraints}}- {{this}}\n{{/each}}{{/if}}\n\n请从以下维度进行分析:\n{{#each dimensions}}- {{this}}\n{{/each}}\n\n输出格式: {{format}}",
  "variables": [
    { "name": "role", "description": "分析师角色", "required": true },
    { "name": "subject", "description": "分析对象", "required": true },
    { "name": "content", "description": "待分析内容", "required": true },
    { "name": "dimensions", "description": "分析维度", "type": "List" },
    { "name": "constraints", "description": "约束条件", "type": "List", "required": false },
    { "name": "format", "description": "输出格式", "default": "Markdown" }
  ],
  "defaults": {
    "format": "Markdown",
    "dimensions": ["优势", "劣势", "机会", "风险"]
  }
}
```

### 代码审查模板

```json
{
  "name": "code_review",
  "description": "代码审查提示词",
  "template": "请审查以下{{language}}代码:\n\n```{{language}}\n{{code}}\n```\n\n关注以下方面:\n{{#each aspects}}- {{this}}\n{{/each}}\n\n{{#if context}}背景信息: {{context}}{{/if}}\n\n请给出改进建议。",
  "variables": [
    { "name": "language", "description": "编程语言" },
    { "name": "code", "description": "代码内容" },
    { "name": "aspects", "description": "审查方面", "type": "List" },
    { "name": "context", "description": "背景信息", "required": false }
  ],
  "defaults": {
    "aspects": ["代码质量", "性能", "安全性", "可维护性"]
  }
}
```

---

## 🎯 高级模式

### Few-Shot 示例

```csharp
public class FewShotPromptBuilder
{
    public string Build(string task, List<Example> examples, string input)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"任务: {task}");
        sb.AppendLine();
        sb.AppendLine("示例:");
        
        foreach (var example in examples)
        {
            sb.AppendLine($"输入: {example.Input}");
            sb.AppendLine($"输出: {example.Output}");
            sb.AppendLine();
        }
        
        sb.AppendLine($"输入: {input}");
        sb.AppendLine("输出:");
        
        return sb.ToString();
    }
}
```

### Chain-of-Thought

```csharp
public string BuildCoTPrompt(string question)
{
    return $@"
问题: {question}

请一步一步思考:
1. 首先，分析问题的关键点
2. 然后，列出可能的解决方案
3. 接着，评估每个方案的优缺点
4. 最后，给出最佳答案

让我们开始:";
}
```

---

## 🚀 使用示例

```csharp
var promptManager = sp.GetRequiredService<IPromptManager>();

// 使用预定义模板
var prompt = promptManager.Render("analysis_report", new Dictionary<string, object>
{
    ["role"] = "市场",
    ["subject"] = "智能手机市场",
    ["content"] = "2024年行业数据...",
    ["dimensions"] = new[] { "市场份额", "用户增长", "技术趋势" }
});

var response = await client.GetResponseAsync(prompt);

// 直接构建提示词
var codeReviewPrompt = promptManager.Render("code_review", new Dictionary<string, object>
{
    ["language"] = "csharp",
    ["code"] = "public void Foo() { ... }",
    ["aspects"] = new[] { "SOLID原则", "异常处理" }
});
```

---

## ⚙️ 配置

```json
{
  "Prompts": {
    "TemplatesPath": "Configuration/Prompts",
    "DefaultLanguage": "zh-CN",
    "MaxTemplateSize": 10000
  }
}
```
