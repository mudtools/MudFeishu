// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）用户角色 SDK 是一组服务端 OpenAPI 的封装，用于分页获取用户的角色（权限身份）分配列表。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/list-2"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireUserRole : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取用户角色列表
    /// <para>按用户、角色或更新时间分页查询用户角色分配关系，返回角色名称与业务管理范围（查询参数采用查询对象模式 <see cref="UserRoleListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页、用户、角色、更新时间范围与用户 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回用户角色分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/user_roles")]
    Task<FeishuApiResult<GetUserRoleListResult>?> GetUserRoleListAsync(
        [Query] UserRoleListQuery? query = null,
        CancellationToken cancellationToken = default);
}
