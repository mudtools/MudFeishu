// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效详情数据（v2）中 360° 评估记录的信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceInvitedReviewRecordInfo
{
    /// <summary>
    /// <para>评估人 ID（open_id / user_id 对象）；如果开启了 360 匿名评估且是对全部查看者匿名，则不返回该值</para>
    /// </summary>
    [JsonPropertyName("reviewer_id")]
    public UserIdInfo? ReviewerId { get; set; }

    /// <summary>
    /// <para>是否拒绝</para>
    /// </summary>
    [JsonPropertyName("is_rejected")]
    public bool? IsRejected { get; set; }

    /// <summary>
    /// <para>360° 评估人拒绝评估的理由，当 360° 评估环节被评估人拒绝时有值</para>
    /// <para>示例值：test</para>
    /// </summary>
    [JsonPropertyName("rejected_reason")]
    public string? RejectedReason { get; set; }

    /// <summary>
    /// <para>360° 评估人的评估尺度标签：1（严格）/ 2（适中）/ 3（宽松）</para>
    /// </summary>
    [JsonPropertyName("distribute_type")]
    public int? DistributeType { get; set; }

    /// <summary>
    /// <para>360° 评估人的评估尺度数值</para>
    /// <para>示例值：1.23</para>
    /// </summary>
    [JsonPropertyName("avg_diff")]
    public string? AvgDiff { get; set; }

    /// <summary>
    /// <para>360° 评估人与被评估人关系；如果开启了 360 匿名评估且是对全部查看者匿名并配置隐藏描述信息则不返回该值。可选值：direct_report（直属下级）/ skiplevel_report（隔级下级）/ former_direct_manager（原直属上级）/ skiplevel_manager（隔级上级）/ teammate（相同上级同事）/ crossteam_colleague（不同上级同事）</para>
    /// <para>示例值：direct_report</para>
    /// </summary>
    [JsonPropertyName("relationship_with_reviewee")]
    public string? RelationshipWithReviewee { get; set; }

    /// <summary>
    /// <para>360° 评估人的邀请人类型；如果开启了 360 匿名评估且是对全部查看者匿名并配置隐藏描述信息则不返回该值。可选值：system_default（系统默认）/ reviewee（被评估人本人）/ manager（上级）/ hrbp_or_others（HRBP或其他人）/ voluntary（自愿评估）</para>
    /// <para>示例值：system_default</para>
    /// </summary>
    [JsonPropertyName("invitedby")]
    public string? Invitedby { get; set; }
}
