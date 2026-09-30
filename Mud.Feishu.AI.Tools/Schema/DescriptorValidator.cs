// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Mud.Feishu.AI.Tools.Extraction;

namespace Mud.Feishu.AI.Tools.Schema;

/// <summary>
/// L4 描述符校验器（对齐 CLI <c>internal/schema/lint.go</c> 的分层思路）。
/// </summary>
/// <remarks>
/// <para>数据驱动：输入是 <see cref="CapabilityEntry"/> 集合，输出是验证结果列表，不依赖具体工具。</para>
/// <para>
/// <b>诊断同源纪律</b>：校验结果直接携带 <see cref="DiagnosticDescriptor"/>（而非裸 ID 字符串）——
/// 字符串 ID 无法机械校验"定义 ↔ 上报点"是否一致，本仓库已发生过
/// 「<c>DescriptorValidator</c> 拿 <c>MUDFT012</c> 报『高风险写缺 confirm』、
/// <c>MUDFT002</c> 报『接口名为空』」这类 ID 与语义错配。
/// </para>
/// </remarks>
internal static class DescriptorValidator
{
    /// <summary>
    /// 校验全部条目，返回违规列表。
    /// </summary>
    public static IReadOnlyList<ValidationResult> ValidateAll(IEnumerable<CapabilityEntry> entries)
    {
        var results = new List<ValidationResult>();
        var entryList = entries.ToList();

        ValidateL1Structure(entryList, results);
        ValidateL2TypeConsistency(entryList, results);
        ValidateL3CrossFieldConsistency(entryList, results);

        return results;
    }

    /// <summary>
    /// 计算覆盖率度量。
    /// </summary>
    public static CoverageReport ComputeCoverage(IEnumerable<CapabilityEntry> entries, int totalMethodCount)
    {
        var entryList = entries.ToList();
        var toolCount = entryList.Count;

        var descriptionCovered = entryList.Count(e => !string.IsNullOrWhiteSpace(e.DocSummary));
        var paramDescCovered = entryList.Sum(e => e.Parameters.Count(p => !string.IsNullOrWhiteSpace(p.DocDescription)));
        var paramTotal = entryList.Sum(e => e.Parameters.Count);
        var scopesCovered = entryList.Count(e => e.Scopes.Count > 0);
        // 所有工具都经过风险分级（Read/Write/HighRiskWrite），故 risk 覆盖率始终为 100%。
        var riskCovered = entryList.Count;

        // W3（R5 修订 → R2-05 补完）：outputSchemaRate 的历史形态是**两次假度量**——
        // 第一版复用 riskCovered（= entryList.Count）故恒为 1.0；R5 改为显式报 0 并挂 TODO，
        // 但 0 同样是"诚实的假值"（它说"没有工具声明输出 Schema"，而事实上 53/53 都有）。
        // R2-05 起按**真实语义**计算：声明了非空 output_schema 且推导过程**未被截断**的工具占比。
        // 该值即"模型可见的返回契约完整"的程度——截断工具（MUDFT009）会拉低它，正是本度量要暴露的事实。
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

    // ────────── L1 结构校验 ──────────

    private static void ValidateL1Structure(List<CapabilityEntry> entries, List<ValidationResult> results)
    {
        var seenNames = new Dictionary<string, string>(); // toolName → interfaceName

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.ToolName))
            {
                results.Add(ValidationResult.Error(Diagnostics.MUDFT001, entry.InterfaceName, entry.InterfaceName));
            }

