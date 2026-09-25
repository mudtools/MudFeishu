// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TrustParty;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书关联组织管理端（directory/v1）SDK 用于管理员视角查询本租户所有已建联的关联组织（返回 i18n_text 结构的名称与简称），为创建可搜可见规则等管理操作提供有效的 tenant key。全部端点同时支持 tenant_access_token 与 user_access_token。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list-2"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1DirectoryCollaborationTenant : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 管理员获取所有关联组织列表
    /// <para>分页获取本租户管理员视角下所有已建联的关联组织列表（tenant key、建联时间、头像、品牌、名称与简称），用于创建规则时获取对方组织的有效 tenant key。仅支持自建应用。</para>
    /// <para>限频：100 次/分钟。所需权限：trust_party:collaboration_rule:read（读取关联组织协作规则）；同时需要关联组织管理员权限。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，取值 0~100，默认 100</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回所有关联组织分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/directory/v1/collaboration_tenants")]
    Task<FeishuApiResult<GetAllCollaborationTenantListResult>?> GetAllCollaborationTenantListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
