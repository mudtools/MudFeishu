// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;


/// <summary>
/// 会议预约功能为用户提供预约会议（创建会议预约）功能，可以提前设置参会成员和会议权限，并获取会议信息，
/// <para>功能包括：预约会议、更新预约会议信息、删除预约会议、获取预约会议详情、获取正在进行的会议。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/reserve/schedule-meeting-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing", InheritedFrom = nameof(FeishuV1VideoConferencingReserves))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1VideoConferencingReserves : IFeishuV1VideoConferencingReserves, ICurrentUserId
{

}