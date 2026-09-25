// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TrustParty;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书可搜可见规则（directory/v1/collaboration_rules）SDK 用于管理关联组织间的协作规则：查询、新增、更新、删除规则，控制双方组织内哪些主体（人员/部门/用户组）可以搜到并看见对方组织内的哪些客体。规则主客体实体数量之和需小于100。全部端点同时支持 tenant_access_token 与 user_access_token，调用者需具备关联组织管理员权限。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/trust_party-v1/searchable-and-visible-rules/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1DirectoryCollaborationRule : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询可搜可见规则
    /// <para>分页查询与对方组织之间的可搜可见规则列表，返回规则 ID、主体、客体及主客体是否在分享范围内（超出分享范围时为 false 且对应实体不返回）。</para>
    /// <para>所需权限：trust_party:collaboration_rule:read（读取关联组织协作规则）；同时需要关联组织管理员权限。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/searchable-and-visible-rules/list">接口文档</see></para>
    /// </summary>
    /// <param name="target_tenant_key">对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获取，示例值：test_key</param>
    /// <param name="page_size">分页大小，取值 0~100，默认 100</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回可搜可见规则分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/directory/v1/collaboration_rules")]
    Task<FeishuApiResult<GetCollaborationRuleListResult>?> GetCollaborationRuleListAsync(
        [Query("target_tenant_key")] string target_tenant_key,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 新增可搜可见规则
    /// <para>为本组织与对方组织之间新增一条可搜可见规则，主体取自我方通讯录实体，客体为对方组织内实体（可通过获取共享成员范围及关联组织部门/成员信息接口获取）。仅支持自建应用。</para>
    /// <para>限频：100 次/分钟。所需权限：trust_party:collaboration_rule:write（变更关联组织协作规则）；同时需要关联组织管理员权限。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/searchable-and-visible-rules/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">新增规则请求体（subjects 主体与 objects 客体必填，主客体实体数量之和需小于100）</param>
    /// <param name="target_tenant_key">对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获取</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回添加的规则 ID（add_rule_id）</returns>
    [Post("/open-apis/directory/v1/collaboration_rules")]
    Task<FeishuApiResult<CreateCollaborationRuleResult>?> CreateCollaborationRuleAsync(
        [Body] CreateCollaborationRuleRequest request,
        [Query("target_tenant_key")] string target_tenant_key,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新可搜可见规则
    /// <para>按规则 ID 更新可搜可见规则的主客体实体。仅支持自建应用。</para>
    /// <para>限频：100 次/分钟。所需权限：trust_party:collaboration_rule:write（变更关联组织协作规则）；同时需要关联组织管理员权限。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/searchable-and-visible-rules/update">接口文档</see></para>
    /// </summary>
    /// <param name="collaboration_rule_id">规则ID，通过查询可搜可见规则接口获取，示例值：12121</param>
    /// <param name="request">更新规则请求体（subjects 主体与 objects 客体必填，主客体实体数量之和需小于100）</param>
    /// <param name="target_tenant_key">对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获取</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Put("/open-apis/directory/v1/collaboration_rules/{collaboration_rule_id}")]
    Task<FeishuNullDataApiResult?> UpdateCollaborationRuleAsync(
        [Path] string collaboration_rule_id,
        [Body] UpdateCollaborationRuleRequest request,
        [Query("target_tenant_key")] string target_tenant_key,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除可搜可见规则
    /// <para>按规则 ID 删除与对方组织之间的一条可搜可见规则。仅支持自建应用。</para>
    /// <para>所需权限：trust_party:collaboration_rule:write（变更关联组织协作规则）；同时需要关联组织管理员权限。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/searchable-and-visible-rules/delete">接口文档</see></para>
    /// </summary>
    /// <param name="collaboration_rule_id">规则ID，通过查询可搜可见规则接口获取</param>
    /// <param name="target_tenant_key">对方组织的 tenant key，可通过管理员获取所有关联组织列表接口获取</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/directory/v1/collaboration_rules/{collaboration_rule_id}")]
    Task<FeishuNullDataApiResult?> DeleteCollaborationRuleAsync(
        [Path] string collaboration_rule_id,
        [Query("target_tenant_key")] string target_tenant_key,
        CancellationToken cancellationToken = default);
}
