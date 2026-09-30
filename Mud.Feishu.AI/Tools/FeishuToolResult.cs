// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具执行结果（§7.1 分型结果契约）：替代 <c>Task&lt;string&gt;</c> 的结构化返回。
/// </summary>
/// <remarks>
/// <para>
/// 当前阶段（批次 1）仅含 <see cref="Text"/> 与截断标记，与原 <c>Task&lt;string&gt;</c> 语义等价。
/// 后续批次（§7.1 多模态）将扩展 <see cref="Data"/>（结构化 JSON）与 <c>Parts</c>（多模态分片：
/// FilePart/ImagePart 等，不含 byte[]）。
/// </para>
/// <para>
/// 设计决策（C.6 决策 2）：移除 <c>implicit operator string</c>——项目未发布，无需兼容老代码，
/// 调用方显式使用 <see cref="ToString"/> 获取文本。
/// </para>
/// </remarks>
public sealed class FeishuToolResult
{
    /// <summary>
    /// 人类可读文本结果（已投影/截断，含 <c>truncated</c> 等标记）。
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// 结构化 JSON 结果（可空；<see cref="FromData"/> 构造，<see cref="ToString"/> 未填充 <see cref="Text"/> 时回退序列化）。
    /// </summary>
    /// <remarks>
    /// <b>与 <c>output_schema</c> 无关（R2-05 决策）</b>：曾计划用编译期 <c>x-feishu.output_schema</c>
    /// 在运行期对本属性做字段裁剪，已驳回——各执行器的<b>有意策展投影</b>才是模型可见结果的真契约，
    /// 叠加 Schema 白名单会把策展后的键（如 <c>task_guid</c>）当作未声明字段丢弃。原注释写"后续批次启用"
    /// 属陈旧表述（该批次已定为"不启用"）。
    /// </remarks>
    public JsonNode? Data { get; init; }

    /// <summary>结果是否被截断。</summary>
    public bool Truncated { get; init; }

    /// <summary>截断原因（可空）。</summary>
    public string? TruncationReason { get; init; }

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
    /// 从结构化 JSON 构造结果（便捷工厂）。
    /// </summary>
    /// <param name="data">结构化 JSON。</param>
    /// <param name="truncated">是否被截断。</param>
    /// <param name="truncationReason">截断原因。</param>
    /// <returns>工具结果实例。</returns>
    public static FeishuToolResult FromData(JsonNode data, bool truncated = false, string? truncationReason = null)
        => new()
        {
            Text = data.ToJsonString(),
            Data = data,
            Truncated = truncated,
            TruncationReason = truncationReason,
        };

    /// <summary>
    /// 从错误文本构造结果（便捷工厂）。
    /// </summary>
    /// <param name="errorText">结构化错误文本。</param>
    /// <returns>工具结果实例。</returns>
    public static FeishuToolResult FromError(string errorText)
        => new() { Text = errorText };

    /// <summary>
    /// 返回回填模型的文本（显式方法，替代 implicit operator string）。
    /// </summary>
    /// <returns>结果文本（不为空时返回 <see cref="Text"/>；否则返回 <see cref="Data"/> 的 JSON 序列化）。</returns>
    public override string ToString()
        => Text ?? Data?.ToJsonString() ?? string.Empty;
}
