// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具执行结果（§7.1 分型结果契约）：替代 <c>Task&lt;string&gt;</c> 的结构化返回。
/// </summary>
/// <remarks>
/// <para>
/// 当前形态只承载 <see cref="Text"/>（人类可读文本，已投影 + 截断）与截断标记。
/// <b>R3-22：已删除零调用的 <c>Data</c> / <c>FromData</c></b>——结构化结果一律由各执行器的
/// <b>有意策展投影</b>转成文本（见下），把 <c>JsonNode</c> 挂在结果上属未被消费的"第二真相源"，
/// 会让"模型看到什么"出现两条口径。多模态分片（<c>Parts</c>）如确需引入，再按 §7.1 以显式契约新增。
/// </para>
/// <para>
/// 设计决策（C.6 决策 2）：移除 <c>implicit operator string</c>——项目未发布，无需兼容老代码，
/// 调用方显式使用 <see cref="ToString"/> 获取文本。
/// </para>
/// <para>
/// <b>与 <c>output_schema</c> 无关（R2-05 决策）</b>：曾计划用编译期 <c>x-feishu.output_schema</c>
/// 在运行期对结构化载荷做字段裁剪，已驳回——各执行器的<b>有意策展投影</b>才是模型可见结果的真契约，
/// 叠加 Schema 白名单会把策展后的键（如 <c>task_guid</c>）当作未声明字段丢弃。
/// </para>
/// </remarks>
public sealed class FeishuToolResult
{
    /// <summary>
    /// 人类可读文本结果（已投影/截断，含 <c>truncated</c> 等标记）。
    /// </summary>
    public string? Text { get; init; }

    /// <summary>结果是否被截断。</summary>
    public bool Truncated { get; init; }

    /// <summary>截断原因（可空）。</summary>
    public string? TruncationReason { get; init; }

    /// <summary>
    /// 结构化错误载荷（B2 错误契约）：成功时为 <see langword="null"/>，失败时携带分类/子类/可重试等信息。
    /// </summary>
    public ToolError? Error { get; init; }

    /// <summary>
    /// 从文本构造结果（便捷工厂）。
    /// </summary>
    /// <param name="text">结果文本。</param>
    /// <param name="truncated">是否被截断。</param>
    /// <param name="truncationReason">截断原因。</param>
    /// <returns>工具结果实例。</returns>
    public static FeishuToolResult FromText(string text, bool truncated = false, string? truncationReason = null)
        => new()
        {
            Text = text,
            Truncated = truncated,
            TruncationReason = truncationReason,
        };

    /// <summary>
    /// 从错误文本构造结果（便捷工厂）。
    /// </summary>
    /// <param name="errorText">结构化错误文本（首行 JSON 载荷 + 人类可读正文）。</param>
    /// <returns>工具结果实例。</returns>
    public static FeishuToolResult FromError(string errorText)
        => new() { Text = errorText };

    /// <summary>
    /// 从结构化错误载荷构造结果（B2 错误契约：首行 JSON + 人类可读正文）。
    /// </summary>
    /// <param name="error">结构化错误载荷。</param>
    /// <param name="humanReadableText">人类可读正文（模型可见，保持向后兼容）。</param>
    /// <returns>工具结果实例。</returns>
    public static FeishuToolResult FromError(ToolError error, string humanReadableText)
        => new() { Text = humanReadableText, Error = error };

    /// <summary>
    /// 返回回填模型的文本（显式方法，替代 implicit operator string）。
    /// </summary>
    /// <returns>结果文本（<see cref="Text"/> 为空时返回空串）。</returns>
    public override string ToString()
        => Text ?? string.Empty;
}
