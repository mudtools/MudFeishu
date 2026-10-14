// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.ToolSurface.Extraction;

/// <summary>
/// handler 特性（<c>[{ToolHandlerAttributeName}]</c>，名字/命名空间来自剖面槽）扫描器：
/// 把「执行器方法 + 工具名」编译为 <see cref="ToolHandlerBinding"/>（含执行器构造器的 DI 依赖分类）。
/// </summary>
/// <remarks>
/// <para>
/// 移植自上游 <c>Mud.Feishu.AI.Tools.Extraction.ToolHandlerScanner</c>，泛化点（设计文档 §4.2）：
/// <list type="bullet">
/// <item><c>AttributeNamespace</c> 常量 → <see cref="SdkToolProfileModel.ToolHandlerAttributeNamespace"/>
/// 第 12 槽（<b>注意与工具特性命名空间不同</b>，不得复用 <c>ToolAttributeNamespace</c>）；</item>
/// <item><c>AttributeName</c> 常量 → <see cref="SdkToolProfileModel.ToolHandlerAttributeName"/> 槽；</item>
/// <item>返回类型名 <c>FeishuToolResult</c> → <see cref="SdkToolProfileModel.ResultTypeName"/> 第 13 槽；</item>
/// <item>软缺席命名空间判定 <c>Mud.Feishu*</c> → <see cref="SdkToolProfileModel.SdkNamespaceRoot"/> 槽；</item>
/// <item>诊断 MUDFT022/023/024/025 → factory 槽 22/23/24/25。</item>
/// </list>
/// </para>
/// <para>
/// <b>工具名来源</b>：handler 特性的 <c>typeof(工具接口)</c> 指向的接口自身的工具特性声明——
/// <b>不</b>用 <c>{P}Names</c> 常量（源生成器看不到自己本趟的输出，特性实参会退化为错误常量）。
/// </para>
/// <para>
/// <b>注册器分组 = 执行器类本身</b>：一枚方法级特性同时携带「工具名」（编译期常量）与
/// 「执行器类型」（特性的宿主类），两类事实一次到位，无需类级声明、无需模块映射、无特例分支。
/// </para>
/// <para>
/// <b>无额外 CompilationProvider</b>：注册器需要执行器<b>构造签名</b>，而它落在
/// <c>SemanticModel</c> 内即可完全解析（<see cref="ISymbol.ContainingType"/> 的实例构造器），
/// 故本扫描器与 Tier C 同层同纪律（源不变则不重发）。
/// </para>
/// </remarks>
internal static class ToolHandlerScanner
{
    /// <summary>扫描一个候选方法；非 handler 特性标注的方法返回 <see langword="null"/>。</summary>
    /// <param name="context">语法上下文（语义模型取自 <c>context.SemanticModel</c>）。</param>
    /// <param name="profile">当前剖面。</param>
    /// <param name="factory">当前剖面的诊断工厂（动态槽位描述符）。</param>
    /// <param name="faultInjection">
    /// P2-2 验证钩子（仅供测试程序集经 <c>InternalsVisibleTo</c> 注入）：实参为剖面名，返回非空异常即在
    /// 扫描体起点抛出。<b>由调用方（生成器实例）持有并传入，本层不保存</b>——曾用进程级静态字段，
    /// 而 xUnit 默认并行跑测试类 ⇒ 注入期内的<b>并发测试</b>扫描一并被打挂（表现为产物静默缺席、
    /// 只剩一条 026），且只在负载下偶发。常态为 <see langword="null"/>，零开销。
    /// </param>
    /// <param name="cancellationToken">取消令牌（CA1068：须为末位参数）。</param>
    /// <returns>扫描结果（<see cref="ScannedHandler.Faulted"/> 或 <see langword="null"/>）。</returns>
    public static ScannedHandler? Scan(
        GeneratorSyntaxContext context,
        SdkToolProfileModel profile,
        ToolSurfaceDiagnostics.Factory factory,
        Func<string, Exception?>? faultInjection,
        System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            if (faultInjection?.Invoke(profile.Name) is { } fault)
            {
                throw fault;
            }

            if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not IMethodSymbol method)
            {
                return null;
            }

            var attribute = GetHandlerAttribute(method, profile);
            if (attribute is null)
            {
                return null;
            }

            var executor = method.ContainingType;
            var owner = executor is null ? method.Name : executor.Name + "." + method.Name;

            // 工具名取自 **被 typeof 指向的接口自身的工具特性声明**（单一真相源）：
            // 不用生成器同趟产出的名字契约常量（源生成器看不到自己本轮的输出，
            // 实参退化为错误常量），也不用字符串字面量（改名需两处同步）。
            if (attribute.ConstructorArguments.FirstOrDefault().Value is not INamedTypeSymbol toolInterface)
            {
                // 槽位 023 上报点①：特性实参不是可解析的类型（缺 typeof / 指向不可解析符号）。
                return ScannedHandler.Faulted(
                    null,
                    PendingDiagnostic.Create(
                        factory[ToolSurfaceDiagnostics.SlotHandlerBindingInvalid],
                        owner,
                        $"实参须为 [{profile.ToolAttributeName}] 接口的 typeof"));
            }

