// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────── contact · 部门面补齐（F-1 P0：Organization 部门面） ───────────────
//
// 既有 contact.* 只能"自上而下"遍历（list_departments 取子部门）+ 取成员。
// 缺的是两条最常被问到、却无从下手的方向：
//   ① 自下而上——"这个部门归哪个中心/事业部管？"（上级链）
//   ② 按名字找部门——用户说"研发中心的部门 ID 是多少"（搜索）
// 二者都是只读、非 PII（部门是组织架构，不含员工个人信息），复用既有
// contact:department.base:readonly**（不新增授权面）。

/// <summary>
/// 工具接口：contact.get_department_parents（映射 <c>IFeishuTenantV3Departments.GetParentDepartmentsByIdAsync</c>）。
/// </summary>
/// <remarks>
/// <b>自下而上</b>的部门链：给定部门 ID 返回其<b>所有上级</b>（由近及远）。
/// 缺它时模型只能从根部门逐层下钻反推——既慢又容易猜错归属。
/// </remarks>
[FeishuTool("contact.get_department_parents",
    Description = "查询指定部门的【全部上级部门】（由近及远，含部门 ID/名称/父部门 ID/成员数）——用于回答「这个部门归哪个中心/事业部管」「它的上级链是什么」。结果按层级由近到远排列，第一个是直属上级。查不到上级说明该部门已是顶层。只读，需 contact:department.base:readonly。",
    RequiredScopes = ["contact:department.base:readonly"],
    Source = nameof(IFeishuTenantV3Departments) + "." + nameof(IFeishuTenantV3Departments.GetParentDepartmentsByIdAsync))]
public interface IFeishuTenantContactGetDepartmentParentsTool
{
    /// <summary>查询部门的上级链。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 department_id/open_department_id/name/parent_department_id/member_count）。</returns>
    Task<string> GetDepartmentParentsAsync(
        [ToolParameter("department_id", "部门 ID（必填，来自 contact.list_departments 或其它工具返回的 department_id）", Required = true)] string department_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：contact.search_departments（映射 <c>IFeishuTenantV3Departments.SearchDepartmentsAsync</c>）。
/// </summary>
/// <remarks>
/// <b>按名称找部门</b>（SDK 侧为 POST 搜索）：用户说"研发中心"时，必须先拿到部门 ID 才能取成员或上级链。
/// 与 <c>contact.list_departments</c>（按父部门下钻）互补，不是替代。
/// </remarks>
[FeishuTool("contact.search_departments",
    Description = "按部门名称关键字【搜索部门】（返回部门 ID/名称/父部门 ID/成员数）——用户只说了部门名（如「研发中心」）而要继续取成员或上级链时，先用它拿到 department_id。同名部门可能不止一个，需结合 parent_department_id / member_count 判断；搜不到可换更短的关键字。只读，需 contact:department.base:readonly。",
    RequiredScopes = ["contact:department.base:readonly"],
    Source = nameof(IFeishuTenantV3Departments) + "." + nameof(IFeishuTenantV3Departments.SearchDepartmentsAsync))]
public interface IFeishuTenantContactSearchDepartmentsTool
{
    /// <summary>按名称搜索部门。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 department_id/open_department_id/name/parent_department_id/member_count）。</returns>
    Task<string> SearchDepartmentsAsync(
        [ToolParameter("query", "部门名称关键字（必填，如「研发」；过长的全名可能搜不到，可先试短关键字）", Required = true)] string query,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
