// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────── hire · 招聘（F-1 P0 首批切片：职位/岗位，只读） ───────────────
//
// Hire 是 SDK 里最大的域（165 个方法）。首批只策展**职位（job）**这一层，理由：
//   • 职位/岗位是**组织对外信息**，不含候选人个人信息 ⇒ 非 PII，可以放心进入模型上下文；
//   • 候选人（candidate）/ 投递 / Offer / 面试 涉及大量个人信息，属下一批的 PII 纪律范围，
//     一次性铺开会同时引入 100+ 方法与一整套 PII 出口，违反"每片可验证"的切片原则。
// 故本批 2 个工具 + 后续批次按"先组织、后个人"推进。

/// <summary>
/// 工具接口：hire.list_jobs（映射 <c>IFeishuTenantV1HireJob.GetJobListAsync</c>）。
/// </summary>
/// <remarks>
/// <b>非 PII</b>：职位是招聘岗位信息（标题/编码/城市/部门/状态），不含候选人个人信息 ⇒ 不进 PII 登记。
/// 投影<b>刻意不给 description / requirement 全文</b>——岗位描述通常很长（数百到数千字），
/// 列表里逐条带出会瞬间打满结果预算；要正文请用 <c>hire.get_job</c> 单条取。
/// </remarks>
[FeishuTool("hire.list_jobs",
    Description = "分页列出【招聘岗位】（职位 ID、标题、编码、启用状态、更新时间）——用于回答「我们有哪些在招岗位」「某岗位 ID 是什么」。列表不含岗位描述正文（较长），需要正文请用 hire.get_job 按 ID 单条获取。page_token 续页（页大小由宿主固定）。只读，需 hire:job:readonly。",
    RequiredScopes = ["hire:job:readonly"],
    Source = nameof(IFeishuTenantV1HireJob) + "." + nameof(IFeishuTenantV1HireJob.GetJobListAsync))]
public interface IFeishuTenantHireListJobsTool
{
    /// <summary>分页列出招聘岗位。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 id/title/code/active_status/update_time）。</returns>
    Task<string> ListJobsAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：hire.get_job（映射 <c>IFeishuTenantV1HireJob.GetJobAsync</c>）。
/// </summary>
/// <remarks>
/// 单条岗位的<b>完整</b>通道（含描述/要求正文）——与 list 的"精简投影"分工明确：
/// 先 list 找到 ID，再 get 取正文，避免一上来就把整本岗位说明书灌进上下文。
/// </remarks>
[FeishuTool("hire.get_job",
    Description = "按职位 ID 查询单个【招聘岗位】详情（标题、编码、岗位描述、任职要求、启用状态、创建/更新时间）——已从 hire.list_jobs 拿到职位 ID 时用它取完整正文（列表不含正文）。描述可能很长（按结果预算截断并标记 truncated）。查不到时返回 found=false。只读，需 hire:job:readonly。",
    RequiredScopes = ["hire:job:readonly"],
    Source = nameof(IFeishuTenantV1HireJob) + "." + nameof(IFeishuTenantV1HireJob.GetJobAsync))]
public interface IFeishuTenantHireGetJobTool
{
    /// <summary>按 ID 查询单个岗位详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（id/title/code/description/requirement/active_status/create_time/update_time），查不到时含 found=false 哨兵。</returns>
    Task<string> GetJobAsync(
        [ToolParameter("job_id", "职位 ID（必填，来自 hire.list_jobs 返回的 id）", Required = true)] string job_id,
        CancellationToken cancellationToken = default);
}
