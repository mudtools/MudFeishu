// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── TrustParty · 信任方 / 跨组织互通（B1 第五域，收官） ───────────────────────────

// 命名口径：工具名前缀 trustparty 在生成器里规范化为 **Trustparty**（常量 / 参数类 / 注册器），
// 执行器类名 TrustPartyTools ⇒ DI 入口 AddFeishuTrustPartyToolsCore()，SDK 接口名仍是
// IFeishuTenantV1TrustPartyCollaborationTenant / IFeishuTenantV1Directory*（SDK 侧按专有名词大写）。
//
// 本域特点（与前面四域不同的一点）：**scope 字面量在 SDK XML 注释里是齐全的**
//   · trust_party:collaboration.tenant:readonly —— 关联组织 / 可见组织 / 外部部门 / 外部成员
//   · trust_party:collaboration_rule:read —— 共享成员范围 / 全部关联组织 / 互通规则
// 故权限对照表这一区段**不标 ⚠️**（其余四域因 SDK 注释只有自然语言而必须标）。

/// <summary>
/// 工具接口：trustparty.list_collaboration_tenants（映射 <c>IFeishuTenantV1TrustPartyCollaborationTenant.GetCollaborationTenantListAsync</c>）。
/// </summary>
/// <remarks>
/// <b>本域第一跳</b>：<c>target_tenant_key</c> 是后续所有互通查询（组织 / 部门 / 成员 / 规则）的作用域句柄，
/// 而它只能从"关联组织列表"里拿到。
/// </remarks>
[FeishuTool("trustparty.list_collaboration_tenants",
    Description = "分页列出本租户已建立互通关系的**关联组织**（返回 target_tenant_key/组织名/短名/连接时间/标签）——「我们和哪些组织有互通」的第一跳；拿到 target_tenant_key 才能查对端可见组织、外部部门与成员。只读，需 trust_party:collaboration.tenant:readonly。",
    RequiredScopes = ["trust_party:collaboration.tenant:readonly"],
    Source = nameof(IFeishuTenantV1TrustPartyCollaborationTenant) + "." + nameof(IFeishuTenantV1TrustPartyCollaborationTenant.GetCollaborationTenantListAsync))]
