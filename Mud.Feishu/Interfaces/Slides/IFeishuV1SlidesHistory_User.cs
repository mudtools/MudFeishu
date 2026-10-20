// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书幻灯片历史版本接口，以用户身份调用历史版本列表、回滚发起与回滚状态查询端点。
/// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=list&amp;project=slides_ai&amp;resource=xml_presentation.history&amp;version=v1">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Slides", InheritedFrom = nameof(FeishuV1SlidesHistory))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1SlidesHistory : IFeishuV1SlidesHistory, ICurrentUserId
{
}
