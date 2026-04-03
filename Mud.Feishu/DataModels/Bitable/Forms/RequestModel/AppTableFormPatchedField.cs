// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>更新表单字段值</para>
/// </summary>
public class AppTableFormPatchedField
{
    /// <summary>
    /// <para>要更新的表单问题的前一个表单问题的 `field_id`，用于更新当前表单问题的位置。若该字段为空字符串，则表示将该表单问题的顺序排至首个位置。</para>
    /// <para>必填：否</para>
    /// <para>示例值：fldjX7dUj5</para>
    /// </summary>
    [JsonPropertyName("pre_field_id")]
    public string? PreFieldId { get; set; }

    /// <summary>
    /// <para>表单问题</para>
    /// <para>必填：否</para>
    /// <para>示例值：任务名称</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>问题描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：请概述该任务</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>该问题是否必填。可选值：</para>
    /// <para>- true：必填</para>
    /// <para>- false：非必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    /// <summary>
    /// <para>该问题是否可见。当值为 false 时，不允许更新其他字段。可选值：</para>
    /// <para>- true：可见</para>
    /// <para>- false：不可见</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("visible")]
    public bool? Visible { get; set; }

    /// <summary>
    /// <para>富文本描述</para>
    /// <para>必填：否</para>
    /// <para>最大长度：500</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("rich_description")]
    public AppRichDescriptionSegment[]? RichDescriptions { get; set; }
}