// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才自定义字段值（获取人才 v1/v2 详情/列表响应自定义字段 value 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TalentCustomizedValue
{
    /// <summary>
    /// <para>文本值（单行/多行文本）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>单选项</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("option")]
    public TalentCustomizedOption? Option { get; set; }

    /// <summary>
    /// <para>多选项列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("option_list")]
    public TalentCustomizedOption[]? OptionList { get; set; }

    /// <summary>
    /// <para>时间段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("time_range")]
    public TalentCustomizedTimeRange? TimeRange { get; set; }

    /// <summary>
    /// <para>日期/月/年选择的秒级时间戳字符串</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// <para>数字值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; set; }

    /// <summary>
    /// <para>附件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_attachment")]
    public TalentCustomizedAttachment[]? CustomizedAttachment { get; set; }
}
