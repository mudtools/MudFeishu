// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效模板评估题信息（可能是评估项或者填写项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewField
{
    /// <summary>
    /// <para>评估题 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("field_id")]
    public string? FieldId { get; set; }

    /// <summary>
    /// <para>评估题名称（填写项/评估项/标签填写题名称，填写项时字段可能为空值）</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>评估项 ID，详细信息请参考获取评估项配置接口</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("indicator_id")]
    public string? IndicatorId { get; set; }

    /// <summary>
    /// <para>标签填写题 ID，详细信息请参考获取标签填写题配置接口</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("tag_based_question_id")]
    public string? TagBasedQuestionId { get; set; }

    /// <summary>
    /// <para>O 的填写项标题</para>
    /// </summary>
    [JsonPropertyName("objective_text_qustion_title")]
    public I18nName? ObjectiveTextQustionTitle { get; set; }

    /// <summary>
    /// <para>KR 的填写项标题</para>
    /// </summary>
    [JsonPropertyName("keyresult_text_qustion_title")]
    public I18nName? KeyresultTextQustionTitle { get; set; }

    /// <summary>
    /// <para>关联的父级评估项 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("parent_field_id")]
    public string? ParentFieldId { get; set; }

    /// <summary>
    /// <para>指标模板 ID</para>
    /// <para>示例值：7494252079230222371</para>
    /// </summary>
    [JsonPropertyName("kpi_template_id")]
    public string? KpiTemplateId { get; set; }
}
