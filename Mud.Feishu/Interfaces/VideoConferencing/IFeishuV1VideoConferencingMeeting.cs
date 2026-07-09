// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 会议管理功能为用户在会议中进行邀请参会成员、移除参会成员和设置主持人等操作。
/// <para>功能包括：获取会议详情、获取与会议号相关联的会议列表、邀请参会人、移除参会人、设置主持人、结束会议。事件包括：会议开始、会议结束、加入会议、离开会议、录制开始、录制停止、录制完成、屏幕共享开始、屏幕共享结束。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting/meeting-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1VideoConferencingMeeting : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 设置主持人
    /// <para>设置会议的主持人。发起设置主持人的操作者必须具有相应的权限（如果操作者为用户，必须是会中当前主持人）；</para>
    /// <para>该操作使用CAS并发安全机制，需传入会中当前主持人，如果操作失败可使用返回的最新数据重试</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting/set_host">接口文档</see></para>
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
    /// <param name="meeting_id">
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>示例值：6911188411932033028</para>
    /// </param>
    /// <param name="setHostMeetingRequest">设置主持人请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/vc/v1/meetings/{meeting_id}/set_host")]
    Task<FeishuApiResult<SetHostMeetingResult>?> SetHostMeetingAsync(
      [Path] string meeting_id,
      [Body] SetHostMeetingRequest setHostMeetingRequest,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取会议详情
    /// <para>根据会议 ID 获取指定会议的详细信息，包括会议主题、链接、主持人、参会人员、状态、时间信息及关联纪要 ID。</para>
    /// <para>只能获取归属于自己的会议，支持查询最近90天内的会议</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting/get">接口文档</see></para>
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
    /// <param name="meeting_id">
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>示例值：6911188411932033028</para>
    /// </param>
    /// <param name="with_participants">
    /// <para>是否返回参会人列表，默认值为 false，不返回参会人列表；设为 true 时返回参会人列表。当 user_id_type 为 user_id 时，参会人列表仅能获取 Lark 用户。</para>
    /// <para>示例值：false</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="with_meeting_ability">
    /// <para>是否返回会中使用能力统计，默认值为 false，不返回能力统计；设为 true 时返回会中使用能力统计（仅限tenant_access_token）</para>
    /// <para>示例值：false</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="query_mode">
    /// <para>此次查询的查询模式，不传，或传0，只查询会议信息；传1，只查询会议产物</para>
    /// <para>示例值：0</para>
    /// <list type="bullet">
    /// <item>0：只查询会议信息（默认）</item>
    /// <item>1：只查询会议产物（纪要、逐字稿）</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/meetings/{meeting_id}")]
    Task<FeishuApiResult<MeetingResult>?> GetMeetingAsync(
       [Path] string meeting_id,
       [Query] bool? with_participants = null,
       [Query] bool? with_meeting_ability = null,
       [Query] int? query_mode = null,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取与会议号关联的会议列表
    /// <para>获取指定时间范围内与会议号关联的会议简要信息列表。仅支持查询 90 天内的数据。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting/list_by_no">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_no">
    /// <para>9位会议号（会议链接最后9位数）</para>
    /// <para>示例值：123456789</para>
    /// </param>
    /// <param name="start_time">
    /// <para>查询开始时间（unix时间，单位sec），需小于end_time的值</para>
    /// <para>示例值：1608888867</para>
    /// </param>
    /// <param name="end_time">
    /// <para>查询结束时间（unix时间，单位sec）</para>
    /// <para>示例值：1608888867</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/meetings/list_by_no")]
    Task<FeishuApiResult<MeetingPageListResult>?> GetMeetingPageListAsync(
        [Query] string meeting_no,
        [Query] string start_time,
        [Query] string end_time,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        CancellationToken cancellationToken = default);
}