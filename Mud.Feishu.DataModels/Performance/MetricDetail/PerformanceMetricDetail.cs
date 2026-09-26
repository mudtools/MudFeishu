// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 被评估人的关键指标详情
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceMetricDetail
{
    /// <summary>
    /// <para>指标 ID，详情可查看获取指标列表接口</para>
    /// <para>示例值：7272581996315099155</para>
    /// </summary>
    [JsonPropertyName("metric_id")]
    public string? MetricId { get; set; }

    /// <summary>
    /// <para>指标名称，指标在该明细数据中的名称</para>
    /// <para>示例值：示例指标</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>指标字段信息</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public PerformanceMetricFieldInDetails[]? Fields { get; set; }

    /// <summary>
    /// <para>指标维度 ID</para>
    /// <para>示例值：7303895818346430484</para>
    /// </summary>
    [JsonPropertyName("dimension_id")]
    public string? DimensionId { get; set; }

    /// <summary>
    /// <para>指标维度名称（下划线形态 zh_cn / en_us）</para>
    /// </summary>
    [JsonPropertyName("dimension_name")]
    public I18nName? DimensionName { get; set; }

    /// <summary>
    /// <para>指标维度权重，如果没有设置则返回为空，单位为百分比</para>
    /// <para>示例值：90%</para>
    /// </summary>
    [JsonPropertyName("dimension_weight")]
    public string? DimensionWeight { get; set; }

    /// <summary>
    /// <para>指标添加来源：reviewee（指标制定人添加）/ admin（管理员添加）</para>
    /// <para>示例值：admin</para>
    /// </summary>
    [JsonPropertyName("add_from")]
    public string? AddFrom { get; set; }

    /// <summary>
    /// <para>指标是否引自指标库</para>
    /// </summary>
    [JsonPropertyName("is_from_library")]
    public bool? IsFromLibrary { get; set; }
}
