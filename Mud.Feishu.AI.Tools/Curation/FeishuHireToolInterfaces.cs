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

// ─────────────── hire · 招聘需求（job_requirement，非 PII） ───────────────

/// <summary>
/// 工具接口：hire.list_job_requirements（映射 <c>IFeishuTenantV1HireJobRequirement.GetJobRequirementListAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么补它</b>："职位（job）"是**对外发布的岗位**，"招聘需求（job_requirement）"是**内部的用人计划**
/// （需求人数 / 职级区间 / 部门 / 起止时间 / 审批状态 / 关联职位）——两者是两套对象：招聘团队问"这个季度还缺几个人"、
/// "某职位挂在哪个需求下"时，只有需求能回答。缺它时模型只能拿职位数硬凑，答不准。
/// </para>
/// <para>
/// <b>非 PII（裁剪后）</b>：需求本体是组织用人信息；但接口同时返回<b>招聘负责人 / 用人经理 / 直属上级</b>
/// （<c>recruiter_list</c> / <c>jr_hiring_managers</c> / <c>direct_leader_list</c>）与 <c>creator_id</c>、
/// 以及 <c>min_salary</c> / <c>max_salary</c> 薪酬区间 ⇒ <b>三类都按"与本工具目的无关"丢弃</b>
/// （目的：定位与概览需求）。裁剪后本工具不进 PII 登记。
/// </para>
/// <para>
/// <b>DP-B1-0</b>：只暴露 <c>page_token</c>（+ 业务过滤 <c>job_id</c>），页大小走 SDK 默认。
/// </para>
/// </remarks>
[FeishuTool("hire.list_job_requirements",
    Description = "分页列出【招聘需求】（需求 ID、编码、名称、需求人数 head_count、优先级、进度、部门、职级区间、起止时间、审批状态、关联职位 ID 列表），可用 job_id 只看某职位挂的需求。用于回答「这个季度还缺几个人」「某职位对应哪条招聘需求」。⚠️ 为最小暴露：招聘负责人/用人经理/直属上级、创建人、薪资区间一律不返回（需这些信息请在招聘系统查看）。page_token 续页。只读，需 hire:job_requirement:readonly。",
    RequiredScopes = ["hire:job_requirement:readonly"],
    Source = nameof(IFeishuTenantV1HireJobRequirement) + "." + nameof(IFeishuTenantV1HireJobRequirement.GetJobRequirementListAsync))]