            // 工具名全仓唯一（跨 Tier 合并后仍须唯一）。
            if (!string.IsNullOrEmpty(entry.ToolName))
            {
                if (seenNames.TryGetValue(entry.ToolName, out var existing))
                {
                    results.Add(ValidationResult.Error(Diagnostics.MUDFT003, entry.InterfaceName, entry.ToolName, existing));
                }
                else
                {
                    seenNames[entry.ToolName] = entry.InterfaceName;
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
    /// <c>entry.Parameters</c>——同一集合自比，条件恒为假，<c>MUDFT015</c> 从未真正触发过。
    /// 这类"有上报点的假门禁"比死定义更危险：它让门禁清单看起来已被覆盖。
    /// </para>
    /// <para>
    /// <b>修正后的两个来源</b>：
    /// ① <b>意图模型</b>——<see cref="CapabilityParameter.IsRequired"/>（来自 <c>[ToolParameter]</c> / C# 可空性）；
    /// ② <b>渲染产物</b>——<see cref="SchemaWriter.WriteInputSchema"/> 产出的 JSON 字符串，
    /// 用 <see cref="RenderedPropertiesKeys"/> 从<b>文本</b>中提取 <c>properties</c> 段顶层键。
    /// </para>
    /// <para>
    /// 由此新增的真实检出能力：<b>参数名归一后重名</b>（如 <c>userId</c> 与 <c>user_id</c> 都归一为
    /// <c>user_id</c>）会让 <c>properties</c> 出现重复键、<c>required</c> 与模型看到的键集不一致——
    /// 这在原实现下完全静默。
    /// </para>
    /// </remarks>
    private static void ValidateL2TypeConsistency(List<CapabilityEntry> entries, List<ValidationResult> results)
    {
        foreach (var entry in entries)
        {
            // 来源②：渲染产物（字符串）中的 properties 顶层键。
            var renderedJson = SchemaWriter.WriteInputSchema(entry);
            if (!RenderedPropertiesKeys.TryExtract(renderedJson, out var renderedKeys, out var parseFailure))
            {
                results.Add(ValidationResult.Error(
                    Diagnostics.MUDFT015, entry.InterfaceName, entry.ToolName,
                    $"无法从渲染产物中提取 properties 键集（{parseFailure}）——门禁无法验证，按失败处理"));
                continue;
            }

            // 真实检出①：意图模型声明的参数数与渲染出的键数不一致（重复键会被 JSON 丢弃 → 模型看到的属性少了）。
            if (renderedKeys.Count != entry.Parameters.Count)
            {
                results.Add(ValidationResult.Error(
                    Diagnostics.MUDFT015, entry.InterfaceName, entry.ToolName,
                    $"渲染出的 properties 键数 {renderedKeys.Count} 与参数数 {entry.Parameters.Count} 不一致"
                    + "（通常是参数名归一后重名，导致 JSON 重复键被覆盖）"));
            }

            // 真实检出②：required 的每个名字必须真的出现在渲染出的 properties 中。
            foreach (var required in entry.Parameters.Where(static p => p.IsRequired).Select(static p => p.Name))
            {
                if (!renderedKeys.Contains(required))
                {
                    results.Add(ValidationResult.Error(
                        Diagnostics.MUDFT015, entry.InterfaceName, entry.ToolName,
                        $"required 参数 '{required}' 不在渲染出的 properties 键集中"));
                }
            }

            // 真实检出③（WP1 driver 测试补强）：[ToolParameter] 声明的参数名（意图源）与渲染键
            // （实现源）漂移。此前两个"来源"均由 entry.Parameters 恒等派生，本条检出分支
            // 在任何源码形状下都不可触发（driver 负例构造时发现）；引入意图源后成为真实门禁。
            foreach (var parameter in entry.Parameters)
            {
                if (!string.IsNullOrEmpty(parameter.DeclaredToolParameterName)
                    && !string.Equals(parameter.DeclaredToolParameterName, parameter.Name, System.StringComparison.Ordinal))
                {
                    results.Add(ValidationResult.Error(
                        Diagnostics.MUDFT015, entry.InterfaceName, entry.ToolName,
                        $"参数的 [ToolParameter] 名 '{parameter.DeclaredToolParameterName}' 与渲染出的 properties 键 '{parameter.Name}' 不一致"
                        + "——模型看到的键以渲染产物为准，请让二者一致（工具参数键 = C# 参数名）"));
                }
            }
        }
    }

    // ────────── L3 跨字段一致校验 ──────────

    private static void ValidateL3CrossFieldConsistency(List<CapabilityEntry> entries, List<ValidationResult> results)
    {
        foreach (var entry in entries)
        {
            // 身份 ↔ 接口令牌类型：User 身份必须落在 IFeishuUserV* 接口上。
            if (entry.Identity == ToolIdentity.User
                && !entry.InterfaceName.StartsWith("IFeishuUser", System.StringComparison.Ordinal))
            {
                results.Add(ValidationResult.Error(
                    Diagnostics.MUDFT016,
                    entry.InterfaceName,
                    entry.ToolName,
                    "User",
                    entry.InterfaceName));
            }

            // 双令牌基接口不应直接产工具（工具必须在 _Tenant / _User 派生接口上）。
            if (entry.Identity == ToolIdentity.Both)
            {
                results.Add(ValidationResult.Error(
                    Diagnostics.MUDFT016,
                    entry.InterfaceName,
                    entry.ToolName,
                    "Both（基接口）",
                    entry.InterfaceName));
            }
        }
    }
}

/// <summary>验证结果（携带诊断描述符与实参，供生成器直接 <c>ReportDiagnostic</c>）。</summary>
internal sealed class ValidationResult
{
    private ValidationResult(
        DiagnosticDescriptor descriptor,
        string interfaceName,
        object[] arguments)
    {
        Descriptor = descriptor;
        InterfaceName = interfaceName;
        Arguments = arguments;
    }

    /// <summary>诊断描述符（与 <see cref="Diagnostics"/> 定义同源）。</summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>承载接口名（供定位与消息上下文）。</summary>
    public string InterfaceName { get; }

    /// <summary>诊断消息实参。</summary>
    public object[] Arguments { get; }

    /// <summary>构造校验结果（级别由 <see cref="Descriptor"/> 自身的 <c>DefaultSeverity</c> 表达）。</summary>
    /// <remarks>
    /// R2-10：此处原有 <c>IsError</c> 属性与 <c>Warning(...)</c> 工厂——二者<b>均无读取方</b>
    /// （全部校验项都走 <c>Error(...)</c>，且严重级已在描述符里定义），留下会诱使"再加一个不生效的级别开关"。
    /// </remarks>
    public static ValidationResult Error(DiagnosticDescriptor descriptor, string iface, params object[] arguments)
        => new(descriptor, iface, arguments);
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
