// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 指标模板中的指标信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceMetricInTemplate
{
    /// <summary>
    /// <para>所属人群分组 ID，模板分人群设置指标时，指标维度按照人群分组；未分人群设置时该值为空</para>
    /// <para>示例值：7272581996315099155</para>
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    /// <summary>
    /// <para>指标 ID（指标的统一标识 ID；如果模板存在分组，需要 + 分组 ID 才能标识到指标模板唯一的指标）</para>
    /// <para>示例值：7272581996315099155</para>
    /// </summary>
    [JsonPropertyName("metric_id")]
    public string? MetricId { get; set; }

    /// <summary>
    /// <para>指标名称（指标在当前模板中的名称）</para>
    /// <para>示例值：销售额</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>指标类型 ID</para>
    /// <para>示例值：7272581996315099155</para>
    /// </summary>
    [JsonPropertyName("type_id")]
    public string? TypeId { get; set; }

    /// <summary>
    /// <para>指标字段信息</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public PerformanceMetricFieldInTemplate[]? Fields { get; set; }

    /// <summary>
    /// <para>指标是否引自指标库</para>
    /// </summary>
    [JsonPropertyName("is_from_library")]
    public bool? IsFromLibrary { get; set; }

    /// <summary>
    /// <para>评分设置类型：socre_manually（手动评分，飞书原始枚举值拼写如此）/ score_by_formula（公式评分）</para>
    /// <para>示例值：score_by_formula</para>
    /// </summary>
    [JsonPropertyName("scoring_setting_type")]
    public string? ScoringSettingType { get; set; }

    /// <summary>
    /// <para>数据源录入人</para>
    /// </summary>
    [JsonPropertyName("data_source_inputters")]
    public UserIdInfo[]? DataSourceInputters { get; set; }

    /// <summary>
    /// <para>指标维度 ID</para>
    /// <para>示例值：7272581996315099155</para>
    /// </summary>
    [JsonPropertyName("metric_dimension_id")]
    public string? MetricDimensionId { get; set; }

    /// <summary>
    /// <para>评估规则</para>
    /// </summary>
    [JsonPropertyName("review_rule_config")]
    public PerformanceMetricReviewRuleConfig? ReviewRuleConfig { get; set; }
}
