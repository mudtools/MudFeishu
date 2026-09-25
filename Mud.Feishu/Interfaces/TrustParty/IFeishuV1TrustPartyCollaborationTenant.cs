// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TrustParty;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书关联组织（trust_party/v1）SDK 是一组服务端 OpenAPI 的封装，用于查询本组织与对方关联组织（协作组织）之间的协作关系，包括可见关联组织列表、关联组织详情、组织内可见的部门/成员/用户组信息以及部门、成员详情。全部端点同时支持 tenant_access_token 与 user_access_token（二者按不同的可见性规则校验）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1TrustPartyCollaborationTenant : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取可见关联组织的列表
    /// <para>分页获取当前用户/应用可见的关联组织（协作组织）列表，返回组织名称、简称、标签、头像、品牌与关联时间等信息。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">单次请求的关联组织数量，取值 1~100，默认 10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回可见关联组织分页列表（target_tenant_list、has_more、page_token）</returns>
    [Get("/open-apis/trust_party/v1/collaboration_tenants")]
    Task<FeishuApiResult<GetCollaborationTenantListResult>?> GetCollaborationTenantListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取关联组织详情
    /// <para>按对方关联组织的 tenant key 获取组织名称、简称、标签、头像、品牌与关联时间等详情。</para>
    /// <para>限频：5 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/get">接口文档</see></para>
    /// </summary>
    /// <param name="target_tenant_key">对方关联组织的 tenant key，可通过获取可见关联组织的列表接口获取，示例值：4e6ac4d14bcd5071a37a39de902c7141</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回对方关联组织详情（target_tenant）</returns>
    [Get("/open-apis/trust_party/v1/collaboration_tenants/{target_tenant_key}")]
    Task<FeishuApiResult<GetCollaborationTenantResult>?> GetCollaborationTenantAsync(
        [Path] string target_tenant_key,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取关联组织的成员信息
    /// <para>分页获取对方关联组织内指定部门或用户组下可见的部门、用户、用户组实体列表（查询参数采用查询对象模式 <see cref="VisibleOrganizationQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。target_department_id 与 target_group_id 二选一。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/visible_organization">接口文档</see></para>
    /// </summary>
    /// <param name="target_tenant_key">对方关联组织的 tenant key，可通过获取可见关联组织的列表接口获取</param>
    /// <param name="query">部门/用户组 ID 类型、目标 ID、分页等查询参数（target_department_id 与 target_group_id 二选一）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回可见实体分页列表（collaboration_entity_list、has_more、page_token）</returns>
    [Get("/open-apis/trust_party/v1/collaboration_tenants/{target_tenant_key}/visible_organization")]
    Task<FeishuApiResult<GetVisibleOrganizationResult>?> GetVisibleOrganizationAsync(
        [Path] string target_tenant_key,
        [Query] VisibleOrganizationQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取关联组织部门详情
    /// <para>按部门 ID 获取对方关联组织的部门详情（名称、i18n 名称、排序、负责人、父部门）。负责人与父部门需对其有可见性权限才会返回。</para>
    /// <para>限频：5 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="target_tenant_key">对方关联组织的 tenant key，可通过获取可见关联组织的列表接口获取</param>
    /// <param name="target_department_id">对方关联组织的部门 ID，需要与 target_department_id_type 中填写的值保持一致</param>
    /// <param name="target_department_id_type">对方关联组织的入参部门类型：department_id（默认，部门ID）/ open_department_id（部门open ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回对方关联组织的部门详情（target_department）</returns>
    [Get("/open-apis/trust_party/v1/collaboration_tenants/{target_tenant_key}/collaboration_departments/{target_department_id}")]
    Task<FeishuApiResult<GetCollaborationDepartmentResult>?> GetCollaborationDepartmentAsync(
        [Path] string target_tenant_key,
        [Path] string target_department_id,
        [Query("target_department_id_type")] string? target_department_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取关联组织成员详情
    /// <para>按用户 ID 获取对方关联组织的成员详情（名称、头像、手机号、职务、工号、自定义属性、部门与主管等）。手机号、职务、工号、自定义属性需要对方租户授权展示。</para>
    /// <para>限频：5 次/秒。所需权限：trust_party:collaboration.tenant:readonly（以应用身份读取关联组织）。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/get-3">接口文档</see></para>
    /// </summary>
    /// <param name="target_tenant_key">对方关联组织的 tenant key，可通过获取可见关联组织的列表接口获取</param>
    /// <param name="target_user_id">请求的关联组织用户ID，需要与 target_user_id_type 中填写的类型保持一致</param>
    /// <param name="target_user_id_type">用户ID类型：user_id（默认）/ union_id / open_id，可从获取关联组织的成员信息接口中获取对应的用户ID</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回对方关联组织的成员详情（target_user）</returns>
    [Get("/open-apis/trust_party/v1/collaboration_tenants/{target_tenant_key}/collaboration_users/{target_user_id}")]
    Task<FeishuApiResult<GetCollaborationUserResult>?> GetCollaborationUserAsync(
        [Path] string target_tenant_key,
        [Path] string target_user_id,
        [Query("target_user_id_type")] string? target_user_id_type = null,
        CancellationToken cancellationToken = default);
}
