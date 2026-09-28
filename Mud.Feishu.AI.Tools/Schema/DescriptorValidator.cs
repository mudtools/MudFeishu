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

        return new CoverageReport(
            toolCount: toolCount,
            totalMethodCount: totalMethodCount,
            toolCoverageRate: toolCount == 0 || totalMethodCount == 0 ? 0 : (double)toolCount / totalMethodCount,
            descriptionCoverageRate: toolCount == 0 ? 0 : (double)descriptionCovered / toolCount,
            paramDescriptionCoverageRate: paramTotal == 0 ? 0 : (double)paramDescCovered / paramTotal,
            outputSchemaRate: toolCount == 0 ? 0 : (double)riskCovered / toolCount,
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

    private static void ValidateL2TypeConsistency(List<CapabilityEntry> entries, List<ValidationResult> results)
    {
        foreach (var entry in entries)
        {
            // required ⊆ properties 键集（SchemaWriter 与模型契约之间的硬一致）。
            var propertyNames = new HashSet<string>(entry.Parameters.Select(p => p.Name));
            foreach (var req in entry.Parameters.Where(p => p.IsRequired).Select(p => p.Name))
            {
                if (!propertyNames.Contains(req))
                {
                    results.Add(ValidationResult.Error(
                        Diagnostics.MUDFT015,
                        entry.InterfaceName,
                        entry.ToolName,
                        $"required 参数 '{req}' 不在 properties 键集中"));
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
        object[] arguments,
        bool isError)
    {
        Descriptor = descriptor;
        InterfaceName = interfaceName;
        Arguments = arguments;
        IsError = isError;
    }

    /// <summary>诊断描述符（与 <see cref="Diagnostics"/> 定义同源）。</summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>承载接口名（供定位与消息上下文）。</summary>
    public string InterfaceName { get; }

    /// <summary>诊断消息实参。</summary>
    public object[] Arguments { get; }

    /// <summary>是否为 Error 级（Error 级结果必须上报，不得静默丢弃）。</summary>
    public bool IsError { get; }

    /// <summary>构造 Error 级结果。</summary>
    public static ValidationResult Error(DiagnosticDescriptor descriptor, string iface, params object[] arguments)
        => new(descriptor, iface, arguments, isError: true);

    /// <summary>构造 Warning 级结果。</summary>
    public static ValidationResult Warning(DiagnosticDescriptor descriptor, string iface, params object[] arguments)
        => new(descriptor, iface, arguments, isError: false);
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
        double scopesCoverageRate,
        double riskCoverageRate)
    {
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
    public double ScopesCoverageRate { get; }
    public double RiskCoverageRate { get; }
}
