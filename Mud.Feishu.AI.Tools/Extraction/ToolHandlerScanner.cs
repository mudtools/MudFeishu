// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mud.Feishu.AI.Tools.Extraction;

/// <summary>
/// <c>[FeishuToolHandler]</c> 扫描器：把「执行器方法 + 工具名」编译为
/// <see cref="ToolHandlerBinding"/>（含执行器构造器的 DI 依赖分类）。
/// </summary>
/// <remarks>
/// <para>
/// <b>工具名来源</b>：<c>[FeishuToolHandler(typeof(IFeishuXxxTool))]</c> 指向的接口自身的
/// <c>[FeishuTool]</c> 声明——<b>不</b>用 <c>FeishuToolNames</c> 常量（源生成器看不到自己本趟的
/// 输出，特性实参会退化为错误常量）。
/// </para>
/// <para>
/// <b>与 R1（被驳回的设计）的关键差异</b>：R1 想从「模块名」推导注册器分组，因而必须引入类级
/// <c>[FeishuToolExecutor("bitable")]</c>——而「模块」是<b>冗余维度</b>（工具名前缀已表达），且
/// 跨模块的写域必然打破「一模块一执行器」。本扫描器把分组键定为<b>执行器类本身</b>：
/// 一枚方法级特性同时携带「工具名」（编译期常量）与「执行器类型」（特性的宿主类），
/// 两类事实一次到位，无需类级声明、无需模块映射、无特例分支。
/// </para>
/// <para>
/// <b>无额外 CompilationProvider</b>：注册器需要执行器<b>构造签名</b>，而它落在
/// <c>SemanticModel</c> 内即可完全解析（<see cref="IMethodSymbol.ContainingType"/> 的实例构造器），
/// 故本扫描器与 Tier C 同层同纪律（源不变则不重发）。
/// </para>
/// </remarks>
internal static class ToolHandlerScanner
{
    /// <summary>特性所属命名空间（与 <c>[FeishuTool]</c> 同款双重判定：简单名 + 命名空间）。</summary>
    public const string AttributeNamespace = "Mud.Feishu.AI.FeishuTools";

    /// <summary>特性简单名。</summary>
    public const string AttributeName = "FeishuToolHandlerAttribute";

    /// <summary>扫描一个候选方法；非 <c>[FeishuToolHandler]</c> 标注的方法返回 <see langword="null"/>。</summary>
    public static ScannedHandler? Scan(GeneratorSyntaxContext context, System.Threading.CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not IMethodSymbol method)
        {
            return null;
        }

        var attribute = GetHandlerAttribute(method);
        if (attribute is null)
        {
            return null;
        }

        var executor = method.ContainingType;
        var owner = executor is null ? method.Name : executor.Name + "." + method.Name;

        // 工具名取自 **被 typeof 指向的接口自身的 [FeishuTool] 声明**（单一真相源）：
        // 不用生成器同趟产出的 FeishuToolNames 常量（源生成器看不到自己本轮的输出，
        // 实参退化为错误常量），也不用字符串字面量（改名需两处同步）。
        if (attribute.ConstructorArguments.FirstOrDefault().Value is not INamedTypeSymbol toolInterface)
        {
            // MUDFT023 上报点①：特性实参不是可解析的类型（缺 typeof / 指向不可解析符号）。
            return ScannedHandler.Faulted(
                null,
                PendingDiagnostic.Create(Diagnostics.MUDFT023, owner, "（非法接口实参）", "实参须为 [FeishuTool] 接口的 typeof"));
        }

        var candidateName = Extractors.TryGetToolNameFromAttribute(toolInterface);
        if (string.IsNullOrWhiteSpace(candidateName))
        {
            // MUDFT023 上报点②：指向的接口未标注 [FeishuTool]（推导不出工具名）。
            return ScannedHandler.Faulted(
                null,
                PendingDiagnostic.Create(
                    Diagnostics.MUDFT023,
                    owner,
                    toolInterface.Name,
                    "该接口未标注 [FeishuTool]，推导不出工具名"));
        }

