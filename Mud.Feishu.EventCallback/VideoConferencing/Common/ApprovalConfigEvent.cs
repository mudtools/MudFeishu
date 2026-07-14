// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.VideoConferencing;


/// <summary>
/// 预定审批设置
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ApprovalConfigEvent
{
    /// <summary>
    /// <para>预定审批开关，0关闭，1打开</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`0` ～ `1`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_switch")]
    public int? ApprovalSwitch { get; set; }

    /// <summary>
    /// <para>预定审批条件，0所有预定需要审批，1满足条件需审批</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`0` ～ `1`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_condition")]
    public int? ApprovalCondition { get; set; }

    /// <summary>
    /// <para>超过 meeting_duration小时需要审批</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_duration")]
    public float? MeetingDuration { get; set; }

    /// <summary>
    /// <para>审批人列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approvers")]
    public SubscribeUserEvent[]? Approvers { get; set; }
}