public interface IFeishuTenantHireListJobRequirementsTool
{
    /// <summary>分页列出招聘需求（可按职位 ID 过滤）。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 id/short_code/name/head_count/priority/display_progress/department/min_level/max_level/start_time/deadline/approval_status/job_id_list/update_time）。</returns>
    Task<string> ListJobRequirementsAsync(
        [ToolParameter("job_id", "职位 ID 过滤（可选，来自 hire.list_jobs 的 id；只返回挂在该职位下的需求）")] string? job_id = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────── hire · Offer（offer，PII：只给状态，不给条件） ───────────────

/// <summary>
/// 工具接口：hire.list_offers（映射 <c>IFeishuTenantV1HireCandidate.GetOfferListAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么补它</b>：招聘链路问得最多的最后一环是"这人 Offer 到哪一步了"（审批中 / 已发送 / 已接受 / 被拒）。
/// 缺它时模型只能答到"面试"为止。
/// </para>
/// <para>
/// <b>PII 且"只给状态不给条件"</b>：某人是否拿到 Offer 属个人信息 ⇒ <b>默认不启用</b>（piiTools 登记）。
/// 好消息是本接口返回的 <see cref="OfferListInfo"/> <b>结构上就不含薪资与 Offer 条件</b>
/// （薪资在 <c>ApplicationOfferSalaryPlan</c>，只在 Offer 详情结构里）——故本工具天然落在
/// 纪律 26/28 的"最小充分集"上：只回状态相关字段，不做任何金额投影。
/// </para>
/// <para><b>DP-B1-0</b>：只暴露 <c>page_token</c>（页大小走 SDK 默认）。</para>
/// </remarks>
[FeishuTool("hire.list_offers",
    Description = "按人才 ID 查询其【Offer 列表】（Offer ID、投递 ID、岗位 ID/名称、Offer 状态、Offer 类型、创建时间）。offer_status 含义：2=审批中 / 3=审批已撤回 / 4=审批通过 / 5=审批不通过 / 6=已发送 / 7=被接受 / 8=被拒绝 / 9=已失效 / 10=已创建。⚠️ 该工具属 PII：某人是否获得 Offer 属个人信息，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；**Offer 薪资与条件一律不返回**（本接口亦不含）。只读，需 hire:offer:readonly。",
    RequiredScopes = ["hire:offer:readonly"],
    Source = nameof(IFeishuTenantV1HireCandidate) + "." + nameof(IFeishuTenantV1HireCandidate.GetOfferListAsync))]
public interface IFeishuTenantHireListOffersTool
{
    /// <summary>按人才 ID 分页查询 Offer 列表（只回状态相关字段）。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 offer_id/application_id/job_id/job_name/offer_status/offer_type/create_time）。</returns>
    Task<string> ListOffersAsync(
        [ToolParameter("talent_id", "人才 ID（必填，来自 hire.list_talents 或面试安排里的候选人）", Required = true)] string talent_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────── hire · 面试安排（interview，PII） ───────────────

/// <summary>
/// 工具接口：hire.list_interviews（映射 <c>IFeishuTenantV1HireCandidate.GetInterviewListAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么补它</b>：招聘日常最高频的问题是"今天/本周有哪些面试""某候选人面到哪一轮了"。
/// 缺它时模型只能拿职位/候选人硬凑，答不出**安排**。
/// </para>
/// <para>
/// <b>PII（DP-A5-1 档位 ①）</b>：面试安排本身即"某人正在应聘某职位"的事实 ⇒ <b>默认不启用</b>，
/// 需宿主显式加白名单；描述必须声明后果。
/// </para>
/// <para>
/// <b>投影纪律（纪律 26"目的判据"）</b>：只保留**安排三要素**——时间（起止）、轮次（<c>round</c> +
/// <c>round_type</c>）、阶段（<c>stage</c>）；<b>面试记录 / 评价正文 / 备注 / 联系人手机 / 面试官 /
/// 会议室与地址一律不返回</b>：评价是对人的主观判断，地址与联系人手机与"知不知道面试安排"无关。
/// </para>
/// <para><b>DP-B1-0</b>：只暴露 <c>page_token</c>；<c>application_id</c> / 时间窗是业务过滤，可下发。</para>
/// </remarks>
[FeishuTool("hire.list_interviews",
    Description = "分页查询【面试安排】（面试 ID、投递 ID、轮次 round 与 round_type 名称、阶段 stage、开始/结束时间（毫秒时间戳）、安排状态）——用于回答「今天/本周有哪些面试」「某投递面到哪一轮」。可用 application_id 只查某投递，用 start_time/end_time（毫秒时间戳字符串）限定时间窗。⚠️ 该工具属 PII：面试安排即「某人正在应聘」的事实，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；面试评价/记录/备注/联系人手机/面试官/会议室一律不返回。只读，需 hire:interview:readonly。",
    RequiredScopes = ["hire:interview:readonly"],
    Source = nameof(IFeishuTenantV1HireCandidate) + "." + nameof(IFeishuTenantV1HireCandidate.GetInterviewListAsync))]
public interface IFeishuTenantHireListInterviewsTool
{
    /// <summary>分页查询面试安排（可限定投递与时间窗）。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 interview_id/application_id/round/round_type/stage/begin_time/end_time/arrangement_status）。</returns>
    Task<string> ListInterviewsAsync(
        [ToolParameter("application_id", "投递 ID 过滤（可选，缩小到某个候选人的某次投递）")] string? application_id = null,
        [ToolParameter("start_time", "时间窗起点（可选，毫秒时间戳字符串，如 \\\"1704067200000\\\"；与 end_time 成对使用）")] string? start_time = null,
        [ToolParameter("end_time", "时间窗终点（可选，毫秒时间戳字符串）")] string? end_time = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────── hire · 候选人（talent）：PII 首批切片 ───────────────
//
// 与"职位"不同，候选人**是个人信息**：姓名、手机、邮箱、学历、工作经历、身份证号……
// 故本批按 DP-A5-1 档位 ① 落地（三处登记缺一不可，由 PiiToolDisciplineContractGuards 机械锁定）：
//   ① 默认不启用——piiTools 登记（scope-authority.json），需宿主显式加入 FeishuAgent:Tools；
//   ② 描述必须声明"结果进第三方模型上下文"的后果；
//   ③ 投影**白名单化**——身份证号 / 性别 / 年龄 / 国籍 / 籍贯 / 学历 / 工作经历 / 作品 / 获奖 / 附件
//      一律不返回；只给"定位与联系"所需的最小集。

/// <summary>
/// 工具接口：hire.list_talents（映射 <c>IFeishuTenantV1HireCandidate.GetTalentListAsync</c>）。
/// </summary>
/// <remarks>
/// 列表投影<b>极小</b>（talent_id / 姓名 / 是否已入职）：翻页找人是"索引"用途，不需要把每人档案带出。
/// </remarks>
[FeishuTool("hire.list_talents",
    Description = "分页检索【候选人（人才）】列表（人才 ID、姓名、是否已入职），支持 keyword 按姓名/手机号等检索。用于「找人」拿到 talent_id 后再用 hire.get_talent 取联系人字段。⚠️ 该工具属 PII：结果是候选人个人信息，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；身份证号/性别/年龄/籍贯/学历/经历等一律不返回。只读，需 hire:talent:readonly。",
    RequiredScopes = ["hire:talent:readonly"],
    Source = nameof(IFeishuTenantV1HireCandidate) + "." + nameof(IFeishuTenantV1HireCandidate.GetTalentListAsync))]
public interface IFeishuTenantHireListTalentsTool
{
    /// <summary>分页检索候选人列表（最小投影）。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 talent_id/name/is_onboarded）。</returns>
    Task<string> ListTalentsAsync(
        [ToolParameter("keyword", "检索关键字（可选，按人才姓名/手机号等匹配；不填=按更新时间倒序翻页）")] string? keyword = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：hire.get_talent（映射 <c>IFeishuTenantV1HireCandidate.GetTalentAsync</c>）。
/// </summary>
/// <remarks>
/// 单条候选人**联系人字段**通道（姓名 / 手机 / 邮箱 / 工作年限 / 是否入职 / 是否在职期间为猎头渠道）。
/// 这是"联系候选人"这一正当用途所需的最小集；档案类字段（学历/经历/作品/获奖/附件/自定义字段）与敏感标识
/// （身份证号/性别/年龄/国籍/籍贯）**不返回**——它们与本工具目的无关，却显著扩大泄露面。
/// </remarks>
[FeishuTool("hire.get_talent",
    Description = "按人才 ID 查询单个【候选人】的联系与在职字段（姓名、手机号、邮箱、工作年限、是否已入职）——已从 hire.list_talents 拿到 talent_id 时用它取联系方式。⚠️ 该工具属 PII：结果是候选人个人信息，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；身份证号/学历/工作经历/备注等一律不返回。查不到时返回 found=false。只读，需 hire:talent:readonly。",
    RequiredScopes = ["hire:talent:readonly"],
    Source = nameof(IFeishuTenantV1HireCandidate) + "." + nameof(IFeishuTenantV1HireCandidate.GetTalentAsync))]
public interface IFeishuTenantHireGetTalentTool
{
    /// <summary>按 ID 查询单个候选人的联系与在职字段。</summary>
    /// <returns>白名单投影后的 JSON 文本（talent_id/name/mobile/email/experience_years/is_onboarded），查不到时含 found=false 哨兵。</returns>
    Task<string> GetTalentAsync(
        [ToolParameter("talent_id", "人才 ID（必填，来自 hire.list_talents 返回的 talent_id）", Required = true)] string talent_id,
        CancellationToken cancellationToken = default);
}