            var candidateName = Extractors.TryGetToolNameFromAttribute(toolInterface, profile);
            if (string.IsNullOrWhiteSpace(candidateName))
            {
                // 槽位 023 上报点②：指向的接口未标注工具特性（推导不出工具名）。
                return ScannedHandler.Faulted(
                    null,
                    PendingDiagnostic.Create(
                        factory[ToolSurfaceDiagnostics.SlotHandlerBindingInvalid],
                        owner,
                        $"该接口未标注 [{profile.ToolAttributeName}]，推导不出工具名"));
            }

            // 显式窄化到独立的非空局部变量（上游 R2-04 纪律：一次窄化同时消除 CS1717 与下游 CS8604）。
            var toolName = candidateName!;

            if (executor is null)
            {
                // 槽位 023 上报点③：特性挂在不属于任何类型的成员上（表达式体/局部函数等非法场景）。
                return ScannedHandler.Faulted(
                    toolName,
                    PendingDiagnostic.Create(
                        factory[ToolSurfaceDiagnostics.SlotHandlerBindingInvalid],
                        toolName,
                        "特性必须标注在执行器类的公开方法上"));
            }

            if (!IsExecutorShape(method, profile))
            {
                // 槽位 024 上报点：方法签名不符合执行器契约——生成产物会在此处崩溃于 .g.cs 内部，
                // 诊断把它换成指向手写代码的可读错误。
                return ScannedHandler.Faulted(
                    toolName,
                    PendingDiagnostic.Create(
                        factory[ToolSurfaceDiagnostics.SlotHandlerSignatureMismatch],
                        toolName,
                        owner,
                        profile.ResultTypeName,
                        DescribeShape(method)));
            }

            var constructor = SelectConstructor(executor);
            var dependencies = new List<ToolExecutorDependency>(constructor.Parameters.Length);
            foreach (var parameter in constructor.Parameters)
            {
                if (!CanBeServiceType(parameter.Type))
                {
                    // 槽位 025 上报点：参数类型无法作为 DI 服务类型解析（数组/指针/元组/类型参数/开放泛型）。
                    return ScannedHandler.Faulted(
                        toolName,
                        PendingDiagnostic.Create(
                            factory[ToolSurfaceDiagnostics.SlotHandlerConstructorUnresolvable],
                            toolName,
                            executor.Name,
                            parameter.Name,
                            parameter.Type.ToDisplayString()));
                }

                dependencies.Add(new ToolExecutorDependency(
                    NormalizeTypeName(parameter.Type),
                    ClassifyDependency(parameter, profile)));
            }

