// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu;


/// <summary>
/// 会议管理功能为用户在会议中进行邀请参会成员、移除参会成员和设置主持人等操作。
/// <para>功能包括：获取会议详情、获取与会议号相关联的会议列表、邀请参会人、移除参会人、设置主持人、结束会议。事件包括：会议开始、会议结束、加入会议、离开会议、录制开始、录制停止、录制完成、屏幕共享开始、屏幕共享结束。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting/meeting-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing", InheritedFrom = nameof(FeishuV1VideoConferencingMeeting))]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1VideoConferencingMeeting : IFeishuV1VideoConferencingMeeting
{


    /// <summary>
    /// 移除参会人
    /// <para>将参会人从会议中移除。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting/kickout">接口文档</see></para>
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
    /// <param name="kickoutMeetingRequest">移除会议用户请求</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/vc/v1/meetings/{meeting_id}/kickout")]
    Task<FeishuApiResult<KickoutMeetingResult>?> KickoutMeetingAsync(
       [Path] string meeting_id,
       [Body] KickoutMeetingRequest kickoutMeetingRequest,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}