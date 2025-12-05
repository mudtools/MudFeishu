// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Organization;

/// <summary>
/// 人员类型的选项信息。
/// </summary>
public class EmployeeTypeEnum
{
    /// <summary>
    /// <para>人员类型的选项 ID。</para>
    /// </summary>
    [JsonPropertyName("enum_id")]
    public string? EnumId { get; set; }

    /// <summary>
    /// <para>人员类型的选项编号。</para>
    /// </summary>
    [JsonPropertyName("enum_value")]
    public string? EnumValue { get; set; }

    /// <summary>
    /// <para>人员类型的选项内容。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `100` 字符</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>人员类型的选项类型。</para>
    /// <para>**可选值有**：</para>
    /// <para>1:内置类型,2:自定义</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：内置类型</item>
    /// <item>2：自定义</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("enum_type")]
    public int? EnumType { get; set; }

    /// <summary>
    /// <para>人员类型的选项激活状态。</para>
    /// <para>**可选值有**：</para>
    /// <para>1:激活,2:未激活</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：激活</item>
    /// <item>2：未激活</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("enum_status")]
    public int? EnumStatus { get; set; }

    /// <summary>
    /// <para>选项内容的国际化配置。</para>
    /// </summary>
    [JsonPropertyName("i18n_content")]
    public I18nContent[]? I18nContent { get; set; }
}
