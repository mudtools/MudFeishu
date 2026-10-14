// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.ToolSurface.Extraction;

namespace Mud.AI.ToolSurface.Schema;

/// <summary>
/// L4 描述符校验器（对齐 CLI <c>internal/schema/lint.go</c> 的分层思路；移植自上游
/// <c>Mud.Feishu.AI.Tools.Schema.DescriptorValidator</c>）。
/// </summary>
/// <remarks>
/// <para>数据驱动：输入是 <see cref="CapabilityEntry"/> 集合，输出是验证结果列表，不依赖具体工具。</para>
/// <para>
/// <b>诊断同源纪律</b>：校验结果直接携带 <see cref="DiagnosticDescriptor"/>（而非裸 ID 字符串）——
/// 描述符经 <see cref="ToolSurfaceDiagnostics.Factory"/> 按剖面实例化（槽位 001/003/015/016/027），
/// 字符串 ID 无法机械校验"定义 ↔ 上报点"是否一致。
/// </para>
/// <para>
/// <b>泛化点</b>（上游硬编码 → 剖面槽/委托）：
/// ① L3 的 <c>StartsWith("IFeishuUser")</c> → 经 <see cref="InterfaceIdentityParser.DeriveTokenKind"/>
/// 用剖面标记表（<c>TokenKindStrategy</c> + <c>TokenKindMarkers</c>）推导承载接口的令牌身份再比对；
/// ② L1 的派生常量名规则属 Emit 层（<c>SchemaEmitter.BuildNameConstant</c>），
/// 经 <c>ValidateAll</c> 的可选委托参数 <c>nameConstantBuilder</c> 注入——为 <see langword="null"/> 时跳过槽位 027
/// 检查（该检查的产物派生规则必须与产物同源，Schema 层不复刻）。
/// </para>
/// </remarks>
internal static class DescriptorValidator
{
    /// <summary>
    /// 校验全部条目，返回违规列表。
    /// </summary>
    /// <param name="entries">能力条目集合。</param>
    /// <param name="profile">当前剖面（L3 令牌身份推导需要标记表）。</param>
    /// <param name="factory">当前剖面的诊断工厂（描述符按 {prefix}{slot:D3} 实例化）。</param>
    /// <param name="nameConstantBuilder">工具名 → 派生常量名的规则（与产物同源，由 Emit 层传入；可空则跳过槽位 027）。</param>
    public static IReadOnlyList<ValidationResult> ValidateAll(
        IEnumerable<CapabilityEntry> entries,
        SdkToolProfileModel profile,
        ToolSurfaceDiagnostics.Factory factory,
        Func<string, string>? nameConstantBuilder = null)
    {
        try
        {
            var results = new List<ValidationResult>();
            var entryList = entries.ToList();

            ValidateL1Structure(entryList, profile, results, factory, nameConstantBuilder);
            ValidateL2TypeConsistency(entryList, results, factory);
            ValidateL3CrossFieldConsistency(entryList, profile, results, factory);

            return results;
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(DescriptorValidator), ex);
            return [];
        }
    }

    /// <summary>
    /// 计算覆盖率度量。
    /// </summary>
    public static CoverageReport ComputeCoverage(IEnumerable<CapabilityEntry> entries, int totalMethodCount)
    {
        try
        {
            var entryList = entries.ToList();
            var toolCount = entryList.Count;

            var descriptionCovered = entryList.Count(e => !string.IsNullOrWhiteSpace(e.DocSummary));
            var paramDescCovered = entryList.Sum(e => e.Parameters.Count(p => !string.IsNullOrWhiteSpace(p.DocDescription)));
            var paramTotal = entryList.Sum(e => e.Parameters.Count);
            var scopesCovered = entryList.Count(e => e.Scopes.Count > 0);
            // 所有工具都经过风险分级（Read/Write/HighRiskWrite），故 risk 覆盖率始终为 100%。
            var riskCovered = entryList.Count;

            // W3（R5 修订 → R2-05 补完）：outputSchemaRate 按**真实语义**计算：
            // 声明了非空 output_schema 且推导过程**未被截断**的工具占比。
            // 该值即"模型可见的返回契约完整"的程度——截断工具（槽位 009）会拉低它，正是本度量要暴露的事实。
            var outputSchemaCovered = entryList.Count(static e =>
                !string.IsNullOrEmpty(e.OutputSchemaJson)
                && e.OutputSchemaJson != "{}"
                && e.OutputSchemaTruncations.Count == 0);

            return new CoverageReport(
                toolCount: toolCount,
                totalMethodCount: totalMethodCount,
                toolCoverageRate: toolCount == 0 || totalMethodCount == 0 ? 0 : (double)toolCount / totalMethodCount,
                descriptionCoverageRate: toolCount == 0 ? 0 : (double)descriptionCovered / toolCount,
                paramDescriptionCoverageRate: paramTotal == 0 ? 0 : (double)paramDescCovered / paramTotal,
                outputSchemaRate: toolCount == 0 ? 0 : (double)outputSchemaCovered / toolCount,
                outputSchemaCoveredToolCount: outputSchemaCovered,
                scopesCoverageRate: toolCount == 0 ? 0 : (double)scopesCovered / toolCount,
                riskCoverageRate: toolCount == 0 ? 0 : (double)riskCovered / toolCount);
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(DescriptorValidator), ex);
            return new CoverageReport(0, totalMethodCount, 0, 0, 0, 0, 0, 0, 0);
        }
    }

    // ────────── L1 结构校验 ──────────

    private static void ValidateL1Structure(
        List<CapabilityEntry> entries,
        SdkToolProfileModel profile,
        List<ValidationResult> results,
        ToolSurfaceDiagnostics.Factory factory,
        Func<string, string>? nameConstantBuilder)
    {
        var seenNames = new Dictionary<string, string>(); // toolName → interfaceName
        var seenConstantNames = new Dictionary<string, string>(); // 派生常量名 → toolName

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.ToolName))
            {
                // 槽位 001 要求两个实参（工具特性名 + 承载接口名）——少传会让消息格式化期抛 FormatException。
                results.Add(ValidationResult.Error(factory[ToolSurfaceDiagnostics.SlotMissingToolName],
                    profile.ToolAttributeName, entry.InterfaceName));
            }

            // 工具名全仓唯一（跨 Tier 合并后仍须唯一）。
            if (!string.IsNullOrEmpty(entry.ToolName))
            {
                if (seenNames.TryGetValue(entry.ToolName, out var existing))
                {
                    results.Add(ValidationResult.Error(factory[ToolSurfaceDiagnostics.SlotToolNameConflict],
                        entry.ToolName, existing));
                }
                else
                {
                    seenNames[entry.ToolName] = entry.InterfaceName;
                }

                // 派生常量名唯一（槽位 027 / R4-10）：字面不同的工具名可归一到同一常量名，
                // 产物会撞成重复常量（CS0101）或重复 hintName（AddSource 异常 → 槽位 026 表面症状）。
                // 派生规则与产物同源（Emit 层 BuildNameConstant），不在此复刻；委托缺席则跳过本检查。
                if (nameConstantBuilder is not null)
                {
                    var constantName = nameConstantBuilder(entry.ToolName);
                    if (seenConstantNames.TryGetValue(constantName, out var sameConstantName))
                    {
                        results.Add(ValidationResult.Error(
                            factory[ToolSurfaceDiagnostics.SlotDerivedConstantNameConflict],
                            entry.ToolName, sameConstantName, constantName));
                    }
                    else
                    {
                        seenConstantNames[constantName] = entry.ToolName;
                    }
                }
            }
        }
    }

    // ────────── L2 类型一致校验 ──────────

    /// <summary>
    /// L2：<c>required ⊆ properties</c>，两个<b>独立</b>来源比对（AT-B15 / R3 评审 C-2）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>历史缺陷（假门禁）</b>：原实现把 <c>propertyNames</c> 与 <c>required</c> 都取自
    /// <c>entry.Parameters</c>——同一集合自比，条件恒为假，槽位 015 从未真正触发过。
    /// 这类"有上报点的假门禁"比死定义更危险：它让门禁清单看起来已被覆盖。
    /// </para>
    /// <para>
    /// <b>修正后的两个来源</b>：
    /// ① <b>意图模型</b>——<see cref="CapabilityParameter.IsRequired"/>（来自参数特性 / C# 可空性）；
    /// ② <b>渲染产物</b>——<see cref="SchemaWriter.WriteInputSchema"/> 产出的 JSON 字符串，
    /// 用 <see cref="RenderedPropertiesKeys"/> 从<b>文本</b>中提取 <c>properties</c> 段顶层键。
    /// </para>
    /// <para>
    /// 由此新增的真实检出能力：<b>参数名归一后重名</b>（如 <c>userId</c> 与 <c>user_id</c> 都归一为
    /// <c>user_id</c>）会让 <c>properties</c> 出现重复键、<c>required</c> 与模型看到的键集不一致——
    /// 这在原实现下完全静默。
    /// </para>
    /// </remarks>
    private static void ValidateL2TypeConsistency(
        List<CapabilityEntry> entries,
        List<ValidationResult> results,
        ToolSurfaceDiagnostics.Factory factory)
    {
        foreach (var entry in entries)
        {
            // 来源②：渲染产物（字符串）中的 properties 顶层键。
            var renderedJson = SchemaWriter.WriteInputSchema(entry);
            if (!RenderedPropertiesKeys.TryExtract(renderedJson, out var renderedKeys, out var parseFailure))
            {
                results.Add(ValidationResult.Error(
                    factory[ToolSurfaceDiagnostics.SlotSchemaInconsistency], entry.ToolName,
                    $"无法从渲染产物中提取 properties 键集（{parseFailure}）——门禁无法验证，按失败处理"));
                continue;
            }

            // 真实检出①：意图模型声明的参数数与渲染出的键数不一致（重复键会被 JSON 丢弃 → 模型看到的属性少了）。
            if (renderedKeys.Count != entry.Parameters.Count)
            {
                results.Add(ValidationResult.Error(
                    factory[ToolSurfaceDiagnostics.SlotSchemaInconsistency], entry.ToolName,
                    $"渲染出的 properties 键数 {renderedKeys.Count} 与参数数 {entry.Parameters.Count} 不一致"
                    + "（通常是参数名归一后重名，导致 JSON 重复键被覆盖）"));
            }

            // 真实检出②：required 的每个名字必须真的出现在渲染出的 properties 中。
            foreach (var required in entry.Parameters.Where(static p => p.IsRequired).Select(static p => p.Name))
            {
                if (!renderedKeys.Contains(required))
                {
                    results.Add(ValidationResult.Error(
                        factory[ToolSurfaceDiagnostics.SlotSchemaInconsistency], entry.ToolName,
                        $"required 参数 '{required}' 不在渲染出的 properties 键集中"));
                }
            }

            // 真实检出③（WP1 driver 测试补强）：参数特性声明的参数名（意图源）与渲染键
            // （实现源）漂移。此前两个"来源"均由 entry.Parameters 恒等派生，本条检出分支
            // 在任何源码形状下都不可触发（driver 负例构造时发现）；引入意图源后成为真实门禁。
            foreach (var parameter in entry.Parameters)
            {
                if (!string.IsNullOrEmpty(parameter.DeclaredToolParameterName)
                    && !string.Equals(parameter.DeclaredToolParameterName, parameter.Name, StringComparison.Ordinal))
                {
                    results.Add(ValidationResult.Error(
                        factory[ToolSurfaceDiagnostics.SlotSchemaInconsistency], entry.ToolName,
                        $"参数的 [ToolParameter] 名 '{parameter.DeclaredToolParameterName}' 与渲染出的 properties 键 '{parameter.Name}' 不一致"
                        + "——模型看到的键以渲染产物为准，请让二者一致（工具参数键 = C# 参数名）"));
                }
            }
        }
    }

    // ────────── L3 跨字段一致校验 ──────────

    /// <remarks>
    /// <b>泛化</b>（上游 <c>StartsWith("IFeishuUser")</c> / <c>ToolIdentity.Both</c> 硬编码）：
    /// 承载接口的令牌身份经剖面标记表推导（<see cref="InterfaceIdentityParser.DeriveTokenKind"/>），
    /// 与条目声明身份比对；双令牌基接口（推导无命中标记 → <see cref="ToolSurfaceTokenKind.Both"/>）
    /// 不应直接产工具。身份字面量一律经 <see cref="ToolSurfaceTokenKindContract.ToLiteral"/>。
    /// </remarks>
    private static void ValidateL3CrossFieldConsistency(
        List<CapabilityEntry> entries,
        SdkToolProfileModel profile,
        List<ValidationResult> results,
        ToolSurfaceDiagnostics.Factory factory)
    {
        foreach (var entry in entries)
        {
            var derived = InterfaceIdentityParser.DeriveTokenKind(entry.InterfaceName, profile, out _);

            // 身份 ↔ 接口令牌类型：声明身份必须落在标记表推导一致的接口上（上游 User→IFeishuUserV* 的同构泛化）。
            if (entry.Identity != ToolSurfaceTokenKind.Unspecified
                && entry.Identity != ToolSurfaceTokenKind.Both
                && derived != entry.Identity)
            {
                results.Add(ValidationResult.Error(
                    factory[ToolSurfaceDiagnostics.SlotTokenKindMismatch],
                    entry.ToolName,
                    ToolSurfaceTokenKindContract.ToLiteral(entry.Identity),
                    entry.InterfaceName));
            }

            // 双令牌基接口不应直接产工具（工具必须在令牌标记派生接口上）。
            if (entry.Identity == ToolSurfaceTokenKind.Both)
            {
                results.Add(ValidationResult.Error(
                    factory[ToolSurfaceDiagnostics.SlotTokenKindMismatch],
                    entry.ToolName,
                    ToolSurfaceTokenKindContract.ToLiteral(entry.Identity),
                    entry.InterfaceName));
            }
        }
    }
}