            var executorTypeName = executor.Name;
            return ScannedHandler.Ok(
                toolName,
                new ToolHandlerBinding(
                    toolName: toolName,
                    executorType: executor.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    executorTypeName: executorTypeName,
                    registrarTypeName: ToolHandlerBinding.BuildRegistrarTypeName(executorTypeName),
                    coreMethodName: ToolHandlerBinding.BuildCoreMethodName(executorTypeName, profile),
                    methodName: method.Name,
                    dependencies: dependencies));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            GeneratorDebugLogger.LogError(nameof(ToolHandlerScanner), ex);
            // P2-2：扫描体异常折算为 {prefix}026 随流上报——此前返回 null，执行器绑定静默消失且不可诊断。
            // ToolName 记 null：绑定缺失，不参与「未绑定」误报抑制（发射器对 null 工具名惰性）。
            // OCE 是宿主取消语义，必须继续上抛。
            return ScannedHandler.Faulted(
                null,
                PendingDiagnostic.Create(
                    factory[ToolSurfaceDiagnostics.SlotGeneratorInternalError],
                    profile.Name + ":" + nameof(ToolHandlerScanner),
                    ex.GetType().Name,
                    ex.Message));
        }
    }

    /// <summary>
    /// 取 handler 特性数据（按「简单名 + 命名空间」双重判定，防同名特性误命中；
    /// 简单名匹配 <c>{ToolHandlerAttributeName}</c> 与 <c>…Attribute</c> 双形态）。
    /// </summary>
    /// <remarks>
    /// P2-2：此处不再就地吞异常——吞掉会让 handler 以「非 handler」身份静默消失（不可诊断）；
    /// 异常上抛给 <see cref="Scan"/> 的 catch，折算为 <c>{prefix}026</c> 随流上报。
    /// </remarks>
    public static AttributeData? GetHandlerAttribute(IMethodSymbol method, SdkToolProfileModel profile)
    {
        var bare = profile.ToolHandlerAttributeName;
        if (bare.Length == 0)
        {
            return null;
        }

        var full = bare + "Attribute";
        foreach (var attribute in method.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is not null
                && (attributeClass.Name == bare || attributeClass.Name == full)
                && string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), profile.ToolHandlerAttributeNamespace, StringComparison.Ordinal))
            {
                return attribute;
            }
        }

        return null;
    }

    // ────────── 依赖分类（唯一的判定点） ──────────

    /// <summary>
    /// 按「是否接口 + 命名空间是否 <c>{SdkNamespaceRoot}*</c> + 是否可空」分类（判定依据见
    /// <see cref="ToolDependencyKind"/> 的说明表）。
    /// </summary>
    /// <remarks>
    /// <b>可空声明优先于命名空间（上游 R2-06 修正保留）</b>：参数的 <c>?</c> 就是"该依赖可缺席"的意图表达——
    /// 此前非 SDK 命名空间的可空接口参数被误判为 <see cref="ToolDependencyKind.RequiredService"/>
    /// （生成 <c>GetRequiredService&lt;T&gt;()</c>），于是"声明可空"与"解析必失败即崩"互相矛盾
    /// （典型：执行器为补日志而新增 <c>ILogger&lt;T&gt;? logger = null</c>）。
    /// 现改为：可空 ⇒ <see cref="ToolDependencyKind.OptionalService"/>（<c>GetService</c>，缺席返回 null），
    /// 只有<b>非可空</b>的非 SDK 依赖才走 <c>GetRequiredService</c>（宿主必须提供的硬依赖，如 <c>IOptions&lt;T&gt;</c>）。
    /// </remarks>
    private static ToolDependencyKind ClassifyDependency(IParameterSymbol parameter, SdkToolProfileModel profile)
    {
        if (parameter.Type is not INamedTypeSymbol { TypeKind: TypeKind.Interface } named)
        {
            return ToolDependencyKind.RequiredService;
        }

        var optional = parameter.NullableAnnotation == NullableAnnotation.Annotated;
        return IsSdkNamespace(named.ContainingNamespace, profile)
            ? (optional ? ToolDependencyKind.OptionalService : ToolDependencyKind.SoftService)
            : (optional ? ToolDependencyKind.OptionalService : ToolDependencyKind.RequiredService);
    }

    /// <summary>
    /// 命名空间是否为「可软缺席」命名空间（软缺席候选的判定前提，剖面槽驱动）。
    /// </summary>
    /// <remarks>
    /// <b>两个槽而非一个（中立化的直接后果）</b>：<see cref="SdkToolProfileModel.SdkNamespaceRoot"/>
    /// 还承担「SDK 程序集定位」与 <c>Source</c> 前缀剥离，不能为推动软缺席判定而放宽；
    /// 中立契约（如 <c>Mud.AI.Abstractions.Knowledge.IRetriever</c>）经
    /// <see cref="SdkToolProfileModel.SoftAbsentNamespaceRoots"/> 追加。
    /// 两者取<b>并集</b>：命中任一根即软缺席候选。
    /// </remarks>
    private static bool IsSdkNamespace(INamespaceSymbol? ns, SdkToolProfileModel profile)
    {
        if (ns is null || ns.IsGlobalNamespace)
        {
            return false;
        }

        var name = ns.ToDisplayString();
        if (IsUnderRoot(name, profile.SdkNamespaceRoot))
        {
            return true;
        }

        foreach (var root in profile.SoftAbsentNamespaceRoots)
        {
            if (IsUnderRoot(name, root))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>命名空间是否等于某根或为其子命名空间（<c>root</c> 或 <c>root.</c> 前缀）。</summary>
    private static bool IsUnderRoot(string namespaceName, string root)
        => root.Length > 0
            && (namespaceName == root || namespaceName.StartsWith(root + ".", StringComparison.Ordinal));

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
    /// 执行器方法契约：<c>public Task&lt;{ResultTypeName}&gt; X(IReadOnlyDictionary&lt;string, object?&gt;, CancellationToken)</c>
    /// （返回类型名来自剖面第 13 槽 <see cref="SdkToolProfileModel.ResultTypeName"/>，按简单名判定）。
    /// </summary>
    private static bool IsExecutorShape(IMethodSymbol method, SdkToolProfileModel profile)
    {
        if (method.IsStatic || method.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (method.ReturnType is not INamedTypeSymbol { Name: "Task", TypeArguments.Length: 1 } task
            || task.TypeArguments[0].Name != profile.ResultTypeName)
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
        => $"{AccessibilityToString(method.DeclaredAccessibility)} "
           + $"{method.ReturnType.ToDisplayString()} {method.Name}("
           + string.Join(", ", method.Parameters.Select(static p => p.Type.ToDisplayString()))
           + ")";

    // 枚举成员有限，直接映射小写形态（避免 ToLower 触发 CA1308 且输出恒定）。
    private static string AccessibilityToString(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        Accessibility.Private => "private",
        Accessibility.NotApplicable => "notapplicable",
        _ => "none",
    };

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