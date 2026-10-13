// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mud.AI.ToolSurface;

/// <summary>
/// 剖面契约守卫分析器（<c>SDKT001</c>，ToolSurface 设计文档 §6.2）。
/// </summary>
/// <remarks>
/// <para>
/// 强制 <c>ISdkToolProfile</c>（标记接口）与 <c>[SdkToolProfile]</c>（数据特性）<b>成对出现</b>：
/// </para>
/// <list type="number">
///   <item>实现接口却缺特性 —— 引擎读不到任何剖面常量，工具面静默不产出（最难排查的故障形态）；</item>
///   <item>标特性却未实现接口 —— 引擎按接口识别剖面，该类型会被完全忽略，特性写错位置。</item>
/// </list>
/// <para>
/// <b>为什么用「全名单字符串」判定而非 <c>typeof</c></b>：本程序集是编译器扩展，
/// 设计纪律（§1.1 工程纪律 3）禁止引用任何 <c>Mud.*</c> 运行时程序集，故与组件仓
/// HTTP 分析器（<c>TokenManagerLifetimeAnalyzer</c>）同例，按
/// <c>GetTypeByMetadataName</c> + 特性「简单名 + 命名空间」双重判定。
/// </para>
/// <para>
/// 本分析器与 HTTP 分析器同程序集（复用「分析器并入 gen 程序集」先例），随
/// <c>Mud.AI.Generator</c> 的 <c>analyzers/dotnet/cs</c> 路径分发。
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProfileContractGuardAnalyzer : DiagnosticAnalyzer
{
    private const string ProfileInterfaceFullName = "Mud.AI.ISdkToolProfile";
    private const string ProfileAttributeSimpleName = "SdkToolProfileAttribute";
    private const string ProfileAttributeNamespace = "Mud.AI.Attributes";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(ProfileContractDiagnostics.SdkToolProfileContractViolation);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        // CA1062：公开可覆写入口显式参数守卫（Roslyn 恒传非 null，但避免第三方直调时 NRE）。
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // 语法级快速过滤：只有「带基列表或特性列表」的类声明才可能涉及剖面契约，
        // 其余类型声明零语义成本跳过（与 TokenManagerLifetimeAnalyzer 的过滤纪律一致）。
        context.RegisterSyntaxNodeAction(
            AnalyzeClassDeclaration,
            SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        // FIX-09 同例：分析器异常护栏——逸出到 csc 的异常会让宿主编译直接失败（AD0001）。
        try
        {
            if (context.Node is not ClassDeclarationSyntax classDeclaration)
            {
                return;
            }

            // 零成本预筛：既无基列表也无特性列表的类不可能命中任一分支。
            if (classDeclaration.BaseList is null && classDeclaration.AttributeLists.Count == 0)
            {
                return;
            }

            if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken) is not INamedTypeSymbol symbol)
            {
                return;
            }

            var profileInterface = context.Compilation.GetTypeByMetadataName(ProfileInterfaceFullName);

            var implementsInterface = profileInterface is not null
                && (SymbolEqualityComparer.Default.Equals(symbol, profileInterface)
                    || symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, profileInterface)));

            var hasAttribute = HasProfileAttribute(symbol);

            // 成对 = 二者同真或同假；同假时与本诊断无关。
            if (implementsInterface == hasAttribute)
            {
                return;
            }

            var location = GetReportLocation(classDeclaration, implementsInterface);

            context.ReportDiagnostic(implementsInterface
                ? Diagnostic.Create(
                    ProfileContractDiagnostics.SdkToolProfileContractViolation,
                    location,
                    symbol.ToDisplayString(),
                    "实现了 ISdkToolProfile 但缺少 [SdkToolProfile]",
                    "补上 [SdkToolProfile(…)] 并填写全部剖面槽位")
                : Diagnostic.Create(
                    ProfileContractDiagnostics.SdkToolProfileContractViolation,
                    location,
                    symbol.ToDisplayString(),
                    "标注了 [SdkToolProfile] 但未实现 ISdkToolProfile",
                    "让该类型实现 ISdkToolProfile"));
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(ProfileContractGuardAnalyzer), ex);
        }
    }

    /// <summary>
    /// 按「简单名 + 命名空间」双重判定剖面特性（防同名特性误命中，与上游
    /// <c>Extractors.GetFeishuToolAttribute</c> 同纪律）。
    /// </summary>
    private static bool HasProfileAttribute(INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is not null
                && attributeClass.Name is ProfileAttributeSimpleName or "SdkToolProfile"
                && string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), ProfileAttributeNamespace, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 定位到「缺失的那一侧」：缺特性时报类标识符（补特性的位置），
    /// 缺接口实现时报基列表（补 <c>: ISdkToolProfile</c> 的位置；无基列表时退化为标识符）。
    /// </summary>
    private static Location GetReportLocation(
        ClassDeclarationSyntax classDeclaration,
        bool implementsInterface)
    {
        if (!implementsInterface && classDeclaration.BaseList is { } baseList)
        {
            return baseList.GetLocation();
        }

        return classDeclaration.Identifier.GetLocation();
    }
}