/// <summary>验证结果（携带诊断描述符与实参，供生成器直接 <c>ReportDiagnostic</c>）。</summary>
internal sealed class ValidationResult
{
    private ValidationResult(DiagnosticDescriptor descriptor, object[] arguments)
    {
        Descriptor = descriptor;
        Arguments = arguments;
    }

    /// <summary>诊断描述符（与 <see cref="ToolSurfaceDiagnostics"/> 槽位表同源）。</summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>诊断消息实参（个数必须与槽位消息模板的占位符个数一致，否则格式化期抛 <c>FormatException</c>）。</summary>
    public object[] Arguments { get; }

    /// <summary>构造校验结果（级别由 <see cref="Descriptor"/> 自身的 <c>DefaultSeverity</c> 表达）。</summary>
    /// <remarks>
    /// R2-10：此处原有 <c>IsError</c> 属性与 <c>Warning(...)</c> 工厂——二者<b>均无读取方</b>
    /// （全部校验项都走 <c>Error(...)</c>，且严重级已在描述符里定义），留下会诱使"再加一个不生效的级别开关"。
    /// 另有 <c>InterfaceName</c> 属性同样无读取方（接口名已作为消息实参之一），一并移除。
    /// </remarks>
    public static ValidationResult Error(DiagnosticDescriptor descriptor, params object[] arguments)
        => new(descriptor, arguments);
}

