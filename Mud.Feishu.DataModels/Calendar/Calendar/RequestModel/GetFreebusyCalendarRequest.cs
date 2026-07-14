// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// 查询主日历日程忙闲信息请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class GetFreebusyCalendarRequest
{
    /// <summary>
    /// <para>查询时段开始时间，[RFC 3339](https://datatracker.ietf.org/doc/html/rfc3339) date_time 格式。</para>
    /// <para>**注意**：time_min 与 time_max 之间的时间间隔不能大于 90 天。</para>
    /// <para>必填：是</para>
    /// <para>示例值：2020-10-28T12:00:00+08:00</para>
    /// </summary>
    [JsonPropertyName("time_min")]
    public string TimeMin { get; set; } = string.Empty;

    /// <summary>
    /// <para>查询时段结束时间，[RFC 3339](https://datatracker.ietf.org/doc/html/rfc3339) date_time 格式。</para>
    /// <para>**注意**：time_min 与 time_max 之间的时间间隔不能大于 90 天。</para>
    /// <para>必填：是</para>
    /// <para>示例值：2020-12-28T12:00:00+08:00</para>
    /// </summary>
    [JsonPropertyName("time_max")]
    public string TimeMax { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID，需要传入与查询参数 user_id_type 相匹配的 id。例如，`user_id_type=open_id` 时，需要传入用户的 open_id。了解用户 ID 参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**注意**：user_id 与 room_id 需要二选一传入，如果同时传入则只生效 user_id。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>会议室 room_id。你可以调用[查询会议室列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/room/list)接口或者[搜索会议室](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/room/search)接口，获取相应会议室的 room_id。</para>
    /// <para>**注意**：user_id 与 room_id 需要二选一传入，如果同时传入则只生效 user_id。</para>
    /// <para>必填：否</para>
    /// <para>示例值：omm_xxxxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    /// <summary>
    /// <para>是否包含绑定的三方日历中的日程。</para>
    /// <para>**取值**：</para>
    /// <para>- true（默认值）：包含</para>
    /// <para>- false：不包含</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("include_external_calendar")]
    public bool? IncludeExternalCalendar { get; set; }

    /// <summary>
    /// <para>是否只查询忙碌日程信息。</para>
    /// <para>**取值**：</para>
    /// <para>- true（默认值）：是，查询结果不包含空闲日程。</para>
    /// <para>- false：否，查询结果包含空闲日程。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("only_busy")]
    public bool? OnlyBusy { get; set; }

    /// <summary>
    /// <para>是否需要RSVP状态信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("need_rsvp_status")]
    public bool? NeedRsvpStatus { get; set; }
}
