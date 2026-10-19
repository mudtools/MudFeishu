// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Application · 飞书应用管理 ───────────────────────────

/// <summary>
/// 工具接口：application.list_apps（映射 <c>IFeishuTenantV6ApplicationAdmin.GetApplicationsPageListAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域的第一跳</b>：拿到 <c>app_id</c> 才能查应用详情 / 可用范围 / 协作者 / 版本。
/// 可按状态与付费类型过滤，避免把全租户应用一次拉回。
/// </para>
/// <para>
/// <b>出站净化（本域最硬的一条）</b>：<c>ApplicationInfo</c> 自带 <c>secret</c>（应用凭据）、
/// <c>encryption</c>（加密密钥 + 校验令牌）、<c>security</c>（回调白名单 / 可信域）、
/// <c>event</c>/<c>callback</c>（回调地址）与 <c>owner</c>（负责人姓名 / 邮箱 / 手机号）。
/// 这些<b>一律不出站</b>——需要"负责人是谁"请拿 <c>owner_id</c> 走通讯录域解析
/// （那里有正确的 PII 分级与授权口径）。
/// </para>
/// <para>
/// ⚠️ <b>scope 待控制台核对</b>：本域 SDK 注释只写"获取应用信息"等自然语言、<b>未给 scope 字面量</b>，
/// 故登记为 <c>application:application:readonly</c> 并同时在权限对照表标 ⚠️——
/// 宿主开通前请按名称在开发者后台搜索确认（与仓库既有 ⚠️ 口径一致）。
/// </para>
/// </remarks>
[AgentTool("application.list_apps",
    Description = "分页列出企业自建/商店应用（返回 app_id/名称/状态/场景类型/在线版本 ID）——进入「应用管理」域的第一步：拿到 app_id 才能查详情、可用范围、协作者与版本。可按 status / payment_type / owner_type 过滤。只读，需 application:application:readonly。",
    RequiredScopes = ["application:application:readonly"],
    Source = nameof(IFeishuTenantV6ApplicationAdmin) + "." + nameof(IFeishuTenantV6ApplicationAdmin.GetApplicationsPageListAsync))]
