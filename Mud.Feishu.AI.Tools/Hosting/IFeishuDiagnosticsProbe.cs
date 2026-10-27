// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Hosting;

/// <summary>
/// 诊断结论的严重度字面量（闭集，与配置面词汇同风格：小写连字符/下划线，机读稳定码）。
/// </summary>
public static class FeishuDiagnosticsSeverities
{
    /// <summary>事实性告知，不指向待处置问题。</summary>
    public const string Info = "info";

    /// <summary>当前装配可运行，但存在运行期逐请求暴露的隐患。</summary>
    public const string Warning = "warning";

    /// <summary>按当前配置，运行期<b>必然</b>失败或<b>必然</b>静默丢弃（不是"可能"）。</summary>
    public const string Error = "error";
}

/// <summary>一条诊断结论。</summary>
/// <param name="Severity">严重度字面量（取值见 <see cref="FeishuDiagnosticsSeverities"/>）。</param>
/// <param name="Code">稳定机读码（如 <c>risk_policy_mismatch</c>）——消费方按码分支，不得按正文文案分支。</param>
/// <param name="Message">人类可读正文（含处置建议）。</param>
public sealed record FeishuDiagnosticsFinding(string Severity, string Code, string Message);

/// <summary>一条事实读数（名称 + 不变文化字符串值）。</summary>
/// <remarks>
/// 名称形如 <c>tools.enabled</c> / <c>guidance.headroom_percent</c>；用「名-值」而非强类型字段，
/// 是为了让诊断面能随策略轴增长，而不再每加一个读数就扩一次公开契约。
/// </remarks>
/// <param name="Name">点分读数名。</param>
/// <param name="Value">读数值（不变文化字符串；未知时为空串）。
/// 值口径：<c>true</c>/<c>false</c> 表示注册状态，其余为计数或字面量。</param>
public sealed record FeishuDiagnosticsFact(string Name, string Value);

/// <summary>一次自检的结构化报告（结论 + 事实读数）。</summary>
/// <param name="Findings">诊断结论（产出顺序即实现顺序；展示请用 <see cref="ToText"/> 的确定性排序）。</param>
/// <param name="Facts">事实读数。</param>
public sealed record FeishuDiagnosticsReport(
    IReadOnlyList<FeishuDiagnosticsFinding> Findings,
    IReadOnlyList<FeishuDiagnosticsFact> Facts)
{
    /// <summary>结论条数。</summary>
    public int FindingCount => Findings.Count;

    /// <summary>是否存在 <see cref="FeishuDiagnosticsSeverities.Error"/> 级结论。</summary>
    public bool HasErrors
        => Findings.Any(static f => string.Equals(f.Severity, FeishuDiagnosticsSeverities.Error, StringComparison.Ordinal));

    /// <summary>取值秩（展示排序用：error → warning → info）。</summary>
    private static int SeverityRank(string severity) => severity switch
    {
        FeishuDiagnosticsSeverities.Error => 0,
        FeishuDiagnosticsSeverities.Warning => 1,
        _ => 2,
    };

    /// <summary>
    /// 确定性文本形态（宿主一行输出 / 健康检查日志可直接落盘）。
    /// </summary>
    /// <remarks>
    /// 排序全确定：结论按（严重度秩, 码）升序，读数按名升序——同一装配两次调用逐字节相同，
    /// 因此可以作为冒烟用例与差异比对的断言面。
    /// </remarks>
    /// <returns>两段式文本报告。</returns>
    public string ToText()
    {
        var lines = new List<string>(Findings.Count + Facts.Count + 2)
        {
            $"诊断结论（{Findings.Count} 条）",
        };

        foreach (var finding in Findings
                     .OrderBy(static f => SeverityRank(f.Severity))
                     .ThenBy(static f => f.Code, StringComparer.Ordinal))
        {
            lines.Add($"  [{finding.Severity}] {finding.Code} — {finding.Message}");
        }

        lines.Add($"事实读数（{Facts.Count} 条）");
        foreach (var fact in Facts.OrderBy(static f => f.Name, StringComparer.Ordinal))
        {
            lines.Add($"  {fact.Name} = {fact.Value}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// 装配自检面（GAP-11）：把「启动后第一次调用才暴露的装配缺陷」变成一次可调用、可读、可断言的报告。
/// </summary>
/// <remarks>
/// <para>
/// 结论码（机读稳定，正文可变）：配置与装配 <c>config_invalid</c> / <c>registry_build_failed</c> /
/// <c>options_missing</c> / <c>no_tools_enabled</c> / <c>probe_fault</c>（自检自身抛异常，报告仍部分有效）；工具面对账 <c>contract_registry_gap</c> /
/// <c>registry_tools_without_contract</c> / <c>risk_policy_mismatch</c> / <c>high_risk_write_enabled</c>；
/// 门禁与 HITL <c>write_tools_without_authorizer</c> / <c>authorization_not_enforced</c> /
/// <c>approval_channel_missing</c> / <c>content_safety_off</c> / <c>audit_sink_missing</c>；
/// user 身份 <c>user_tool_wiring_missing</c> / <c>user_token_resolver_missing</c> /
/// <c>user_token_precheck_off</c> / <c>user_token_manager_unreachable</c>；
/// 应用与预算 <c>no_apps_configured</c> / <c>guidance_domains_omitted</c> / <c>guidance_budget_low</c>。
/// </para>
/// <para>
/// <b>零副作用契约</b>：同步、不发起任何网络调用、不刷新令牌、不实例化应用。令牌维度只报「管理器可达性」，
/// 不报「有效期」——读有效期需要触达存储并可能触发刷新，那会让一条诊断命令变成一次真实调用。
/// </para>
/// </remarks>
public interface IFeishuDiagnosticsProbe
{
    /// <summary>执行自检（同一装配重复调用给出逐字节相同的报告）。</summary>
    /// <returns>结构化报告；本方法不抛（装配错误以结论形式回填报告）。</returns>
    FeishuDiagnosticsReport Probe();
}
