using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Admin.NET.Ai.Extensions;

namespace HeMaCupAICheck.Demos;

/// <summary>
/// 场景21: 代码生成助手
/// 
/// 📌 展示 Structured Output + Tool 能力
/// 
/// 功能:
/// 1. 解析需求生成代码结构
/// 2. 生成可执行代码
/// 3. 模拟单元测试验证
/// </summary>
public static class CodeGeneratorDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("CodeGeneratorDemo");
        var aiFactory = sp.GetRequiredService<IAiFactory>();

        Console.WriteLine("\n=== [5] 代码生成助手 (Structured Output) ===\n");

        // ===== 1. 需求输入 =====
        Console.WriteLine("--- 1. 需求描述 ---");
        
        var requirement = """
            创建一个 C# 类 UserService，包含以下功能：
            1. 根据 ID 获取用户 (async)
            2. 创建新用户 (返回创建的用户)
            3. 验证邮箱格式
            用户模型包含: Id, Name, Email, CreatedAt
            """;
        
        Console.WriteLine(requirement);

        // ===== 2. 结构化分析 =====
        Console.WriteLine("\n--- 2. 需求结构化分析 ---");

        var codeSpec = new
        {
            ClassName = "UserService",
            Methods = new[]
            {
                new { Name = "GetByIdAsync", ReturnType = "Task<User?>", Params = "int id" },
                new { Name = "CreateAsync", ReturnType = "Task<User>", Params = "string name, string email" },
                new { Name = "IsValidEmail", ReturnType = "bool", Params = "string email" }
            },
            Model = new
            {
                Name = "User",
                Properties = new[] { "int Id", "string Name", "string Email", "DateTime CreatedAt" }
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(codeSpec, new JsonSerializerOptions { WriteIndented = true }));

        // ===== 3. 代码生成 =====
        Console.WriteLine("\n--- 3. 生成代码 ---");

        try
        {
            var chatClient = aiFactory.GetDefaultChatClient();

            var prompt = $"""
                基于以下需求生成完整的 C# 代码：
                
                {requirement}
                
                要求：
                1. 包含 User 模型类
                2. 包含 UserService 类
                3. 使用 async/await
                4. 邮箱验证使用正则表达式
                5. 代码简洁，有必要注释
                
                只输出代码，不要解释。
                """;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("生成代码: ");
            await chatClient!.GetStreamingResponseAsync(prompt).WriteToConsoleAsync();
            Console.ResetColor();
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            // 模拟输出
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = """";
    public string Email { get; set; } = """";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserService
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public async Task<User?> GetByIdAsync(int id)
    {
        await Task.Delay(10); // 模拟异步
        return _users.FirstOrDefault(u => u.Id == id);
    }

    public async Task<User> CreateAsync(string name, string email)
    {
        if (!IsValidEmail(email))
            throw new ArgumentException(""Invalid email"");
            
        var user = new User 
        { 
            Id = _nextId++, 
            Name = name, 
            Email = email,
            CreatedAt = DateTime.UtcNow 
        };
        _users.Add(user);
        return await Task.FromResult(user);
    }

    public bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex
            .IsMatch(email, @""^[\w\.-]+@[\w\.-]+\.\w+$"");
    }
}
");
            Console.ResetColor();
            Console.WriteLine($"\n(模拟输出，实际需配置 LLM: {ex.Message})");
        }

        // ===== 4. 模拟测试 =====
        Console.WriteLine("\n--- 4. 单元测试生成 ---");
        Console.WriteLine(@"
[TestClass]
public class UserServiceTests
{
    [TestMethod]
    public async Task CreateAsync_ValidEmail_ReturnsUser()
    {
        var service = new UserService();
        var user = await service.CreateAsync(""张三"", ""test@example.com"");
        
        Assert.IsNotNull(user);
        Assert.AreEqual(""张三"", user.Name);
    }

    [TestMethod]
    public void IsValidEmail_InvalidFormat_ReturnsFalse()
    {
        var service = new UserService();
        Assert.IsFalse(service.IsValidEmail(""invalid-email""));
    }
}
");

        // ===== 5. Structured Output 示例 =====
        Console.WriteLine("--- 5. Structured Output 高级用法 ---");
        Console.WriteLine(@"
// 使用 JSON Schema 约束输出格式
var response = await chatClient.GetResponseAsync<CodeGenerationResult>(prompt);

// 定义输出结构
public class CodeGenerationResult
{
    public string ClassName { get; set; }
    public List<MethodSpec> Methods { get; set; }
    public string GeneratedCode { get; set; }
}

public class MethodSpec
{
    public string Name { get; set; }
    public string ReturnType { get; set; }
    public List<string> Parameters { get; set; }
}
");

        Console.WriteLine("\n========== 代码生成助手演示结束 ==========");
    }
}
