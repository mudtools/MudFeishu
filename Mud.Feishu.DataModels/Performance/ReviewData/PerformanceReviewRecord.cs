// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效详情数据（v2）中的评估内容记录
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewRecord
{
    /// <summary>
    /// <para>评估人的环节状态。查看绩效结果环节：0（已开通）/ 1（待确认）/ 2（已截止）/ 3（已确认）/ 4（已复议）；绩效结果复议环节：1（待完成）/ 2（已截止）/ 3（已完成）；其他环节类型：0（未开始）/ 1（待完成）/ 2（已截止）/ 3（已完成）</para>
    /// </summary>
    [JsonPropertyName("progress")]
    public int? Progress { get; set; }

    /// <summary>
    /// <para>评估记录中的评估内容明细</para>
    /// </summary>
    [JsonPropertyName("units")]
    public PerformanceReviewRecordUnit[]? Units { get; set; }

    /// <summary>
    /// <para>360°评估记录的信息；如果开启了 360 匿名评估且是对全部查看者匿名，则不返回评估人的部分信息</para>
    /// </summary>
    [JsonPropertyName("invited_review_record_info")]
    public PerformanceInvitedReviewRecordInfo? InvitedReviewRecordInfo { get; set; }

    /// <summary>
    /// <para>合作项目中上级的评估记录信息，仅在「项目直属上级环节」有值</para>
    /// </summary>
    [JsonPropertyName("direct_project_leader_record_info")]
    public PerformanceDirectProjectLeaderRecordInfo? DirectProjectLeaderRecordInfo { get; set; }

    /// <summary>
    /// <para>评估记录 ID</para>
    /// <para>示例值：7385000219907457024-7385000219907457025</para>
    /// </summary>
    [JsonPropertyName("record_id")]
    public string? RecordId { get; set; }
}
