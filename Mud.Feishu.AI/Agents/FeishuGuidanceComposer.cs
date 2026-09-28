// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 单个域的 guidance 资产（域 = 工具名首个 <c>.</c> 之前的部分，如 <c>bitable</c>）。
/// </summary>
/// <param name="Domain">域名（装配与截断报告的可读标识）。</param>
/// <param name="Content">guidance 正文（来自生成器发射的 <c>FeishuToolGuidance.ByDomain</c>）。</param>
public readonly record struct FeishuGuidanceBlock(string Domain, string Content);

/// <summary>
/// guidance 装配结果（<b>可断言信号</b>：截断与否落在返回值上，而不是只写日志）。
/// </summary>
/// <param name="Instructions">最终指令（宿主指令在前，guidance 在后）。</param>
/// <param name="Truncated">是否因超过 <see cref="FeishuGuidanceComposer.MaxGuidanceLength"/> 而丢弃了部分域。</param>
/// <param name="IncludedDomains">实际注入的域（按输入顺序）。</param>
/// <param name="OmittedDomains">因超限被丢弃的域（按输入顺序；<paramref name="Truncated"/> 为 false 时为空）。</param>
public sealed record FeishuGuidanceResult(
    string Instructions,
    bool Truncated,
    IReadOnlyList<string> IncludedDomains,
    IReadOnlyList<string> OmittedDomains);

/// <summary>
/// 域级 guidance 装配器（<b>WP6 / AT-F09</b>）：把"已启用工具所属域"的 guidance 追加到宿主指令之后。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是纯字符串函数（依赖方向）</b>：guidance 的真相源由源生成器发射进工具面实现包
/// （<c>Mud.Feishu.AI.Tools.Generated.FeishuToolGuidance</c>），而本类在 AI 底座包
/// （工具包单向依赖 AI，不可反向）。故工具包经 <see cref="Tools.FeishuAgentToolSource.GetGuidance"/>
/// 把<b>已启用域</b>的 guidance 传进来，本类只做装配与截断——零反向依赖，且截断逻辑可独立单测。
/// </para>
/// <para>
/// <b>顺序语义</b>：宿主 <c>Instructions</c> 恒在 guidance <b>之前</b>（宿主指令优先，
/// 域 guidance 只是补充；反过来会让域资产覆盖宿主的语气与边界设定）。
/// </para>
/// <para>
/// <b>上限是常量（R4.1 评审 R-4）</b>：不设公开配置键——"可配"意味着新配置键，
/// 与 R5 治理（每公开属性须有消费点 + 守卫登记）冲突，且当前没有第二个取值需求。
/// 超限按<b>输入顺序</b>（= 域优先级）丢弃尾部域，并在 <see cref="FeishuGuidanceResult"/> 上给出
/// <c>Truncated</c> 与丢弃清单，调用方可据此告警或调整资产长度。
/// </para>
/// </remarks>
public static class FeishuGuidanceComposer
{
    /// <summary>guidance 总长硬上限（字符数；超限按域顺序截断）。</summary>
    public const int MaxGuidanceLength = 2048;

    private const string Separator = "\n\n";

    /// <summary>
    /// 装配最终指令：宿主指令 +（已启用域的）guidance。
    /// </summary>
    /// <param name="hostInstructions">宿主指令（可空/空串）。</param>
    /// <param name="blocks">已启用工具所属域的 guidance（可空/空集 = 不注入任何域资产）。</param>
    /// <returns>装配结果（含 <c>Truncated</c> 信号与丢弃清单）。</returns>
    public static FeishuGuidanceResult Compose(string? hostInstructions, IReadOnlyList<FeishuGuidanceBlock>? blocks)
    {
        var host = hostInstructions ?? string.Empty;
        if (blocks is null || blocks.Count == 0)
        {
            // 未启用任何带 guidance 的工具：不污染指令（保持宿主原文，零追加）。
            return new FeishuGuidanceResult(host, false, [], []);
        }

        var included = new List<string>();
        var omitted = new List<string>();
        var builder = new StringBuilder(host);

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block.Content))
            {
                continue;
            }

            // 前缀长度：追加前的分隔符 + 正文（宿主指令为空时不额外插入前导空行）。
            var extra = (builder.Length > 0 ? Separator.Length : 0) + block.Content.Length;
            if (builder.Length + extra > MaxGuidanceLength)
            {
                omitted.Add(block.Domain);
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(Separator);
            }

            builder.Append(block.Content);
            included.Add(block.Domain);
        }

        return new FeishuGuidanceResult(builder.ToString(), omitted.Count > 0, included, omitted);
    }
}
