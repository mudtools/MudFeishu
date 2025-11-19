using Mud.Feishu.DataModels.Roles;

namespace Mud.Feishu;

/// <summary>
/// 飞书角色指的是团队成员的专业分工类别，如人事、行政、财务等，一个角色可由一名或多名成员组成。
/// <para>目前，角色主要用于应用审批场景。在审批管理后台，管理员可以选择某一角色作为审批人。</para> 
/// <para>例如，选择财务角色作为报销流程的审批人。这样做可以避免因成员离职变动导致的审批流失效的情况，角色内的其他成员可以继续完成审批，提高审批效率。</para> 
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuRole))]
public interface IFeishuRoleApi
{
    /// <summary>
    /// 创建一个角色。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="roleRequest">创建角色请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/functional_roles")]
    Task<FeishuApiResult<RoleCreateResult>> CreateRoleAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        [Body] RoleRequest roleRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改指定角色的角色名称。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="roleRequest">创建角色请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}")]
    Task<FeishuNullDataApiResult> UpdateRoleAsync(
      [Token][Header("Authorization")] string tenant_access_token,
      [Path] string role_id,
      [Body] RoleRequest roleRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定角色。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}")]
    Task<FeishuNullDataApiResult> DeleteRoleByIdAsync(
      [Token][Header("Authorization")] string tenant_access_token,
      [Path] string role_id,
      CancellationToken cancellationToken = default);
}
