// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tools;

/// <summary>
/// glob / 域级工具策略的<b>匹配判定结果</b>（产品中立；产品侧把结果映射为自己的
/// reason_code 与拒绝文案——拒绝词汇是产品面）。
/// </summary>
public enum ToolPolicyDecision
{
    /// <summary>放行（未配置策略或未命中任何拒绝规则）。</summary>
    Allowed,

    /// <summary>域命中 Deny 规则。</summary>
    DomainDenied,

    /// <summary>域不在 Allow 白名单（Allow 非空时未命中即拒）。</summary>
    DomainNotAllowed,

    /// <summary>工具名命中 Deny 规则。</summary>
    ToolDenied,

    /// <summary>工具名不在 Allow 白名单（Allow 非空时未命中即拒）。</summary>
    ToolNotAllowed,
}

/// <summary>
/// glob / 域级工具策略规则（产品中立的值载体：四个模式集合 + 求值算法）。
/// </summary>
/// <param name="ToolAllow">工具名白名单（glob）。空 = 该轴不启用。</param>
/// <param name="ToolDeny">工具名黑名单（glob，优先于 <paramref name="ToolAllow"/>）。空 = 该轴不启用。</param>
/// <param name="DomainAllow">域白名单（单段 glob）。空 = 该轴不启用。</param>
/// <param name="DomainDeny">域黑名单（单段 glob，优先于 <paramref name="DomainAllow"/>）。空 = 该轴不启用。</param>
public sealed record ToolPolicyRules(
    IReadOnlyList<string>? ToolAllow = null,
    IReadOnlyList<string>? ToolDeny = null,
    IReadOnlyList<string>? DomainAllow = null,
    IReadOnlyList<string>? DomainDeny = null)
{
    /// <summary>四个集合全空 ⇒ 策略未配置（不生效，逐字节等价"无策略"行为）。</summary>
    public bool IsConfigured =>
        (ToolAllow is { Count: > 0 }) || (ToolDeny is { Count: > 0 })
        || (DomainAllow is { Count: > 0 }) || (DomainDeny is { Count: > 0 });
}

/// <summary>
/// <b>S-1：glob / 域级策略匹配器</b>（纯函数，零 IO）。
/// </summary>
/// <remarks>
/// <para>
/// <b>中立化说明（N2-d，方案 v2 §5.1 A10）</b>：匹配算法自产品执行链
/// （飞书 <c>FeishuToolBinding.EvaluateToolPolicy</c>）原样抽出——「域轴先于工具名轴」
/// 的判定顺序、Deny 优先于 Allow、空集合语义（Allow 空 = 该轴不启用，**不是**"全拒"）
/// 全部是<b>产品中立</b>的策略语义；抽走后产品侧只保留 reason_code 映射与文案构造。
/// </para>
/// <para>
/// <b>为什么域轴先于工具名轴</b>：域轴更便宜（单段字符串比较）且拒绝原因更精确——
/// "该域被宿主禁用"比"该工具未列明"更接近宿主的配置意图，排障路径更短。
/// </para>
/// <para>
/// <b>未配置即零开销</b>：<see cref="ToolPolicyRules.IsConfigured"/> 为 <see langword="false"/>
/// 时直接返回 <see cref="ToolPolicyDecision.Allowed"/>（等价无策略行为），且不做任何 glob 匹配
/// （默认部署的工具调用热路径不承担策略面的成本）。
/// </para>
/// </remarks>
public static class ToolPolicyMatcher
{
    /// <summary>
    /// 对一次工具调用求值策略（四步顺序：域 Deny → 域 Allow → 工具名 Deny → 工具名 Allow）。
    /// </summary>
    /// <param name="toolName">契约工具名（<c>域.动作</c> 形态）。</param>
    /// <param name="rules">策略规则（四个模式集合）。</param>
    /// <returns>匹配判定结果；放行时为 <see cref="ToolPolicyDecision.Allowed"/>。</returns>
    public static ToolPolicyDecision Evaluate(string toolName, ToolPolicyRules rules)
    {
        if (string.IsNullOrWhiteSpace(toolName) || rules is null || !rules.IsConfigured)
        {
            return ToolPolicyDecision.Allowed;
        }

        var domain = DomainOf(toolName);

        // ① 域 Deny（优先于域 Allow）
        if (MatchesAny(rules.DomainDeny, domain))
        {
            return ToolPolicyDecision.DomainDenied;
        }

        // ② 域 Allow（非空时未命中即拒）
        if (rules.DomainAllow is { Count: > 0 } && !MatchesAny(rules.DomainAllow, domain))
        {
            return ToolPolicyDecision.DomainNotAllowed;
        }

        // ③ 工具名 Deny（优先于工具名 Allow）
        if (MatchesAny(rules.ToolDeny, toolName))
        {
            return ToolPolicyDecision.ToolDenied;
        }

        // ④ 工具名 Allow（非空时未命中即拒）
        if (rules.ToolAllow is { Count: > 0 } && !MatchesAny(rules.ToolAllow, toolName))
        {
            return ToolPolicyDecision.ToolNotAllowed;
        }

        return ToolPolicyDecision.Allowed;
    }

    /// <summary>任一模式命中即真（<b>空集合恒假</b>——调用方须先判"该轴是否启用"）。</summary>
    public static bool MatchesAny(IReadOnlyList<string>? patterns, string value)
    {
        if (patterns is null)
        {
            return false;
        }

        foreach (var pattern in patterns)
        {
            if (ToolNamePattern.IsMatch(pattern, value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>取工具名所属域（首个 <c>.</c> 之前的部分；无点号时即工具名本身）。</summary>
    public static string DomainOf(string toolName)
    {
        // 注：netstandard2.0 没有 IndexOf(char, StringComparison)，用字符串重载。
        var index = toolName.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? toolName.Substring(0, index) : toolName;
    }
}
