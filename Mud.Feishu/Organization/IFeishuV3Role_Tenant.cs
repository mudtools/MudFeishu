// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Roles;

namespace Mud.Feishu;

/// <summary>
/// 飞书角色指的是团队成员的专业分工类别，如人事、行政、财务等，一个角色可由一名或多名成员组成。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// <para>目前，角色主要用于应用审批场景。在审批管理后台，管理员可以选择某一角色作为审批人。</para> 
/// <para>例如，选择财务角色作为报销流程的审批人。这样做可以避免因成员离职变动导致的审批流失效的情况，角色内的其他成员可以继续完成审批，提高审批效率。</para> 
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/functional_role/resource-introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Organization")]
[Header("Authorization")]
public interface IFeishuTenantV3Role
{
    /// <summary>
    /// 创建一个角色。
    /// </summary>
    /// <param name="roleRequest">创建角色请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/functional_roles")]
    Task<FeishuApiResult<RoleCreateResult>?> CreateRoleAsync(
        [Body] RoleRequest roleRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改指定角色的角色名称。
    /// </summary>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="roleRequest">创建角色请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}")]
    Task<FeishuNullDataApiResult?> UpdateRoleAsync(
      [Path] string role_id,
      [Body] RoleRequest roleRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定角色。
    /// </summary>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}")]
    Task<FeishuNullDataApiResult?> DeleteRoleByIdAsync(
      [Path] string role_id,
      CancellationToken cancellationToken = default);
}
