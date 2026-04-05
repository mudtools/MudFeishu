// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// <para>飞书多维表格高级权限允许用户针对单一数据表设置哪些用户可以查看、编辑指定的行，或是设置针对某用户可以编辑的列。。</para>
/// <para>高级权限接口分为 自定义角色 和 协作者 两部分，多维表格的 所有者 或者 有可管理权限 的用户可通过接口设置高级权限，管理高级权限的协作者。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/advanced-permission-guide"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Bitable", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV2BitableRole))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV2BitableRole : IFeishuV2BitableRole, ICurrentUserId
{
}