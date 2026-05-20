// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>预定审批设置</para>
/// </summary>
public class ApprovalConfig
{
    /// <summary>
    /// <para>预定审批开关：0 代表关闭，1 代表打开。</para>
    /// <para>说明：</para>
    /// <para>- 未设置值时不更新原开关的值，但此时必填 approval_condition</para>
    /// <para>- 设置值为 1 时，必填 approval_condition</para>
    /// <para>- 设置值为 0 时整个</para>
    /// <para>approval_config 其他字段均可省略。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：1</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("approval_switch")]
    public int? ApprovalSwitch { get; set; }

    /// <summary>
    /// <para>预定审批条件：0 代表所有预定均需审批，1 代表满足条件的需审批</para>
    /// <para>说明：为 1 时必填 **meeting_duration**</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：1</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("approval_condition")]
    public int? ApprovalCondition { get; set; }

    /// <summary>
    /// <para>超过 meeting_duration</para>
    /// <para>的预定需要审批（单位：小时，取值范围[0.1-99]）</para>
    /// <para>说明：</para>
    /// <para>- 当 approval_condition</para>
    /// <para>为 0 ，更新时如果未设置值，默认更新为 99 .</para>
    /// <para>- 传入的值小数点后超过 2 位，自动四舍五入保留两位。</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("meeting_duration")]
    public float? MeetingDuration { get; set; }

    /// <summary>
    /// <para>审批人列表，当打开审批开关时，至少需要设置一位审批人</para>
    /// <para>必填：否</para>
    /// <para>示例值：[{user_id:"ou_e8bce6c3935ef1fc1b432992fd9d3db8"}]</para>
    /// </summary>
    [JsonPropertyName("approvers")]
    public SubscribeUser[]? Approvers { get; set; }
}