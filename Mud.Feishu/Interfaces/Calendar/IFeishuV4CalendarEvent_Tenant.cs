// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Calendar;

namespace Mud.Feishu;

/// <summary>
/// 日程是存在于日历内的实例资源，开发人员可以通过关联特定日期或时间段、参与人、地点等规则，构建指定主题内容的工作安排。
/// <para>例如，个人工作提醒、团队会议沟通、活动直播等类型的日程。开发人员可以通过日程资源 API 构建与管理日程。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/introduction"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Calendar", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV4CalendarEvent))]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV4CalendarEvent : IFeishuV4CalendarEvent
{

    /// <summary>
    /// 创建请假日程
    /// <para>为指定用户创建一个请假日程。请假日程分为普通日程和全天日程。创建请假日程后，在请假时间内，用户个人签名页会展示请假信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/timeoff_event/create">接口文档</see></para>
    /// </summary> 
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
    /// <param name="createTimeoffEventRequest">创建会议纪要请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/timeoff_events")]
    Task<FeishuApiResult<CreateTimeoffEventResult>?> CreateTimeoffEventAsync(
        [Body] CreateTimeoffEventRequest createTimeoffEventRequest,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除请假日程
    /// <para>删除一个指定的请假日程。请假日程删除后，用户个人签名页的请假信息也会消失。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/timeoff_event/delete">接口文档</see></para>
    /// </summary> 
    /// <param name="timeoff_event_id">
    /// <para>请假日程 ID，在创建请假日程时从返回结果中获取。</para>
    /// <para>示例值：timeoff:XXXXXX-XXXX-0917-1623-aa493d591a39</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/calendar/v4/timeoff_events/{timeoff_event_id}")]
    Task<FeishuNullDataApiResult?> DeleteTimeoffEventAsync(
       [Query] string timeoff_event_id,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 查询会议室日程主题和会议详情
    /// <para>使用日程的 Uid 和 Original time 查询会议室日程主题与详情。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/meeting-room-event/">接口文档</see></para>
    /// </summary> 
    /// <param name="getMeetingRoomSummaryRequest">查询会议室日程主题和会议详情请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/meeting_room/summary/batch_get")]
    Task<FeishuApiResult<GetMeetingRoomSummaryResult>?> GetMeetingRoomSummaryAsync(
       [Body] GetMeetingRoomSummaryRequest getMeetingRoomSummaryRequest,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 回复会议室日程实例
    /// <para>用于回复会议室日程实例，支持回复未签到释放、提前结束释放、被管理员置为接受、被管理员置为拒绝。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/meeting-room-event/reply-meeting-room-event-instance">接口文档</see></para>
    /// </summary> 
    /// <param name="replyMeetingRoomInstanceRequest">回复会议室日程实例请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/meeting_room/instance/reply")]
    Task<FeishuNullDataApiResult?> ReplyMeetingRoomEventInstanceAsync(
      [Body] ReplyMeetingRoomEventInstanceRequest replyMeetingRoomInstanceRequest,
      CancellationToken cancellationToken = default);
}