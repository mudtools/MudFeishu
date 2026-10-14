// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tools;

/// <summary>
/// 产品业务码失败的<b>中立读数</b>：由产品侧读出自有码表后填入 <see cref="ToolApiOutcome{T}"/>，
/// 中立层据此构造载荷与提示——<b>中立层不认识任何产品的码表</b>（独立产品线纪律）。
/// </summary>
/// <param name="Category">错误大类（中立闭集 <c>ToolErrorCategory</c>）。</param>
/// <param name="Subtype">错误子类（中立闭集常量 <c>ToolErrorSubtype</c>）。</param>
/// <param name="Suggestions">
/// 码级恢复候选（产品侧按<b>有实证</b>的码表给出；无实证时必须为 <see langword="null"/>——
/// 猜测性码表会把模型引向错误处置）。它同时是 hint 行的码级行动来源。
/// </param>
/// <remarks>
/// <para>
/// <b>为什么读数由产品侧给出（而不是中立层自带码表）</b>：v2.3 P1-D 期间中立层自带一张产品码表
/// （<c>ForbiddenApiCodes</c> / <c>RetryableApiCodes</c> / <c>ToolErrorCodeMeta</c>），后果有二：
/// ① 独立产品线无法脱离该产品的码语义发布（P3 上游化时码表会被一并带走，且只能由"知道全部产品"
/// 的中立包维护）；② 码表新增要改中立包的公开面。现在方向反过来——
/// 产品在<b>自己读信封的那一处</b>（飞书侧 <c>FeishuApiResultReader</c>）完成读数，
/// 中立层只消费已分类的结果。
/// </para>
/// <para>
/// <b>默认值纪律</b>：产品未给读数（<see langword="null"/>）时使用 <see cref="Unclassified"/>
/// ——中性的 <c>api/upstream_error</c>，<b>不猜语义</b>（DP-R7-4：猜错的处置比"没有分类"更糟）。
/// </para>
/// <para>
/// <b>不是"分类的替身"</b>：本类型只承载<b>产品侧已经读出</b>的事实。中立的
/// 异常分类（<c>ToolErrorClassifier.Classify</c>）与它并列，两者产出同一种词汇。
/// </para>
/// </remarks>
public sealed record ToolApiFailure(
    ToolErrorCategory Category,
    string Subtype,
    IReadOnlyList<string>? Suggestions = null)
{
    /// <summary>
    /// 产品侧未提供读数时的中性回落：<c>api/upstream_error</c>（不猜语义）。
    /// </summary>
    public static ToolApiFailure Unclassified { get; } =
        new(ToolErrorCategory.Api, ToolErrorSubtype.UpstreamError);
}
