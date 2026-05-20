// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu;

/// <summary>
/// 用户可以进行查询会议室、创建会议室、更新会议室等操作
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/room/room-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing")]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1VideoConferencingRoom
{

    /// <summary>
    /// 创建会议室
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/room/create">接口文档</see></para>
    /// </summary>   
    /// <param name="createMeetingRoomRequest">创建会议室请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/vc/v1/rooms")]
    Task<FeishuApiResult<CreateMeetingRoomResult>?> CreateMeetingRoomAsync(
      [Body] CreateMeetingRoomRequest createMeetingRoomRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除会议室
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/room/delete">接口文档</see></para>
    /// </summary>   
    /// <param name="room_id">
    /// <para>会议室ID</para>
    /// <para>示例值：omm_4de32cf10a4358788ff4e09e37ebbf9b</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/vc/v1/rooms/{room_id}")]
    Task<FeishuNullDataApiResult?> DeleteMeetingRoomAsync(
       [Path] string room_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新会议室
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/room/patch">接口文档</see></para>
    /// </summary>   
    /// <param name="room_id">
    /// <para>会议室ID</para>
    /// <para>示例值：omm_4de32cf10a4358788ff4e09e37ebbf9b</para>
    /// </param>
    /// <param name="updateMeetingRoomRequest">更新会议室请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/vc/v1/rooms/{room_id}")]
    Task<FeishuNullDataApiResult?> UpdateMeetingRoomAsync(
        [Path] string room_id,
        [Body] UpdateMeetingRoomRequest updateMeetingRoomRequest,
        CancellationToken cancellationToken = default);
}