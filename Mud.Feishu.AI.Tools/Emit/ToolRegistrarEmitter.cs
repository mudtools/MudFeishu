// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Mud.Feishu.AI.Tools.Extraction;
using Mud.Feishu.AI.Tools.Schema;

namespace Mud.Feishu.AI.Tools.Emit;

/// <summary>
/// Tier C 发射器：产出 <c>FeishuToolDomainRegistrars/*.g.cs</c>（每执行器一个域注册器文件）与
/// <c>FeishuToolsServiceCollectionCoreExtensions.g.cs</c>（逐执行器 DI 装配）。
/// </summary>
/// <remarks>
/// <para>
/// <b>分组键 = 执行器类</b>（不是「模块」）：一枚 <c>[FeishuToolHandler]</c> 同时携带工具名与执行器类型，
/// 故注册器 / DI 核心方法可按执行器类机械聚合。<b>由此消失的三类特例</b>：
/// </para>
/// <list type="bullet">
/// <item>「Write 域跨 im / bitable / approval 三模块」——三个执行器即三个注册器，无需「多执行器声明」机制；</item>
/// <item>「一模块一执行器」的伪约束（<c>bitable</c>/<c>im</c>/<c>approval</c> 的只读与写执行器共存）；</item>
/// <item>「软缺席语义需人工声明」——改为由执行器构造器参数的符号事实推导（<see cref="ToolDependencyKind"/>）。</item>
/// </list>
/// <para>
/// <b>与 R1（被驳回设计）的差异</b>：R1 假设「tool → 执行器方法可派生」（错：<c>MethodName</c> 是 SDK 方法名）、
/// 「4 行依赖分类表可覆盖全量域」（错：漏了可空客户端与非 <c>IFeishu*</c> 软缺席依赖）、
/// 「生成的 Core 可调私有助手」（错：CS0122）。本发射器以「方法级声明 + 符号推导 + 稳定 seam」三项落地，
/// 并修正了 R1 §5 的 <c>IsOwnerAssembly(input.Right)</c> 索引错误（此处不再需要执行器符号管线）。
/// </para>
/// </remarks>
internal static class ToolRegistrarEmitter
{
    /// <summary>域注册器产物目录名（每注册器类一个文件，hintName 前缀）。</summary>
    public const string RegistrarsOutputFolder = "FeishuToolDomainRegistrars";

    /// <summary>DI 装配产物文件名。</summary>
    public const string CoreFileName = "FeishuToolsServiceCollectionCoreExtensions.g.cs";

    /// <summary>域注册器产物命名空间。</summary>
    public const string RegistrarsNamespace = "Mud.Feishu.AI.FeishuTools.Registration";

    /// <summary>DI 装配产物命名空间。</summary>
    public const string CoreNamespace = "Mud.Feishu.AI.FeishuTools";

    private const string CtorArgumentIndent = "                        ";

