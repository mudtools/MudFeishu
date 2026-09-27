// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 指标模板信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceMetricTemplate
{
    /// <summary>
    /// <para>指标模板 ID</para>
    /// <para>示例值：7296488199415660563</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>模版名称</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>模板描述</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18nName? Description { get; set; }

    /// <summary>
    /// <para>模版状态：to_be_configured（待完成配置）/ to_be_activated（待启用）/ enabled（已启用）/ disabled（已停用）</para>
    /// <para>示例值：to_be_configured</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>模版是否分人群设置指标</para>
    /// </summary>
    [JsonPropertyName("is_set_by_group")]
    public bool? IsSetByGroup { get; set; }

    /// <summary>
    /// <para>模版指标总分计算方式：review_manually（手动评估）/ sum（加和计算）/ weight（加权计算）/ formula（自定义公式）</para>
    /// <para>示例值：weight</para>
    /// </summary>
    [JsonPropertyName("total_metric_score_method")]
    public string? TotalMetricScoreMethod { get; set; }

    /// <summary>
    /// <para>指标权重计算方式：sum_of_metric_weights_for_each_dimension_equals_1（每个维度内的指标权重之和等于 100%）/ total_sum_of_all_metric_weight_equals_1（全部指标权重之和等于 100%）</para>
    /// </summary>
    [JsonPropertyName("metric_weight_method")]
    public string? MetricWeightMethod { get; set; }

    /// <summary>
    /// <para>指标维度列表</para>
    /// </summary>
    [JsonPropertyName("metric_dimensions")]
    public PerformanceMetricDimension[]? MetricDimensions { get; set; }

    /// <summary>
    /// <para>指标列表</para>
    /// </summary>
    [JsonPropertyName("metrics")]
    public PerformanceMetricInTemplate[]? Metrics { get; set; }

    /// <summary>
    /// <para>人群分组</para>
    /// </summary>
    [JsonPropertyName("groups")]
    public PerformanceMetricGroup[]? Groups { get; set; }
}
