// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）角色 SDK 是一组服务端 OpenAPI 的封装，用于获取角色详情与角色列表。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireRole : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取角色信息
    /// <para>按角色 ID 获取角色详情，包括角色名称、描述、适用范围与社招/校招权限配置。</para>
    /// <para>限频：10 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/get">接口文档</see></para>
    /// </summary>
    /// <param name="role_id">角色 ID，可通过获取角色列表接口获取，示例值：7350589232462807068</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回角色详情（role）</returns>
    [Get("/open-apis/hire/v1/roles/{role_id}")]
    Task<FeishuApiResult<GetRoleResult>?> GetRoleAsync(
        [Path] string role_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取角色列表
    /// <para>分页获取企业内飞书招聘角色列表，返回角色名称、描述与适用范围。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 200</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回角色分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/roles")]
    Task<FeishuApiResult<GetRoleListResult>?> GetRoleListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
