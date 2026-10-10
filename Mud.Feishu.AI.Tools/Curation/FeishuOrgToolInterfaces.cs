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
