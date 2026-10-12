// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu;

/// <summary>
/// 飞书应用权限接口，用于以应用身份向租户管理员申请需要审核的 API 权限，以及查询应用向租户申请授权的状态。
/// <para><see href="https://open.feishu.cn/document/application-v6/scope/apply">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Application")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV6ApplicationScope : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 向管理员申请授权。
    /// <para>以应用身份向租户管理员申请应用内需要审核的 API 权限。同一租户下，其他员工在一个应用的同一个版本向管理员申请授权的次数不能超过 10 次。</para>
    /// <para><see href="https://open.feishu.cn/document/application-v6/scope/apply">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/application/v6/scopes/apply")]
    Task<FeishuNullDataApiResult?> ApplyScopeAsync(
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询租户授权状态。
    /// <para>查询当前应用向租户申请授权的状态。</para>
    /// <para><see href="https://open.feishu.cn/document/application-v6/scope/list">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v6/scopes")]
    Task<FeishuApiResult<GetScopesResult>?> GetScopesAsync(
      CancellationToken cancellationToken = default);
}
