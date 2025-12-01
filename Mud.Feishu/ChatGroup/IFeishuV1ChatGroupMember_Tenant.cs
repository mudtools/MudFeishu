// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书群成员包括用户和机器人。在飞书群组内，支持添加用户或者机器人作为群成员，同时支持将用户或者机器人设置为群管理员。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/group/chat-member/intro"/>
/// </summary>
[HttpClientApi(RegistryGroupName = "ChatGroup", TokenManage = nameof(ITenantTokenManager), InheritedFrom = nameof(FeishuV1ChatGroupMember))]
[Header("Authorization")]
public interface IFeishuTenantV1ChatGroupMember : IFeishuV1ChatGroupMember
{
}
