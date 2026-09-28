// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Mud.Feishu.AI.Tools.Emit;
using Mud.Feishu.AI.Tools.Extraction;
using Mud.Feishu.AI.Tools.Schema;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// Mud.Feishu 工具面源生成器：把 <c>[FeishuTool]</c> 接口编译为「模型可调用工具」的全部编译期产物。
/// </summary>
/// <remarks>
/// <para><b>管道（L1 → L2 → L4）</b>：</para>
/// <list type="number">
/// <item>L1 抽取（<see cref="CuratedToolScanner"/>）：接口符号 + <c>Source</c> 源挂钩 → <see cref="CapabilityEntry"/>；</item>
/// <item>L2 渲染（<see cref="SchemaWriter"/>）：条目 → 描述符 JSON（纯参数 Schema + <c>x-feishu</c> 元数据）；</item>
/// <item>L4 校验（<see cref="DescriptorValidator"/>）：结构/类型/跨字段一致 → 诊断。</item>
/// </list>
/// <para><b>产物</b>：</para>
/// <list type="bullet">
/// <item><c>FeishuToolSchemas.g.cs</c> —— Schema 常量 + 注册表快照（已决策⑤）；</item>
/// <item><c>FeishuToolNames.g.cs</c> —— 工具名契约表（D2 单一真相源，取代手写常量表）；</item>
/// <item><c>FeishuToolContracts.g.cs</c> —— 类型化契约表（WP2 / R-B：risk/identity/scopes/isWrite/
/// source 的类型化出口，消费方零运行期解析；只发射进 FeishuTools 程序集）；</item>
/// <item><c>FeishuToolGuidance.g.cs</c> —— 域级 guidance 资产（WP6：<c>Guidance/{domain}.md</c>
/// → 编译期字典，装配期按"已启用工具所属域"注入指令；只发射进 FeishuTools 程序集）；</item>
/// <item><c>FeishuToolArgs.g.cs</c> —— 参数解包器（每工具一个 <c>{Tool}Args</c> 类型 + <c>Unpack</c>，
/// 取代执行器首部的逐参 <c>ToolArgs.*</c> 读取；只发射进 FeishuTools 程序集）；</item>
/// <item><c>FeishuCapabilityCatalog.g.cs</c> —— Tier R 能力目录聚合（<c>build_property.FeishuToolCatalog=true</c> 时）。</item>
/// </list>
/// <para>
/// <b>增量纪律</b>：Tier C 全程在 <c>SemanticModel</c> 上完成（不引入 <c>CompilationProvider</c>）——
/// 管线输出是纯值模型，源码不变则不重发；Tier R 需要全程序集扫描，故显式 opt-in，避免拖累
/// 每个引用本生成器的工程。
/// </para>
/// </remarks>
[Generator]
public sealed class FeishuToolSchemaGenerator : IIncrementalGenerator
{
    /// <summary>Tier R 能力目录的开启开关（MSBuild 属性名）。</summary>
    public const string CatalogPropertyName = "build_property.FeishuToolCatalog";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ── L1：Tier C 扫描（语法候选 → 符号校验 → 值模型）──
        var scanned = context.SyntaxProvider
            .CreateSyntaxProvider(IsToolCandidate, ScanTool)
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        var models = scanned
            .Where(static result => result.Model is not null)
            .Select(static (result, _) => result.Model!)
            .Collect();

        // ── L4 诊断出口（与扫描同源，不额外解析符号）──
        var diagnostics = scanned
            .SelectMany(static (result, _) => result.Diagnostics)
            .Collect();

        context.RegisterSourceOutput(diagnostics, ReportPendingDiagnostics);

