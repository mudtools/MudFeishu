// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;


/// <summary>
/// 用于分页查询一段时间内租户的会议数据，包括：查询会议明细、查询参会人明细、查询参会人会议质量数据、查询会议室预定数据。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/meeting-room-data/resource-introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing", InheritedFrom = nameof(FeishuV1VideoConferencingMeetinData))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV1VideoConferencingMeetinData : IFeishuV1VideoConferencingMeetinData, ICurrentUserId
{
}