// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书群组 OpenAPI 提供了群组管理能力，包括创建群、解散群、更新群信息、获取群信息、管理群置顶以及获取群分享链接等。
/// <para>当前接口使用用户令牌访问，适应于用户应用场景。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/group/chat/intro"/>
/// </summary>
[HttpClientApi(RegistryGroupName = "ChatGroup", TokenManage = nameof(IUserTokenManager), InheritedFrom = nameof(FeishuV1ChatGroup))]
[Header("Authorization")]
public interface IFeishuUserV1ChatGroup : IFeishuV1ChatGroup
{

}
