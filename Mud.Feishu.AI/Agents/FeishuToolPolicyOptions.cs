// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// <b>S-1：工具策略（glob / 域级）</b>——配置节 <c>FeishuAgent:ToolPolicy</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>与既有 <see cref="FeishuAgentOptions.Tools"/> / <see cref="FeishuAgentOptions.WriteAllowList"/>
/// 的关系 = <b>叠加收敛</b></b>：三者同时生效，任一拒绝即拒。既有白名单回答"哪些工具被启用"
/// （装配期，未启用者根本不在注册表里），本策略回答"已启用的工具里，哪些**允许执行**"
/// （运行期，早于授权门禁）。两轴不重叠，故不构成双白名单。
/// </para>
/// <para>
/// <b>判定顺序（先便宜且拒绝原因更精确的轴）</b>：
/// <list type="number">
/// <item><b>域 Deny</b>（命中即拒，reason_code = <c>domain_not_allowed</c>）；</item>
/// <item><b>域 Allow</b>（非空时未命中即拒，reason_code = <c>domain_not_allowed</c>）；</item>
/// <item><b>工具名 Deny</b>（命中即拒；写工具 → <c>write_not_allowed</c>，只读 → <c>tool_not_allowed</c>）；</item>
/// <item><b>工具名 Allow</b>（非空时未命中即拒；同上分类）。</item>
/// </list>
/// <b>Deny 恒优先于 Allow</b>（同一轴上先判 Deny）——这与"fail-closed 默认拒未列明"一致：
/// 一旦配了 Allow，未列明者被拒；而 Deny 的存在保证"就算列进了 Allow 也能被显式否决"。
/// </para>
/// <para>
/// <b>空集合语义（易误读，逐条写明）</b>：
/// <list type="bullet">
/// <item><b>四个集合全空</b> ⇒ 本策略<b>不生效</b>（逐字节等价改造前行为；这是默认值）。</item>
/// <item><b>Allow 为空、Deny 非空</b> ⇒ <b>该轴不启用</b>（即"只拉黑名单"模式）。
/// 理由：把空 Allow 读成"全拒"会让最常见的 <c>"Deny": ["payroll.*"]</c> 变成
/// <b>全工具不可用</b>——那是方案 §10 风险登记里"glob 策略误配置导致生产不可用"的形态，
/// 必须由语义本身排除，而不是靠宿主不犯错。</item>
/// <item><b>Allow 非空</b> ⇒ 未命中即拒（真正的白名单模式）。</item>
/// </list>
/// </para>
/// <para>
/// <b>非法模式 fail-closed</b>：任何模式非法（见 <c>ToolNamePattern</c> 的语法）都在
/// <see cref="Validate"/> 期抛——拼错一个字符不会静默变成"不命中"（那会让宿主以为策略生效了）。
/// </para>
/// </remarks>
public sealed class FeishuToolPolicyOptions
{
    /// <summary>
    /// 工具名白名单（glob）。空 = 该轴不启用（<b>不是</b>"全放"也不是"全拒"，见类型 remarks）。
    /// </summary>
    /// <remarks>示例：<c>["bitable.*", "im.get_*", "calendar.*"]</c>。</remarks>
    public string[] Allow { get; set; } = [];

    /// <summary>
    /// 工具名黑名单（glob，<b>优先于</b> <see cref="Allow"/>）。空 = 该轴不启用。
    /// </summary>
    /// <remarks>示例：<c>["*.delete_*", "drive.transfer_owner", "payroll.*"]</c>。</remarks>
    public string[] Deny { get; set; } = [];

    /// <summary>域级策略（域 = 工具名首个 <c>.</c> 之前的部分）：先于工具名轴求值。</summary>
    public FeishuToolPolicyDomainOptions Domains { get; set; } = new();

    /// <summary>本策略是否已配置（四个集合全空 ⇒ 未配置 ⇒ 不生效）。</summary>
    public bool IsConfigured
        => Allow.Length > 0 || Deny.Length > 0 || Domains.Allow.Length > 0 || Domains.Deny.Length > 0;