/// <summary>覆盖率度量报告。</summary>
internal sealed class CoverageReport
{
    public CoverageReport(
        int toolCount,
        int totalMethodCount,
        double toolCoverageRate,
        double descriptionCoverageRate,
        double paramDescriptionCoverageRate,
        double outputSchemaRate,
        int outputSchemaCoveredToolCount,
        double scopesCoverageRate,
        double riskCoverageRate)
    {
        OutputSchemaCoveredToolCount = outputSchemaCoveredToolCount;
        ToolCount = toolCount;
        TotalMethodCount = totalMethodCount;
        ToolCoverageRate = toolCoverageRate;
        DescriptionCoverageRate = descriptionCoverageRate;
        ParamDescriptionCoverageRate = paramDescriptionCoverageRate;
        OutputSchemaRate = outputSchemaRate;
        ScopesCoverageRate = scopesCoverageRate;
        RiskCoverageRate = riskCoverageRate;
    }

    public int ToolCount { get; }
    public int TotalMethodCount { get; }
    public double ToolCoverageRate { get; }
    public double DescriptionCoverageRate { get; }
    public double ParamDescriptionCoverageRate { get; }
    public double OutputSchemaRate { get; }

    /// <summary>输出契约完整的工具数（<see cref="OutputSchemaRate"/> 的分子；整数形态便于生成期直接发射）。</summary>
    public int OutputSchemaCoveredToolCount { get; }
    public double ScopesCoverageRate { get; }
    public double RiskCoverageRate { get; }
}