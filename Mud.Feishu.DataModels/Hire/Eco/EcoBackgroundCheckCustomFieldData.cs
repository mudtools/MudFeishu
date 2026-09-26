// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 背调自定义字段
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class EcoBackgroundCheckCustomFieldData
{
    /// <summary>
    /// <para>自定义字段类型：text（单行文本，最多 100 个汉字）、textarea（多行文本，最多 200 个汉字）、number（数字）、boolean（布尔）、select（单选）、multiselect（多选）、date（日期）、file（附件）、resume（候选人简历）</para>
    /// <para>必填：是</para>
    /// <para>示例值：text</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>自定义字段的标识，在同一账号内唯一</para>
    /// <para>必填：是</para>
    /// <para>示例值：candidate_resume</para>
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// <para>自定义字段的名称，用户在安排背调表单看到的控件标题</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18n? Name { get; set; }

    /// <summary>
    /// <para>是否必填</para>
    /// <para>必填：是</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_required")]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// <para>自定义字段的描述；如果为输入控件，即用户在安排背调表单看到的 placeholder 或提示文字</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18n? Description { get; set; }

    /// <summary>
    /// <para>type 为 select 或 multiselect 时必填，单选或多选的选项</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("options")]
    public EcoBackgroundCheckCustomFieldOption[]? Options { get; set; }
}