        // ── L2/L4 + golden：Schema 与工具名契约表发射 ──
        var assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName);
        var golden = context.AdditionalTextsProvider
            .Where(static text => text.Path.Replace('\\', '/').EndsWith(SchemaEmitter.GoldenFileName, StringComparison.OrdinalIgnoreCase))
            .Collect();

        context.RegisterSourceOutput(
            models.Combine(assemblyName).Combine(golden),
            static (spc, input) => EmitToolSurface(spc, input.Left.Left, input.Left.Right, input.Right));

        // ── L2：参数解包器（FeishuToolArgs.g.cs）──
        // 与 Schema/契约表同一 pass、同一模型集合（零新增扫描）；发射门槛为**名字契约所有者程序集**——
        // 产物消费 FeishuTools 的 internal ToolArgs，其他声明样例 [FeishuTool] 接口的工程（AI.Tests）
        // 若一并发射会因 ToolArgs 不可见而 CS0103。
        context.RegisterSourceOutput(
            models.Combine(assemblyName),
            static (spc, input) => ToolArgsEmitter.Emit(spc, input.Left, input.Right));

        // ── WP6：域级 guidance 资产（Guidance/{domain}.md → FeishuToolGuidance.g.cs）──
        // 与工具面同一 pass、同一发射门槛：素材是 AdditionalFiles（与 golden 同机制），
        // 域 = 文件名，装配期按"已启用工具所属域"取用（见 FeishuGuidanceComposer）。
        var guidanceFiles = context.AdditionalTextsProvider
            .Where(static text => GuidanceEmitter.IsGuidanceFile(text.Path))
            .Collect();

        context.RegisterSourceOutput(
            assemblyName.Combine(guidanceFiles),
            static (spc, input) => GuidanceEmitter.Emit(spc, input.Left, input.Right, spc.CancellationToken));

        // ── Tier R：能力目录（聚合；显式 opt-in）──
        var catalogEnabled = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
            provider.GlobalOptions.TryGetValue(CatalogPropertyName, out var value)
            && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        // 关闭时投影为常量 null——增量缓存据此判定"无变化"，下游不会随每次编辑重跑。
        var catalogInput = context.CompilationProvider
            .Combine(catalogEnabled)
            .Select(static (pair, _) => pair.Right ? pair.Left : null)
            .Combine(models);

        context.RegisterSourceOutput(
            catalogInput,
            static (spc, pair) =>
            {
                if (pair.Left is not null)
                {
                    CapabilityCatalogEmitter.Emit(spc, pair.Left, pair.Right);
                }
            });
    }

    // ────────── 候选与扫描 ──────────

    private static bool IsToolCandidate(SyntaxNode node, System.Threading.CancellationToken _)
        => node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 };

    private static ScannedTool? ScanTool(GeneratorSyntaxContext context, System.Threading.CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        // 非 [FeishuTool] 接口直接跳过（避免把无关接口变成诊断）。
        return Extractors.GetFeishuToolAttribute(symbol) is null
            ? null
            : CuratedToolScanner.Scan(symbol, context.SemanticModel.Compilation);
    }

    // ────────── 输出 ──────────

    private static void ReportPendingDiagnostics(
        SourceProductionContext context,
        ImmutableArray<PendingDiagnostic> diagnostics)
    {
        foreach (var pending in diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(pending.Descriptor, Location.None, pending.Arguments));
        }
    }

    private static void EmitToolSurface(
        SourceProductionContext context,
        ImmutableArray<ToolSchemaModel> models,
        string? assemblyName,
        ImmutableArray<AdditionalText> goldenTexts)
    {
        if (models.IsEmpty)
        {
            return;
        }

        // L4 校验：结构 / 类型一致 / 跨字段一致（含工具名唯一性 → MUDFT003）。
        foreach (var result in DescriptorValidator.ValidateAll(models.Select(static m => m.Entry)))
        {
            context.ReportDiagnostic(Diagnostic.Create(result.Descriptor, Location.None, result.Arguments));
        }

        // MUDFT009 上报点（AT-B14 接线）：输出 Schema 被截断（深度超限 / 循环引用）。
        // **聚合为单条**——SDK 中层级超过上限的 DTO 数量可观，逐处上报会产生成千条警告淹没构建输出
        // （与 CapabilityCatalogEmitter 的 MUDFT018 同一体例）。
        ReportOutputSchemaTruncations(context, models);

        var goldenText = ReadGolden(goldenTexts, context.CancellationToken);
        var drift = SchemaEmitter.Emit(context, models, assemblyName, goldenText);
        if (drift is not null)
        {
            // MUDFT014 上报点：描述符静默漂移（golden 快照不一致）。
            Diagnostics.Report(context, Diagnostics.MUDFT014, null, drift);
        }
    }

    /// <summary>
    /// 汇总输出 Schema 的截断情况并上报<b>单条</b> <c>MUDFT009</c>（AT-B14）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 截断本身<b>不是缺陷</b>（递归深度上限是防 Schema 爆炸的有意设计），但它此前是<b>完全静默</b>的：
    /// 模型看到的 <c>output_schema</c> 少了一层，没有任何人知道。本条把它变成"构建期可见的一个数字"。
    /// </para>
    /// <para>
    /// <b>严重级为 Warning 且不纳入 <c>ZeroToleranceIds</c></b>：当前 SDK 必然存在深层 DTO
    /// （例如 <c>docx.get_document_blocks</c> 的返回结构天然超过 4 层），纳入零容忍会立即阻断构建。
    /// </para>
    /// </remarks>
    private static void ReportOutputSchemaTruncations(
        SourceProductionContext context,
        ImmutableArray<ToolSchemaModel> models)
    {
        var affected = new List<string>();
        foreach (var model in models)
        {
            if (model.Entry.OutputSchemaTruncations.Count == 0)
            {
                continue;
            }

            // 只列工具名（+ 截断点数），不列逐条路径：一处截断的路径样本可达数百字符，
            // 全量展开会让一条 Warning 变成几千字的噪声（路径样本仍保留在 CapabilityEntry 上供测试读取）。
            affected.Add($"{model.Entry.ToolName}(×{model.Entry.OutputSchemaTruncations.Count})");
        }

        if (affected.Count == 0)
        {
            return;
        }

        const int MaxListedTools = 5;
        var listed = affected.Count <= MaxListedTools
            ? string.Join(" | ", affected)
            : string.Join(" | ", affected.Take(MaxListedTools)) + $" | …另有 {affected.Count - MaxListedTools} 个工具";

        Diagnostics.Report(
            context,
            Diagnostics.MUDFT009,
            null,
            $"{affected.Count} 个工具的输出 Schema 被截断（深度超限或循环引用）",
            listed);
    }

    private static string? ReadGolden(ImmutableArray<AdditionalText> texts, System.Threading.CancellationToken cancellationToken)
    {
        foreach (var text in texts)
        {
            if (text.GetText(cancellationToken) is { } sourceText)
            {
                return sourceText.ToString();
            }
        }

        return null;
    }
}
