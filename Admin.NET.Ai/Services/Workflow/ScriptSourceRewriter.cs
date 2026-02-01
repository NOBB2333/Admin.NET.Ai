using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Admin.NET.Ai.Services.Workflow;

/// <summary>
/// 基于 Roslyn 的脚本源码重写器，用于自动注入追踪代码
/// </summary>
public class ScriptSourceRewriter : CSharpSyntaxRewriter
{
    private string? _className;

    public static string Rewrite(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = (CompilationUnitSyntax)tree.GetRoot();
        
        // 添加必要的 using 指令
        var requiredUsings = new[] { 
            "Admin.NET.Ai.Models.Workflow", 
            "Admin.NET.Ai.Abstractions", 
            "System" 
        };
        
        var existingUsings = root.Usings.Select(u => u.Name?.ToString()).ToHashSet();
        foreach (var ns in requiredUsings)
        {
            if (!existingUsings.Contains(ns))
            {
                var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))
                    .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space));
                root = root.AddUsings(usingDirective);
            }
        }

        var rewriter = new ScriptSourceRewriter();
        var newRoot = rewriter.Visit(root);
        return newRoot.ToFullString();
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        _className = node.Identifier.Text;
        
        var newNode = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
        
        // 注入私有追踪上下文变量
        var contextField = SyntaxFactory.ParseMemberDeclaration(
            "private Admin.NET.Ai.Models.Workflow.IScriptExecutionContext? _trace;")!;
        newNode = newNode.AddMembers(contextField);
        
        return newNode;
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.Body == null && node.ExpressionBody == null) return base.VisitMethodDeclaration(node);
        if (node.Identifier.Text == "GetMetadata") return base.VisitMethodDeclaration(node);

        var methodName = node.Identifier.Text;
        var originalBody = GetBlock(node);
        var returnType = node.ReturnType.ToString();
        var isVoid = returnType == "void";
        
        // 1. 构造参数捕获对象 (匿名对象)
        // 自动过滤掉 IScriptExecutionContext 类型的参数和 ct/trace 参数
        string inputExpr = "null";
        var validParams = node.ParameterList.Parameters
            .Where(p => p.Type?.ToString() != "IScriptExecutionContext" 
                     && p.Type?.ToString() != "CancellationToken"
                     && p.Identifier.Text != "trace"
                     && p.Identifier.Text != "ct")
            .Select(p => p.Identifier.Text)
            .ToList();

        if (validParams.Count > 0)
        {
            inputExpr = "new { " + string.Join(", ", validParams) + " }";
        }

        // 2. 特殊处理 ExecuteAsync 方法 - 从 trace 参数捕获上下文
        string syncContext = "";
        if (methodName == "ExecuteAsync" && node.ParameterList.Parameters.Any(p => p.Identifier.Text == "trace"))
        {
             syncContext = "_trace = trace;";
        }

        // 3. 构造重写后的方法体
        BlockSyntax newBody;
        if (isVoid)
        {
            newBody = SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(syncContext),
                SyntaxFactory.ParseStatement($@"
                    using (var scope = _trace?.BeginStep(""{methodName}"", {inputExpr}))
                    {{
                        try 
                        {{
                            {originalBody.Statements.ToFullString()}
                        }}
                        catch (Exception ex)
                        {{
                            scope?.SetError(ex);
                            throw;
                        }}
                    }}")
            );
        }
        else
        {
            // 对于有返回值的方法，使用局部函数包装原始逻辑以捕获返回值
            newBody = SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(syncContext),
                SyntaxFactory.ParseStatement($@"
                    using (var scope = _trace?.BeginStep(""{methodName}"", {inputExpr}))
                    {{
                        try 
                        {{
                            {returnType} __internal_func() 
                            {{
                                {originalBody.Statements.ToFullString()}
                            }}
                            var __result = __internal_func();
                            scope?.SetOutput(__result);
                            return __result;
                        }}
                        catch (Exception ex)
                        {{
                            scope?.SetError(ex);
                            throw;
                        }}
                    }}")
            );
        }

        return node.WithBody(newBody).WithExpressionBody(null).WithSemicolonToken(default);
    }

    private BlockSyntax GetBlock(MethodDeclarationSyntax node)
    {
        if (node.Body != null) return node.Body;
        if (node.ExpressionBody != null)
        {
            // 如果是表达式主体，转换为带有 return 的语句块
            return SyntaxFactory.Block(SyntaxFactory.ReturnStatement(node.ExpressionBody.Expression));
        }
        return SyntaxFactory.Block();
    }
}
