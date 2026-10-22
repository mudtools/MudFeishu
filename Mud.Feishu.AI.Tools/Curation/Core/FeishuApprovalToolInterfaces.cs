// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

/// <summary>
/// 工具接口：approval.reject_task（映射 <c>IFeishuTenantV4ApprovalTask.RejectApprovalAsync</c>）。
/// </summary>
/// <remarks>
/// <b>审批是双向决策</b>：只有"同意"（<c>approval.approve_task</c> 内部只调
/// <c>AgreeApprovalAsync</c>）会让 Agent 在应拒场景下强行通过，故动作面必须同时提供"拒绝"。
/// SDK 的 <c>RejectApprovalAsync</c> 已存在，零 SDK 改动。
/// </remarks>
[AgentTool("approval.reject_task",
    Description = "拒绝一个审批任务（拒绝后审批流程结束）。与 approval.approve_task 构成完整的双向决策面；task_id 来自 approval.list_pending_tasks。写操作：需 approval:approval。",
    RequiredScopes = ["approval:approval"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4ApprovalTask) + "." + nameof(IFeishuTenantV4ApprovalTask.RejectApprovalAsync))]
public interface IFeishuTenantApprovalRejectTaskTool
{
    /// <summary>拒绝审批任务。</summary>
    /// <returns>结构化文本（rejected=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> RejectTaskAsync(
        [ToolParameter("approval_code", "审批定义 Code", Required = true)] string approval_code,
        [ToolParameter("instance_code", "审批实例 Code", Required = true)] string instance_code,
        [ToolParameter("task_id", "任务 ID（来自 approval.list_pending_tasks）", Required = true)] string task_id,
        [ToolParameter("user_id", "操作人 user_id", Required = true)] string user_id,
        [ToolParameter("comment", "拒绝理由（可选，会展示给审批发起人）")] string? comment = null,
        [ToolParameter("dry_run", "仅预演不提交（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：approval.transfer_task（映射 <c>IFeishuTenantV4ApprovalTask.TransferApprovalAsync</c>）。
/// </summary>
/// <remarks>
/// <b>转交</b>是审批场景的另一个高频动作（"这条我不该批，转给张三"），
/// SDK 的 <c>TransferApprovalAsync</c> 已存在，零 SDK 改动。
/// </remarks>
[AgentTool("approval.transfer_task",
    Description = "把一个审批任务转交给他人（转交后流程流转给被转交人）。适用于'这条不该我批/我无法判断'的场景。高危写（high-risk-write）：需 approval:approval。建议先 dry_run 预演确认转交对象。",
    RequiredScopes = ["approval:approval"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4ApprovalTask) + "." + nameof(IFeishuTenantV4ApprovalTask.TransferApprovalAsync))]
public interface IFeishuTenantApprovalTransferTaskTool
{
    /// <summary>转交审批任务。</summary>
    /// <returns>结构化文本（transferred=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> TransferTaskAsync(
        [ToolParameter("approval_code", "审批定义 Code", Required = true)] string approval_code,
        [ToolParameter("instance_code", "审批实例 Code", Required = true)] string instance_code,
        [ToolParameter("task_id", "任务 ID（来自 approval.list_pending_tasks）", Required = true)] string task_id,
        [ToolParameter("user_id", "操作人 user_id（当前审批人）", Required = true)] string user_id,
        [ToolParameter("transfer_user_id", "被转交人的 user_id", Required = true)] string transfer_user_id,
        [ToolParameter("comment", "转交说明（可选）")] string? comment = null,
        [ToolParameter("dry_run", "仅预演不提交（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Approval 写（1 个） ───────────────────────────

/// <summary>工具接口：approval.create_instance（映射 <c>IFeishuTenantV4Approval.CreateInstanceAsync</c>）。</summary>
[AgentTool("approval.create_instance",
    Description = "按审批定义 Code 发起一个审批实例，form 为审批表单 Value（JSON 数组字符串，按定义的表单控件结构填写；非 JSON 数组会在下发前被拒绝）。写操作：需 approval:approval。",
    RequiredScopes = ["approval:approval"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4Approval) + "." + nameof(IFeishuTenantV4Approval.CreateInstanceAsync))]
public interface IFeishuTenantApprovalCreateInstanceTool
{
    /// <summary>发起审批实例。</summary>
    /// <returns>白名单投影后的 JSON 文本（instance_code）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateInstanceAsync(
        [ToolParameter("approval_code", "审批定义 Code", Required = true)] string approval_code,
        [ToolParameter("form", "审批表单 Value（须为 JSON 数组字符串，按审批定义的表单控件结构）", Required = true)] string form,
        [ToolParameter("user_id", "发起人用户 ID（可选；user_id 类型，数据权限校验用）")] string? user_id = null,
        [ToolParameter("idempotency_key", "幂等键（可选，形如 UUID）：相同键重复创建将返回错误码 60012（含义是「该幂等键已用过」，不是审批功能故障）；省略时不保证幂等。建议由调用方给出稳定值。幂等键不跨工具共享（不同工具的同名键互不影响）。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不发起（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Approval 只读（1 个） ───────────────────────────

/// <summary>工具接口：approval.list_pending_tasks（映射 <c>IFeishuTenantV4ApprovalQuery.GetTasksPageListAsync</c>）。</summary>
[AgentTool("approval.list_pending_tasks",
    Description = "查询审批待办任务列表（按用户 ID 过滤 PENDING 状态任务）。task_id/instance_code 可传给 approval.approve_task 完成同意操作。只读，需 approval:approval:readonly。",
    RequiredScopes = ["approval:approval:readonly"],
    Source = nameof(IFeishuTenantV4ApprovalQuery) + "." + nameof(IFeishuTenantV4ApprovalQuery.GetTasksPageListAsync))]
public interface IFeishuTenantApprovalListPendingTasksTool
{
    /// <summary>查询审批待办任务列表。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetTasksPageListAsync(
        [ToolParameter("user_id", "审批人用户 ID（open_id/user_id/union_id，与宿主配置的 user_id_type 一致）", Required = true)] string user_id,
        [ToolParameter("approval_code", "审批定义 Code（可选，限定特定审批流）")] string? approval_code = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Approval 写补充（1 个） ───────────────────────────

/// <summary>工具接口：approval.approve_task（映射 <c>IFeishuTenantV4ApprovalTask.AgreeApprovalAsync</c>）。</summary>
[AgentTool("approval.approve_task",
    Description = "同意指定审批任务（需 approval_code/instance_code/task_id/user_id 四要素，task_id/instance_code 来自 approval.list_pending_tasks）。同意后审批流程流转到下一个审批人。写操作：需 approval:approval。",
    RequiredScopes = ["approval:approval"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4ApprovalTask) + "." + nameof(IFeishuTenantV4ApprovalTask.AgreeApprovalAsync))]
public interface IFeishuTenantApprovalApproveTaskTool
{
    /// <summary>同意审批任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（approved=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> ApproveTaskAsync(
        [ToolParameter("approval_code", "审批定义 Code", Required = true)] string approval_code,
        [ToolParameter("instance_code", "审批实例 Code", Required = true)] string instance_code,
        [ToolParameter("task_id", "审批任务 ID（来自 approval.list_pending_tasks 结果的 task_id）", Required = true)] string task_id,
        [ToolParameter("user_id", "审批人用户 ID（与查询时使用的 user_id_type 一致）", Required = true)] string user_id,
        [ToolParameter("comment", "审批意见（可选，如「同意」）")] string? comment = null,
        [ToolParameter("dry_run", "仅预演不审批（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Approval 实例闭环（撤回 + 抄送） ───────────────────────────
//
// 策展口径：本域此前有「发起 + 待办 + 同意/拒绝/转交 + 实例详情」，缺**实例级的两个收尾动作**：
// 撤回（提交人反悔）与抄送（让相关人可见）。
// - `approval.cancel_instance` 走 **Tenant** 接口（`IFeishuV4Approval.CancelInstanceAsync`）：
//   平台要求同时给 approval_code + instance_code + 提交人 user_id，且审批定义须勾选
//   「允许撤销审批中的申请 / 允许撤销 x 天内通过的审批」，否则返回业务错误——这是配置前提，
//   不是本工具的缺陷，故写进描述而不是吞掉。
// - `approval.cc_instance` 走 **User** 接口（`IFeishuV4ApprovalInstance.AddCcInstanceAsync`，
//   path 为 instances/add_cc）：参数面只有 instance_code + cc_user_ids，比 Tenant 侧的
//   instances/cc（还要 approval_code + 操作人 user_id）收敛，且语义正是"我以我的身份抄送给谁"。
//   身份轴真实存在：宿主须在 `FeishuAgent:AllowedIdentities` 放行 user，否则装配期 fail-fast。

/// <summary>工具接口：approval.cancel_instance（映射 <c>IFeishuTenantV4Approval.CancelInstanceAsync</c>）。</summary>
[AgentTool("approval.cancel_instance",
    Description = "撤回一个审批实例（撤销后实例不再是'审批中/已通过'的正常状态）。需要 approval_code + instance_code + **审批提交人** user_id（三者缺一平台即拒绝；ID 类型与 user_id_type 一致）。⚠️ 前提是该审批定义在后台勾选了「允许撤销审批中的申请」或「允许撤销 x 天内通过的审批」，否则返回业务错误——这属于审批配置限制，不要重试。高危写（high-risk-write）：需 approval:approval。建议先 dry_run 预演确认。",
    RequiredScopes = ["approval:approval"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4Approval) + "." + nameof(IFeishuTenantV4Approval.CancelInstanceAsync))]
public interface IFeishuTenantApprovalCancelInstanceTool
{
    /// <summary>撤回审批实例。</summary>
    /// <returns>结构化文本（cancelled=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CancelInstanceAsync(
        [ToolParameter("approval_code", "审批定义 Code", Required = true)] string approval_code,
        [ToolParameter("instance_code", "审批实例 Code（来自 approval.create_instance 或 approval.list_pending_tasks）", Required = true)] string instance_code,
        [ToolParameter("user_id", "审批提交人的用户 ID（不是操作人；类型与 user_id_type 一致）", Required = true)] string user_id,
        [ToolParameter("user_id_type", "用户 ID 类型（可选：open_id / union_id / user_id，默认 open_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不撤回（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：approval.cc_instance（映射 <c>IFeishuUserV4ApprovalInstance.AddCcInstanceAsync</c>）。</summary>
/// <remarks>
/// 抄送只放开**查看权限**（被抄送人在「抄送我」列表可见），不改变流程与审批人——
/// 与 <c>approval.transfer_task</c>（改审批人）是两件不同的事，描述里对照写明以免混用。
/// </remarks>
[AgentTool("approval.cc_instance",
    Description = "把当前审批实例抄送给若干用户（被抄送人可在审批中心「抄送我」里查看详情）。只放开可见性，**不改变流程、不更换审批人**——要让别人来批用 approval.transfer_task。instance_code 来自 approval.list_pending_tasks / approval.get_instance；cc_user_ids 的类型须与 user_id_type 一致。用户身份工具——宿主须在 AllowedIdentities 放行 user。写操作：需 approval:approval。",
    RequiredScopes = ["approval:approval"],
    IsWrite = true,
    Source = nameof(IFeishuUserV4ApprovalInstance) + "." + nameof(IFeishuUserV4ApprovalInstance.AddCcInstanceAsync))]
public interface IFeishuUserApprovalCcInstanceTool
{
    /// <summary>抄送审批实例。</summary>
    /// <returns>结构化文本（cc_sent=true + 抄送人数）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CcInstanceAsync(
        [ToolParameter("instance_code", "审批实例 Code", Required = true)] string instance_code,
        [ToolParameter("cc_user_ids", "被抄送人的用户 ID 列表（非空；类型与 user_id_type 一致）", Required = true)] string[] cc_user_ids,
        [ToolParameter("comment", "抄送留言（可选，不超过 500 字）")] string? comment = null,
        [ToolParameter("user_id_type", "用户 ID 类型（可选：open_id / union_id / user_id，默认 open_id）")] string? user_id_type = null,
        [ToolParameter("dry_run", "仅预演不抄送（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Approval 实例详情（1 个只读，user-only） ───────────────────────────

/// <summary>工具接口：approval.get_instance（映射 <c>IFeishuUserV4ApprovalInstance.GetInstanceDetailAsync</c>）。</summary>
[AgentTool("approval.get_instance",
    Description = "获取审批实例详情（含表单/状态/审批流程节点）。instance_code 来自 approval.list_pending_tasks 或审批事件。只读，需 approval:approval:readonly。用户身份工具——宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["approval:approval:readonly"],
    Source = nameof(IFeishuUserV4ApprovalInstance) + "." + nameof(IFeishuUserV4ApprovalInstance.GetInstanceDetailAsync))]
public interface IFeishuUserApprovalGetInstanceTool
{
    /// <summary>获取审批实例详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（instance_code/status/form/auditors），超长截断并标记 truncated。</returns>
    Task<string> GetInstanceAsync(
        [ToolParameter("instance_code", "审批实例 Code（来自 approval.list_pending_tasks 的 instance_code）", Required = true)] string instance_code,
        [ToolParameter("locale", "语言（可选，如 zh-CN/en-US，默认 zh-CN）")] string? locale = null,
        CancellationToken cancellationToken = default);
}
