// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 会议预约功能为用户提供预约会议（创建会议预约）功能，可以提前设置参会成员和会议权限，并获取会议信息，
/// <para>功能包括：预约会议、更新预约会议信息、删除预约会议、获取预约会议详情、获取正在进行的会议。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/reserve/schedule-meeting-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1VideoConferencingReserves : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 预约会议
    /// <para>创建一个会议预约。支持预约最近30天内的会议（到期时间距离当前时间不超过30天），预约到期后会议号将被释放，如需继续使用可通过"更新预约"接口进行续期;</para>
    /// <para>预约会议时可配置参会人在会中的权限，以达到控制会议的目的。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/reserve/apply">接口文档</see></para>
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
    /// <param name="applyReserveRequest">预约会议请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/vc/v1/reserves/apply")]
    Task<FeishuApiResult<ApplyReserveResult>?> ApplyReserveAsync(
      [Body] ApplyReserveRequest applyReserveRequest,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);



    /// <summary>
    /// 删除预约
    /// <para>删除一个预约。只能删除归属于自己的预约；删除后数据不可恢复。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/reserve/delete">接口文档</see></para>
    /// </summary> 
    /// <param name="reserve_id">
    /// <para>预约ID（预约的唯一标识）</para>
    /// <para>示例值：6911188411932033028</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/vc/v1/reserves/{reserve_id}")]
    Task<FeishuNullDataApiResult?> DeleteReserveAsync(
       [Path] string reserve_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新预约
    /// <para>更新一个预约。只能更新归属于自己的预约，不需要更新的字段不传（如果传空则会被更新为空）；可用于续期操作，到期时间距离当前时间不超过30天</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/reserve/update">接口文档</see></para>
    /// </summary> 
    /// <param name="reserve_id">
    /// <para>预约ID（预约的唯一标识）</para>
    /// <para>示例值：6911188411932033028</para>
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
    /// <param name="updateReserveRequest">更新预约请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/vc/v1/reserves/{reserve_id}")]
    Task<FeishuApiResult<UpdateReserveResult>?> UpdateReserveAsync(
      [Path] string reserve_id,
      [Body] UpdateReserveRequest updateReserveRequest,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取预约
    /// <para>获取一个预约的详情。只能获取归属于自己的预约。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/reserve/get">接口文档</see></para>
    /// </summary> 
    /// <param name="reserve_id">
    /// <para>预约ID（预约的唯一标识）</para>
    /// <para>示例值：6911188411932033028</para>
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
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/reserves/{reserve_id}")]
    Task<FeishuApiResult<GetReserveResult>?> GetReserveAsync(
       [Path] string reserve_id,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 获取活跃会议
    /// <para>获取一个预约的当前活跃会议。只能获取归属于自己的预约的活跃会议（一个预约最多有一个正在进行中的会议）</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/reserve/get_active_meeting">接口文档</see></para>
    /// </summary> 
    /// <param name="reserve_id">
    /// <para>预约ID（预约的唯一标识）</para>
    /// <para>示例值：6911188411932033028</para>
    /// </param>
    /// <param name="with_participants">
    /// <para>是否需要参会人列表，默认为false</para>
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
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/reserves/{reserve_id}/get_active_meeting")]
    Task<FeishuApiResult<GetReserveResult>?> GetActiveMeetingReserveAsync(
      [Path] string reserve_id,
      [Query] bool? with_participants = null,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}