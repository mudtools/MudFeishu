// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 指标模板的指标维度信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceMetricDimension
{
    /// <summary>
    /// <para>所属人群分组 ID，模板分人群设置指标时，指标维度按照人群分组；未分人群设置时该值为空</para>
    /// <para>示例值：7296753366268215316</para>
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    /// <summary>
    /// <para>指标维度 ID</para>
    /// <para>示例值：7296753366268215316</para>
    /// </summary>
    [JsonPropertyName("metric_dimension_id")]
    public string? MetricDimensionId { get; set; }

    /// <summary>
    /// <para>指标维度名称</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>指标评估规则 ID</para>
    /// <para>示例值：7296701873237786643</para>
    /// </summary>
    [JsonPropertyName("evaluation_rule_id_for_each_metric")]
    public string? EvaluationRuleIdForEachMetric { get; set; }

    /// <summary>
    /// <para>维度权重，如果没有设置则返回为空（和设置为 0 进行区分），单位为百分比</para>
    /// <para>示例值：90</para>
    /// </summary>
    [JsonPropertyName("dimension_weight")]
    public string? DimensionWeight { get; set; }

    /// <summary>
    /// <para>维度描述</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18nName? Description { get; set; }

    /// <summary>
    /// <para>各指标的评估规则：0（使用相同规则）/ 1（使用不同规则）</para>
    /// </summary>
    [JsonPropertyName("review_rule_option")]
    public int? ReviewRuleOption { get; set; }

    /// <summary>
    /// <para>被评估人添加指标的设置</para>
    /// </summary>
    [JsonPropertyName("custom_metric_config")]
    public PerformanceCustomMetricConfig? CustomMetricConfig { get; set; }
}
