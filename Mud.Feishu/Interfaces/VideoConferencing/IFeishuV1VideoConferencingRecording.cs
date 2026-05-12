// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 用户可以录制一场会议，在会议结束后获得会议录制文件链接，包括：开始录制、停止录制、获取录制文件、授权录制文件。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-recording/recording-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1VideoConferencingRecording : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 获取录制文件
    /// <para>获取一个会议的录制文件。会议结束后并且收到了录制完成的事件方可获取录制文件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-recording/start">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_id">
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>示例值：6911188411932033028</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/meetings/{meeting_id}/recording")]
    Task<FeishuApiResult<GetMeetingRecordingResult>?> GetMeetingRecordingAsync(
      [Path] string meeting_id,
      CancellationToken cancellationToken = default);
}