public interface IFeishuTenantApplicationListAppsTool
{
    /// <summary>列出应用。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token/total：app_id/app_name/status/scene_type/…），超长截断并标记 truncated。</returns>
    Task<string> ListAppsAsync(
        [ToolParameter("status", "应用状态过滤（可选：0=停用 / 1=启用 / 2=未启用 / 3=未启用（历史））")] int? status = null,
        [ToolParameter("payment_type", "付费类型过滤（可选：0=免费 / 1=付费）")] int? payment_type = null,
        [ToolParameter("owner_type", "应用类型过滤（可选：0=企业自建 / 1=应用商店）")] int? owner_type = null,
        [ToolParameter("lang", "返回文案语言（可选，默认 zh_cn；可选 en_us / ja_jp）")] string? lang = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：application.get_app（映射 <c>IFeishuTenantV6ApplicationAppInfo.GetApplicationAsync</c>）。
/// </summary>
/// <remarks>
/// <b>为什么投影里没有 <c>secret</c> / <c>encryption</c> / <c>security</c></b>：
/// 它们是应用的凭据与安全配置，进模型上下文即等于把密钥写进对话历史与第三方模型。
/// 本工具只回答"这个应用叫什么、什么状态、开了哪些能力"。
/// </remarks>
[AgentTool("application.get_app",
    Description = "按 app_id 查询应用详情（名称/描述/状态/场景类型/主语言/在线与待审版本 ID/移动端与 PC 端默认能力/已申请权限的 scope 名）。⚠️ 不返回应用密钥、加密配置与负责人联系方式（出站净化）。只读，需 application:application:readonly。",
    RequiredScopes = ["application:application:readonly"],
    Source = nameof(IFeishuTenantV6ApplicationAppInfo) + "." + nameof(IFeishuTenantV6ApplicationAppInfo.GetApplicationAsync))]
public interface IFeishuTenantApplicationGetAppTool
{
    /// <summary>查询应用详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + app：app_id/app_name/status/scopes 等），超长截断并标记 truncated。</returns>
    Task<string> GetAppAsync(
        [ToolParameter("app_id", "应用 ID（必填，形如 cli_xxx；来自 application.list_apps）", Required = true)] string app_id,
        [ToolParameter("lang", "返回文案语言（可选，默认 zh_cn）")] string? lang = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：application.get_app_visibility（映射 <c>IFeishuTenantV6ApplicationAdmin.GetAppVisibilityAsync</c>）。
/// </summary>
/// <remarks>
/// 返回"谁看得见这个应用"（全员可见 / 可见与不可见的部门、用户组、用户）。
/// <b>只给标识符</b>（department_id / group_id / user_id·open_id），不给姓名——
/// 名字与联系方式由通讯录域按各自的 PII 口径解析。
/// </remarks>
[AgentTool("application.get_app_visibility",
    Description = "查询应用可用范围（是否全员可见 + 可见/不可见的部门、用户组、用户列表）。回答「这个应用谁能看到」用它；成员过多时按 has_more_users/user_page_token 继续取用户列表。只读，需 application:application:readonly。",
    RequiredScopes = ["application:application:readonly"],
    Source = nameof(IFeishuTenantV6ApplicationAdmin) + "." + nameof(IFeishuTenantV6ApplicationAdmin.GetAppVisibilityAsync))]
public interface IFeishuTenantApplicationGetAppVisibilityTool
{
    /// <summary>查询应用可用范围。</summary>
    /// <returns>白名单投影后的 JSON 文本（is_visible_to_all/可见与不可见的部门·用户组·用户/分页游标），超长截断并标记 truncated。</returns>
    Task<string> GetAppVisibilityAsync(
        [ToolParameter("app_id", "应用 ID（必填，形如 cli_xxx，来自 application.list_apps）", Required = true)] string app_id,
        [ToolParameter("user_page_token", "用户列表续页游标（可选，来自上一次结果的 user_page_token）")] string? user_page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：application.list_collaborators（映射 <c>IFeishuTenantV6Application.GetApplicationCollaboratorsAsync</c>）。
/// </summary>
/// <remarks>
/// 协作者 = 能进开发者后台改这个应用的人（管理员 / 开发 / 运营）。
/// 返回 <c>type</c> + <c>user_id</c>：这是**权限治理的关键事实**（"谁能动这个应用"），
/// 但同样只给标识符不给姓名。
/// </remarks>
[AgentTool("application.list_collaborators",
    Description = "列出应用协作者（谁能进开发者后台改这个应用；返回 type=管理员/开发/运营 + user_id）——用于回答「这个应用谁能改」。只读，需 application:application:readonly。",
    RequiredScopes = ["application:application:readonly"],
    Source = nameof(IFeishuTenantV6Application) + "." + nameof(IFeishuTenantV6Application.GetApplicationCollaboratorsAsync))]
public interface IFeishuTenantApplicationListCollaboratorsTool
{
    /// <summary>列出应用协作者。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：type/user_id），超长截断并标记 truncated。</returns>
    Task<string> ListCollaboratorsAsync(
        [ToolParameter("app_id", "应用 ID（必填，形如 cli_xxx，来自 application.list_apps）", Required = true)] string app_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：application.get_scopes（映射 <c>IFeishuTenantV6ApplicationScope.GetScopesAsync</c>）。
/// </summary>
/// <remarks>
/// 返回**本应用**已申请/已授权的权限点及状态（<c>scope_name</c> / <c>grant_status</c> / <c>scope_type</c>）。
/// 注意与 <c>feishu.capability_lookup</c> 的区别：后者回答"工具有哪些能力"，
/// 本工具回答"这个应用在飞书侧开了哪些权限"。
/// </remarks>
[AgentTool("application.get_scopes",
    Description = "查询本应用已申请的权限点及授权状态（scope_name/grant_status/scope_type）——回答「这个应用现在开了哪些权限」「某权限是否已生效」。只读，需 application:application:readonly。",
    RequiredScopes = ["application:application:readonly"],
    Source = nameof(IFeishuTenantV6ApplicationScope) + "." + nameof(IFeishuTenantV6ApplicationScope.GetScopesAsync))]
public interface IFeishuTenantApplicationGetScopesTool
{
    /// <summary>查询应用权限点。</summary>
    /// <returns>白名单投影后的 JSON 文本（items：scope_name/grant_status/scope_type），超长截断并标记 truncated。</returns>
    Task<string> GetScopesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：application.list_app_versions（映射 <c>IFeishuTenantV6ApplicationAppVersion.GetAppVersionsPageListAsync</c>）。
/// </summary>
/// <remarks>
/// 版本列表是"发布历史"的主入口：配合 <c>online_version_id</c>/<c>unaudit_version_id</c>
/// 判断"线上跑的是哪一版、有没有待审版本"。
/// </remarks>
[AgentTool("application.list_app_versions",
    Description = "分页列出应用的版本历史（version_id/version/status/创建与发布时间）——用于回答「线上是哪一版」「有没有待审版本」。只读，需 application:application.app_version:readonly。",
    RequiredScopes = ["application:application.app_version:readonly"],
    Source = nameof(IFeishuTenantV6ApplicationAppVersion) + "." + nameof(IFeishuTenantV6ApplicationAppVersion.GetAppVersionsPageListAsync))]
public interface IFeishuTenantApplicationListAppVersionsTool
{
    /// <summary>列出应用版本。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token：version_id/version/status/时间），超长截断并标记 truncated。</returns>
    Task<string> ListAppVersionsAsync(
        [ToolParameter("app_id", "应用 ID（必填，形如 cli_xxx，来自 application.list_apps）", Required = true)] string app_id,
        [ToolParameter("order", "排序方式（可选：0=按创建时间倒序 / 1=升序）")] int? order = null,
        [ToolParameter("lang", "返回文案语言（可选，默认 zh_cn）")] string? lang = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：application.get_app_version（映射 <c>IFeishuTenantV6ApplicationAppVersion.GetAppVersionAsync</c>）。
/// </summary>
/// <remarks>
/// 版本详情含该版本的<b>能力开关</b>与<b>可用范围</b>（<c>remark.visibility</c>）——
/// 回答"这一版上了哪些能力、对谁可见"。
/// </remarks>
[AgentTool("application.get_app_version",
    Description = "按 version_id 查询单个应用版本的详情（版本号/状态/创建与发布时间/备注/该版本申请的能力与可用范围）。只读，需 application:application.app_version:readonly。",
    RequiredScopes = ["application:application.app_version:readonly"],
    Source = nameof(IFeishuTenantV6ApplicationAppVersion) + "." + nameof(IFeishuTenantV6ApplicationAppVersion.GetAppVersionAsync))]
public interface IFeishuTenantApplicationGetAppVersionTool
{
    /// <summary>查询应用版本详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（found + version：version_id/version/status/时间/备注/能力/范围），超长截断并标记 truncated。</returns>
    Task<string> GetAppVersionAsync(
        [ToolParameter("app_id", "应用 ID（必填，形如 cli_xxx）", Required = true)] string app_id,
        [ToolParameter("version_id", "版本 ID（必填，来自 application.list_app_versions）", Required = true)] string version_id,
        [ToolParameter("lang", "返回文案语言（可选，默认 zh_cn）")] string? lang = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────── 写面（可见性 / 应用管理开关） ───────────────────
//
// 与 ChatGroup 写面同款：SDK XML 注释里**没有任何 scope 字面量**（全目录搜 `xxx:yyy:zzz` 零命中，
// 只有「需要申请『获取用户 user ID』权限」这类自然语言）⇒ 按官方 API 文档取应用管理写权限并标 ⚠️ 待核对。
//
// ⚠️ **刻意不做**的 17 个写方法（理由写进 Guidance，不做静默缺口）：
//    · update_owner / update_collaborators：改群主/协作者属高危管理动作，但 SDK 注释连自然语言权限都没给；
//    · update_contacts_range / update_app_info / update_app_version_status / set_app_badge /
//      apply_scope / update_feedback_status：配置面，价值低于可见性与管理开关；
//    · v7 的 update_base / update_ability / update_config / submit_publish：`update_config`
//      **直写加密密钥与回调白名单**（安全面最高危），且请求体 7 个一级对象深嵌套，扁平参数无法表达；
//    · upload_app_avatar：`[FormContent]` + `[FilePath]` 本地路径，模型侧不可达（同 vc.apply_reserve 纪律）。

/// <summary>
/// 工具接口：application.update_visibility（映射 <c>IFeishuTenantV6ApplicationAdmin.UpdateApplicationVisibilityAsync</c>）。
/// </summary>
/// <remarks>
/// 四个名单（可见 / 不可见 × 增 / 删）都是**增量**语义：只改传了的那些，
/// 与 <c>lingo.update_entity</c> 的覆盖式不同——回执里会点明这一点。
/// </remarks>
[AgentTool("application.update_visibility",
    Description = "更新应用的可见范围（把应用开放/关闭给指定的用户、部门、用户组，或设为全员可见）——四类名单均为**增量**语义，未传的不动。写操作：默认不启用，需宿主授权（IToolExecutionAuthorizer），需 application:application（⚠️ scope 名待控制台核对）。建议先 dry_run 预演。",
    RequiredScopes = ["application:application"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV6ApplicationAdmin) + "." + nameof(IFeishuTenantV6ApplicationAdmin.UpdateApplicationVisibilityAsync))]
public interface IFeishuTenantApplicationUpdateVisibilityTool
{
    /// <summary>更新应用可见范围。</summary>
    /// <returns>白名单投影后的 JSON 回执（app_id + updated + 本次改动条数），超长截断并标记 truncated。</returns>
    Task<string> UpdateVisibilityAsync(
        [ToolParameter("app_id", "应用 ID（必填，形如 cli_xxx）", Required = true)] string app_id,
        [ToolParameter("is_visible_to_all", "是否全员可见（可选；true=全员可见 / false=按名单控制）")] bool? is_visible_to_all = null,
        [ToolParameter("add_visible_user_ids", "加入**可见**名单的用户 ID 列表（可选）")] string[]? add_visible_user_ids = null,
        [ToolParameter("add_visible_department_ids", "加入**可见**名单的部门 ID 列表（可选）")] string[]? add_visible_department_ids = null,
        [ToolParameter("add_visible_group_ids", "加入**可见**名单的用户组 ID 列表（可选）")] string[]? add_visible_group_ids = null,
        [ToolParameter("del_visible_user_ids", "从**可见**名单移除的用户 ID 列表（可选）")] string[]? del_visible_user_ids = null,
        [ToolParameter("del_visible_department_ids", "从**可见**名单移除的部门 ID 列表（可选）")] string[]? del_visible_department_ids = null,
        [ToolParameter("del_visible_group_ids", "从**可见**名单移除的用户组 ID 列表（可选）")] string[]? del_visible_group_ids = null,
        [ToolParameter("add_invisible_user_ids", "加入**不可见**名单的用户 ID 列表（可选）")] string[]? add_invisible_user_ids = null,
        [ToolParameter("add_invisible_department_ids", "加入**不可见**名单的部门 ID 列表（可选）")] string[]? add_invisible_department_ids = null,
        [ToolParameter("add_invisible_group_ids", "加入**不可见**名单的用户组 ID 列表（可选）")] string[]? add_invisible_group_ids = null,
        [ToolParameter("del_invisible_user_ids", "从**不可见**名单移除的用户 ID 列表（可选）")] string[]? del_invisible_user_ids = null,
        [ToolParameter("del_invisible_department_ids", "从**不可见**名单移除的部门 ID 列表（可选）")] string[]? del_invisible_department_ids = null,
        [ToolParameter("del_invisible_group_ids", "从**不可见**名单移除的用户组 ID 列表（可选）")] string[]? del_invisible_group_ids = null,
        [ToolParameter("user_id_type", "用户 ID 类型（可选：open_id / user_id / union_id）")] string? user_id_type = null,
        [ToolParameter("department_id_type", "部门 ID 类型（可选：open_department_id / department_id）")] string? department_id_type = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 app_id 与本次改动条数，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：application.update_management（映射 <c>IFeishuTenantV6Application.UpdateApplicationManagementAsync</c>）。
/// </summary>
/// <remarks>
/// <b>停用应用管理面是影响面极大的动作</b>（管理员将无法在后台管理该应用），
/// 故虽是普通写（方法名不命中危险词），描述里仍自行标注后果。
/// </remarks>
[AgentTool("application.update_management",
    Description = "启用或停用应用的管理面（⚠️ 停用后管理员将无法在后台管理该应用，影响面极大——务必先 dry_run 预演并确认 app_id 与 enable 取值）。写操作：默认不启用，需宿主授权，需 application:application（⚠️ scope 名待控制台核对）。",
    RequiredScopes = ["application:application"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV6Application) + "." + nameof(IFeishuTenantV6Application.UpdateApplicationManagementAsync))]
public interface IFeishuTenantApplicationUpdateManagementTool
{
    /// <summary>启用 / 停用应用管理面。</summary>
    /// <returns>白名单投影后的 JSON 回执（app_id + enable + updated），超长截断并标记 truncated。</returns>
    Task<string> UpdateManagementAsync(
        [ToolParameter("app_id", "应用 ID（必填，形如 cli_xxx）", Required = true)] string app_id,
        [ToolParameter("enable", "是否启用管理面（必填：true=启用 / false=停用）")] bool? enable = null,
        [ToolParameter("dry_run", "仅预演不下发（可选，默认 false）：返回 app_id 与将要设置的开关值，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
