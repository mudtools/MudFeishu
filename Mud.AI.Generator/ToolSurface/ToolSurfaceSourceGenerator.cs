// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------
using System.Threading;
using Mud.AI.ToolSurface.Emit;
using Mud.AI.ToolSurface.Extraction;
using Mud.AI.ToolSurface.Schema;

namespace Mud.AI.ToolSurface;

/// <summary>
/// 工具面 Schema 生成引擎入口（设计文档 §5.3）：把 <c>[SdkToolProfile]</c> 剖面驱动的
/// 工具特性接口编译为「模型可调用工具」的全部编译期产物。
/// </summary>
/// <remarks>
/// <para><b>管道（L1 → L2 → L4，结构照搬上游 <c>FeishuToolSchemaGenerator</c>）</b>：</para>
/// <list type="number">
/// <item>L1 抽取（<see cref="ToolSurfaceScanner"/> / <see cref="ToolHandlerScanner"/>）：
/// 接口符号 + <c>Source</c> 源挂钩 → 值模型；</item>
/// <item>L2 渲染（<see cref="SchemaWriter"/> + Emit 家族）：条目 → 描述符 JSON 与全部产物；</item>
/// <item>L4 校验（<see cref="DescriptorValidator"/>）：结构/类型/跨字段一致 → 动态槽位诊断。</item>
/// </list>
/// <para>
/// <b>剖面扇出（§5.3 v2.1）</b>：一个编译可含 0/1/N 个 <c>ISdkToolProfile</c> 实现。
/// 剖面集合经 <c>CompilationProvider</c> 解析并按 <see cref="SdkToolProfileModel.Name"/> 排序；
/// 每条输出路径对每个 profile 各跑一遍，hintName 由 <c>{ProductPrefix}</c> 天然隔离。
/// 工具接口按「哪个剖面的 <c>ToolAttributeName/Namespace</c> 命中」归属唯一剖面，互不串味。
/// </para>
/// <para>
/// <b>空剖面短路（§5.3/§7.2 硬性前提）</b>：纯 HTTP 消费方（无 <c>ISdkToolProfile</c> 实现）下，
/// 候选谓词是 O(1) 语法判定，语义变换在 <see cref="ProfileDiscovery.ResolveProfiles"/> 返回空集合后
/// 即返回 <see langword="null"/>——不进入任何扫描/发射逻辑，0 工具诊断、0 工具 AddSource。
/// 注：<c>ISdkToolProfile</c> 定义于 Abstractions、所有消费方恒引用该程序集，剖面存在性判定
/// 无法借 <c>GetTypeByMetadataName</c> 早退；解析结果按 Compilation 缓存（见
/// <see cref="ProfileDiscovery"/>），全程序集类型枚举每编译至多支付一次、由 profiles 节点
/// 与全部变换共享。另：<c>IIncrementalGenerator.Initialize</c> 无法在注册前拿到编译，
/// 故「不注册任何 SyntaxProvider」的表述落地为「注册最廉价的语法谓词 + 语义变换零产物短路」；
/// §10 测试门禁断言的「语法树步进不进 ToolSurface（扫描/发射逻辑）」由此满足。
/// </para>
/// <para>
/// <b>故障隔离</b>：每条注册路径经 <see cref="Guard.WrapUnchecked{T}"/> 兜底；多剖面循环内
/// 再按 profile 逐个 try/catch 上报 <c>{prefix}026</c>——单剖面异常不阻断其余剖面。
/// SDKT002（缺槽剖面）路径无法拼 <c>{prefix}026</c>（缺 <c>DiagnosticPrefix</c> 正是其主题），
/// 由 <see cref="Guard.WrapUnchecked{T}"/> 退化为日志兜底。
/// </para>
/// </remarks>
[Generator]
public sealed class ToolSurfaceSourceGenerator : TransitiveCodeGenerator
{
    /// <summary>
    /// 工具扫描的故障注入钩子（仅供测试程序集经 <c>InternalsVisibleTo</c> 注入）：实参为剖面名，
    /// 返回非空异常即在扫描体起点抛出，用于驱动 <c>{prefix}026</c> 兜底路径。
    /// </summary>
    /// <remarks>
    /// <b>为什么是实例槽而不是进程级静态字段</b>：本钩子是「验证一条失败路径」的测试 seam，
    /// 而 xUnit 默认<b>并行执行测试类</b>。静态字段的作用域是整个测试进程 ⇒ 注入期内的并发测试
    /// 扫描会被一并打挂：产物静默缺席、诊断只剩一条 026（affordance 资产则从 028 退化为
    /// 「文件名不命中工具名」029），症状是与被测行为无关的用例偶发红（2026-10-14 windows-latest
    /// CI 实测：<c>Affordance_MissingSectionAndEmptySection_ReportStructureError</c> 取不到 028）。
    /// 实例槽 ⇒ 作用域收敛为「本生成器实例 ⇒ 本次 driver 运行 ⇒ 本测试」，跨测试串味在结构上不可达。
    /// </remarks>
    internal Func<string, Exception?>? ScanFaultInjectionForTests { get; set; }

