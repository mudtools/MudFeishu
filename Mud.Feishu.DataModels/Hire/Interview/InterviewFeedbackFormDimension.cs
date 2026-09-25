// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 面试反馈表维度（面试反馈表模块子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewFeedbackFormDimension
{
    /// <summary>
    /// <para>维度 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>维度名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>维度描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18nName? Description { get; set; }

    /// <summary>
    /// <para>维度类型：1 单选题 / 2 多选题 / 3 主观题 / 5 职级建议 / 6 打分题（单选）/ 7 打分题（填空）/ 10 系统预置-结论 / 11 系统预置-分数 / 12 系统预置-记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// <para>是否启用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// <para>维度顺序</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sequence")]
    public int? Sequence { get; set; }

    /// <summary>
    /// <para>是否必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_required")]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// <para>维度权重</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    /// <summary>
    /// <para>评价维度的分数配置，适用于打分题（type=6 或 type=7）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("score_dimension_config")]
    public ScoreDimensionConfig? ScoreDimensionConfig { get; set; }

    /// <summary>
    /// <para>选项列表，适用于单选题（type=1）和多选题（type=2）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("option_items")]
    public InterviewDimensionOption[]? OptionItems { get; set; }

    /// <summary>
    /// <para>是否展示「无法判断」选项，仅适用于「职级建议」的维度类型（type=5）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("display_not_evident")]
    public bool? DisplayNotEvident { get; set; }

    /// <summary>
    /// <para>能力项列表，全类型适用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ability_list")]
    public DimensionAbility[]? AbilityList { get; set; }

    /// <summary>
    /// <para>维度间关联配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("related_dimension_config")]
    public RelatedDimensionConfig? RelatedDimensionConfig { get; set; }
}