        // R2-04：显式窄化到独立的非空局部变量（取代 `toolName = toolName!;` 的自赋值空抑制）——
        // 自赋值不改变运行时值，后续若语义变为可空编译器不再提醒（CS1717）；且抑制后仍处处传 `string?`，
        // 会连带产生 5 处 CS8604。此处一次窄化，下游全部用非空名。
        var toolName = candidateName!;

        if (executor is null)
        {
            // MUDFT023 上报点②：特性挂在不属于任何类型的成员上（表达式体/局部函数等非法场景）。
            return ScannedHandler.Faulted(
                toolName,
                PendingDiagnostic.Create(Diagnostics.MUDFT023, owner, toolName, "特性必须标注在执行器类的公开方法上"));
        }

        if (!IsExecutorShape(method))
        {
            // MUDFT024 上报点：方法签名不符合执行器契约——生成产物会在此处崩溃于 .g.cs 内部，
            // 诊断把它换成指向手写代码的可读错误。
            return ScannedHandler.Faulted(
                toolName,
                PendingDiagnostic.Create(Diagnostics.MUDFT024, toolName, owner, DescribeShape(method)));
        }

        var constructor = SelectConstructor(executor);
        var dependencies = new List<ToolExecutorDependency>(constructor.Parameters.Length);
        foreach (var parameter in constructor.Parameters)
        {
            if (!CanBeServiceType(parameter.Type))
            {
                // MUDFT025 上报点：参数类型无法作为 DI 服务类型解析（数组/指针/元组/类型参数/开放泛型）。
                return ScannedHandler.Faulted(
                    toolName,
                    PendingDiagnostic.Create(
                        Diagnostics.MUDFT025,
                        toolName,
                        executor.Name,
                        parameter.Name,
                        parameter.Type.ToDisplayString()));
            }

            dependencies.Add(new ToolExecutorDependency(
                NormalizeTypeName(parameter.Type),
                ClassifyDependency(parameter)));
        }