    /// <summary>
    /// 发射注册器与 DI 装配（仅名字契约所有者程序集；产物引用 FeishuTools 的 <c>internal</c> 类型）。
    /// </summary>
    /// <param name="context">源产出上下文。</param>
    /// <param name="models">Tier C 工具模型集合。</param>
    /// <param name="assemblyName">当前编译的程序集名（决定是否发射）。</param>
    /// <param name="handlers">扫描到的执行器绑定（含形态非法者，供诊断与抑制判断）。</param>
    public static void Emit(
        SourceProductionContext context,
        ImmutableArray<ToolSchemaModel> models,
        string? assemblyName,
        ImmutableArray<ScannedHandler> handlers)
    {
        if (models.IsEmpty
            || !string.Equals(assemblyName, SchemaEmitter.ToolNamesOwnerAssembly, StringComparison.Ordinal))
        {
            return;
        }

        // netstandard2.0 无 Enumerable.ToHashSet，故显式构造（生成器宿主 TFM 约束）。
        var toolNames = new HashSet<string>(models.Select(static m => m.Entry.ToolName), StringComparer.Ordinal);

        // 已声明（含形态非法）的工具名：形态错误已由 MUDFT023/024/025 上报，
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

        // MUDFT022：契约工具未绑定执行器（漂移守卫——新增工具忘标 handler 时构建即失败）。
        foreach (var toolName in toolNames.OrderBy(static name => name, StringComparer.Ordinal))
        {
            if (!declared.Contains(toolName))
            {
                Diagnostics.Report(context, Diagnostics.MUDFT022, null, toolName);
            }
        }

        // 注：绑定「指向不存在的工具」在此结构下不可达——工具名取自被 typeof 指向接口自身的
        // [FeishuTool] 声明，与契约表同源（不存在字符串字面量绕过常量的路径）。

        // MUDFT023：同一工具被多个执行器方法绑定——产物会在注册期抛"工具已注册"，故编译期拦下。
        foreach (var duplicate in valid
            .Where(binding => toolNames.Contains(binding.ToolName))
            .GroupBy(static binding => binding.ToolName, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            Diagnostics.Report(
                context,
                Diagnostics.MUDFT023,
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

        // 域注册器：每执行器一个独立产物文件（同一注册器类不再与他类共文件）。
        foreach (var executor in executors)
        {
            context.AddSource(
                RegistrarHintName(executor.First().RegistrarTypeName),
                SourceText.From(EmitRegistrar(executor), Encoding.UTF8));
        }

        context.AddSource(CoreFileName, SourceText.From(EmitCoreExtensions(executors), Encoding.UTF8));
    }

    /// <summary>域注册器产物 hintName：<c>BitableToolDomainRegistrar</c> → <c>FeishuToolDomainRegistrars/BitableToolDomainRegistrar.g.cs</c>。</summary>
    private static string RegistrarHintName(string registrarTypeName)
        => $"{RegistrarsOutputFolder}/{registrarTypeName}.g.cs";

    // ────────── 产物一：域注册器（每执行器一个文件） ──────────

    private static string EmitRegistrar(IGrouping<string, ToolHandlerBinding> executor)
    {
        var first = executor.First();
        var source = new StringBuilder();
        AppendHeader(source, RegistrarsNamespace);
        source.AppendLine("{");
        source.AppendLine($"    /// <summary>{first.ExecutorTypeName} 的域注册器（编译期按 [FeishuToolHandler] 聚合）。</summary>");
        source.AppendLine($"    {GeneratedCodeMarker.Attribute}");
        source.AppendLine($"    internal sealed class {first.RegistrarTypeName}({first.ExecutorType} executor, FeishuToolBinding binding) : IFeishuToolDomainRegistrar");
        source.AppendLine("    {");
        source.AppendLine($"        {GeneratedCodeMarker.Attribute}");
        source.AppendLine("        public void Register(FeishuToolRegistry registry)");
        source.AppendLine("        {");

        foreach (var binding in executor.OrderBy(static b => b.ToolName, StringComparer.Ordinal))
        {
            source.AppendLine($"            FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.{SchemaEmitter.BuildNameConstant(binding.ToolName)}, binding,");
            source.AppendLine($"                (args, ct) => executor.{binding.MethodName}(args, ct));");
        }

        source.AppendLine("        }");
        source.AppendLine("    }");
        source.AppendLine("}");
        return source.ToString();
    }

    // ────────── 产物二：DI 装配 ──────────

    private static string EmitCoreExtensions(IGrouping<string, ToolHandlerBinding>[] executors)
    {
        var source = new StringBuilder();
        AppendHeader(source, CoreNamespace, "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.DependencyInjection.Extensions");
        source.AppendLine("{");

        source.AppendLine("    /// <summary>逐执行器 DI 装配（软缺席语义由执行器构造器签名推导，见 ToolDependencyKind）。</summary>");
        source.AppendLine($"    {GeneratedCodeMarker.Attribute}");
        source.AppendLine("    internal static class FeishuToolsServiceCollectionCoreExtensions");
        source.AppendLine("    {");

        foreach (var executor in executors)
        {
            EmitCoreMethod(source, executor);
        }

        source.AppendLine("    }");
        source.AppendLine("}");
        return source.ToString();
    }

    private static void EmitCoreMethod(StringBuilder source, IGrouping<string, ToolHandlerBinding> executor)
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

        source.AppendLine();
        source.AppendLine($"        /// <summary>{first.ExecutorTypeName} 的执行器 + 域注册器装配（{executor.Count()} 枚工具）。</summary>");
        if (softIndexes.Length > 0)
        {
            source.AppendLine("        /// <remarks>软缺席："
                + string.Join(" / ", softIndexes.Select(i => dependencies[i].TypeName))
                + " 任一缺席 → 执行器解析为 null → 该域工具不进注册表（白名单期 fail-fast）。</remarks>");
        }

        source.AppendLine($"        {GeneratedCodeMarker.Attribute}");
        source.AppendLine($"        internal static IServiceCollection {first.CoreMethodName}(this IServiceCollection services)");
        source.AppendLine("        {");

        if (softIndexes.Length == 0)
        {
            // 无软缺席候选 → 执行器恒可构造（如依赖仅 IOptions<T> 的能力出处元工具）。
            source.AppendLine($"            services.TryAddSingleton(static sp => new {first.ExecutorType}({BuildCtorArguments(arguments)}));");
        }
        else
        {
            source.AppendLine("            services.TryAddSingleton(static sp =>");
            source.AppendLine("            {");
            foreach (var index in softIndexes)
            {
                source.AppendLine($"                var {softLocalByIndex[index]} = sp.GetService<{dependencies[index].TypeName}>();");
            }

            source.AppendLine("                return "
                + string.Join(" && ", softIndexes.Select(i => softLocalByIndex[i] + " is not null")));
            source.AppendLine($"                    ? new {first.ExecutorType}({BuildCtorArguments(arguments, "                        ", "                    ")})");
            source.AppendLine("                    : null!;");
            source.AppendLine("            });");
        }

        // 注册器登记：执行器缺席（工厂返回 null）时工厂返回 null → 该域工具不进注册表。
        // 经 internal 静态助手登记（**非** private 扩展——生成产物是独立类型，跨类不可见：R1 §4.5 的 CS0122 根因）。
        source.AppendLine($"            {RegistrarsNamespace}.FeishuToolDomainRegistrars.Add(services, static sp =>");
        source.AppendLine($"                sp.GetService<{first.ExecutorType}>() is {{ }} executor");
        source.AppendLine($"                    ? new {RegistrarsNamespace}.{first.RegistrarTypeName}(executor, sp.GetRequiredService<{CoreNamespace}.FeishuToolBinding>())");
        source.AppendLine("                    : null);");
        source.AppendLine("            return services;");
        source.AppendLine("        }");
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

        // 显式 LF：生成器内禁用 Environment（RS1035），且产物的换行形态与 golden 归一化口径一致。
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
    private static void AppendHeader(StringBuilder source, string ns, params string[] usings)
    {
        source.AppendLine("// <auto-generated> 由 FeishuToolSchemaGenerator 编译期产出，禁止手工修改 </auto-generated>");
        source.AppendLine("#nullable enable");
        source.AppendLine("#pragma warning disable CS1591 // 生成代码不逐一补 XML 注释");
        foreach (var @using in usings)
        {
            source.AppendLine($"using {@using};");
        }

        source.AppendLine($"namespace {ns}");
    }
}
