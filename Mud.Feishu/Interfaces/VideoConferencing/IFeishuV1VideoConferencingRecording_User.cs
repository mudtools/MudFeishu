// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu;



/// <summary>
/// 用户可以录制一场会议，在会议结束后获得会议录制文件链接，包括：开始录制、停止录制、获取录制文件、授权录制文件。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-recording/recording-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing", InheritedFrom = nameof(FeishuV1VideoConferencingRecording))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV1VideoConferencingRecording : IFeishuV1VideoConferencingRecording, ICurrentUserId
{

    /// <summary>
    /// 开始录制
    /// <para>在会议中开始录制。会议正在进行中，且操作者具有相应权限（如果操作者为用户，必须是会中当前主持人）</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-recording/start">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_id">
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>示例值：6911188411932033028</para>
    /// </param>
    /// <param name="startMeetingRecordingRequest">开始会议录制请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/vc/v1/meetings/{meeting_id}/recording/start")]
    Task<FeishuNullDataApiResult?> StartMeetingRecordingAsync(
      [Path] string meeting_id,
      [Body] StartMeetingRecordingRequest startMeetingRecordingRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 停止录制
    /// <para>在会议中停止录制。会议正在录制中，且操作者具有相应权限（如果操作者为用户，必须是会中当前主持人）</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-recording/stop">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_id">
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>示例值：6911188411932033028</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/vc/v1/meetings/{meeting_id}/recording/stop")]
    Task<FeishuNullDataApiResult?> StopMeetingRecordingAsync(
      [Path] string meeting_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 授权录制文件
    /// <para>将一个会议的录制文件授权给组织、用户或公开到公网。</para>
    /// <para>会议结束后并且收到了"录制完成"的事件方可进行授权；会议owner（通过开放平台预约的会议即为预约人）才有权限操作</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-recording/start">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_id">
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
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
    /// </param>
    /// <param name="setPermissionMeetingRecordingRequest">授权录制文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/vc/v1/meetings/{meeting_id}/recording/set_permission")]
    Task<FeishuNullDataApiResult?> SetPermissionMeetingRecordingAsync(
      [Path] string meeting_id,
      [Body] SetPermissionMeetingRecordingRequest setPermissionMeetingRecordingRequest,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}