    /// <summary>
    /// 校验策略合法性（非法即抛；由 <see cref="FeishuAgentOptions.Validate"/> 调用，装配期 fail-fast）。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在非法 glob 模式或空项。</exception>
    public void Validate()
    {
        ValidatePatterns(Allow, $"{nameof(FeishuAgentOptions.ToolPolicy)}.{nameof(Allow)}");
        ValidatePatterns(Deny, $"{nameof(FeishuAgentOptions.ToolPolicy)}.{nameof(Deny)}");

        // 域轴用**单段**模式（域就是单段）：写成 "payroll.*" 在域轴上恒不命中——
        // 那会让宿主的 Deny 静默失效（比配错更危险），故在装配期直接拒。
        ValidateDomainPatterns(
            Domains?.Allow, $"{nameof(FeishuAgentOptions.ToolPolicy)}.{nameof(Domains)}.{nameof(FeishuToolPolicyDomainOptions.Allow)}");
        ValidateDomainPatterns(
            Domains?.Deny, $"{nameof(FeishuAgentOptions.ToolPolicy)}.{nameof(Domains)}.{nameof(FeishuToolPolicyDomainOptions.Deny)}");
    }

    /// <summary>域轴模式必须单段（不允许 <c>.</c>）：域是工具名的首段，多段模式在该轴上恒不命中。</summary>
    private static void ValidateDomainPatterns(string[]? patterns, string path)
    {
        if (patterns is null)
        {
            throw new InvalidOperationException($"FeishuAgent:{path} 不得为 null（空数组表示该轴不启用）");
        }

        ValidatePatterns(patterns, path);

        foreach (var pattern in patterns)
        {
            if (pattern.IndexOf('.') >= 0)
            {
                throw new InvalidOperationException(
                    $"FeishuAgent:{path} 的模式 '{pattern}' 含点号——域轴只接受**单段**模式"
                    + "（域 = 工具名首个 '.' 之前的部分，如 'payroll' / 'bit*' / '*'）。"
                    + "写成 'payroll.*' 在域轴上**恒不命中**（静默失效），故在装配期拒绝。");
            }
        }
    }

    /// <summary>逐条校验 glob 模式（空项/非法字符/空段/连续星号一律拒）。</summary>
    private static void ValidatePatterns(string[] patterns, string path)
    {
        if (patterns is null)
        {
            throw new InvalidOperationException($"FeishuAgent:{path} 不得为 null（空数组表示该轴不启用）");
        }

        foreach (var pattern in patterns)
        {
            if (!AgentTools.ToolNamePattern.IsLegal(pattern))
            {
                throw new InvalidOperationException(
                    $"FeishuAgent:{path} 含非法模式 '{pattern}'——合法形态为点分小写段（段内允许 a-z 0-9 _ 与单个 *），"
                    + "如 'bitable.*' / 'im.get_*' / '*.delete_*' / '*'（详见 ToolNamePattern 的语法说明）。"
                    + "非法模式在装配期即拒，避免「拼错一个字符 ⇒ 策略静默不生效」");
            }
        }
    }
}

/// <summary>
/// 域级策略（<c>FeishuAgent:ToolPolicy:Domains</c>）：域 = 工具名首个 <c>.</c> 之前的部分。
/// </summary>
/// <remarks>
/// 与工具名轴的关系：<b>域轴先求值</b>（更便宜，且拒绝原因更精确——"这个域被禁"比
/// "这个工具未列明"更接近宿主的意图）。语义与空集处理同 <see cref="FeishuToolPolicyOptions"/>。
/// </remarks>
public sealed class FeishuToolPolicyDomainOptions
{
    /// <summary>允许的域集合。空 = 该轴不启用。</summary>
    /// <remarks>示例：<c>["bitable","im","calendar"]</c>。</remarks>
    public string[] Allow { get; set; } = [];

    /// <summary>禁止的域集合（<b>优先于</b> <see cref="Allow"/>）。空 = 该轴不启用。</summary>
    /// <remarks>示例：<c>["payroll","performance"]</c>。</remarks>
    public string[] Deny { get; set; } = [];
}
