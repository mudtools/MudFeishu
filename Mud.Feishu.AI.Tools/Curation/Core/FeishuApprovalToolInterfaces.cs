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
/// <b>R5 / F-11：补齐真实硬缺陷。</b> 此前审批动作面只有 <c>approval.approve_task</c>
/// （内部只调 <c>AgreeApprovalAsync</c>），模型<b>无法拒绝审批</b> —— 审批是双向决策，
/// 只有"同意"会让 Agent 在应拒场景下强行通过。SDK 的 <c>RejectApprovalAsync</c> 已存在，
/// 故本项<b>零 SDK 改动</b>。
/// </remarks>
[AgentTool("approval.reject_task",
    Description = "拒绝一个审批任务（拒绝后审批流程结束）。与 approval.approve_task 构成完整的双向决策面；task_id 来自 approval.list_pending_tasks。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。",
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
/// 与 reject 同批补齐：<b>转交</b>是审批场景的另一个高频动作（"这条我不该批，转给张三"），
/// SDK 的 <c>TransferApprovalAsync</c> 已存在，零 SDK 改动。
/// </remarks>
[AgentTool("approval.transfer_task",
    Description = "把一个审批任务转交给他人（转交后流程流转给被转交人）。适用于'这条不该我批/我无法判断'的场景。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。建议先 dry_run 预演确认转交对象。",
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
    Description = "按审批定义 Code 发起一个审批实例，form 为审批表单 Value（JSON 数组字符串，按定义的表单控件结构填写；非 JSON 数组会在下发前被拒绝）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。",
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
    Description = "同意指定审批任务（需 approval_code/instance_code/task_id/user_id 四要素，task_id/instance_code 来自 approval.list_pending_tasks）。同意后审批流程流转到下一个审批人。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 approval:approval。",
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

// ─────────────────────────── Approval 实例详情（1 个只读，user-only，从 WP4 移入） ───────────────────────────

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
