// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Mud.AI.ToolSurface.Extraction;

namespace Mud.AI.ToolSurface.Emit;

/// <summary>
/// Tier C 发射器：产出 <c>{P}DomainRegistrars/*.g.cs</c>（每执行器一个域注册器文件）与
/// <c>{Plural}ServiceCollectionCoreExtensions.g.cs</c>（逐执行器 DI 装配）。
/// </summary>
/// <remarks>
/// <para>
/// 移植自上游 <c>Mud.Feishu.AI.Tools.Emit.ToolRegistrarEmitter</c>（设计文档 §5.1 第 5 条输出路径，
/// 实为两个独立 hintName）。泛化点：
/// <list type="bullet">
/// <item>owner 门槛：上游 <c>CoreNamespace</c>（与 <c>ToolNamesOwnerAssembly</c> 同值的第三处常量）
/// 收敛为 <see cref="SdkToolProfileModel.OwnerAssembly"/> 单一槽消费（§5.2-2）；</item>
/// <item>域注册器命名空间 → <see cref="SdkToolProfileModel.RegistrationNamespace"/>；
/// DI 装配命名空间 → <see cref="SdkToolProfileModel.ContractNamespace"/>（上游 <c>CoreNamespace</c>
/// 的"产物落点"语义，与 owner 门槛值同串但独立消费）；</item>
/// <item>DI 装配文件名用 <see cref="SdkToolProfileModel.ProductPluralPrefix"/>（<c>FeishuTools</c>，
/// <b>不是</b> ProductPrefix+s，§4.2 第 16 项）；</item>
/// <item>绑定类型名 <c>FeishuToolBinding</c> → <see cref="SdkToolProfileModel.BindingTypeName"/>（§4.2 第 17 项）；
/// 注册器接口名 <c>I{P}DomainRegistrar</c>、登记表 <c>{P}DomainRegistrars</c>、注册助手 <c>{P}Registration</c>、
/// 注册表 <c>{P}Registry</c>、名字契约 <c>{P}Names</c> 均由 ProductPrefix 推导（不入槽）；</item>
/// <item>诊断 MUDFT022/023 → factory 槽位 22/23（024/025 已由扫描层产出为 PendingDiagnostic）。</item>
/// </list>
/// </para>
/// <para>
/// <b>分组键 = 执行器类</b>（不是「模块」）：一枚 handler 特性同时携带工具名与执行器类型，
/// 故注册器 / DI 核心方法可按执行器类机械聚合。<b>由此消失的三类特例</b>：
/// 「Write 域跨多模块」「一模块一执行器」的伪约束、「软缺席语义需人工声明」
/// （改为由执行器构造器参数的符号事实推导，见 <see cref="ToolDependencyKind"/>）。
/// </para>
/// </remarks>
internal static class ToolRegistrarEmitter
{
    private const string CtorArgumentIndent = "                        ";

    /// <summary>域注册器类型名的固定尾缀（守卫据此断言"域注册器家族在位"）。</summary>
    private const string RegistrarTypeSuffix = "ToolDomainRegistrar";

    /// <summary>DI 核心方法名的固定尾缀（手写入口以 <c>Add{SDK名}{X}Core</c> 调用）。</summary>
    private const string CoreMethodSuffix = "Core";

    /// <summary>域注册器产物目录名（每注册器类一个文件，hintName 前缀）。</summary>
    public static string RegistrarsOutputFolder(SdkToolProfileModel profile)
        => profile.ProductPrefix + "DomainRegistrars";

