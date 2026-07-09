// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 通过不同条件查询审批系统中符合条件的审批实例、审批抄送、审批抄送列表(适用于原生审批及三方审批)。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/approval-search/query-2"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Approval", InheritedFrom = nameof(FeishuV4ApprovalQuery))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV4ApprovalQuery : IFeishuV4ApprovalQuery, ICurrentUserId
{
}