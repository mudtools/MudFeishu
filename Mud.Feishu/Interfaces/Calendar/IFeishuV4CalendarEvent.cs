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
    /// 获取日程
    /// <para>以当前身份（应用或用户）获取指定日历内的某一日程信息，包括日程的标题、时间段、视频会议信息、公开范围以及参与人权限等。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/get">接口文档</see></para>
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


    /// <summary>
    /// 获取日程列表
    /// <para>以当前身份（应用或用户）获取指定日历内的日程列表，包括日程的标题、时间段、视频会议信息、公开范围以及参与人权限等。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/list">接口文档</see></para>
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
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="anchor_time">
    /// <para>必填：否</para>
    /// <para>时间锚点，Unix 时间戳（秒）。anchor_time 用于设置一个时间点，以便直接拉取该时间点之后的日程数据，从而避免拉取全量日程数据。可使用 page_token 或 sync_token 进行分页或增量拉取 anchor_time 之后的所有日程数据。</para>
    /// <para>**使用说明**：</para>
    /// <para>- 对于单次日程，会获取到 **日程结束时间 &gt;= anchor_time** 的日程信息。</para>
    /// <para>- 对于重复性日程，目前设置 anchor_time 后均会获取到，包括在 anchor_time 之前的已结束的历史重复性日程。</para>
    /// <para>- 对于例外日程，会获取到 **original_time &gt;= anchor_time** 以及 **日程结束时间 &gt;= anchor_time** 的日程信息，其中 original_time 从例外日程 ID 中获取，ID 结构为 `{uid}_{original_time}`。</para>
    /// <para>**注意**：该参数不可与 start_time 和 end_time 一起使用。</para>
    /// <para>**默认值**：空</para>
    /// <para>示例值：1609430400</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="sync_token">
    /// <para>必填：否</para>
    /// <para>增量同步标记，第一次请求不填。当分页查询结束（page_token 返回值为空）时，接口会返回 sync_token 字段，下次调用可使用该 sync_token 增量获取日历变更数据。</para>
    /// <para>**默认值**：空</para>
    /// <para>示例值：ListCalendarsSyncToken_1632452910</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="start_time">
    /// <para>必填：否</para>
    /// <para>时间区间的开始时间， Unix 时间戳（秒），与end_time搭配使用，用于拉取指定时间区间内的日程数据.</para>
    /// <para>**注意**：</para>
    /// <para>- 该方式只能一次性返回数据，无法进行分页。一次性返回的数据大小受page_size限制，超过限制的数据将被截断。</para>
    /// <para>- 在使用start_time和end_time时，不能与page_token或sync_token一起使用。</para>
    /// <para>- 在使用start_time和end_time时，不能与anchor_time一起使用。</para>
    /// <para>**默认值**：空</para>
    /// <para>示例值：1631777271</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="end_time">
    /// <para>必填：否</para>
    /// <para>时间区间的结束时间， Unix 时间戳（秒）。与start_time搭配使用，用于拉取指定时间区间内的日程数据.</para>
    /// <para>**注意**：</para>
    /// <para>- 该方式只能一次性返回数据，无法进行分页。一次性返回的数据大小受page_size限制，超过限制的数据将被截断。</para>
    /// <para>- 在使用start_time和end_time时不能与page_token或sync_token一起使用。</para>
    /// <para>- 在使用start_time和end_time时，不能与anchor_time一起使用。</para>
    /// <para>**默认值**：空</para>
    /// <para>示例值：1631777271</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/calendar/v4/calendars/{calendar_id}/events")]
    Task<FeishuApiResult<GetCalendarEventPageListResult>?> GetCalendarEventPageListAsync(
        [Path] string calendar_id,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        [Query] string? anchor_time = null,
        [Query] string? sync_token = null,
        [Query] string? start_time = null,
        [Query] string? end_time = null,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 搜索日程
    /// <para>搜索指定日历下的相关日程，支持关键词搜索、过滤条件搜索。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/search">接口文档</see></para>
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
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="searchCalendarEventRequest">搜索日程请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/events/search")]
    Task<FeishuApiPageListResult<SearchCalendarEventResult>?> SearchCalendarEventPageListAsync(
       [Path] string calendar_id,
       [Body] SearchCalendarEventRequest searchCalendarEventRequest,
       [Query] int page_size = Consts.PageSize_20,
       [Query] string? page_token = null,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 回复日程
    /// <para>以当前身份（应用或用户）回复日程。</para>
    /// <para><see href="https://open.feishu.cn/document/calendar-v4/calendar-event/reply">接口文档</see></para>
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
    /// <param name="replyCalendarEventRequest">回复日程请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}/reply")]
    Task<FeishuNullDataApiResult?> ReplyCalendarEventAsync(
      [Path] string calendar_id,
      [Path] string event_id,
      [Body] ReplyCalendarEventRequest replyCalendarEventRequest,
      CancellationToken cancellationToken = default);



    /// <summary>
    /// 获取重复日程实例
    /// <para>以当前身份（应用或用户）获取指定日历中的某一重复日程信息。</para>
    /// <para><see href="https://open.feishu.cn/document/calendar-v4/calendar-event/instances">接口文档</see></para>
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
    /// <param name="start_time">
    /// <para>开始时间，Unix 时间戳，单位为秒。该参数与 end_time 用于设置时间范围，即重复日程的查询区间为 （start_time, end_time）</para>
    /// <para>**注意**：start_time 与 end_time 之间的时间区间不能超过 2年。</para>
    /// <para>示例值：1631777271</para>
    /// </param>
    /// <param name="end_time">
    /// <para>结束时间，Unix 时间戳，单位为秒。该参数与 start_time 用于设置时间范围，即重复日程的查询区间为 （start_time, end_time）</para>
    /// <para>**注意**：start_time 与 end_time 之间的时间区间不能超过 2年。</para>
    /// <para>示例值：1631777271</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}/instances")]
    Task<FeishuApiPageListResult<CalendarEventInstanceResult>?> GetInstancesCalendarEventPageListAsync(
      [Path] string calendar_id,
      [Path] string event_id,
      [Query] string start_time,
      [Query] string end_time,
      [Query] int page_size = Consts.PageSize_50,
      [Query] string? page_token = null,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询日程视图
    /// <para>以用户身份查询指定日历下的日程视图。与获取日程列表不同的是，当前接口会按照重复日程的重复性规则展开成多个日程实例（instance），并根据查询的时间区间返回相应的日程实例信息。</para>
    /// <para><see href="https://open.feishu.cn/document/calendar-v4/calendar-event/instance_view">接口文档</see></para>
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
    /// <param name="start_time">
    /// <para>开始时间，Unix 时间戳，单位为秒。该参数与 end_time 用于设置时间范围，即重复日程的查询区间为 （start_time, end_time）</para>
    /// <para>**注意**：start_time 与 end_time 之间的时间区间不能超过 2年。</para>
    /// <para>示例值：1631777271</para>
    /// </param>
    /// <param name="end_time">
    /// <para>结束时间，Unix 时间戳，单位为秒。该参数与 start_time 用于设置时间范围，即重复日程的查询区间为 （start_time, end_time）</para>
    /// <para>**注意**：start_time 与 end_time 之间的时间区间不能超过 2年。</para>
    /// <para>示例值：1631777271</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/calendar/v4/calendars/{calendar_id}/events/instance_view")]
    Task<FeishuApiResult<GetInstanceViewCalendarEventResult>?> GetInstanceViewCalendarEventAsync(
       [Path] string calendar_id,
       [Query] string start_time,
       [Query] string end_time,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建会议群
    /// <para>以当前身份（应用或用户）为指定日程创建一个会议群。</para>
    /// <para><see href="https://open.feishu.cn/document/calendar-v4/calendar-event-meeting_chat/create">接口文档</see></para>
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
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}/meeting_chat")]
    Task<FeishuApiResult<CreateCalendarEventMeetingChatResult>?> CreateCalendarEventMeetingChatAsync(
        [Path] string calendar_id,
        [Path] string event_id,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 解绑会议群
    /// <para>以当前身份（应用或用户）为日程解绑已创建的会议群。</para>
    /// <para><see href="https://open.feishu.cn/document/calendar-v4/calendar-event-meeting_chat/delete">接口文档</see></para>
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
    /// <param name="meeting_chat_id">
    /// <para>会议群 ID。在创建会议群时会返回会议群 ID。</para>
    /// <para>示例值：oc_xxx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}/meeting_chat")]
    Task<FeishuNullDataApiResult?> DeleteCalendarEventMeetingChatAsync(
       [Path] string calendar_id,
       [Path] string event_id,
       [Query] string meeting_chat_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建会议纪要
    /// <para>为指定的日程创建会议纪要。纪要以文档形式展示，成功创建后会返回纪要文档 URL。</para>
    /// <para><see href="https://open.feishu.cn/document/calendar-v4/calendar-event-meeting_minute/create">接口文档</see></para>
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
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}/meeting_minute")]
    Task<FeishuApiResult<CreateCalendarEventMeetingMinuteResult>?> CreateCalendarEventMeetingMinuteAsync(
      [Path] string calendar_id,
      [Path] string event_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询会议室忙闲
    /// <para>获取指定会议室的忙碌、空闲日程信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/meeting-room-event/query-room-availability">接口文档</see></para>
    /// </summary> 
    /// <param name="room_ids">
    /// <para>会议室 ID。你可以通过[查询会议室列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/room/list)或[搜索会议室](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/room/search)接口获取指定会议室 ID。</para>
    /// <para>1. room_ids个数不要超过20。</para>
    /// <para>2. GET 请求中传入多个会议室 ID 的格式示例为 `room_ids=omm_83d09ad4f6896e02029a6a075f71xxxx&amp;room_ids=omm_eada1d61a550955240c28757e7dexxxx`。</para>
    /// </param>
    /// <param name="time_min">
    /// <para>查询的起始时间，需要遵循 [RFC3339](https://tools.ietf.org/html/rfc3339) 格式，示例：2019-09-04T08:45:00+08:00。</para>
    /// <para>**注意**：传入该参数时需要进行 URL 编码。</para>
    /// </param>
    /// <param name="time_max">
    /// <para>查询的结束时间，需要遵循 [RFC3339](https://tools.ietf.org/html/rfc3339) 格式，示例：2019-09-04T09:45:00+08:00。</para>
    /// <para>**注意**：传入该参数时需要进行 URL 编码。</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/meeting_room/freebusy/batch_get")]
    Task<FeishuApiResult<QueryMeetingRoomFreebusyResult>?> QueryMeetingRoomFreebusyAsync(
       [Query] string[] room_ids,
       [Query] string time_min,
       [Query] string time_max,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 添加日程参与人
    /// <para>以当前身份（应用或用户）为指定日程添加一个或多个参与人，参与人类型包括用户、群组、会议室以及邮箱。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event-attendee/create">接口文档</see></para>
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
    /// <param name="createCalendarEventAttendeeRequest">创建日程参与人请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}/attendees")]
    Task<FeishuApiResult<CreateCalendarEventAttendeeResult>?> CreateCalendarEventAttendeeAsync(
      [Path] string calendar_id,
      [Path] string event_id,
      [Body] CreateCalendarEventAttendeeRequest createCalendarEventAttendeeRequest,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除日程参与人
    /// <para>以当前身份（应用或用户）删除指定日程的一个或多个参与人。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event-attendee/batch_delete">接口文档</see></para>
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
    /// <param name="deleteCalendarEventAttendeeRequest">删除日程参与人请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}/attendees/batch_delete")]
    Task<FeishuNullDataApiResult?> DeleteCalendarEventAttendeeAsync(
        [Path] string calendar_id,
        [Path] string event_id,
        [Body] DeleteCalendarEventAttendeeRequest deleteCalendarEventAttendeeRequest,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


}