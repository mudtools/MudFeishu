// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 指标库中的指标信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceMetricInLibrary
{
    /// <summary>
    /// <para>指标 ID</para>
    /// <para>示例值：7272581996315099155</para>
    /// </summary>
    [JsonPropertyName("metric_id")]
    public string? MetricId { get; set; }

    /// <summary>
    /// <para>指标名称</para>
    /// <para>示例值：销售额</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>指标类型 ID</para>
    /// <para>示例值：7272578300650717203</para>
    /// </summary>
    [JsonPropertyName("type_id")]
    public string? TypeId { get; set; }

    /// <summary>
    /// <para>指标标签信息</para>
    /// </summary>
    [JsonPropertyName("tags")]
    public PerformanceMetricTagInfo[]? Tags { get; set; }

    /// <summary>
    /// <para>指标字段信息</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public PerformanceMetricFieldInLibrary[]? Fields { get; set; }

    /// <summary>
    /// <para>指标评分设置类型：score_manually（手动评分）/ score_by_formula（公式评分）</para>
    /// <para>示例值：score_by_formula</para>
    /// </summary>
    [JsonPropertyName("scoring_setting_type")]
    public string? ScoringSettingType { get; set; }

    /// <summary>
    /// <para>指标评分公式</para>
    /// </summary>
    [JsonPropertyName("scoring_formula")]
    public PerformanceMetricScoringFormula? ScoringFormula { get; set; }

    /// <summary>
    /// <para>指标数据源录入人</para>
    /// </summary>
    [JsonPropertyName("data_source_inputters")]
    public UserIdInfo[]? DataSourceInputters { get; set; }

    /// <summary>
    /// <para>指标可用范围：admins_and_reviewees（允许管理员下发和被评估人选用）/ only_admins（仅允许管理员下发）</para>
    /// <para>示例值：admins_and_reviewees</para>
    /// </summary>
    [JsonPropertyName("range_of_availability")]
    public string? RangeOfAvailability { get; set; }

    /// <summary>
    /// <para>指标状态是否为启用</para>
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }
}
