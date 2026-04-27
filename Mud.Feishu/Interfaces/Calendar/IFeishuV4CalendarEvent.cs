// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Calendar;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 日程是存在于日历内的实例资源，开发人员可以通过关联特定日期或时间段、参与人、地点等规则，构建指定主题内容的工作安排。
/// <para>例如，个人工作提醒、团队会议沟通、活动直播等类型的日程。开发人员可以通过日程资源 API 构建与管理日程。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV4CalendarEvent : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 创建日程
    /// <para>以当前身份（应用或用户）在指定日历上创建一个日程。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/create">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="idempotency_key">
    /// <para>创建日程的幂等 key，该 key 在应用和日历维度下唯一，用于避免重复创建资源。建议按照示例值的格式进行取值。</para>
    /// <para>示例值：25fdf41b-8c80-2ce1-e94c-de8b5e7aa7e6</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="createCalendarEventRequest">创建日程请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/events")]
    Task<FeishuApiResult<CalendarEventOopsResult>?> CreateCalendarEventAsync(
       [Path] string calendar_id,
       [Body] CreateCalendarEventRequest createCalendarEventRequest,
       [Query] string? idempotency_key = null,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除日程
    /// <para>以当前身份（应用或用户）删除指定日历上的一个日程。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/delete">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="event_id">
    /// <para>日程 ID。</para>
    /// <para>示例值：xxxxxxxxx_0</para>
    /// </param>
    /// <param name="need_notification">
    /// <para>删除日程是否给日程参与人发送 Bot 通知。</para>
    /// <para>**默认值**：true</para>
    /// <para>示例值：false</para>
    /// <list type="bullet">
    /// <item>true：发送</item>
    /// <item>false：不发送</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}")]
    Task<FeishuNullDataApiResult?> DeleteCalendarEventAsync(
       [Path] string calendar_id,
       [Path] string event_id,
       [Query] bool? need_notification = true,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新日程
    /// <para>以当前身份（应用或用户）更新指定日历上的一个日程，包括日程标题、描述、开始与结束时间、视频会议以及日程地点等信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/patch">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="event_id">
    /// <para>日程 ID。</para>
    /// <para>示例值：xxxxxxxxx_0</para>
    /// </param>
    /// <param name="updateCalendarEventRequest">更新日程请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}")]
    Task<FeishuApiResult<CalendarEventOopsResult>?> UpdateCalendarEventAsync(
      [Path] string calendar_id,
      [Path] string event_id,
      [Body] UpdateCalendarEventRequest updateCalendarEventRequest,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新日程
    /// <para>以当前身份（应用或用户）更新指定日历上的一个日程，包括日程标题、描述、开始与结束时间、视频会议以及日程地点等信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/patch">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="event_id">
    /// <para>日程 ID。</para>
    /// <para>示例值：xxxxxxxxx_0</para>
    /// </param>
    /// <param name="need_meeting_settings">
    /// <para>是否需要返回飞书视频会议（VC）的会前设置。需满足以下条件才可以获取到返回结果：</para>
    /// <para>- 日程的会议类型（vc_type）需要是 vc。</para>
    /// <para>- 需要有日程的编辑权限。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true：需要</para>
    /// <para>- false（默认值）：不需要</para>
    /// <para>示例值：false</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="need_attendee">
    /// <para>是否需要返回参与人信息。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true：需要</para>
    /// <para>- false（默认值）：不需要</para>
    /// <para>示例值：false</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="max_attendee_num">
    /// <para>返回的最大参与人数量。</para>
    /// <para>示例值：10</para>
    /// <para>默认值：10</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}")]
    Task<FeishuApiResult<GetCalendarEventResult>?> GetCalendarEventAsync(
      [Path] string calendar_id,
      [Path] string event_id,
      [Query] bool? need_meeting_settings = false,
      [Query] bool? need_attendee = null,
      [Query] int? max_attendee_num = 10,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}