    /// <summary>
    /// 执行器扫描的故障注入钩子（语义同 <see cref="ScanFaultInjectionForTests"/>，命中 <c>ScanHandler</c> 路径）。
    /// </summary>
    internal Func<string, Exception?>? HandlerFaultInjectionForTests { get; set; }

    /// <inheritdoc />
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Extraction ↔ Schema 层接缝注册（幂等赋值；工厂签名携带 profile，注册点与剖面无关——扇出安全）。
        ToolSurfaceSchemaResolver.Factory = static (compilation, profile) => new TypeSchemaResolver(compilation, profile);

        // 追踪名（§10「引擎增量」门禁的断言锚点）：
        // 与 HTTP 生成器同体例——无追踪名时 IncrementalStepRunReason 无法归属到具体步骤，
        // 「无关文件编辑 ⇒ Cached/Unchanged」只能靠整体结果反推，粒度不足以定位回归。
        // 剖面集合经 SdkToolProfileSet 值相等包装：ImmutableArray 的相等性是数组引用比较，
        // 直接投图会让「编译变了但剖面未变」（IDE 每次击键）把整条工具面扇出判为 Modified。
        var profiles = context.CompilationProvider
            .Select(static (compilation, _) => SdkToolProfileSet.Create(ProfileDiscovery.ResolveProfiles(compilation)))
            .WithTrackingName("ToolSurface_Profiles");
        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName);
        var additionalTexts = context.AdditionalTextsProvider.Collect();

        // ── L1：工具特性扫描（语法候选 → 剖面归属 → 值模型；空剖面在变换内短路）──
        // 故障注入钩子在 Initialize 读取一次并由变换闭包捕获：捕获的是**局部**（不捕获 this，
        // 保持变换的值语义/增量缓存纪律），且作用域收敛到本生成器实例 ⇒ 无跨测试串味。
        var scanFault = ScanFaultInjectionForTests;
        var scanned = context.SyntaxProvider
            .CreateSyntaxProvider(IsToolCandidate, (ctx, ct) => ScanTool(ctx, scanFault, ct))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!)
            .WithTrackingName("ToolSurface_ToolScan");

        // ── L1：执行器绑定扫描（handler 特性 → 工具名 + 执行器构造签名）──
        var handlerFault = HandlerFaultInjectionForTests;
        var handlers = context.SyntaxProvider
            .CreateSyntaxProvider(IsHandlerCandidate, (ctx, ct) => ScanHandler(ctx, handlerFault, ct))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!)
            .WithTrackingName("ToolSurface_HandlerScan");

        // ── 诊断出口（与模型同源一次扫描；PendingDiagnostic 自带所属剖面的描述符，混合流无需 factory）──
        context.RegisterSourceOutput(
            scanned.SelectMany(static (result, _) => result.Scan.Diagnostics).Collect(),
            Guard.WrapUnchecked<ImmutableArray<PendingDiagnostic>>("ToolSurfaceDiagnostics(工具扫描)", ReportPendingDiagnostics));

        context.RegisterSourceOutput(
            handlers.SelectMany(static (result, _) => result.Scan.Diagnostics).Collect(),
            Guard.WrapUnchecked<ImmutableArray<PendingDiagnostic>>("ToolSurfaceDiagnostics(执行器扫描)", ReportPendingDiagnostics));

        // ── SDKT002：必填槽缺失的剖面（静态描述符上报，并从一切扇出中排除）──
        context.RegisterSourceOutput(
            profiles,
            Guard.WrapUnchecked<SdkToolProfileSet>("ToolSurfaceProfiles(槽位守卫)", ReportInvalidProfiles));

        // ── 按剖面分组的模型/绑定流（元组元素全为值相等类型，增量缓存安全）──
        var modelsByProfile = scanned
            .Where(static result => result.Scan.Model is not null)
            .Select(static (result, _) => (result.Profile, Model: result.Scan.Model!))
            .Collect()
            .WithTrackingName("ToolSurface_ModelsByProfile");

        var handlersByProfile = handlers
            .Select(static (result, _) => (result.Profile, result.Scan))
            .Collect()
            .WithTrackingName("ToolSurface_HandlersByProfile");

        // ── L2/L4 + golden：Schema 常量 / 名字契约表 / 类型化契约表 ──
        // 注：WrapUnchecked 的 T 必须显式指定——方法组实参不参与泛型推断（CS0411）。
        context.RegisterSourceOutput(
            modelsByProfile.Combine(assemblyName).Combine(profiles).Combine(additionalTexts),
            Guard.WrapUnchecked<(((ImmutableArray<(SdkToolProfileModel, ToolSchemaModel)>, string?), SdkToolProfileSet), ImmutableArray<AdditionalText>)>(
                "ToolSurfaceSchemas", EmitToolSurface));

        // ── L2：参数解包器（{P}Args/{Tool}Args.g.cs，每类型一文件）──
        context.RegisterSourceOutput(
            modelsByProfile.Combine(assemblyName).Combine(profiles),
            Guard.WrapUnchecked<((ImmutableArray<(SdkToolProfileModel, ToolSchemaModel)>, string?), SdkToolProfileSet)>(
                "ToolSurfaceArgs", EmitToolArgs));

        // ── L2：域注册器 + DI 装配 ──
        context.RegisterSourceOutput(
            modelsByProfile.Combine(handlersByProfile).Combine(assemblyName).Combine(profiles),
            Guard.WrapUnchecked<(((ImmutableArray<(SdkToolProfileModel, ToolSchemaModel)>, ImmutableArray<(SdkToolProfileModel, ScannedHandler)>), string?), SdkToolProfileSet)>(
                "ToolSurfaceDomainRegistrars", EmitRegistrars));

        // ── 域级 guidance 资产（{GuidanceDirectory}{domain}.md → {P}Guidance.g.cs）──
        context.RegisterSourceOutput(
            assemblyName.Combine(profiles).Combine(additionalTexts),
            Guard.WrapUnchecked<((string?, SdkToolProfileSet), ImmutableArray<AdditionalText>)>(
                "ToolSurfaceGuidance", EmitGuidance));

        // ── Tier R：能力目录（开关 = build_property.{CapabilityCatalogPropertyName}，剖面槽动态键）──
        // 动态键经拼接读取：组件侧 props 不预注册消费方属性（§5.3 v2.1 消费方注册义务）。
        var catalogFlags = profiles
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, _) =>
            {
                var flags = ImmutableArray.CreateBuilder<bool>();
                foreach (var profile in pair.Left.Items)
                {
                    var key = profile.CapabilityCatalogPropertyName;
                    flags.Add(key.Length > 0
                        && pair.Right.GlobalOptions.TryGetValue("build_property." + key, out var value)
                        && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
                }

                return flags.ToImmutable();
            });

        // 全部关闭时投影为常量 null（照搬上游 R4-9 增量纪律）——增量缓存据此判定「无变化」，
        // 下游不会随每次编辑重跑；任一剖面开启时才让 Compilation 进入输入。
        var catalogCompilation = context.CompilationProvider
            .Combine(catalogFlags)
            .Select(static (pair, _) => pair.Right.Any(static enabled => enabled) ? pair.Left : null);

        context.RegisterSourceOutput(
            catalogCompilation.Combine(modelsByProfile).Combine(profiles).Combine(catalogFlags),
            Guard.WrapUnchecked<(((Compilation?, ImmutableArray<(SdkToolProfileModel, ToolSchemaModel)>), SdkToolProfileSet), ImmutableArray<bool>)>(
                "ToolSurfaceCapabilityCatalog", EmitCapabilityCatalog));
    }

    // ────────── 候选谓词与扫描变换 ──────────

    private static bool IsToolCandidate(SyntaxNode node, CancellationToken _)
        => node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 };

    private static bool IsHandlerCandidate(SyntaxNode node, CancellationToken _)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

    /// <summary>
    /// 工具接口扫描：解析剖面 → 找到「工具特性命中」的唯一剖面 → 扫描。
    /// 空剖面 / 无命中 / 缺槽剖面（SDKT002 已接管）一律返回 <see langword="null"/>。
    /// </summary>
    /// <param name="context">语法上下文。</param>
    /// <param name="faultInjection">故障注入钩子（实例级；见 <see cref="ScanFaultInjectionForTests"/>）。</param>
    /// <param name="cancellationToken">取消令牌（CA1068：须为末位参数）。</param>
    private static ProfiledScan? ScanTool(
        GeneratorSyntaxContext context,
        Func<string, Exception?>? faultInjection,
        CancellationToken cancellationToken)
    {
        // P2-2：符号与剖面归属提升到 try 外——catch 据此把可归属异常折算为 {prefix}026 随流上报。
        INamedTypeSymbol? symbol = null;
        SdkToolProfileModel? attributedProfile = null;
        try
        {
            if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol resolved)
            {
                return null;
            }

            symbol = resolved;
            var compilation = context.SemanticModel.Compilation;
            var profiles = ProfileDiscovery.ResolveProfiles(compilation);
            if (profiles.IsDefaultOrEmpty)
            {
                // 空剖面短路：不进入任何扫描/发射逻辑（解析结果按 Compilation 缓存，重复调用零枚举成本）。
                return null;
            }

            foreach (var profile in profiles)
            {
                if (profile.MissingRequiredSlots.Length > 0)
                {
                    continue;
                }

                attributedProfile = profile;
                if (Extractors.GetToolAttribute(symbol, profile) is null)
                {
                    continue;
                }

                return new ProfiledScan(
                    profile,
                    ToolSurfaceScanner.Scan(symbol, compilation, profile, ToolSurfaceDiagnostics.For(profile), faultInjection));
            }

            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            GeneratorDebugLogger.LogError(nameof(ScanTool), ex);
            // P2-2：异常可归属剖面时，构造「引擎内部错误」{prefix}026 随流上报——否则产物静默缺席而构建全绿。
            // 归属未知（符号/剖面解析阶段抛出）时无法拼动态 ID，退化为仅日志（profile 必填槽保证前缀非空，
            // 故 attributedProfile 非 null 即可安全拼 {prefix}026；此时 symbol 也必已解析，"!" 断言成立）。
            if (attributedProfile is null)
            {
                return null;
            }

            return new ProfiledScan(
                attributedProfile,
                ScannedTool.Faulted(
                    symbol!.Name,
                    PendingDiagnostic.Create(
                        ToolSurfaceDiagnostics.For(attributedProfile)[ToolSurfaceDiagnostics.SlotGeneratorInternalError],
                        attributedProfile.Name + ":" + nameof(ScanTool),
                        ex.GetType().Name,
                        ex.Message)));
        }
    }

    /// <summary>执行器方法扫描（按 handler 特性槽归属剖面；同 ScanTool 的短路纪律）。</summary>
    /// <param name="context">语法上下文。</param>
    /// <param name="faultInjection">故障注入钩子（实例级；见 <see cref="HandlerFaultInjectionForTests"/>）。</param>
    /// <param name="cancellationToken">取消令牌（CA1068：须为末位参数）。</param>
    private static ProfiledHandler? ScanHandler(
        GeneratorSyntaxContext context,
        Func<string, Exception?>? faultInjection,
        CancellationToken cancellationToken)
    {
        // P2-2：剖面归属提升到 try 外（同 ScanTool）。
        SdkToolProfileModel? attributedProfile = null;
        try
        {
            var compilation = context.SemanticModel.Compilation;
            var profiles = ProfileDiscovery.ResolveProfiles(compilation);
            if (profiles.IsDefaultOrEmpty)
            {
                return null;
            }

            foreach (var profile in profiles)
            {
                if (profile.MissingRequiredSlots.Length > 0)
                {
                    continue;
                }

                attributedProfile = profile;
                var scan = ToolHandlerScanner.Scan(
                    context,
                    profile,
                    ToolSurfaceDiagnostics.For(profile),
                    faultInjection,
                    cancellationToken);
                if (scan is not null)
                {
                    return new ProfiledHandler(profile, scan);
                }
            }

            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            GeneratorDebugLogger.LogError(nameof(ScanHandler), ex);
            // P2-2：异常可归属剖面时折算为 {prefix}026 随流上报；归属未知时退化为仅日志。
            if (attributedProfile is null)
            {
                return null;
            }

            return new ProfiledHandler(
                attributedProfile,
                ScannedHandler.Faulted(
                    null,
                    PendingDiagnostic.Create(
                        ToolSurfaceDiagnostics.For(attributedProfile)[ToolSurfaceDiagnostics.SlotGeneratorInternalError],
                        attributedProfile.Name + ":" + nameof(ScanHandler),
                        ex.GetType().Name,
                        ex.Message)));
        }
    }

    // ────────── 诊断出口 ──────────

    private static void ReportPendingDiagnostics(SourceProductionContext context, ImmutableArray<PendingDiagnostic> diagnostics)
    {
        foreach (var pending in diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(pending.Descriptor, Location.None, pending.Arguments));
        }
    }

    private static void ReportInvalidProfiles(SourceProductionContext context, SdkToolProfileSet profiles)
    {
        foreach (var profile in profiles.Items)
        {
            if (profile.MissingRequiredSlots.Length == 0)
            {
                continue;
            }

            var display = profile.Name.Length == 0 ? "(未命名剖面)" : profile.Name;
            context.ReportDiagnostic(Diagnostic.Create(
                ProfileContractDiagnostics.SdkToolProfileMissingRequiredSlots,
                Location.None,
                display,
                string.Join(", ", profile.MissingRequiredSlots)));
        }
    }

    // ────────── 输出路径（每条 = Guard.WrapUnchecked 外层 + ForEachProfile 逐剖面 026 隔离）──────────

    private static void EmitToolSurface(
        SourceProductionContext context,
        (((ImmutableArray<(SdkToolProfileModel Profile, ToolSchemaModel Model)> Models, string? AssemblyName) WithName,
          SdkToolProfileSet Profiles) Grouped,
         ImmutableArray<AdditionalText> Texts) input)
        => ForEachProfile(
            context,
            "ToolSurfaceSchemas",
            input.Grouped.Profiles,
            (input.Grouped.WithName.Models, input.Grouped.WithName.AssemblyName, input.Texts),
            static (ctx, profile, factory, data) =>
            {
                var models = ModelsFor(data.Models, profile);

                // per-tool affordance（F-7）：解析 / 校验 / 发射同 pass，零新增注册、零新增遍历
                // （AdditionalTexts 已在输入元组里）。**必须在 models.IsEmpty 早退之前**：
                // 孤儿资产（文件名不命中任何工具名）的诊断不依赖"本剖面是否存在工具"。
                var affordance = AffordanceAssets.Collect(
                    data.Texts.Where(text => AffordanceAssets.IsAffordanceFile(text.Path, profile)).ToImmutableArray(),
                    models,
                    profile,
                    factory,
                    ctx.CancellationToken);

                foreach (var pending in affordance.Diagnostics)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(pending.Descriptor, Location.None, pending.Arguments));
                }

                if (models.IsEmpty)
                {
                    return;
                }

                // L4 校验：结构 / 类型一致 / 跨字段一致（含工具名唯一 → 槽位 003、派生常量名冲突 → 槽位 027）。
                foreach (var result in DescriptorValidator.ValidateAll(
                             models.Select(static m => m.Entry), profile, factory, SchemaEmitter.BuildNameConstant))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(result.Descriptor, Location.None, result.Arguments));
                }

                ReportOutputSchemaTruncations(ctx, models, factory);

                var goldenText = ReadGolden(data.Texts, profile, ctx.CancellationToken);
                var drift = SchemaEmitter.Emit(
                    ctx,
                    models,
                    profile,
                    data.AssemblyName,
                    goldenText,
                    ToLookup(affordance.Entries));
                if (drift is not null)
                {
                    // 槽位 014 上报点：描述符静默漂移（golden 快照不一致）。
                    factory.Report(ctx, ToolSurfaceDiagnostics.SlotGoldenDrift, null, drift, GoldenUpdateHint(profile));
                }
            });

    private static void EmitToolArgs(
        SourceProductionContext context,
        ((ImmutableArray<(SdkToolProfileModel Profile, ToolSchemaModel Model)> Models, string? AssemblyName) WithName,
         SdkToolProfileSet Profiles) input)
        => ForEachProfile(
            context,
            "ToolSurfaceArgs",
            input.Profiles,
            (input.WithName.Models, input.WithName.AssemblyName),
            static (ctx, profile, factory, data) =>
                ToolArgsEmitter.Emit(ctx, ModelsFor(data.Models, profile), profile, data.AssemblyName, factory));

    private static void EmitRegistrars(
        SourceProductionContext context,
        (((ImmutableArray<(SdkToolProfileModel Profile, ToolSchemaModel Model)> Models,
           ImmutableArray<(SdkToolProfileModel Profile, ScannedHandler Handler)> Handlers) WithBindings,
          string? AssemblyName) Outer,
         SdkToolProfileSet Profiles) input)
        => ForEachProfile(
            context,
            "ToolSurfaceDomainRegistrars",
            input.Profiles,
            (input.Outer.WithBindings.Models, input.Outer.WithBindings.Handlers, input.Outer.AssemblyName),
            static (ctx, profile, factory, data) =>
            {
                var handlers = ImmutableArray.CreateBuilder<ScannedHandler>();
                foreach (var (p, handler) in data.Handlers)
                {
                    if (p.Equals(profile))
                    {
                        handlers.Add(handler);
                    }
                }

                ToolRegistrarEmitter.Emit(ctx, ModelsFor(data.Models, profile), profile, data.AssemblyName, handlers.ToImmutable(), factory);
            });

    private static void EmitGuidance(
        SourceProductionContext context,
        ((string? AssemblyName, SdkToolProfileSet Profiles) WithName,
         ImmutableArray<AdditionalText> Texts) input)
        => ForEachProfile(
            context,
            "ToolSurfaceGuidance",
            input.WithName.Profiles,
            (input.WithName.AssemblyName, input.Texts),
            static (ctx, profile, _, data) =>
            {
                var files = data.Texts
                    .Where(text => GuidanceEmitter.IsGuidanceFile(text.Path, profile))
                    .ToImmutableArray();
                GuidanceEmitter.Emit(ctx, profile, data.AssemblyName, files, ctx.CancellationToken);
            });

    private static void EmitCapabilityCatalog(
        SourceProductionContext context,
        (((Compilation? Compilation, ImmutableArray<(SdkToolProfileModel Profile, ToolSchemaModel Model)> Models) WithModels,
          SdkToolProfileSet Profiles) Grouped,
         ImmutableArray<bool> Flags) input)
    {
        // 全剖面关闭时上游投影为 null，此处直接返回（增量缓存已判定「无变化」）。
        if (input.Grouped.WithModels.Compilation is not { } compilation)
        {
            return;
        }

        var profiles = input.Grouped.Profiles;
        for (var i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            if (profile.MissingRequiredSlots.Length > 0)
            {
                continue;
            }

            var factory = ToolSurfaceDiagnostics.For(profile);
            try
            {
                if (!input.Flags[i])
                {
                    // 该剖面开关关闭：不产出（与 flags 的「常量 null」投影同语义）。
                    continue;
                }

                CapabilityCatalogEmitter.Emit(
                    context,
                    compilation,
                    ModelsFor(input.Grouped.WithModels.Models, profile),
                    profile,
                    catalogEnabled: true,
                    factory);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                factory.Report(
                    context,
                    ToolSurfaceDiagnostics.SlotGeneratorInternalError,
                    null,
                    profile.Name + ":ToolSurfaceCapabilityCatalog",
                    ex.GetType().Name,
                    ex.Message);
            }
        }
    }

    // ────────── 管线辅助 ──────────

    /// <summary>逐剖面执行发射回调：单剖面异常上报该剖面的 <c>{prefix}026</c>，不阻断其余剖面。</summary>
    private static void ForEachProfile<T>(
        SourceProductionContext context,
        string product,
        SdkToolProfileSet profiles,
        T data,
        Action<SourceProductionContext, SdkToolProfileModel, ToolSurfaceDiagnostics.Factory, T> action)
    {
        foreach (var profile in profiles.Items)
        {
            if (profile.MissingRequiredSlots.Length > 0)
            {
                continue;
            }

            var factory = ToolSurfaceDiagnostics.For(profile);
            try
            {
                action(context, profile, factory, data);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                factory.Report(
                    context,
                    ToolSurfaceDiagnostics.SlotGeneratorInternalError,
                    null,
                    profile.Name + ":" + product,
                    ex.GetType().Name,
                    ex.Message);
            }
        }
    }

    /// <summary>
    /// 把 affordance 条目按工具名归为查找表（供 <see cref="SchemaEmitter"/> 逐工具取用）。
    /// </summary>
    /// <remarks>
    /// 空集合返回空字典（非 <see langword="null"/>）：<c>SchemaWriter</c> 只需"取不到即不发射"，
    /// 不需要区分"未启用本特性"与"启用了但没写资产"——两者对描述符的影响完全一致（都不发射）。
    /// </remarks>
    private static IReadOnlyDictionary<string, AffordanceEntry> ToLookup(ImmutableArray<AffordanceEntry> entries)
    {
        var lookup = new Dictionary<string, AffordanceEntry>(entries.Length, StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            lookup[entry.ToolName] = entry;
        }

        return lookup;
    }

    private static ImmutableArray<ToolSchemaModel> ModelsFor(
        ImmutableArray<(SdkToolProfileModel Profile, ToolSchemaModel Model)> all,
        SdkToolProfileModel profile)
    {
        var builder = ImmutableArray.CreateBuilder<ToolSchemaModel>();
        foreach (var (p, model) in all)
        {
            if (p.Equals(profile))
            {
                builder.Add(model);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// 槽位 014 的「重新固化」提示（消息第二实参）。
    /// </summary>
    /// <remarks>
    /// <c>GoldenUpdatePropertyName</c> 是<b>非必填槽</b>：未声明时直接拼串会产出 <c>-p:=true</c> 这种
    /// 不可执行的提示文本，故缺槽时改为明确话术，避免用户照抄一个空属性名。
    /// </remarks>
    private static string GoldenUpdateHint(SdkToolProfileModel profile)
        => profile.GoldenUpdatePropertyName.Length > 0
            ? "-p:" + profile.GoldenUpdatePropertyName + "=true"
            : "剖面未声明 GoldenUpdatePropertyName 槽，请手动更新快照文件";

    /// <summary>读取该剖面的 golden 快照（<see cref="SchemaEmitter.IsGoldenFile"/> 全名匹配，§5.2-3）。</summary>
    private static string? ReadGolden(ImmutableArray<AdditionalText> texts, SdkToolProfileModel profile, CancellationToken cancellationToken)
    {
        foreach (var text in texts)
        {
            if (SchemaEmitter.IsGoldenFile(text.Path, profile) && text.GetText(cancellationToken) is { } sourceText)
            {
                return sourceText.ToString();
            }
        }

        return null;
    }

    /// <summary>
    /// 汇总输出 Schema 的截断情况并上报<b>单条</b>槽位 009（照搬上游 AT-B14 聚合纪律：
    /// 逐处上报会被 SDK 中大量深层 DTO 淹没；截断本身不是缺陷，但必须构建期可见）。
    /// </summary>
    private static void ReportOutputSchemaTruncations(
        SourceProductionContext context,
        ImmutableArray<ToolSchemaModel> models,
        ToolSurfaceDiagnostics.Factory factory)
    {
        var affected = new List<string>();
        foreach (var model in models)
        {
            if (model.Entry.OutputSchemaTruncations.Count == 0)
            {
                continue;
            }

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

        factory.Report(
            context,
            ToolSurfaceDiagnostics.SlotOutputSchemaTruncation,
            null,
            $"{affected.Count} 个工具的输出 Schema 被截断（深度超限或循环引用）",
            listed);
    }

    /// <summary>扫描结果 + 归属剖面（值相等：剖面与扫描产物均已实现 <see cref="IEquatable{T}"/>）。</summary>
    private sealed class ProfiledScan : IEquatable<ProfiledScan?>
    {
        public ProfiledScan(SdkToolProfileModel profile, ScannedTool scan)
        {
            Profile = profile;
            Scan = scan;
        }

        public SdkToolProfileModel Profile { get; }
        public ScannedTool Scan { get; }

        public bool Equals(ProfiledScan? other)
            => other is not null && Profile.Equals(other.Profile) && Scan.Equals(other.Scan);

        public override bool Equals(object? obj) => Equals(obj as ProfiledScan);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Profile.GetHashCode() * 31) + Scan.GetHashCode();
            }
        }
    }

    /// <summary>执行器扫描结果 + 归属剖面（同 <see cref="ProfiledScan"/> 纪律）。</summary>
    private sealed class ProfiledHandler : IEquatable<ProfiledHandler?>
    {
        public ProfiledHandler(SdkToolProfileModel profile, ScannedHandler scan)
        {
            Profile = profile;
            Scan = scan;
        }

        public SdkToolProfileModel Profile { get; }
        public ScannedHandler Scan { get; }

        public bool Equals(ProfiledHandler? other)
            => other is not null && Profile.Equals(other.Profile) && Scan.Equals(other.Scan);

        public override bool Equals(object? obj) => Equals(obj as ProfiledHandler);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Profile.GetHashCode() * 31) + Scan.GetHashCode();
            }
        }
    }
}