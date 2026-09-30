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
/// <item><c>FeishuToolArgs/{Tool}Args.g.cs</c> —— 参数解包器（每工具一个 <c>{Tool}Args</c> 类型
/// + <c>Unpack</c>，一类型一文件；取代执行器首部的逐参 <c>ToolArgs.*</c> 读取；只发射进
/// FeishuTools 程序集）；</item>
/// <item><c>FeishuToolDomainRegistrars/{Registrar}.g.cs</c> —— 域注册器（每执行器类一个文件）+
/// <c>FeishuToolsServiceCollectionCoreExtensions.g.cs</c>（逐执行器 DI 装配；只发射进 FeishuTools 程序集）。</item>
/// <item><c>FeishuCapabilityCatalog.g.cs</c> —— Tier R 能力目录聚合（<c>build_property.FeishuToolCatalog=true</c> 时）。</item>
/// </list>
/// <para>
/// <b>增量纪律（R3-04 更新）</b>：Tier C 的 <c>ScanTool</c> 在语法变换内读
/// <c>context.SemanticModel.Compilation</c>（用于源挂钩交叉校验）。这是一个<b>已知限制</b>：
/// 只编辑 <c>Source</c> 指向的 SDK 文件时，工具接口所在语法树不变 ⇒ 变换不重跑 ⇒ 缓存的
/// route/risk 可能陈旧。方案 B（性能优先）选择保留现状，以 <c>GeneratorDriver</c> 用例锁定行为
/// （见 <c>GeneratorIncrementalBehaviorTests</c>）。若实测证明陈旧真实发生，则需接入
/// <c>CompilationProvider</c> 把该产物降为编译级粒度（方案 A，代价是增量构建耗时恶化）。
/// </para>
/// <para>
/// <b>输出路径计数</b>（R3-04 修正）：实际 7 条输出路径——
/// <c>FeishuToolSchemas</c> / <c>FeishuToolArgs</c> / <c>FeishuToolDomainRegistrars</c> /
/// <c>FeishuToolGuidance</c> / <c>FeishuCapabilityCatalog</c> / <c>FeishuToolDiagnostics</c> /
/// <c>Guard&lt;T&gt;</c> 兜底层。守卫 <c>GeneratorOutputGuardContractGuards</c> 用
/// <c>HaveCountGreaterThanOrEqualTo(6)</c> 下界断言 + 逐条 <c>IsWrapped</c> 结构断言，不依赖该数字。
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

        context.RegisterSourceOutput(
            diagnostics,
            Guard<ImmutableArray<PendingDiagnostic>>("FeishuToolDiagnostics(工具扫描)", ReportPendingDiagnostics));

        // ── L1：执行器绑定扫描（[FeishuToolHandler] → 工具名 + 执行器构造签名）──
        // 与 Tier C 同层同纪律：执行器构造签名在 SemanticModel 内即可完全解析，
        // **不**引入 CompilationProvider（否则注册产物会退化为编译级粒度，每次编辑重跑）。
        var handlers = context.SyntaxProvider
            .CreateSyntaxProvider(IsHandlerCandidate, ScanHandler)
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        context.RegisterSourceOutput(
            handlers.SelectMany(static (result, _) => result.Diagnostics).Collect(),
            Guard<ImmutableArray<PendingDiagnostic>>("FeishuToolDiagnostics(执行器扫描)", ReportPendingDiagnostics));

        var handlerBindings = handlers.Collect();

        // ── L2/L4 + golden：Schema 与工具名契约表发射 ──
        var assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName);
        var golden = context.AdditionalTextsProvider
            .Where(static text => text.Path.Replace('\\', '/').EndsWith(SchemaEmitter.GoldenFileName, StringComparison.OrdinalIgnoreCase))
            .Collect();

        context.RegisterSourceOutput(
            models.Combine(assemblyName).Combine(golden),
            Guard<((ImmutableArray<ToolSchemaModel> Models, string? AssemblyName) Left, ImmutableArray<AdditionalText> Right)>(
                "FeishuToolSchemas", EmitToolSurface));

        // ── L2：参数解包器（FeishuToolArgs/{Tool}Args.g.cs，每类型一文件）──
        // 与 Schema/契约表同一 pass、同一模型集合（零新增扫描）；发射门槛为**名字契约所有者程序集**——
        // 产物消费 FeishuTools 的 internal ToolArgs，其他声明样例 [FeishuTool] 接口的工程（AI.Tests）
        // 若一并发射会因 ToolArgs 不可见而 CS0103。
        context.RegisterSourceOutput(
            models.Combine(assemblyName),
            Guard<(ImmutableArray<ToolSchemaModel> Left, string? Right)>("FeishuToolArgs", EmitToolArgs));

        // ── L2：域注册器 + DI 装配（FeishuToolDomainRegistrars/{Registrar}.g.cs 每执行器一文件
        // / FeishuToolsServiceCollectionCoreExtensions.g.cs）──
        // 同一 pass、同一模型集合 + 执行器绑定集合；owner 门槛同 FeishuToolNames/Contracts/Args。
        // 注意元组层级：models.Combine(assemblyName) 后再 Combine(handlerBindings)——
        // 程序集名在 input.Left.Right（R1 §5 曾把它误写为 input.Right，那是一处编译错误）。
        context.RegisterSourceOutput(
            models.Combine(assemblyName).Combine(handlerBindings),
            Guard<((ImmutableArray<ToolSchemaModel> Models, string? AssemblyName) Left, ImmutableArray<ScannedHandler> Right)>(
                "FeishuToolDomainRegistrars", EmitRegistrars));

        // ── WP6：域级 guidance 资产（Guidance/{domain}.md → FeishuToolGuidance.g.cs）──
        // 与工具面同一 pass、同一发射门槛：素材是 AdditionalFiles（与 golden 同机制），
        // 域 = 文件名，装配期按"已启用工具所属域"取用（见 FeishuGuidanceComposer）。
        var guidanceFiles = context.AdditionalTextsProvider
            .Where(static text => GuidanceEmitter.IsGuidanceFile(text.Path))
            .Collect();

        context.RegisterSourceOutput(
            assemblyName.Combine(guidanceFiles),
            Guard<(string? Left, ImmutableArray<AdditionalText> Right)>("FeishuToolGuidance", EmitGuidance));

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
            Guard<(Compilation? Left, ImmutableArray<ToolSchemaModel> Right)>("FeishuCapabilityCatalog", EmitCapabilityCatalog));
    }

    // ────────── 故障隔离（R2-03：6/6 输出路径统一兜底）──────────

    /// <summary>
    /// 输出路径统一异常兜底（R2-03）：把任一条 <c>RegisterSourceOutput</c> 回调内的未捕获异常
    /// 转为可定位的 <see cref="Diagnostics.MUDFT026"/>（Error + 零容忍），而不是无定位的 <c>CS8785</c>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么是包装器而不是在每个回调里写 try/catch</b>（根因 R-C）：行为用例的覆盖面 = 你想到的路径数，
    /// 结构包装的覆盖面 = 代码里的实际路径数。此前 <c>MUDFT026</c> 的"故障隔离"承诺只兑现 1/6
    /// （只有 <c>EmitToolSurface</c> 有兜底），另 5 条路径的异常退化为 <c>CS8785</c>——
    /// 而当时的验收方式是"对那一条路径注入人为异常"，因此漏项无声。
    /// 现在把"每条注册路径都必须经本包装器"做成结构断言（生成器测试侧的元守卫），
    /// <b>新增路径自动被覆盖</b>。
    /// </para>
    /// <para>
    /// <b><see cref="OperationCanceledException"/> 必须放行</b>：编译被取消不是生成器故障；
    /// 上报 <c>MUDFT026</c>（Error 级 + 零容忍）会把"用户取消构建"变成"构建失败"。
    /// </para>
    /// </remarks>
    /// <typeparam name="T">该路径的输入载荷类型（由管线决定）。</typeparam>
    /// <param name="product">产物名（诊断消息中用于定位是哪条路径失败）。</param>
    /// <param name="emit">实际发射逻辑。</param>
    /// <returns>带兜底的输出回调。</returns>
    private static Action<SourceProductionContext, T> Guard<T>(string product, Action<SourceProductionContext, T> emit)
        => (spc, input) =>
        {
            try
            {
                emit(spc, input);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Diagnostics.Report(spc, Diagnostics.MUDFT026, null, product, ex.GetType().Name, ex.Message);
            }
        };

    // ────────── 候选与扫描 ──────────

    private static bool IsToolCandidate(SyntaxNode node, System.Threading.CancellationToken _)
        => node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 };

    private static bool IsHandlerCandidate(SyntaxNode node, System.Threading.CancellationToken _)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    private static ScannedHandler? ScanHandler(
        GeneratorSyntaxContext context,
        System.Threading.CancellationToken cancellationToken)
        => ToolHandlerScanner.Scan(context, cancellationToken);

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

    /// <summary>Schema 与工具名契约表发射（管线适配层；异常兜底由 <see cref="Guard{T}"/> 承担）。</summary>
    private static void EmitToolSurface(
        SourceProductionContext context,
        ((ImmutableArray<ToolSchemaModel> Models, string? AssemblyName) Left, ImmutableArray<AdditionalText> Right) input)
        => EmitToolSurfaceCore(context, input.Left.Models, input.Left.AssemblyName, input.Right);

    /// <summary>参数解包器发射（管线适配层；异常兜底由 <see cref="Guard{T}"/> 承担）。</summary>
    private static void EmitToolArgs(
        SourceProductionContext context,
        (ImmutableArray<ToolSchemaModel> Left, string? Right) input)
        => ToolArgsEmitter.Emit(context, input.Left, input.Right);

    /// <summary>域注册器 + DI 装配发射（管线适配层；异常兜底由 <see cref="Guard{T}"/> 承担）。</summary>
    private static void EmitRegistrars(
        SourceProductionContext context,
        ((ImmutableArray<ToolSchemaModel> Models, string? AssemblyName) Left, ImmutableArray<ScannedHandler> Right) input)
        => ToolRegistrarEmitter.Emit(context, input.Left.Models, input.Left.AssemblyName, input.Right);

    /// <summary>域级 guidance 资产发射（管线适配层；异常兜底由 <see cref="Guard{T}"/> 承担）。</summary>
    private static void EmitGuidance(
        SourceProductionContext context,
        (string? Left, ImmutableArray<AdditionalText> Right) input)
        => GuidanceEmitter.Emit(context, input.Left, input.Right, context.CancellationToken);

    /// <summary>
    /// Tier R 能力目录发射（管线适配层；关闭开关时输入为常量 <see langword="null"/>，此处按"不产出"处理）。
    /// </summary>
    private static void EmitCapabilityCatalog(
        SourceProductionContext context,
        (Compilation? Left, ImmutableArray<ToolSchemaModel> Right) input)
    {
        if (input.Left is not null)
        {
            CapabilityCatalogEmitter.Emit(context, input.Left, input.Right);
        }
    }

    private static void EmitToolSurfaceCore(
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
