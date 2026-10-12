// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── org · 组织元数据（F-1 第三个补域） ───────────────────────────

/// <summary>
/// 工具接口：org.list_job_titles（映射 <c>IFeishuTenantV3JobTitle.GetJobTitlesListAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要本域</b>：通讯录工具（<c>contact.get_user</c>/<c>list_department_members</c>）返回的用户对象里
/// <c>job_title</c> 是<b>职务 ID</b>，模型却没有任何通道把它换成可读名称——只能对用户编造或含糊回答。
/// 本域把"用户属性 ID → 组织元数据名称"补上（与 <c>mdm.get_countries</c> 解决"地区编码 → 名称"同一类缺口）。
/// </para>
/// <para>
/// <b>非 PII</b>：职务是<b>组织元数据</b>（组织内的岗位定义），不含员工个人信息 ⇒ 不进 PII 登记。
/// 但"某人是什么职务"仍属个人信息——本域只提供"职务目录"，不提供"谁是什么职务"（那要经 <c>contact.*</c> 授权面）。
/// </para>
/// <para>
/// <b>权限口径</b>：职务是"用户属性之一"（SDK 接口注释原话），故复用通讯录用户只读权限
/// <c>contact:user.base:readonly</c>，与 <c>contact.*</c> 五个只读工具同口径（同一授权面，宿主一次授权即可用齐）。
/// </para>
/// <para>
/// <b>DP-B1-0「页不进、预算进」</b>：只暴露 <c>page_token</c>，页大小固定走 SDK 默认（10），
/// 不由模型控制——否则模型会把 page_size 调到上限以"少翻几页"，反而更早触发结果截断。
/// </para>
/// </remarks>
[FeishuTool("org.list_job_titles",
    Description = "分页列出当前租户的【职务】目录（职务 ID、名称、多语言名称、启用状态）——用于把通讯录返回的 job_title 职务 ID 换成可读名称，或确认组织内有哪些职务。page_token 续页（页大小由宿主固定，模型不可调）；结果为空说明租户未配置职务。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3JobTitle) + "." + nameof(IFeishuTenantV3JobTitle.GetJobTitlesListAsync))]
public interface IFeishuTenantOrgListJobTitlesTool
{
    /// <summary>分页列出职务目录。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 job_title_id/name/i18n_name/status），超长截断并标记 truncated。</returns>
    Task<string> ListJobTitlesAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：org.get_job_title（映射 <c>IFeishuTenantV3JobTitle.GetJobTitleByIdAsync</c>）。
/// </summary>
/// <remarks>
/// 已知职务 ID 时的精确取值通道：比翻页列表更省上下文，也避免"猜 ID"。与 <c>org.list_job_titles</c> 同权限口径。
/// </remarks>
[FeishuTool("org.get_job_title",
    Description = "按职务 ID 查询单个【职务】详情（名称、多语言名称、启用状态）——已从通讯录拿到 job_title ID 时用它精确取值，不必翻页。ID 形如 od-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx（也可能是数字串）；查不到时返回 found=false。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3JobTitle) + "." + nameof(IFeishuTenantV3JobTitle.GetJobTitleByIdAsync))]
public interface IFeishuTenantOrgGetJobTitleTool
{
    /// <summary>按 ID 查询单个职务。</summary>
    /// <returns>白名单投影后的 JSON 文本（job_title_id/name/i18n_name/status），查不到时含 found=false 哨兵。</returns>
    Task<string> GetJobTitleAsync(
        [ToolParameter("job_title_id", "职务 ID（必填，来自通讯录用户对象的 job_title 字段）", Required = true)] string job_title_id,
        CancellationToken cancellationToken = default);
}

// ─────────────── org 同族：职级 / 职务族 / 工作城市（同为组织元数据 · 只读 · 非 PII） ───────────────
//
// 三者与"职务"是同一类断裂：通讯录用户对象里 `job_level_id` / `city` 同样是 ID，模型需要通道换成名称。
// 权限口径与 contact.* 一致（复用 `contact:user.base:readonly`），宿主一次授权即可用齐整个 org 域。

/// <summary>
/// 工具接口：org.list_job_levels（映射 <c>IFeishuTenantV3JobLevel.GetJobLevelListAsync</c>）。
/// </summary>
/// <remarks>
/// 支持 SDK 原生的 <c>name</c> **模糊过滤**（业务参数，可下发）；<c>page_size</c> 仍不下发（DP-B1-0）。
/// </remarks>
[FeishuTool("org.list_job_levels",
    Description = "分页列出/按名称模糊匹配【职级】目录（职级 ID、名称、说明、排序序号 order、启用状态）——用于把通讯录返回的 job_level_id 换成可读名称，或确认组织职级序列。name 为可选模糊过滤；page_token 续页（页大小由宿主固定）。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3JobLevel) + "." + nameof(IFeishuTenantV3JobLevel.GetJobLevelListAsync))]
public interface IFeishuTenantOrgListJobLevelsTool
{
    /// <summary>分页列出职级目录（可按名称过滤）。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 job_level_id/name/description/order/status/i18n_name）。</returns>
    Task<string> ListJobLevelsAsync(
        [ToolParameter("name", "职级名称关键字（可选，模糊匹配）")] string? name = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：org.get_job_level（映射 <c>IFeishuTenantV3JobLevel.GetJobLevelByIdAsync</c>）。</summary>
[FeishuTool("org.get_job_level",
    Description = "按职级 ID 查询单个【职级】详情（名称、说明、排序序号、启用状态）——已从通讯录拿到 job_level_id 时用它精确取值。查不到时返回 found=false。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3JobLevel) + "." + nameof(IFeishuTenantV3JobLevel.GetJobLevelByIdAsync))]
public interface IFeishuTenantOrgGetJobLevelTool
{
    /// <summary>按 ID 查询单个职级。</summary>
    /// <returns>白名单投影后的 JSON 文本（job_level_id/name/description/order/status/i18n_name），查不到时含 found=false 哨兵。</returns>
    Task<string> GetJobLevelAsync(
        [ToolParameter("job_level_id", "职级 ID（必填，来自通讯录用户对象的 job_level_id 字段）", Required = true)] string job_level_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：org.list_job_families（映射 <c>IFeishuTenantV3JobFamilies.GetJobFamilesListAsync</c>）。
/// </summary>
/// <remarks>
/// <b>职务族有层级</b>（<c>parent_job_family_id</c>）：返回里带父族 ID，模型可据此还原树，不要臆造层级。
/// </remarks>
[FeishuTool("org.list_job_families",
    Description = "分页列出/按名称模糊匹配【职务族】目录（职务族 ID、名称、说明、父职务族 ID、启用状态）——用于把通讯录返回的 job_family_id 换成可读名称。职务族有层级：parent_job_family_id 为空者即顶层。name 为可选模糊过滤；page_token 续页。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3JobFamilies) + "." + nameof(IFeishuTenantV3JobFamilies.GetJobFamilesListAsync))]
public interface IFeishuTenantOrgListJobFamiliesTool
{
    /// <summary>分页列出职务族目录（可按名称过滤）。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 job_family_id/name/description/parent_job_family_id/status/i18n_name）。</returns>
    Task<string> ListJobFamiliesAsync(
        [ToolParameter("name", "职务族名称关键字（可选，模糊匹配）")] string? name = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：org.get_job_family（映射 <c>IFeishuTenantV3JobFamilies.GetJobFamilyByIdAsync</c>）。</summary>
[FeishuTool("org.get_job_family",
    Description = "按职务族 ID 查询单个【职务族】详情（名称、说明、父职务族 ID、启用状态）——已从通讯录拿到 job_family_id 时用它精确取值。查不到时返回 found=false。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3JobFamilies) + "." + nameof(IFeishuTenantV3JobFamilies.GetJobFamilyByIdAsync))]
public interface IFeishuTenantOrgGetJobFamilyTool
{
    /// <summary>按 ID 查询单个职务族。</summary>
    /// <returns>白名单投影后的 JSON 文本（job_family_id/name/description/parent_job_family_id/status/i18n_name），查不到时含 found=false 哨兵。</returns>
    Task<string> GetJobFamilyAsync(
        [ToolParameter("job_family_id", "职务族 ID（必填，来自通讯录用户对象的 job_family_id 字段）", Required = true)] string job_family_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：org.list_work_cities（映射 <c>IFeishuTenantV3WorkCity.GetWorkCitesListAsync</c>）。
/// </summary>
/// <remarks>
/// 工作城市与 <c>mdm.get_countries</c> <b>不是一回事</b>：后者是国家/地区参考表（含区号、国码），
/// 前者是"租户在通讯录里为该员工登记的工作城市"（含中国城市层级）。模型问"XX 在哪个城市办公"用本工具，
/// 问"某国家代码代表哪个国家"用 mdm。
/// </remarks>
[FeishuTool("org.list_work_cities",
    Description = "分页列出【工作城市】目录（城市 ID、名称、多语言名称、启用状态）——用于把通讯录返回的 city 城市 ID 换成可读城市名。注意与 mdm.get_countries（国家/地区参考表）不同：本工具是员工工作城市。page_token 续页（页大小由宿主固定）。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3WorkCity) + "." + nameof(IFeishuTenantV3WorkCity.GetWorkCitesListAsync))]
public interface IFeishuTenantOrgListWorkCitiesTool
{
    /// <summary>分页列出工作城市目录。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 work_city_id/name/status/i18n_name）。</returns>
    Task<string> ListWorkCitiesAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：org.get_work_city（映射 <c>IFeishuTenantV3WorkCity.GetWorkCityByIdAsync</c>）。</summary>
[FeishuTool("org.get_work_city",
    Description = "按城市 ID 查询单个【工作城市】详情（名称、多语言名称、启用状态）——已从通讯录拿到 city 城市 ID 时用它精确取值。查不到时返回 found=false。只读，需 contact:user.base:readonly。",
    RequiredScopes = ["contact:user.base:readonly"],
    Source = nameof(IFeishuTenantV3WorkCity) + "." + nameof(IFeishuTenantV3WorkCity.GetWorkCityByIdAsync))]
public interface IFeishuTenantOrgGetWorkCityTool
{
    /// <summary>按 ID 查询单个工作城市。</summary>
    /// <returns>白名单投影后的 JSON 文本（work_city_id/name/status/i18n_name），查不到时含 found=false 哨兵。</returns>
    Task<string> GetWorkCityAsync(
        [ToolParameter("work_city_id", "工作城市 ID（必填，来自通讯录用户对象的 city 字段）", Required = true)] string work_city_id,
        CancellationToken cancellationToken = default);
}