        var executorTypeName = executor.Name;
        return ScannedHandler.Ok(
            toolName,
            new ToolHandlerBinding(
                toolName: toolName,
                executorType: executor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                executorTypeName: executorTypeName,
                registrarTypeName: ToolHandlerBinding.BuildRegistrarTypeName(executorTypeName),
                coreMethodName: ToolHandlerBinding.BuildCoreMethodName(executorTypeName),
                methodName: method.Name,
                dependencies: dependencies));
    }

    /// <summary>取 <c>[FeishuToolHandler]</c> 特性数据（按简单名 + 命名空间双重判定，防同名特性误命中）。</summary>
    public static AttributeData? GetHandlerAttribute(IMethodSymbol method)
        => method.GetAttributes().FirstOrDefault(static a =>
            a.AttributeClass is not null
            && a.AttributeClass.Name == AttributeName
            && a.AttributeClass.ContainingNamespace.ToDisplayString() == AttributeNamespace);

    // ────────── 依赖分类（唯一的判定点） ──────────

    /// <summary>
    /// 按「是否接口 + 命名空间是否 <c>Mud.Feishu*</c> + 是否可空」分类（判定依据见
    /// <see cref="ToolDependencyKind"/> 的说明表）。
    /// </summary>
    /// <remarks>
    /// <b>可空声明优先于命名空间（R2-06 修正）</b>：参数的 <c>?</c> 就是"该依赖可缺席"的意图表达——
    /// 此前非 <c>Mud.Feishu*</c> 命名空间的可空接口参数被误判为 <see cref="ToolDependencyKind.RequiredService"/>
    /// （生成 <c>GetRequiredService&lt;T&gt;()</c>），于是"声明可空"与"解析必失败即崩"互相矛盾
    /// （典型：执行器为补日志而新增 <c>ILogger&lt;T&gt;? logger = null</c>）。
    /// 现改为：可空 ⇒ <see cref="ToolDependencyKind.OptionalService"/>（<c>GetService</c>，缺席返回 null），
    /// 只有<b>非可空</b>的非 Feishu 依赖才走 <c>GetRequiredService</c>（宿主必须提供的硬依赖，如 <c>IOptions&lt;T&gt;</c>）。
    /// </remarks>
    private static ToolDependencyKind ClassifyDependency(IParameterSymbol parameter)
    {
        if (parameter.Type is not INamedTypeSymbol { TypeKind: TypeKind.Interface } named)
        {
            return ToolDependencyKind.RequiredService;
        }

        var optional = parameter.NullableAnnotation == NullableAnnotation.Annotated;
        return IsFeishuNamespace(named.ContainingNamespace)
            ? (optional ? ToolDependencyKind.OptionalService : ToolDependencyKind.SoftService)
            : (optional ? ToolDependencyKind.OptionalService : ToolDependencyKind.RequiredService);
    }

    /// <summary>命名空间是否为 <c>Mud.Feishu</c> 或其子命名空间（软缺席候选的判定前提）。</summary>
    private static bool IsFeishuNamespace(INamespaceSymbol? ns)
    {
        if (ns is null || ns.IsGlobalNamespace)
        {
            return false;
        }

        var name = ns.ToDisplayString();
        return name == "Mud.Feishu" || name.StartsWith("Mud.Feishu.", System.StringComparison.Ordinal);
    }

    /// <summary>
    /// 参数类型能否作为 DI 服务类型（类/接口，非开放泛型、非元组、非值类型、<b>非 BCL 内建类型</b>）。
    /// </summary>
    /// <remarks>
    /// <c>SpecialType != None</c> 的排除是必要的：<c>string</c>/<c>object</c> 等在符号层是
    /// <see cref="TypeKind.Class"/>，但把它们当服务解析（<c>GetRequiredService&lt;string&gt;()</c>）是
    /// 无意义甚至有害的（会把任意 <c>string</c> 注册当依赖注入）。
    /// </remarks>
    private static bool CanBeServiceType(ITypeSymbol type)
        => type is INamedTypeSymbol
           {
               IsValueType: false,
               IsUnboundGenericType: false,
               IsAnonymousType: false,
               IsTupleType: false,
               SpecialType: SpecialType.None,
               TypeKind: TypeKind.Class or TypeKind.Interface,
           };

    // ────────── 形状校验 ──────────

    /// <summary>
    /// 执行器方法契约：<c>public Task&lt;FeishuToolResult&gt; X(IReadOnlyDictionary&lt;string, object?&gt;, CancellationToken)</c>。
    /// </summary>
    /// <remarks>31/31 工具方法均满足该形状（由 <c>ToolExecutorSkeletonGuards</c> 在测试侧同时锁定）。</remarks>
    private static bool IsExecutorShape(IMethodSymbol method)
    {
        if (method.IsStatic || method.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (method.ReturnType is not INamedTypeSymbol { Name: "Task", TypeArguments.Length: 1 } task
            || task.TypeArguments[0].Name != "FeishuToolResult")
        {
            return false;
        }

        if (method.Parameters.Length != 2
            || method.Parameters[0].Type is not INamedTypeSymbol { Name: "IReadOnlyDictionary", TypeArguments.Length: 2 } args
            || args.TypeArguments[0].SpecialType != SpecialType.System_String
            || args.TypeArguments[1].SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        return method.Parameters[1].Type.Name == "CancellationToken";
    }

    private static string DescribeShape(IMethodSymbol method)
        => $"{method.DeclaredAccessibility.ToString().ToLowerInvariant()} "
           + $"{method.ReturnType.ToDisplayString()} {method.Name}("
           + string.Join(", ", method.Parameters.Select(static p => p.Type.ToDisplayString()))
           + ")";

    /// <summary>取执行器主构造器（实例构造器中参数最多者；无显式构造器时为隐式无参构造器）。</summary>
    private static IMethodSymbol SelectConstructor(INamedTypeSymbol executor)
        => executor.InstanceConstructors
            .Where(static c => !c.IsStatic && c.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .OrderByDescending(static c => c.Parameters.Length)
            .First();

    /// <summary>
    /// 类型显示名归一（去可空标注、补 <c>global::</c>）——产物中会写成 <c>GetService&lt;T&gt;()</c>，
    /// 带 <c>?</c> 的泛型实参在低 TFM 上是无效写法。
    /// </summary>
    private static string NormalizeTypeName(ITypeSymbol type)
        => type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
