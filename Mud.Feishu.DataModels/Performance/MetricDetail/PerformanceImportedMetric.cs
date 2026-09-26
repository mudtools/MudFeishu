// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 录入的关键指标明细
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceImportedMetric
{
    /// <summary>
    /// <para>被评估人 ID，与入参 user_id_type 类型一致</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_3245842393d09e9428ad4655da6e30b3</para>
    /// </summary>
    [JsonPropertyName("reviewee_user_id")]
    public string? RevieweeUserId { get; set; }

    /// <summary>
    /// <para>指标 ID，可通过获取指标列表接口获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：7272580325522276372</para>
    /// </summary>
    [JsonPropertyName("metric_id")]
    public string? MetricId { get; set; }

    /// <summary>
    /// <para>指标字段信息（1~99 个）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public PerformanceImportedMetricField[]? Fields { get; set; }
}
