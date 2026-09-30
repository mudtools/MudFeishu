// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具风险词汇表（AT-B13 / R3 决策 D2）：配置键 <c>FeishuAgent:MaxToolRisk</c> 与 Schema 的
/// <c>x-feishu.risk</c> <b>共用同一套字面量</b>，杜绝"两套词汇"导致的静默失效。
/// </summary>
/// <remarks>
/// <para>
/// 连字符风格（<c>high-risk-write</c>）是源生成器 <c>RiskToString</c> 的既有输出，也是
/// <c>FeishuToolSchemas.golden.txt</c> 中可见的字面量；刻意<b>不</b>依赖 C# 枚举名解析
/// （<c>Enum.Parse</c> 无法消费 <c>high-risk-write</c>，而那正是宿主最容易写出的形态之一）。
/// </para>
/// <para>
/// 与 <see cref="FeishuToolRisk"/> 的关系：枚举供代码内比较（单调序关系），本词汇表供配置面/Error 消息
/// 使用——两者由 <see cref="TryParse"/> / <see cref="ToLiteral"/> 单向对应，不构成双真相源。
/// </para>
/// </remarks>
public static class FeishuToolRiskNames
{
    /// <summary>只读。</summary>
    public const string Read = "read";

    /// <summary>写操作。</summary>
    public const string Write = "write";

    /// <summary>高风险写操作。</summary>
    public const string HighRiskWrite = "high-risk-write";

    /// <summary>合法取值清单（错误消息用）。</summary>
    public const string AllowedValuesText = $"{Read} / {Write} / {HighRiskWrite}";

    /// <summary>解析风险字面量为分级枚举（非法取值返回 <see langword="false"/>）。</summary>
    /// <param name="value">风险字面量。</param>
    /// <param name="risk">解析结果。</param>
    public static bool TryParse(string? value, out FeishuToolRisk risk)
    {
        switch (value)
        {
            case Read:
                risk = FeishuToolRisk.Read;
                return true;
            case Write:
                risk = FeishuToolRisk.Write;
                return true;
            case HighRiskWrite:
                risk = FeishuToolRisk.HighRiskWrite;
                return true;
            default:
                risk = FeishuToolRisk.Read;
                return false;
        }
    }

    /// <summary>把分级枚举转回 Schema 词汇（契约守卫比对 / 报错消息用）。</summary>
    /// <param name="risk">风险分级。</param>
    public static string ToLiteral(FeishuToolRisk risk) => risk switch
    {
        FeishuToolRisk.Write => Write,
        FeishuToolRisk.HighRiskWrite => HighRiskWrite,
        _ => Read,
    };
}

/// <summary>
/// 工具身份词汇表（AT-B13 Identity 轴）：配置键 <c>FeishuAgent:AllowedIdentities</c> 与 Schema 的
/// <c>x-feishu.identity</c> <b>共用同一套字面量</b>（同 <see cref="FeishuToolRiskNames"/> 的治理口径）。
/// </summary>
/// <remarks>
/// 闭集校验（P2-9）把「拼写错误静默拒绝全部工具身份」变成装配期可读错误：
/// <see cref="Agents.FeishuAgentOptions.AllowedIdentities"/> 与工具定义 / 策略轴
/// （<c>FeishuToolBinding</c> 的 <c>identity_mismatch</c> 判定）必须共用本词汇表。
/// </remarks>
public static class FeishuToolIdentityNames
{
    /// <summary>租户/应用令牌身份（默认）。</summary>
    public const string Tenant = "tenant";

    /// <summary>用户令牌身份。</summary>
    public const string User = "user";

    /// <summary>合法取值清单（错误消息用）。</summary>
    public const string AllowedValuesText = $"{Tenant} / {User}";

    /// <summary>是否合法取值。</summary>
    /// <param name="identity">身份字面量。</param>
    public static bool IsValid(string? identity) => identity is Tenant or User;
}

/// <summary>
/// 出站内容安全模式闭集（AT-F14；配置键 <c>FeishuAgent:ContentSafetyMode</c>）。
/// </summary>
/// <remarks>
/// 与"净化"的边界：净化是<b>安全基线</b>（强制、无开关），内容安全是<b>策略</b>
/// （会改变工具语义，故允许 <c>off</c>，默认 <c>warn</c> 比官方 CLI 的默认 <c>off</c> 更安全）。
/// </remarks>
public static class ContentSafetyModes
{
    /// <summary>不扫描（宿主显式承担风险）。</summary>
    public const string Off = "off";

    /// <summary><b>默认</b>：命中即在结果前加 <c>[untrusted_content: 规则]</c> 标注，不阻断。</summary>
    public const string Warn = "warn";

    /// <summary>命中即返回结构化拒绝（不下发结果）。</summary>
    public const string Block = "block";

    /// <summary>合法取值清单（错误消息用）。</summary>
    public const string AllowedValuesText = $"{Off} / {Warn} / {Block}";

    /// <summary>是否合法取值。</summary>
    /// <param name="mode">模式字面量。</param>
    public static bool IsValid(string? mode) => mode is Off or Warn or Block;
}
