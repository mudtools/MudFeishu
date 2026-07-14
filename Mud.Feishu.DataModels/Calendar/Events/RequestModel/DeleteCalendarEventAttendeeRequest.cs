// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 删除日程参与人请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class DeleteCalendarEventAttendeeRequest
{
    /// <summary>
    /// <para>需要删除的参与人 ID 列表。</para>
    /// <para>添加日程参与人时，会返回参与人 ID（attendee_id），你也可以调用[获取日程参与人列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar-event-attendee/list)接口，查询指定日程的参与人 ID。</para>
    /// <para>- 一次最多删除500个参与人（与delete_ids一起计算）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["user_xxxxx", "chat_xxxxx", "resource_xxxxx", "third_party_xxxxx"]</para>
    /// </summary>
    [JsonPropertyName("attendee_ids")]
    public string[]? AttendeeIds { get; set; }

    /// <summary>
    /// <para>参与人类型对应的 ID，该 ID 是 attendee_ids 字段的补充字段。</para>
    /// <para>- 一次最多删除500个参与人（与attendee_ids一起计算）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("delete_ids")]
    public CalendarEventAttendeeId[]? DeleteIds { get; set; }

    /// <summary>
    /// <para>删除日程参与人时，是否向参与人发送 Bot 通知。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true（默认值）：发送</para>
    /// <para>- false：不发送</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("need_notification")]
    public bool? NeedNotification { get; set; }

    /// <summary>
    /// <para>使用管理员身份访问时，要修改的实例（仅用于重复日程修改其中的一个实例，非重复日程无需填此字段）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1647320400</para>
    /// </summary>
    [JsonPropertyName("instance_start_time_admin")]
    public string? InstanceStartTimeAdmin { get; set; }

    /// <summary>
    /// <para>是否启用会议室管理员身份（需先在管理后台设置某人为会议室管理员）。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true：启用</para>
    /// <para>- false（默认值）：不启用</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_enable_admin")]
    public bool? IsEnableAdmin { get; set; }
}
