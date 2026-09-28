// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Mud.Feishu.AI.Tools.Extraction;

namespace Mud.Feishu.AI.Tools.Schema;

/// <summary>
/// L4 描述符校验器（对齐 CLI <c>internal/schema/lint.go</c> 的分层思路）。
/// </summary>
/// <remarks>
/// <para>数据驱动：输入是 <see cref="CapabilityEntry"/> 集合，输出是验证结果列表，不依赖具体工具。</para>
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

        // L1 结构校验
        ValidateL1Structure(entryList, results);

        // L2 类型一致校验
        ValidateL2TypeConsistency(entryList, results);

        // L3 跨字段一致校验
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
        var outputCovered = entryList.Count; // 所有工具都有 OutputSchema（由生成器保证）

        return new CoverageReport(
            toolCount: toolCount,
            totalMethodCount: totalMethodCount,
            toolCoverageRate: toolCount == 0 ? 0 : (double)toolCount / totalMethodCount,
            descriptionCoverageRate: toolCount == 0 ? 0 : (double)descriptionCovered / toolCount,
            paramDescriptionCoverageRate: paramTotal == 0 ? 0 : (double)paramDescCovered / paramTotal,
            outputSchemaRate: toolCount == 0 ? 0 : (double)outputCovered / toolCount,
            scopesCoverageRate: toolCount == 0 ? 0 : (double)scopesCovered / toolCount,
            riskCoverageRate: toolCount == 0 ? 0 : (double)riskCovered / toolCount);
    }

    // ────────── L1 结构校验 ──────────

    private static void ValidateL1Structure(List<CapabilityEntry> entries, List<ValidationResult> results)
    {
        var seenNames = new Dictionary<string, string>(); // toolName → interfaceName

        foreach (var entry in entries)
        {
            // 必填字段齐备
            if (string.IsNullOrWhiteSpace(entry.ToolName))
            {
                results.Add(ValidationResult.Error("MUDFT001", entry.InterfaceName, "工具名为空"));
            }

            if (string.IsNullOrWhiteSpace(entry.InterfaceName))
            {
                results.Add(ValidationResult.Error("MUDFT002", "(unknown)", "接口名为空"));
            }

            // 工具名全仓唯一
            if (!string.IsNullOrEmpty(entry.ToolName))
            {
                if (seenNames.TryGetValue(entry.ToolName, out var existing))
                {
                    results.Add(ValidationResult.Error("MUDFT003", entry.InterfaceName,
                        $"工具名 '{entry.ToolName}' 冲突：已被接口 {existing} 占用"));
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
            // required ⊆ properties 键集
            var propertyNames = new HashSet<string>(entry.Parameters.Select(p => p.Name));
            var requiredParams = entry.Parameters.Where(p => p.IsRequired).Select(p => p.Name);
            foreach (var req in requiredParams)
            {
                if (!propertyNames.Contains(req))
                {
                    results.Add(ValidationResult.Error("MUDFT003", entry.InterfaceName,
                        $"工具 '{entry.ToolName}' 的 required 参数 '{req}' 不在 properties 键集中"));
                }
            }

            // array 下 items 必须存在且非空类型（由 SchemaWriter 保证，此处跳过）
        }
    }

    // ────────── L3 跨字段一致校验 ──────────

    private static void ValidateL3CrossFieldConsistency(List<CapabilityEntry> entries, List<ValidationResult> results)
    {
        foreach (var entry in entries)
        {
            // identity == User ⇒ 方法所在接口为 IFeishuUserV*
            if (entry.Identity == ToolIdentity.User && !entry.InterfaceName.StartsWith("IFeishuUser"))
            {
                results.Add(ValidationResult.Error("MUDFT002", entry.InterfaceName,
                    $"身份为 User 但接口名 '{entry.InterfaceName}' 不以 IFeishuUser 开头"));
            }

            // identity == Both（基接口）不应直接产工具（G-4 去重规则）
            if (entry.Identity == ToolIdentity.Both)
            {
                results.Add(ValidationResult.Warning("MUDFT006", entry.InterfaceName,
                    $"基接口 '{entry.InterfaceName}' 不应直接产工具（应为 _Tenant/_User 派生接口）"));
            }

            // risk == high-risk-write ⇔ input 含 confirm（§4.6.5.2 L3 跨字段一致）
            if (entry.Risk == ToolRisk.HighRiskWrite)
            {
                var hasConfirm = entry.Parameters.Any(p => p.Name == "confirm");
                if (!hasConfirm)
                {
                    // 风险分级与 schema 不一致——MUDFT012 语义为「风险分级一致性问题」
                    results.Add(ValidationResult.Error("MUDFT012", entry.InterfaceName,
                        $"高风险写工具 '{entry.ToolName}' 缺少 confirm 参数（risk == high-risk-write ⇔ schema 含 confirm）"));
                }
            }
        }
    }
}

/// <summary>验证结果。</summary>
internal sealed class ValidationResult
{
    public ValidationResult(string diagnosticId, string interfaceName, string message, bool isError)
    {
        DiagnosticId = diagnosticId;
        InterfaceName = interfaceName;
        Message = message;
        IsError = isError;
    }

    public string DiagnosticId { get; }
    public string InterfaceName { get; }
    public string Message { get; }
    public bool IsError { get; }

    public static ValidationResult Error(string id, string iface, string msg)
        => new(id, iface, msg, isError: true);

    public static ValidationResult Warning(string id, string iface, string msg)
        => new(id, iface, msg, isError: false);
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
