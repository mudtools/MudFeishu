// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书会议机器人资源，机器人可以加入会议、在会中发送消息、设置会中倒计时、获取会中事件，以及离会。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/bot/user-guide/agent-meeting-user-guide">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing", InheritedFrom = nameof(FeishuV1VideoConferencingBot))]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1VideoConferencingBot : IFeishuV1VideoConferencingBot
{
}
