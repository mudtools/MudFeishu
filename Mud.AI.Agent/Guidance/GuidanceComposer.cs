// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Guidance;

/// <summary>
/// 单个域的 guidance 资产（域 = 工具名首个 <c>.</c> 之前的部分，如 <c>bitable</c>）。
/// </summary>
/// <param name="Domain">域名（装配与截断报告的可读标识）。</param>
/// <param name="Content">guidance 正文（由产品侧生成器发射的域资产表提供）。</param>
public readonly record struct GuidanceBlock(string Domain, string Content);

/// <summary>
/// guidance 装配结果（<b>可断言信号</b>：截断与否落在返回值上，而不是只写日志）。
/// </summary>
/// <param name="Instructions">最终指令（宿主指令在前，guidance 在后）。</param>
/// <param name="Truncated">是否因超过预算额度而丢弃了部分域。</param>
/// <param name="IncludedDomains">实际注入的域（按输入顺序）。</param>
/// <param name="OmittedDomains">因超限被丢弃的域（按输入顺序；<paramref name="Truncated"/> 为 false 时为空）。</param>
public sealed record GuidanceResult(
    string Instructions,
    bool Truncated,
    IReadOnlyList<string> IncludedDomains,
    IReadOnlyList<string> OmittedDomains);

/// <summary>
/// 域级 guidance 装配器：把"已启用工具所属域"的 guidance 追加到宿主指令之后。
/// </summary>
/// <remarks>
/// <b>为什么是纯字符串函数（依赖方向）</b>：guidance 真相源由产品侧源生成器发射进工具面实现包，而本类型位于
/// <b>产品中立</b>的 AI 运行期包（工具包单向依赖运行期，不可反向）⇒ 工具包经自家工具源把<b>已启用域</b>的
/// guidance 传进来，本类只做装配与截断：零反向依赖，截断逻辑可独立单测。
/// <para>
/// <b>顺序语义</b>：宿主 <c>Instructions</c> 恒在 guidance <b>之前</b>（宿主指令优先，域 guidance 只是补充；
/// 反过来会让域资产覆盖宿主的语气与边界设定）。
/// </para>
/// <para>
/// <b>预算为什么是参数而不是常量</b>：额度值是<b>产品语义</b>（由该产品域资产实测长度与余量策略推出），
/// 把常量搬进中立包等于把某个产品的预算焊进包族底层（违背「共享结构、不共享语义」）⇒ 本类只接收
/// <c>maxGuidanceLength</c>，由产品侧（如 <c>FeishuGuidanceBudget.MaxGuidanceLength</c>）声明自己的额度；
/// 这也是本类型的"零配置键"纪律：不做可选项、不做默认值。
/// </para>
/// <para>
/// <b>超限策略</b>：按<b>输入顺序</b>整域丢弃尾部域，<see cref="GuidanceResult"/> 给出 <c>Truncated</c> 与丢弃清单；
/// 失败方式本身静默（无日志无异常），故调用方<b>必须</b>消费返回值上的信号。
/// </para>
/// </remarks>
public static class GuidanceComposer
{
    /// <summary>块间分隔符。</summary>
    private const string Separator = "\n\n";

    /// <summary>
    /// 装配最终指令：宿主指令 +（已启用域的）guidance。
    /// </summary>
    /// <param name="hostInstructions">宿主指令（可空/空串）。</param>
    /// <param name="blocks">已启用工具所属域的 guidance（可空/空集 = 不注入任何域资产）。</param>
    /// <param name="maxGuidanceLength">guidance 本体的总长硬上限（字符数；由产品侧声明，必须为正）。</param>
    /// <returns>装配结果（含 <c>Truncated</c> 信号与丢弃清单）。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxGuidanceLength"/> 非正。</exception>
    public static GuidanceResult Compose(
        string? hostInstructions,
        IReadOnlyList<GuidanceBlock>? blocks,
        int maxGuidanceLength)
    {
        if (maxGuidanceLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxGuidanceLength),
                maxGuidanceLength,
                "guidance 预算必须为正数——预算为 0/负数会让全部域资产静默丢弃（本类不做可选项，故无默认值）");
        }

        var host = hostInstructions ?? string.Empty;
        if (blocks is null || blocks.Count == 0)
        {
            // 未启用任何带 guidance 的工具：不污染指令（保持宿主原文，零追加）。
            return new GuidanceResult(host, false, [], []);
        }

        var included = new List<string>();
        var omitted = new List<string>();
        var builder = new StringBuilder(host);

        // 额度只计 **guidance 本体**：宿主 Instructions 不得挤占域资产预算——
        // 企业 system prompt 普遍 > 预算，用 builder.Length 作判据会让全部域 guidance 静默丢弃。
        var guidanceLength = 0;

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block.Content))
            {
                continue;
            }

            // 前缀长度：追加前的分隔符 + 正文（宿主指令为空时不额外插入前导空行）。
            var extra = (builder.Length > 0 ? Separator.Length : 0) + block.Content.Length;
            if (guidanceLength + extra > maxGuidanceLength)
            {
                omitted.Add(block.Domain);
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(Separator);
            }

            builder.Append(block.Content);
            guidanceLength += extra;
            included.Add(block.Domain);
        }

        return new GuidanceResult(builder.ToString(), omitted.Count > 0, included, omitted);
    }
}
