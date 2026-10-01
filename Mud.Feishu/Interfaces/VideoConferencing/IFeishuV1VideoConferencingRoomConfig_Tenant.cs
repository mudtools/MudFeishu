// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu;

/// <summary>
/// 会议室配置用于统一管理国家/地区、城市、楼宇、楼层与会议室各层级上的展示与状态配置。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/historic-version/meeting_room-v1/room_config/rooms-configuration-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1VideoConferencingRoomConfig : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询指定节点范围内的会议室级别配置，包括背景图、数字标牌与会议室状态等。
    /// <para><see href="https://open.feishu.cn/document/server-docs/historic-version/meeting_room-v1/room_config/query">接口文档</see></para>
    /// </summary>
    /// <param name="scope">查询配置的节点范围。</param>
    /// <param name="country_id">国家/地区 ID，scope 为 2 或 3 时必填。</param>
    /// <param name="district_id">城市 ID，scope 为 3 时必填。</param>
    /// <param name="building_id">楼宇 ID，scope 为 4 或 5 时必填。</param>
    /// <param name="floor_name">楼层名称，scope 为 5 时必填。</param>
    /// <param name="room_id">会议室 ID，scope 为 6 时必填。</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/vc/v1/room_configs/query")]
    Task<FeishuApiResult<RoomLevelConfig>?> QueryRoomConfigAsync(
       [Query("scope")] int scope,
       [Query("country_id")] string? country_id = null,
       [Query("district_id")] string? district_id = null,
       [Query("building_id")] string? building_id = null,
       [Query("floor_name")] string? floor_name = null,
       [Query("room_id")] string? room_id = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置指定节点范围内的会议室级别配置，包括背景图、数字标牌与会议室状态等。
    /// <para><see href="https://open.feishu.cn/document/server-docs/historic-version/meeting_room-v1/room_config/set">接口文档</see></para>
    /// </summary>
    /// <param name="setRoomConfigRequest">设置会议室级别配置请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/vc/v1/room_configs/set")]
    Task<FeishuNullDataApiResult?> SetRoomConfigAsync(
       [Body] SetRoomConfigRequest setRoomConfigRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置签到板的部署访问码，返回生成的访问码。
    /// <para><see href="https://open.feishu.cn/document/server-docs/historic-version/meeting_room-v1/room_config/set_checkboard_access_code">接口文档</see></para>
    /// </summary>
    /// <param name="setAccessCodeRequest">设置部署访问码请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/vc/v1/room_configs/set_checkboard_access_code")]
    Task<FeishuApiResult<SetAccessCodeResult>?> SetCheckboardAccessCodeAsync(
       [Body] SetAccessCodeRequest setAccessCodeRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置会议室的部署访问码，返回生成的访问码。
    /// <para><see href="https://open.feishu.cn/document/server-docs/historic-version/meeting_room-v1/room_config/set_room_access_code">接口文档</see></para>
    /// </summary>
    /// <param name="setAccessCodeRequest">设置部署访问码请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/vc/v1/room_configs/set_room_access_code")]
    Task<FeishuApiResult<SetAccessCodeResult>?> SetRoomAccessCodeAsync(
       [Body] SetAccessCodeRequest setAccessCodeRequest,
       CancellationToken cancellationToken = default);
}
