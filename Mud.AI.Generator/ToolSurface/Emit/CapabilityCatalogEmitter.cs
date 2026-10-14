// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Mud.AI.ToolSurface.Extraction;
using Mud.AI.ToolSurface.Schema;

namespace Mud.AI.ToolSurface.Emit;

/// <summary>
/// Tier R 能力目录发射器（<b>可选</b>，由 <c>build_property.{CapabilityCatalogPropertyName}=true</c> 开启）：
/// 遍历 SDK 根程序集的全部接口，产出<b>聚合</b>能力目录。
/// </summary>
/// <remarks>
/// <para>
/// 移植自上游 <c>Mud.Feishu.AI.Tools.Emit.CapabilityCatalogEmitter</c>（设计文档 §5.1 第 7 条输出路径）。泛化点：
/// <list type="bullet">
/// <item>SDK 程序集定位：上游硬编码 <c>"Mud.Feishu"</c> → <see cref="SdkToolProfileModel.SdkNamespaceRoot"/>
/// 槽（§4.2 第 2 项，程序集名与命名空间根同串）；</item>
/// <item>接口扫描前缀 <c>StartsWith("IFeishu")</c> → <see cref="SdkToolProfileModel.SdkInterfacePrefix"/>
/// （§4.2 第 15 项）；</item>
/// <item>接口名解析（domain 轴）→ <see cref="InterfaceIdentityParser.TryParse"/>
/// （<c>InterfaceNameRegex</c> 槽，替代上游 <c>Extractors.TryParseSdkInterfaceName</c>）；</item>
/// <item>开关属性名 <c>FeishuToolCatalog</c> → <see cref="SdkToolProfileModel.CapabilityCatalogPropertyName"/>
/// （§4.2；<c>build_property</c> 读取由<b>入口生成器</b>完成，本发射器只接收 <c>catalogEnabled</c> bool——
/// 上游即由入口把开关折进 <c>Compilation?</c> 输入，泛化后显式化为参数）；</item>
/// <item>诊断 MUDFT018 → factory 槽位 018（消息第 5 实参 = 开关属性名槽）。</item>
/// </list>
/// </para>
/// <para>
/// <b>为什么不逐方法发射</b>（本方案评审的纠正点）：执行器承载的是<b>有意的策展</b>——哪些字段回填模型、
/// JSON 键怎么命名、错误如何归类——这恰恰是护城河（白名单投影），生成器无从得知。
/// 逐方法发射全部 Schema 还会把编译期成本与程序集体积推高一个量级。故 Tier R 只交付<b>可度量的目录事实</b>：
/// SDK 能力总量、模块分布、策展覆盖率——使"能力面差距"从主观判断变成构建期可见的数字。
/// </para>
/// <para>
/// <b>粒度纪律</b>：枚举只统计各接口<b>自身声明</b>的方法（<c>GetMembers()</c>，不含
/// <c>AllInterfaces</c>）——SDK 的方法声明在基接口上时，把继承算进来会让同一方法被每个派生接口重复计数。
/// </para>
/// </remarks>
internal static class CapabilityCatalogEmitter
{
    /// <summary>
    /// 扫描 SDK 程序集并产出能力目录源；未引用 SDK 根程序集或未开启开关时静默跳过。
    /// </summary>
    /// <param name="context">源产出上下文。</param>
    /// <param name="compilation">当前编译。</param>
    /// <param name="curatedModels">Tier C 已策展模型集合（覆盖率分母事实源）。</param>
    /// <param name="profile">当前剖面（SDK 根 / 接口前缀 / 开关属性名槽的唯一来源）。</param>
    /// <param name="catalogEnabled">能力目录开关（入口读 <c>build_property.{CapabilityCatalogPropertyName}</c> 传入）。</param>
    /// <param name="factory">当前剖面的诊断工厂（槽位 018 描述符）。</param>
    public static void Emit(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ToolSchemaModel> curatedModels,
        SdkToolProfileModel profile,
        bool catalogEnabled,
        ToolSurfaceDiagnostics.Factory factory)
    {
        try
        {
            if (!catalogEnabled)
            {
                return;
            }

            var sdkRoot = profile.SdkNamespaceRoot;
            var sdkAssembly = compilation.SourceModule.ReferencedAssemblySymbols
                .FirstOrDefault(a => string.Equals(a.Name, sdkRoot, StringComparison.Ordinal));
            if (sdkAssembly is null)
            {
                return;
            }

            var curatedToolByMethod = BuildCuratedToolIndex(curatedModels);

            var methodsByModule = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var methodEntries = new List<MethodCatalogEntryData>();
            var totalMethods = 0;

            foreach (var type in EnumerateInterfaces(sdkAssembly.GlobalNamespace, profile))
            {
                var declared = type.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(static m => m.MethodKind == MethodKind.Ordinary && m.DeclaredAccessibility == Accessibility.Public)
                    .ToArray();
                if (declared.Length == 0)
                {
                    continue;
                }

                totalMethods += declared.Length;

                InterfaceIdentityParser.TryParse(type.Name, profile, out var identity);
                var module = identity?.Domain ?? "Unparsed";
                var tokenKind = identity?.Kind ?? ToolSurfaceTokenKind.Unspecified;

                methodsByModule[module] = methodsByModule.TryGetValue(module, out var count)
                    ? count + declared.Length
                    : declared.Length;

                foreach (var method in declared)
                {
                    methodEntries.Add(MethodCatalogEmitter.CreateEntry(
                        type, method, module, tokenKind, profile, curatedToolByMethod));
                }
            }

            var coverage = DescriptorValidator.ComputeCoverage(curatedModels.Select(static m => m.Entry), totalMethods);

            EmitSource(context, methodsByModule, totalMethods, coverage, profile);
            MethodCatalogEmitter.Emit(context, methodEntries, profile);

            // 槽位 018 上报点：聚合单条（避免逐方法刷屏）。
            factory.Report(
                context,
                ToolSurfaceDiagnostics.SlotCapabilityCoverageReport,
                null,
                totalMethods,
                coverage.ToolCount,
                coverage.ToolCoverageRate.ToString("P1", CultureInfo.InvariantCulture),
                methodsByModule.Count,
                profile.CapabilityCatalogPropertyName);
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(CapabilityCatalogEmitter), ex);
        }
    }

    /// <summary>
    /// 构建「SDK 方法名 → 策展工具名」索引（方法目录的 <c>curated</c> 判定依据）。
    /// </summary>
    /// <remarks>
    /// 按键取 <see cref="CapabilityEntry.MethodName"/>：策展工具的 <c>Source</c> 常写派生接口名
    /// （如 <c>IFeishuTenantV2OkrObjective.GetObjectiveAsync</c>），而方法在基接口上声明——
    /// 按接口名精确匹配会因基/派生不一致而失效；方法名在 SDK 内高度唯一，按方法名匹配即可满足
    /// 「引导优先用已策展工具」的启发式语义。同名方法多重策展时取首个（低频，可接受）。
    /// </remarks>
    private static Dictionary<string, string> BuildCuratedToolIndex(ImmutableArray<ToolSchemaModel> curatedModels)
    {
        var index = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var model in curatedModels)
        {
            var entry = model.Entry;
            if (entry.MethodName.Length > 0 && entry.ToolName.Length > 0
                && !index.ContainsKey(entry.MethodName))
            {
                index.Add(entry.MethodName, entry.ToolName);
            }
        }

        return index;
    }

    private static void EmitSource(
        SourceProductionContext context,
        SortedDictionary<string, int> methodsByModule,
        int totalMethods,
        CoverageReport coverage,
        SdkToolProfileModel profile)
    {
        var source = new StringBuilder();
        source.Line($"// <auto-generated> 由 {GeneratedCodeMarker.GeneratorName(profile)} 编译期产出，禁止手工修改 </auto-generated>");
        source.Line("#nullable enable");
        source.Line("#pragma warning disable CS1591 // 生成代码不逐一补 XML 注释");
        source.Line($"namespace {profile.GeneratedNamespace}");
        source.Line("{");
        source.Line("    /// <summary>SDK 能力目录快照（聚合事实；未暴露单元的具体方法名不进产物）。</summary>");
        source.Line($"    {GeneratedCodeMarker.Attribute(profile)}");
        source.Line($"    internal static class {profile.ProductPrefix}CapabilityCatalog");
        source.Line("    {");
        source.Line("        /// <summary>SDK 接口声明的方法总数。</summary>");
        source.Line($"        public const int SdkMethodCount = {totalMethods};");
        source.Line("        /// <summary>已策展（对外暴露）的工具数。</summary>");
        source.Line($"        public const int CuratedToolCount = {coverage.ToolCount};");
        source.Line("        /// <summary>");
        source.Line("        /// 输出契约完整的工具数（声明了非空 output_schema 且推导未被截断）。");
        source.Line("        /// </summary>");
        source.Line("        /// <remarks>");
        source.Line("        /// R2-05：这是 <c>CoverageReport.OutputSchemaRate</c> 的<b>分子</b>（分母 = CuratedToolCount）——");
        source.Line("        /// 该度量在 R5 之前恒为 1.0、R5 之后恒为 0，两次都是假值；此处是它的第一个真实消费点");
        source.Line("        /// （契约守卫断言 OutputSchemaCoveredToolCount ≤ CuratedToolCount，且与 golden 快照的 output_schema 面一致）。");
        source.Line("        /// 单列分子/分母而<b>不</b>发射浮点率：整数是精确的、跨区域性无关的，且可由消费者自行求商。");
        source.Line("        /// </remarks>");
        source.Line($"        public const int OutputSchemaCoveredToolCount = {coverage.OutputSchemaCoveredToolCount};");
        source.Line("        /// <summary>覆盖的能力分组个数（分组轴 = 接口名解析出的 domain 段）。</summary>");
        source.Line($"        public const int DomainCount = {methodsByModule.Count};");
        source.Line();
        source.Line("        /// <summary>按能力分组统计的方法数（分组轴 = 接口名解析出的 domain 段）。</summary>");
        source.Line($"        {GeneratedCodeMarker.Attribute(profile)}");
        source.Line("        public static System.Collections.Generic.IReadOnlyDictionary<string, int> MethodsByDomain { get; } =");
        source.Line("            new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal)");
        source.Line("            {");
        foreach (var pair in methodsByModule)
        {
            source.Line($"                [{JsonText.ToCSharpLiteral(pair.Key)}] = {pair.Value},");
        }

        source.Line("            };");
        source.Line("    }");
        source.Line("}");

        TransitiveCodeGenerator.AddSourceValidated(context, $"{profile.ProductPrefix}CapabilityCatalog.g.cs", source.ToString());
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateInterfaces(INamespaceSymbol ns, SdkToolProfileModel profile)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (type.TypeKind == TypeKind.Interface
                && type.DeclaredAccessibility == Accessibility.Public
                && type.Name.StartsWith(profile.SdkInterfacePrefix, StringComparison.Ordinal))
            {
                yield return type;
            }
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            foreach (var type in EnumerateInterfaces(child, profile))
            {
                yield return type;
            }
        }
    }
}