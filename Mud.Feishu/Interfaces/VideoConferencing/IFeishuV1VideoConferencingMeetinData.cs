// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 用于分页查询一段时间内租户的会议数据，包括：查询会议明细、查询参会人明细、查询参会人会议质量数据、查询会议室预定数据。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-room-data/resource-introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1VideoConferencingMeetinData : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 查询会议明细
    /// <para>查询会议明细，具体权限要求请参考资源介绍</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-room-data/get">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_no">
    /// <para>9位会议号（会议链接最后9位数）</para>
    /// <para>示例值：123456789</para>
    /// </param>
    /// <param name="user_id">
    /// <para>按参会飞书用户筛选（最多一个筛选条件，如果设置多个，参数校验会失败）</para>
    /// <para>示例值：ou_3ec3f6a28a0d08c45d895276e8e5e19b</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="room_id">
    /// <para>按参会Rooms筛选（最多一个筛选条件，如果设置多个，参数校验会失败）</para>
    /// <para>示例值：omm_eada1d61a550955240c28757e7dec3af</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="meeting_type">
    /// <para>按会议类型筛选（最多一个筛选条件，如果设置多个，参数校验会失败）</para>
    /// <para>示例值：2</para>
    /// <list type="bullet">
    /// <item>1：全部类型（默认）</item>
    /// <item>2：视频会议</item>
    /// <item>3：本地投屏</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="meeting_status">
    /// <para>会议状态（不传默认为已结束会议）</para>
    /// <para>示例值：2</para>
    /// <list type="bullet">
    /// <item>1：进行中</item>
    /// <item>2：已结束</item>
    /// <item>3：待召开。该枚举值只读，请求时不支持选择。</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="start_time">
    /// <para>查询开始时间（unix时间，单位sec），需小于end_time的值</para>
    /// <para>示例值：1608888867</para>
    /// </param>
    /// <param name="end_time">
    /// <para>查询结束时间（unix时间，单位sec）</para>
    /// <para>示例值：1608888867</para>
    /// </param>
    /// <param name="include_external_meetings">
    /// <para>是否查询外部会议（不传默认为不查询）</para>
    /// <para>示例值：false</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="include_webinar">
    /// <para>是否查询网络研讨会（不传默认为不查询）</para>
    /// <para>示例值：false</para>
    /// <para>默认值：null</para>
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
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/meeting_list")]
    Task<FeishuApiResult<GetMeetingListResult>?> GetMeetingPageListAsync(
        [Query] string start_time,
        [Query] string end_time,
        [Query] int? meeting_status = null,
        [Query] string? meeting_no = null,
        [Query] string? user_id = null,
        [Query] string? room_id = null,
        [Query] int? meeting_type = null,
        [Query] bool? include_external_meetings = null,
        [Query] bool? include_webinar = null,
        [Query] int? page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询参会人明细
    /// <para>查询参会人明细，具体权限要求请参考资源介绍</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-room-data/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_no">
    /// <para>9位会议号（会议链接最后9位数）</para>
    /// <para>示例值：123456789</para>
    /// </param>
    /// <param name="user_id">
    /// <para>按参会飞书用户筛选（最多一个筛选条件，如果设置多个，参数校验会失败）</para>
    /// <para>示例值：ou_3ec3f6a28a0d08c45d895276e8e5e19b</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="meeting_start_time">
    /// <para>会议开始时间（unix时间，单位sec）</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="meeting_end_time">
    /// <para>会议结束时间（unix时间，单位sec，若是进行中会议可填当前时间，否则填准确的会议结束时间）</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="room_id">
    /// <para>按参会Rooms筛选（最多一个筛选条件，如果设置多个，参数校验会失败）</para>
    /// <para>示例值：omm_eada1d61a550955240c28757e7dec3af</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="meeting_status">
    /// <para>会议状态（不传默认为已结束会议）</para>
    /// <para>示例值：2</para>
    /// <list type="bullet">
    /// <item>1：进行中</item>
    /// <item>2：已结束</item>
    /// <item>3：待召开。该枚举值只读，请求时不支持选择。</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="webinar_user_role">
    /// <para>查询网络研讨会时的观众类型,"0"为嘉宾，"3"为观众</para>
    /// <para>示例值：0</para>
    /// <para>默认值：null</para>
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
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/participant_list")]
    Task<FeishuApiResult<GetParticipantListResult>?> GetParticipantPageListAsync(
        [Query] string meeting_start_time,
        [Query] string meeting_end_time,
        [Query] string meeting_no,
        [Query] int? meeting_status = null,
        [Query] string? user_id = null,
        [Query] string? room_id = null,
        [Query] string? webinar_user_role = null,
        [Query] int? page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);




    /// <summary>
    /// 查询参会人会议质量数据
    /// <para>查询参会人会议质量数据（仅支持已结束会议），具体权限要求请参考「资源介绍」。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-room-data/get-3">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_no">
    /// <para>9位会议号（会议链接最后9位数）</para>
    /// <para>示例值：123456789</para>
    /// </param>
    /// <param name="user_id">
    /// <para>按参会飞书用户筛选（最多一个筛选条件，如果设置多个，参数校验会失败）</para>
    /// <para>示例值：ou_3ec3f6a28a0d08c45d895276e8e5e19b</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="meeting_start_time">
    /// <para>会议开始时间（unix时间，单位sec）</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="meeting_end_time">
    /// <para>会议结束时间（unix时间，单位sec，若是进行中会议可填当前时间，否则填准确的会议结束时间）</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="join_time">
    /// <para>参会人入会时间（unix时间，单位sec），可从「查询参会人明细」返回结果获取</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="room_id">
    /// <para>按参会Rooms筛选（最多一个筛选条件，如果设置多个，参数校验会失败）</para>
    /// <para>示例值：omm_eada1d61a550955240c28757e7dec3af</para>
    /// <para>默认值：null</para>
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
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/participant_quality_list")]
    Task<FeishuApiResult<GetParticipantQualityListResult>?> GetParticipantQualityPageListAsync(
       [Query] string meeting_start_time,
       [Query] string meeting_end_time,
       [Query] string meeting_no,
       [Query] string join_time,
       [Query] string? user_id = null,
       [Query] string? room_id = null,
       [Query] int? page_size = Consts.PageSize_20,
       [Query] string? page_token = null,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询会议室预定数据
    /// <para>查询会议室预定数据，具体权限要求请参考「资源介绍」。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-room-data/get-4">接口文档</see></para>
    /// </summary>
    /// <param name="room_level_id">
    /// <para>层级ID，如传递非omb前缀的异常ID时，会默认使用租户层级进行兜底</para>
    /// <para>示例值：omb_57c9cc7d9a81e27e54c8fabfd02759e7</para>
    /// </param>
    /// <param name="need_topic">
    /// <para>是否展示会议主题</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="start_time">
    /// <para>查询开始时间（unix时间，单位sec）</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="end_time">
    /// <para>查询结束时间（unix时间，单位sec）</para>
    /// <para>示例值：1655276858</para>
    /// </param>
    /// <param name="room_ids">
    /// <para>待筛选的会议室ID列表；如需要传递多个会议室ID，需要通过room_ids=aaaa&amp;room_ids=bbbb&amp;room_ids=cccc的形式传递</para>
    /// <para>示例值：["omm_12443435556"]</para>
    /// </param>
    /// <param name="is_exclude">
    /// <para>默认为false；若为false，则获取room_ids字段传入的会议室列表预定数据；若为true，则根据room_level_id字段获取层级下的会议室列表，并过滤掉room_ids范围的会议室，获取剩余会议室的预定数据</para>
    /// <para>示例值：false</para>
    /// <para>默认值：null</para>
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
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/resource_reservation_list")]
    Task<FeishuApiResult<GetResourceReservationListResult>?> GetResourceReservationPageListAsync(
        [Query] string room_level_id,
        [Query] string start_time,
        [Query] string end_time,
        [Query] string[] room_ids,
        [Query] bool? need_topic = null,
        [Query] bool? is_exclude = null,
        [Query] int? page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}