// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 关键指标对应的环节状态
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceRevieweeStageStatus
{
    /// <summary>
    /// <para>环节 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("stage_id")]
    public string? StageId { get; set; }

    /// <summary>
    /// <para>环节类型：kpi_metric_setting（指标制定环节）/ kpi_result_recording（结果录入环节）</para>
    /// <para>示例值：kpi_metric_setting</para>
    /// </summary>
    [JsonPropertyName("stage_type")]
    public string? StageType { get; set; }

    /// <summary>
    /// <para>环节状态：0（未开始）/ 1（待提交）/ 2（已逾期）/ 3（确认中）/ 4（被驳回）/ 5（已完成）</para>
    /// </summary>
    [JsonPropertyName("stage_status")]
    public int? StageStatus { get; set; }
}
