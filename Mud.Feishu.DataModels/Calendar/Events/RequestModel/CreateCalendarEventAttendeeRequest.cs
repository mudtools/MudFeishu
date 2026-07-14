// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 创建日程事件参会人请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CreateCalendarEventAttendeeRequest
{
    /// <summary>
    /// <para>新增参与人列表。</para>
    /// <para>**注意**：</para>
    /// <para>- 单次请求可设置的参与人数量（含会议室）上限为 1000。</para>
    /// <para>- 单次请求可设置的会议室数量上限为 100。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attendees")]
    public CalendarEventAttendeeData[]? Attendees { get; set; }

    /// <summary>
    /// <para>是否给参与人发送 Bot 通知。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true（默认值）：发送</para>
    /// <para>- false：不发送</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("need_notification")]
    public bool? NeedNotification { get; set; }

    /// <summary>
    /// <para>使用管理员身份访问时，要修改的日程实例。</para>
    /// <para>**注意**：</para>
    /// <para>- 该参数仅用于修改重复日程中的某一日程实例，非重复日程无需填此字段。</para>
    /// <para>- 你可以调用[获取重复日程实例](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar-event/instances)接口，获取重复日程中某一日程实例的 event_id。该参数取值为 event_id 的时间戳后缀。例如查询到的日程实例 ID 为 `2cf525f0-1e67-4b04-ad4d-30b7f003903c_1713168000`，则当前的 `instance_start_time_admin` 取值为 `1713168000`。</para>
    /// <para>**默认值**：空</para>
    /// <para>必填：否</para>
    /// <para>示例值：1647320400</para>
    /// </summary>
    [JsonPropertyName("instance_start_time_admin")]
    public string? InstanceStartTimeAdmin { get; set; }

    /// <summary>
    /// <para>是否启用会议室管理员身份（需先在管理后台设置某成员为会议室管理员)。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true：启用</para>
    /// <para>- false（默认值）：不启用</para>
    /// <para>**说明**：开启后，本次请求只处理会议室数据，其他参与人操作不会生效。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_enable_admin")]
    public bool? IsEnableAdmin { get; set; }

    /// <summary>
    /// <para>是否添加会议室联系人（operate_id）到日程参与人。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true（默认值）：启用</para>
    /// <para>- false：不启用</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("add_operator_to_attendee")]
    public bool? AddOperatorToAttendee { get; set; }
}
