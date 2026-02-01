using Admin.NET.Ai;
using Admin.NET.Ai.Options;
using HeMaCupAICheck;
using HeMaCupAICheck.Demos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Natasha;
using Natasha.CSharp;


Console.Title = "Admin.NET.Ai 模块全功能演示程序";

// 0. Natasha 初始化 (热重载脚本引擎)
NatashaManagement.RegistDomainCreator<NatashaDomainCreator>(); // lightweight init to avoid Preheating NRE

// 1. 依赖注入与服务启动
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAdminNetAi(builder.Configuration); // 核心：注册 Admin.NET.Ai 模块

// Console app 兼容性：添加 ASP.NET Core 服务的 Stubs
builder.Services.AddDistributedMemoryCache(); // IDistributedCache stub
builder.Services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, NullHttpContextAccessor>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// builder.Logging.SetMinimumLevel(LogLevel.Warning); // 减少杂音，只看关键输出
builder.Logging.SetMinimumLevel(LogLevel.Information); // 显示 Token 监控等信息日志

// 添加演示所需的日志 (已在上面配置，此处移除或保留注释)
// builder.Services.AddLogging(...)09 


var host = builder.Build();
using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;


Console.WriteLine("Admin.NET.Ai 模块已成功启动");
Console.WriteLine("按任意键开始演示...");

// 3. 交互菜单
while (true)
{
    // Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(@"
    ==================================================
        Admin.NET.Ai 模块功能全景演示 (Console)
    ==================================================
    1. 基础对话与中间件 (Chat, Audit, Tokens)
    2. 多 Agent 工作流 (MAF Sequential & Autonomous)
    3. 结构化数据提取 (JSON Schema, TOON)
    4. 智能工具与审批流 (Discover, Approval)
    5. 动态脚本热重载 (Natasha Scripting)
    6. 上下文压缩策略 (Compression Reducers)
    7. 提示词工程 (Prompt Templates)
    8. RAG 知识检索 (GraphRAG & Vector)
    9. 多模态能力 (Vision & Audio)
    10. 对话持久化 (Thread & Database)
    11. 综合场景应用 (Real-world Scenario)
    --------------------------------------------------
    12. 内置 Agent (情感/知识图谱/质量评估)
    13. 中间件详解 (Middleware Stack)
    14. MCP 协议 (外部工具集成)
    15. 监控与指标 (OpenTelemetry)
    16. 存储策略 (Hot/Cold/Vector)
    17. ★ 媒体生成 (TTS/ASR/图像/视频)
    --------------------------------------------------
    ★★ 新增实用场景 ★★
    18. RAG + Agent 智能问答 (知识库+推理)
    19. MCP 日历助手 (官方SDK工具调用)
    20. 多 Agent 文档审核 (Writer→Reviewer→Editor)
    21. 代码生成助手 (Structured Output)
    22. 客服智能分流 (意图识别+路由)
    23. 内容安全过滤 (敏感词替换+PII脱敏)
    24. MCP MiniApi 服务 (外部工具集成)
    --------------------------------------------------
    0. 退出程序
    ==================================================");
    Console.ResetColor();
    Console.Write("\n请选择功能编号: ");

    var choice = Console.ReadLine();

    try 
    {
        switch (choice)
        {
            case "1": await ChatDemo.RunAsync(sp); break;
            case "2": await WorkflowDemo.RunAsync(sp); break;
            case "3": await StructuredOutputDemo.RunAsync(sp); break;
            case "4": await ToolDemo.RunAsync(sp); break;
            case "5": await ScriptingDemo.RunAsync(sp); break;
            case "6": await CompressionDemo.RunAsync(sp); break;
            case "7": await PromptDemo.RunAsync(sp); break;
            case "8": await RagDemo.RunAsync(sp); break;
            case "9": await MultimodalDemo.RunAsync(sp); break;
            case "10": await PersistenceDemo.RunAsync(sp); break;
            case "11": await ScenarioDemo.RunAsync(sp); break;
            case "12": await BuiltInAgentDemo.RunAsync(sp); break;
            case "13": await MiddlewareDemo.RunAsync(sp); break;
            case "14": await McpDemo.RunAsync(sp); break;
            case "15": await MonitoringDemo.RunAsync(sp); break;
            case "16": await StorageDemo.RunAsync(sp); break;
            case "17": await MediaDemo.RunAsync(sp); break;
            // ★ 新增实用场景
            case "18": await RagAgentDemo.RunAsync(sp); break;
            case "19": await McpCalendarDemo.RunAsync(sp); break;
            case "20": await MultiAgentReviewDemo.RunAsync(sp); break;
            case "21": await CodeGeneratorDemo.RunAsync(sp); break;
            case "22": await CustomerServiceDemo.RunAsync(sp); break;
            case "23": await ContentSafetyDemo.RunAsync(sp); break;
            case "24": await MiniApiServerDemo.RunAsync(sp); break;
            case "99": 
                {
                    // Reflection Inspector
                    Console.WriteLine("=== Inspecting Checkpointed<T> ===");
                    var cpType = typeof(Microsoft.Agents.AI.Workflows.Checkpointed<>);
                    foreach (var member in cpType.GetMembers())
                    {
                        Console.WriteLine($"[Checkpointed] {member.MemberType} {member.Name}");
                    }

                    Console.WriteLine("\n=== Inspecting StreamingRun ===");
                    var runType = typeof(Microsoft.Agents.AI.Workflows.StreamingRun);
                     foreach (var prop in runType.GetProperties())
                    {
                        Console.WriteLine($"[StreamingRun Prop] {prop.Name} : {prop.PropertyType.Name}");
                    }
                    foreach (var method in runType.GetMethods())
                    {
                         var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                         Console.WriteLine($"[StreamingRun Method] {method.Name}({parameters}) : {method.ReturnType.Name}");
                    }
                    
                    Console.WriteLine("\n=== Inspecting Checkpointed<StreamingRun> Properties ===");
                    var cpRunType = cpType.MakeGenericType(runType);
                    foreach (var prop in cpRunType.GetProperties())
                    {
                        Console.WriteLine($"[Checkpointed<StreamingRun> Prop] {prop.Name} : {prop.PropertyType.Name}");
                    }

                } 
                break;
            case "0": return;
            default: Console.WriteLine("❌ 无效的选择，请重新输入。"); break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n发生未处理异常: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine("\n演示结束，按任意键返回主菜单...");
    Console.ReadKey();
}