    /// <summary>
    /// 发射注册器与 DI 装配（仅名字契约所有者程序集；产物引用 owner 程序集的 <c>internal</c> 类型）。
    /// </summary>
    /// <param name="context">源产出上下文。</param>
    /// <param name="models">Tier C 工具模型集合。</param>
    /// <param name="profile">当前剖面（门槛 / 命名空间 / 绑定类型名 / 前缀槽的唯一来源）。</param>
    /// <param name="assemblyName">当前编译的程序集名（决定是否发射）。</param>
    /// <param name="handlers">扫描到的执行器绑定（含形态非法者，供诊断与抑制判断）。</param>
    /// <param name="factory">当前剖面的诊断工厂（槽位 022/023 描述符）。</param>
    public static void Emit(
        SourceProductionContext context,
        ImmutableArray<ToolSchemaModel> models,
        SdkToolProfileModel profile,
        string? assemblyName,
        ImmutableArray<ScannedHandler> handlers,
        ToolSurfaceDiagnostics.Factory factory)
    {
        try
        {
            if (models.IsEmpty
                || !string.Equals(assemblyName, profile.OwnerAssembly, StringComparison.Ordinal))
            {
                return;
            }

            // netstandard2.0 无 Enumerable.ToHashSet，故显式构造（生成器宿主 TFM 约束）。
            var toolNames = new HashSet<string>(models.Select(static m => m.Entry.ToolName), StringComparer.Ordinal);

            // 已声明（含形态非法）的工具名：形态错误已由槽位 023/024/025 上报，
            // 此处用它抑制"未绑定"的重复诊断（同一根因只报一次）。
            var declared = new HashSet<string>(
                handlers
                    .Select(static h => h.ToolName)
                    .Where(static name => !string.IsNullOrEmpty(name))
                    .Select(static name => name!),
                StringComparer.Ordinal);

            var valid = handlers
                .Select(static h => h.Binding)
                .Where(static binding => binding is not null)
                .Select(static binding => binding!)
                .OrderBy(static binding => binding.RegistrarTypeName, StringComparer.Ordinal)
                .ThenBy(static binding => binding.ToolName, StringComparer.Ordinal)
                .ToArray();

            // 槽位 022：契约工具未绑定执行器（漂移守卫——新增工具忘标 handler 时构建即失败）。
            foreach (var toolName in toolNames.OrderBy(static name => name, StringComparer.Ordinal))
            {
                if (!declared.Contains(toolName))
                {
                    factory.Report(context, ToolSurfaceDiagnostics.SlotHandlerUnbound, null,
                        profile.ToolAttributeName, toolName);
                }
            }

            // 注：绑定「指向不存在的工具」在此结构下不可达——工具名取自被 typeof 指向接口自身的
            // 工具特性声明，与契约表同源（不存在字符串字面量绕过常量的路径）。

            // 槽位 023：同一工具被多个执行器方法绑定——产物会在注册期抛"工具已注册"，故编译期拦下。
            foreach (var duplicate in valid
                .Where(binding => toolNames.Contains(binding.ToolName))
                .GroupBy(static binding => binding.ToolName, StringComparer.Ordinal)
                .Where(static group => group.Count() > 1)
                .OrderBy(static group => group.Key, StringComparer.Ordinal))
            {
                factory.Report(
                    context,
                    ToolSurfaceDiagnostics.SlotHandlerBindingInvalid,
                    null,
                    duplicate.Key,
                    $"被 {duplicate.Count()} 个执行器方法绑定（{string.Join("、", duplicate.Select(static b => b.ExecutorTypeName + "." + b.MethodName))}）");
            }

            // 分组键为执行器类型（同类的多枚绑定进同一个注册器 / 同一个 Core 方法）。
            var executors = valid
                .Where(binding => toolNames.Contains(binding.ToolName))
                .GroupBy(static binding => binding.ExecutorType, StringComparer.Ordinal)
                .OrderBy(static group => group.First().RegistrarTypeName, StringComparer.Ordinal)
                .ToArray();

            if (executors.Length == 0)
            {
                return;
            }

            // R4-10：注册器类型名 / 核心方法名 / hintName 的**碰撞消歧**。
            // 三者都由执行器**简单名**派生，却发射进同一个命名空间与同一个静态类——
            // 两个命名空间下的同名执行器（A.FooTools / B.FooTools）会产出同名注册器（CS0101）
            // 与同名核心方法（CS0111）。此处仅为**发生碰撞**的执行器追加稳定的命名空间消歧后缀：
            // 无碰撞的执行器名字保持不变（手写 Add{SDK名}{X}Core 调用点因此无需改动）。
            var suffixByExecutor = ResolveCollidingNames(executors);

            // 域注册器：每执行器一个独立产物文件（同一注册器类不再与他类共文件）。
            foreach (var executor in executors)
            {
                var registrarName = ApplySuffix(
                    executor.First().RegistrarTypeName,
                    suffixByExecutor.TryGetValue(executor.Key, out var suffix) ? suffix : string.Empty,
                    RegistrarTypeSuffix);

                TransitiveCodeGenerator.AddSourceValidated(
                    context,
                    RegistrarHintName(registrarName, profile),
                    EmitRegistrar(executor, registrarName, profile));
            }

            TransitiveCodeGenerator.AddSourceValidated(
                context,
                $"{profile.ProductPluralPrefix}ServiceCollectionCoreExtensions.g.cs",
                EmitCoreExtensions(executors, suffixByExecutor, profile));
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(ToolRegistrarEmitter), ex);
        }
    }

    /// <summary>域注册器产物 hintName：<c>BitableToolDomainRegistrar</c> → <c>{P}DomainRegistrars/BitableToolDomainRegistrar.g.cs</c>。</summary>
    private static string RegistrarHintName(string registrarTypeName, SdkToolProfileModel profile)
        => $"{RegistrarsOutputFolder(profile)}/{registrarTypeName}.g.cs";

    /// <summary>
    /// R4-10：找出「派生名碰撞」的执行器并给出消歧后缀（无碰撞者为空串）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 注册器名与核心方法名**分别**判碰撞——两者派生规则不同，可能一个撞另一个不撞
    /// （如 <c>FooTools</c> 与 <c>FooToolTools</c>：注册器名都归约为 <c>FooToolDomainRegistrar</c>，
    /// 核心方法名却是 <c>Add{SDK}FooToolsCore</c> / <c>Add{SDK}FooToolToolsCore</c>）。
    /// </para>
    /// <para>
    /// 后缀取自执行器<b>全限定名中的命名空间</b>（剥离非字母数字字符）——<c>命名空间 + 类名</c>在 C# 中唯一，
    /// 故后缀能消除碰撞；且它与编译顺序无关（增量构建下名字稳定，不会引起无谓重发）。
    /// </para>
    /// </remarks>
    private static Dictionary<string, string> ResolveCollidingNames(
        IGrouping<string, ToolHandlerBinding>[] executors)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var selector in new Func<IGrouping<string, ToolHandlerBinding>, string>[]
                 {
                     static executor => executor.First().RegistrarTypeName,
                     static executor => executor.First().CoreMethodName,
                 })
        {
            foreach (var group in executors.GroupBy(selector, StringComparer.Ordinal))
            {
                // 同一 ExecutorType 的多个绑定本就在同一组，只有**跨执行器**同名才算碰撞。
                if (group.Select(static executor => executor.Key).Distinct(StringComparer.Ordinal).Count() <= 1)
                {
                    continue;
                }

                foreach (var executor in group)
                {
                    result[executor.Key] = NamespaceSuffix(executor.First().ExecutorType);
                }
            }
        }

        return result;
    }

    /// <summary>把后缀插到固定尾缀<b>之前</b>（保持 <c>…ToolDomainRegistrar</c> / <c>…Core</c> 形态）。</summary>
    private static string ApplySuffix(string name, string suffix, string trailingMarker)
        => string.IsNullOrEmpty(suffix) || !name.EndsWith(trailingMarker, StringComparison.Ordinal)
            ? name
            : name.Substring(0, name.Length - trailingMarker.Length) + suffix + trailingMarker;

    /// <summary>从全限定类型名取"命名空间"消歧后缀（<c>global::A.B.C.Foo</c> → <c>ABC</c>）。</summary>
    private static string NamespaceSuffix(string executorType)
    {
        const string GlobalPrefix = "global::";
        var full = executorType.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? executorType.Substring(GlobalPrefix.Length)
            : executorType;

        var lastDot = full.LastIndexOf('.');
        if (lastDot <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(lastDot);
        foreach (var ch in full.Substring(0, lastDot))
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    // ────────── 产物一：域注册器（每执行器一个文件） ──────────

    private static string EmitRegistrar(IGrouping<string, ToolHandlerBinding> executor, string registrarName, SdkToolProfileModel profile)
    {
        var first = executor.First();
        var source = new StringBuilder();
        AppendHeader(source, profile, profile.RegistrationNamespace);
        source.Line("{");
        source.Line($"    /// <summary>{first.ExecutorTypeName} 的域注册器（编译期按 [{profile.ToolHandlerAttributeName}] 聚合）。</summary>");
        source.Line($"    {GeneratedCodeMarker.Attribute(profile)}");
        source.Line($"    internal sealed class {registrarName}({first.ExecutorType} executor, {profile.BindingTypeName} binding) : I{profile.ProductPrefix}DomainRegistrar");
        source.Line("    {");
        source.Line($"        {GeneratedCodeMarker.Attribute(profile)}");
        source.Line($"        public void Register({profile.ProductPrefix}Registry registry)");
        source.Line("        {");

        foreach (var binding in executor.OrderBy(static b => b.ToolName, StringComparer.Ordinal))
        {
            source.Line($"            {profile.ProductPrefix}Registration.RegisterExecution(registry, {profile.ProductPrefix}Names.{SchemaEmitter.BuildNameConstant(binding.ToolName)}, binding,");
            source.Line($"                (args, ct) => executor.{binding.MethodName}(args, ct));");
        }

        source.Line("        }");
        source.Line("    }");
        source.Line("}");
        return source.ToString();
    }

    // ────────── 产物二：DI 装配 ──────────

    private static string EmitCoreExtensions(
        IGrouping<string, ToolHandlerBinding>[] executors,
        Dictionary<string, string> suffixByExecutor,
        SdkToolProfileModel profile)
    {
        var source = new StringBuilder();
        AppendHeader(
            source,
            profile,
            profile.ContractNamespace,
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Extensions");
        source.Line("{");

        source.Line("    /// <summary>逐执行器 DI 装配（软缺席语义由执行器构造器签名推导，见 ToolDependencyKind）。</summary>");
        source.Line($"    {GeneratedCodeMarker.Attribute(profile)}");
        source.Line($"    internal static class {profile.ProductPluralPrefix}ServiceCollectionCoreExtensions");
        source.Line("    {");

        foreach (var executor in executors)
        {
            // R4-10：核心方法名与**其引用的注册器类型名**必须同步消歧——
            // 同名执行器会让本静态类出现签名相同的方法（CS0111）；而方法体里的
            // `new {RegistrationNamespace}.{RegistrarTypeName}(...)` 若仍用未消歧名则 CS0246。
            var suffix = suffixByExecutor.TryGetValue(executor.Key, out var resolved) ? resolved : string.Empty;
            var coreMethodName = ApplySuffix(executor.First().CoreMethodName, suffix, CoreMethodSuffix);
            var registrarName = ApplySuffix(executor.First().RegistrarTypeName, suffix, RegistrarTypeSuffix);

            EmitCoreMethod(source, executor, coreMethodName, registrarName, profile);
        }

        source.Line("    }");
        source.Line("}");
        return source.ToString();
    }

    private static void EmitCoreMethod(
        StringBuilder source,
        IGrouping<string, ToolHandlerBinding> executor,
        string coreMethodName,
        string registrarName,
        SdkToolProfileModel profile)
    {
        var first = executor.First();
        var dependencies = first.Dependencies;
        var softIndexes = dependencies
            .Select((dependency, index) => (dependency, index))
            .Where(static pair => pair.dependency.Kind == ToolDependencyKind.SoftService)
            .Select(static pair => pair.index)
            .ToArray();

        // 软缺席路径把待判定项改成局部变量；其余一律走解析表达式。
        var softLocalByIndex = new Dictionary<int, string>();
        for (var i = 0; i < softIndexes.Length; i++)
        {
            softLocalByIndex[softIndexes[i]] = "soft" + i.ToString(CultureInfo.InvariantCulture);
        }

        var arguments = new string[dependencies.Count];
        for (var i = 0; i < dependencies.Count; i++)
        {
            arguments[i] = softLocalByIndex.TryGetValue(i, out var local)
                ? local
                : BuildResolveExpression(dependencies[i]);
        }

        source.Line();
        source.Line($"        /// <summary>{first.ExecutorTypeName} 的执行器 + 域注册器装配（{executor.Count()} 枚工具）。</summary>");
        if (softIndexes.Length > 0)
        {
            source.Line("        /// <remarks>软缺席："
                + string.Join(" / ", softIndexes.Select(i => dependencies[i].TypeName))
                + " 任一缺席 → 执行器解析为 null → 该域工具不进注册表（白名单期 fail-fast）。</remarks>");
        }

        source.Line($"        {GeneratedCodeMarker.Attribute(profile)}");
        source.Line($"        internal static IServiceCollection {coreMethodName}(this IServiceCollection services)");
        source.Line("        {");

        if (softIndexes.Length == 0)
        {
            // 无软缺席候选 → 执行器恒可构造（如依赖仅 IOptions<T> 的能力出处元工具）。
            source.Line($"            services.TryAddSingleton(static sp => new {first.ExecutorType}({BuildCtorArguments(arguments)}));");
        }
        else
        {
            source.Line("            services.TryAddSingleton(static sp =>");
            source.Line("            {");
            foreach (var index in softIndexes)
            {
                source.Line($"                var {softLocalByIndex[index]} = sp.GetService<{dependencies[index].TypeName}>();");
            }

            source.Line("                return "
                + string.Join(" && ", softIndexes.Select(i => softLocalByIndex[i] + " is not null")));
            source.Line($"                    ? new {first.ExecutorType}({BuildCtorArguments(arguments, "                        ", "                    ")})");
            source.Line("                    : null!;");
            source.Line("            });");
        }

        // 注册器登记：执行器缺席（工厂返回 null）时工厂返回 null → 该域工具不进注册表。
        // 经 internal 静态助手登记（**非** private 扩展——生成产物是独立类型，跨类不可见：R1 §4.5 的 CS0122 根因）。
        source.Line($"            {profile.RegistrationNamespace}.{profile.ProductPrefix}DomainRegistrars.Add(services, static sp =>");
        source.Line($"                sp.GetService<{first.ExecutorType}>() is {{ }} executor");
        source.Line($"                    ? new {profile.RegistrationNamespace}.{registrarName}(executor, sp.GetRequiredService<{profile.ContractNamespace}.{profile.BindingTypeName}>())");
        source.Line("                    : null);");
        source.Line("            return services;");
        source.Line("        }");
    }

    /// <summary>构造实参列表（零参返回空、单参同行、多参逐行）。</summary>
    private static string BuildCtorArguments(
        IReadOnlyList<string> arguments,
        string firstLineIndent = CtorArgumentIndent,
        string closingIndent = "                    ")
    {
        if (arguments.Count == 0)
        {
            return string.Empty;
        }

        // 显式 LF（FIX-16）：生成器内禁用 Environment（RS1035），且产物的换行形态与 golden 归一化口径一致。
        const string NewLine = "\n";
        return NewLine
            + string.Join("," + NewLine, arguments.Select(argument => firstLineIndent + argument))
            + NewLine
            + closingIndent;
    }

    /// <summary>依赖 → DI 解析表达式（软缺席候选在无软缺席路径下同样走 GetService）。</summary>
    private static string BuildResolveExpression(ToolExecutorDependency dependency)
        => dependency.Kind == ToolDependencyKind.RequiredService
            ? $"sp.GetRequiredService<{dependency.TypeName}>()"
            : $"sp.GetService<{dependency.TypeName}>()";

    /// <summary>产物文件头（<c>using</c> 必须位于 <c>namespace</c> <b>之前</b>）。</summary>
    private static void AppendHeader(StringBuilder source, SdkToolProfileModel profile, string ns, params string[] usings)
    {
        source.Line($"// <auto-generated> 由 {GeneratedCodeMarker.GeneratorName(profile)} 编译期产出，禁止手工修改 </auto-generated>");
        source.Line("#nullable enable");
        source.Line("#pragma warning disable CS1591 // 生成代码不逐一补 XML 注释");
        foreach (var @using in usings)
        {
            source.Line($"using {@using};");
        }

        source.Line($"namespace {ns}");
    }
}