public interface IFeishuTenantTrustpartyListCollaborationTenantsTool
{
    /// <summary>列出关联组织。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：target_tenant_key/tenant_name/tenant_short_name/connect_time/tenant_tag），超长截断并标记 truncated。</returns>
    Task<string> ListCollaborationTenantsAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：trustparty.get_collaboration_tenant（映射 <c>GetCollaborationTenantAsync</c>）。
/// </summary>
[FeishuTool("trustparty.get_collaboration_tenant",
    Description = "按 target_tenant_key 查询单个关联组织的详情（组织名/短名/连接时间/标签/品牌）——「这个外部组织是谁、什么时候连上的」。只读，需 trust_party:collaboration.tenant:readonly。",
    RequiredScopes = ["trust_party:collaboration.tenant:readonly"],
    Source = nameof(IFeishuTenantV1TrustPartyCollaborationTenant) + "." + nameof(IFeishuTenantV1TrustPartyCollaborationTenant.GetCollaborationTenantAsync))]
public interface IFeishuTenantTrustpartyGetCollaborationTenantTool
{
    /// <summary>查询关联组织详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + tenant：target_tenant_key/组织名/短名/连接时间/标签），超长截断并标记 truncated。</returns>
    Task<string> GetCollaborationTenantAsync(
        [ToolParameter("target_tenant_key", "关联组织的租户标识（必填，来自 trustparty.list_collaboration_tenants）", Required = true)] string target_tenant_key,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：trustparty.list_visible_organization（映射 <c>GetVisibleOrganizationAsync</c>）。
/// </summary>
/// <remarks>
/// 回答"对端组织对我们开放了哪些人 / 部门 / 群"——返回的实体带<b>名称与标识</b>
/// （姓名在本仓工具面是既有口径，如 <c>contact</c> 域；联系方式类字段不在本结果里）。
/// </remarks>
[FeishuTool("trustparty.list_visible_organization",
    Description = "分页查询关联组织**对本租户可见**的组织实体（成员 / 部门 / 群，含名称与 open_id|department_id|group_id）——「对端开放了哪些人、哪些部门」。可用 target_department_id / target_group_id 收窄到某个分支。只读，需 trust_party:collaboration.tenant:readonly。",
    RequiredScopes = ["trust_party:collaboration.tenant:readonly"],
    Source = nameof(IFeishuTenantV1TrustPartyCollaborationTenant) + "." + nameof(IFeishuTenantV1TrustPartyCollaborationTenant.GetVisibleOrganizationAsync))]
public interface IFeishuTenantTrustpartyListVisibleOrganizationTool
{
    /// <summary>查询可见组织实体。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：entity_type + 部门/用户/群三选一的名称与标识），超长截断并标记 truncated。</returns>
    Task<string> ListVisibleOrganizationAsync(
        [ToolParameter("target_tenant_key", "关联组织的租户标识（必填，来自 trustparty.list_collaboration_tenants）", Required = true)] string target_tenant_key,
        [ToolParameter("target_department_id", "只看该部门分支（可选；传该部门的 ID，类型由 department_id_type 指定）")] string? target_department_id = null,
        [ToolParameter("department_id_type", "部门 ID 类型（可选：open_department_id / department_id；省略按平台默认）")] string? department_id_type = null,
        [ToolParameter("target_group_id", "只看该用户组分支（可选；类型由 group_id_type 指定）")] string? target_group_id = null,
        [ToolParameter("group_id_type", "用户组 ID 类型（可选：open_group_id / group_id；省略按平台默认）")] string? group_id_type = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：trustparty.get_collaboration_department（映射 <c>GetCollaborationDepartmentAsync</c>）。
/// </summary>
/// <remarks>
/// 直属上级只给<b>负责人标识</b>（<c>leader_type</c> + user_id/open_id），不含姓名——
/// 要姓名请先用本工具拿到 ID，再走 <c>trustparty.get_collaboration_user</c>。
/// </remarks>
[FeishuTool("trustparty.get_collaboration_department",
    Description = "按 target_tenant_key + target_department_id 查询关联组织的外部部门详情（部门名/排序/负责人标识/上级部门标识）——「对端这个部门是什么、谁负责」。只读，需 trust_party:collaboration.tenant:readonly。",
    RequiredScopes = ["trust_party:collaboration.tenant:readonly"],
    Source = nameof(IFeishuTenantV1TrustPartyCollaborationTenant) + "." + nameof(IFeishuTenantV1TrustPartyCollaborationTenant.GetCollaborationDepartmentAsync))]
public interface IFeishuTenantTrustpartyGetCollaborationDepartmentTool
{
    /// <summary>查询外部部门详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + department：部门名/排序/负责人与上级部门标识），超长截断并标记 truncated。</returns>
    Task<string> GetCollaborationDepartmentAsync(
        [ToolParameter("target_tenant_key", "关联组织的租户标识（必填，来自 trustparty.list_collaboration_tenants）", Required = true)] string target_tenant_key,
        [ToolParameter("target_department_id", "外部部门 ID（必填，类型由 target_department_id_type 指定）", Required = true)] string target_department_id,
        [ToolParameter("target_department_id_type", "外部部门 ID 类型（可选：open_department_id / department_id）")] string? target_department_id_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：trustparty.get_collaboration_user（映射 <c>GetCollaborationUserAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>出站净化（本域最硬的一条）</b>：平台结果里的 <c>mobile</c>（手机号，需对方授权才有值）、
/// <c>employee_no</c>（工号）、<c>custom_attrs</c>（自定义属性，可能含任意个人信息）、
/// <c>avatar</c>、<c>status</c>（在职/离职细分状态）<b>一律不带出</b>。
/// 本工具回答"这个人是谁、在哪个部门、什么职务"，而不是"怎么联系他、他是不是离职了"。
/// </para>
/// <para>
/// <b>为什么保留 <c>job_title</c></b>：职务属组织角色信息（与 <c>org.*</c> 域口径一致），
/// 不含个人联系方式。
/// </para>
/// </remarks>
[FeishuTool("trustparty.get_collaboration_user",
    Description = "按 target_tenant_key + target_user_id 查询关联组织的外部成员详情（姓名/职务/所属部门标识/上级标识）——「对端这个联系人是谁、在哪个部门」。⚠️ 不返回手机号、工号、自定义属性与在职状态（出站净化）。只读，需 trust_party:collaboration.tenant:readonly。",
    RequiredScopes = ["trust_party:collaboration.tenant:readonly"],
    Source = nameof(IFeishuTenantV1TrustPartyCollaborationTenant) + "." + nameof(IFeishuTenantV1TrustPartyCollaborationTenant.GetCollaborationUserAsync))]
public interface IFeishuTenantTrustpartyGetCollaborationUserTool
{
    /// <summary>查询外部成员详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + user：姓名/职务/open_id·user_id·union_id/部门标识/上级标识），超长截断并标记 truncated。</returns>
    Task<string> GetCollaborationUserAsync(
        [ToolParameter("target_tenant_key", "关联组织的租户标识（必填，来自 trustparty.list_collaboration_tenants）", Required = true)] string target_tenant_key,
        [ToolParameter("target_user_id", "外部成员 ID（必填，类型由 target_user_id_type 指定）", Required = true)] string target_user_id,
        [ToolParameter("target_user_id_type", "外部成员 ID 类型（可选：open_id / user_id / union_id）")] string? target_user_id_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：trustparty.list_share_entities（映射 <c>IFeishuTenantV1DirectoryShareEntity.GetShareEntityListAsync</c>）。
/// </summary>
/// <remarks>
/// 三份清单（共享的<b>部门 / 用户组 / 用户</b>）来自同一个结果对象，投影时合成一份带 <c>kind</c> 的
/// <c>items</c>（统一分页出口按 <c>items</c> 聚合），同时保留三份分类清单便于直接读。
/// </remarks>
[FeishuTool("trustparty.list_share_entities",
    Description = "分页查询目录里**被共享出去**的实体（部门 / 用户组 / 用户三份清单，含名称与 open_id|open_department_id|open_group_id）——「我们把哪些组织单元共享给了关联组织」。可用 target_tenant_key / target_department_id / target_group_id 收窄，is_select_subject 区分主体侧与客体侧。只读，需 trust_party:collaboration_rule:read。",
    RequiredScopes = ["trust_party:collaboration_rule:read"],
    Source = nameof(IFeishuTenantV1DirectoryShareEntity) + "." + nameof(IFeishuTenantV1DirectoryShareEntity.GetShareEntityListAsync))]
public interface IFeishuTenantTrustpartyListShareEntitiesTool
{
    /// <summary>查询共享实体。</summary>
    /// <returns>白名单投影后的 JSON 文本（items（带 kind）+ departments/groups/users 三份清单 + has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> ListShareEntitiesAsync(
        [ToolParameter("target_tenant_key", "关联组织的租户标识（可选，来自 trustparty.list_collaboration_tenants）")] string? target_tenant_key = null,
        [ToolParameter("target_department_id", "收窄到该部门（可选）")] string? target_department_id = null,
        [ToolParameter("target_group_id", "收窄到该用户组（可选）")] string? target_group_id = null,
        [ToolParameter("is_select_subject", "是否查询**主体侧**（可选：true=主体侧 / false=客体侧；省略按平台默认）")] bool? is_select_subject = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：trustparty.list_all_collaboration_tenants（映射 <c>IFeishuTenantV1DirectoryCollaborationTenant.GetAllCollaborationTenantListAsync</c>）。
/// </summary>
/// <remarks>
/// 与 <c>trustparty.list_collaboration_tenants</c> 的差别（易混点）：后者是
/// <c>trust_party</c> 面的"已互通组织"，本工具是 <c>directory</c> 面的**目录侧可见关联组织**
/// （同一批关系、不同接口与字段口径）。两者都读；要写规则用 <c>directory</c> 面所需的 key。
/// </remarks>
[FeishuTool("trustparty.list_all_collaboration_tenants",
    Description = "分页列出目录侧的全部关联组织（返回 target_tenant_key/名称/短名/连接时间/品牌）——与 trustparty.list_collaboration_tenants（互通面）口径不同：本工具是 directory 面，用于核对「目录里关联了哪些组织」。只读，需 trust_party:collaboration_rule:read。",
    RequiredScopes = ["trust_party:collaboration_rule:read"],
    Source = nameof(IFeishuTenantV1DirectoryCollaborationTenant) + "." + nameof(IFeishuTenantV1DirectoryCollaborationTenant.GetAllCollaborationTenantListAsync))]
public interface IFeishuTenantTrustpartyListAllCollaborationTenantsTool
{
    /// <summary>列出目录侧关联组织。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：target_tenant_key/名称/短名/连接时间/品牌），超长截断并标记 truncated。</returns>
    Task<string> ListAllCollaborationTenantsAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：trustparty.list_collaboration_rules（映射 <c>IFeishuTenantV1DirectoryCollaborationRule.GetCollaborationRuleListAsync</c>）。
/// </summary>
/// <remarks>
/// 互通规则是"谁（主体）能与对端的谁（客体）互通"的配置项，返回**实体标识集合**
/// （open_user_ids / open_department_ids / open_group_ids）与<b>两侧是否仍然有效</b>
/// （<c>subject_is_valid</c> / <c>object_is_valid</c>——实体被删除或离职后规则会失效，这是排查
/// "规则配了却不生效"的关键信号）。
/// </remarks>
[FeishuTool("trustparty.list_collaboration_rules",
    Description = "分页列出某个关联组织的互通规则（rule_id + 主体与客体的成员/部门/群标识集合 + 两侧是否仍有效）——回答「我们和对端开了哪些互通规则」「规则为什么没生效」（看 subject_is_valid/object_is_valid）。只读，需 trust_party:collaboration_rule:read。",
    RequiredScopes = ["trust_party:collaboration_rule:read"],
    Source = nameof(IFeishuTenantV1DirectoryCollaborationRule) + "." + nameof(IFeishuTenantV1DirectoryCollaborationRule.GetCollaborationRuleListAsync))]
public interface IFeishuTenantTrustpartyListCollaborationRulesTool
{
    /// <summary>列出互通规则。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：rule_id/subjects/objects/两侧有效性），超长截断并标记 truncated。</returns>
    Task<string> ListCollaborationRulesAsync(
        [ToolParameter("target_tenant_key", "关联组织的租户标识（必填，来自 trustparty.list_all_collaboration_tenants）", Required = true)] string target_tenant_key,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}
