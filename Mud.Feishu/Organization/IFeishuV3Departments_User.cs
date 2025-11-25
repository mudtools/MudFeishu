// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书组织机构部门是指企业组织架构树上的某一个节点。
/// <para>当前接口使用用户令牌访问，适应于用户应用场景。</para>
/// <para>在部门内部，可添加用户作为部门成员，也可添加新的部门作为子部门。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/department/field-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IUserTokenManager), RegistryGroupName = "Organization")]
[Header("Authorization")]
public interface IFeishuUserV3Departments : IFeishuV3Departments
{
}
