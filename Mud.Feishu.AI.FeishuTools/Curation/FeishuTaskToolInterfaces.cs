// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Curation;

// <summary>
// WP5 任务工具接口集（AT-F17）：<c>task.create_task</c>（tenant）与 <c>task.list_my_tasks</c>
// （<b>user 身份</b>——工具面首个非 tenant 工具，R4 §5.3）。
// </summary>
// <remarks>
// <para>
// <b>时间语义铁律</b>：模型侧 RFC3339；任务 v2 的 <c>due.timestamp</c> 是<b>毫秒</b>字符串，
// 转换在工具层完成（模型不感知毫秒——若把毫秒当秒会产生 1970 年静默错误）。
// </para>
// <para>
// <b>task.list_my_tasks 的身份语义</b>：以<b>用户令牌</b>身份读取「我负责的」任务——宿主须在
// 执行上下文（<c>FeishuToolContext.UserId</c>）中提供当前用户 open_id，且该用户的令牌须已就绪
// （经 <c>IFeishuUserTokenManager</c> 体系）。执行链在执行期把该用户写入
// <c>IFeishuCurrentUserContext</c>（AsyncLocal）并在执行后清理（防跨用户令牌误用）。
// </para>
// </remarks>

/// <summary>工具接口：task.create_task（映射 <c>IFeishuTenantV2Task.CreateTaskAsync</c>）。</summary>
[FeishuTool("task.create_task",
    Description = "创建一条任务（summary 必填；due 为 RFC3339 时间由工具层转换为毫秒时间戳；assignee_ids 为被指派人 open_id 列表）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = "IFeishuTenantV2Task.CreateTaskAsync")]
public interface IFeishuTaskCreateTaskTool
{
    /// <summary>创建任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（task guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateTaskAsync(
        [ToolParameter("summary", "任务标题", Required = true)] string summary,
        [ToolParameter("description", "任务描述（可选）")] string? description = null,
        [ToolParameter("due", "截止时间（RFC3339，如 2026-10-01T18:00:00+08:00；可选）")] string? due = null,
        [ToolParameter("assignee_ids", "被指派人 open_id 数组（可选；如 [\"ou_xxx\"]）")] string[]? assignee_ids = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：平台原生幂等——相同键至多创建一次任务；省略时不保证幂等。建议由调用方给出稳定值，不要用随机数。幂等键不跨工具共享。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.list_my_tasks（映射 <c>IFeishuUserV2Task.GetTasksPageListByIdAsync</c>）。</summary>
[FeishuTool("task.list_my_tasks",
    Description = "以用户令牌身份读取「我负责的」任务列表（按用户在任务界面的自定义排序返回；可翻页）。要求宿主已提供当前用户身份与该用户的用户令牌。只读，需 task:task:readonly。",
    RequiredScopes = ["task:task:readonly"],
    IsWrite = false,
    Source = "IFeishuUserV2Task.GetTasksPageListByIdAsync")]
public interface IFeishuUserTaskListMyTasksTool
{
    /// <summary>列出我的任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（items + 翻页契约）。</returns>
    Task<string> ListMyTasksAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("completed", "是否包含已完成任务（可选，默认不包含）")] bool? completed = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Task 写（2 个，WP4 动作面） ───────────────────────────

/// <summary>工具接口：task.update_task（映射 <c>IFeishuTenantV2Task.UpdateTaskAsync</c>）。</summary>
[FeishuTool("task.update_task",
    Description = "更新任务信息（summary/description/due 等字段，至少传一个要更新的字段）。task_guid 来自 task.create_task 或 task.list_my_tasks。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = "IFeishuTenantV2Task.UpdateTaskAsync",

    // R5 / B-6：至少要更新一个字段——跨参数约束，required 表达不了（会让三者都必填）。
    // 成员集与执行器的运行时校验（TaskTools.UpdateTaskAsync）严格一致，不多不少。
    // 注意：task.complete_task 共用同一 SDK 方法但**不加** AnyOf——它只传 completed_at，
    // 语义是"固定字段写入"，不是"开放字段任选其一"。
    AnyOf = ["summary|description|due"])]
public interface IFeishuTaskUpdateTaskTool
{
    /// <summary>更新任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateTaskAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("summary", "任务标题（可选更新；如更新不可为空）")] string? summary = null,
        [ToolParameter("description", "任务描述（可选更新）")] string? description = null,
        [ToolParameter("due", "截止时间（RFC3339，如 2026-10-01T18:00:00+08:00；可选更新）")] string? due = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.complete_task（映射 <c>IFeishuTenantV2Task.UpdateTaskAsync</c>，update_fields=["completed_at"]）。</summary>
[FeishuTool("task.complete_task",
    Description = "将任务标记为已完成（通过 update_task 设置 completed_at 字段）。task_guid 来自 task.create_task 或 task.list_my_tasks。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = "IFeishuTenantV2Task.UpdateTaskAsync")]
public interface IFeishuTaskCompleteTaskTool
{
    /// <summary>完成任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_guid/completed=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CompleteTaskAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("dry_run", "仅预演不完成（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Task 写面成环（R7/WP5，4 个） ───────────────────────────

/// <summary>工具接口：task.delete_task（映射 <c>IFeishuTenantV2Task.DeleteTaskByIdAsync</c>）。</summary>
[FeishuTool("task.delete_task",
    Description = "删除指定任务（删除后任务无法再被获取到）。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。建议先 dry_run 预演确认。task_guid 来自 task.create_task 或 task.list_my_tasks。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = "IFeishuTenantV2Task.DeleteTaskByIdAsync")]
public interface IFeishuTaskDeleteTaskTool
{
    /// <summary>删除任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteTaskAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.create_subtask（映射 <c>IFeishuTenantV2Task.CreateSubTaskAsync</c>）。</summary>
[FeishuTool("task.create_subtask",
    Description = "为指定父任务创建子任务（summary 必填；接口功能除了额外需要父任务 GUID 外，和创建任务完全一致）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = "IFeishuTenantV2Task.CreateSubTaskAsync")]
public interface IFeishuTaskCreateSubtaskTool
{
    /// <summary>创建子任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（subtask_guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateSubtaskAsync(
        [ToolParameter("task_guid", "父任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("summary", "子任务标题", Required = true)] string summary,
        [ToolParameter("description", "子任务描述（可选）")] string? description = null,
        [ToolParameter("due", "截止时间（RFC3339，如 2026-10-01T18:00:00+08:00；可选）")] string? due = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：平台原生幂等——相同键至多创建一次子任务；省略时不保证幂等。建议由调用方给出稳定值，不要用随机数。幂等键不跨工具共享。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.add_comment（映射 <c>IFeishuTenantV2TaskComments.CreateCommentAsync</c>）。</summary>
[FeishuTool("task.add_comment",
    Description = "为指定任务添加评论（content 必填，最长 3000 个 utf8 字符）。可通过 reply_to_comment_id 回复已有评论。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = "IFeishuTenantV2TaskComments.CreateCommentAsync")]
public interface IFeishuTaskAddCommentTool
{
    /// <summary>添加评论。</summary>
    /// <returns>白名单投影后的 JSON 文本（comment_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AddCommentAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（评论归属的资源 ID）", Required = true)] string task_guid,
        [ToolParameter("content", "评论内容（最长 3000 个 utf8 字符）", Required = true)] string content,
        [ToolParameter("reply_to_comment_id", "回复目标评论 ID（可选；不填表示创建非回复评论）")] string? reply_to_comment_id = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.add_members（映射 <c>IFeishuTenantV2Task.AddMembersByIdAsync</c>）。</summary>
[FeishuTool("task.add_members",
    Description = "向指定任务添加成员（负责人或关注人，member_ids 为 open_id 数组，role 指定角色 assignee 或 follower）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = "IFeishuTenantV2Task.AddMembersByIdAsync")]
public interface IFeishuTaskAddMembersTool
{
    /// <summary>添加任务成员。</summary>
    /// <returns>白名单投影后的 JSON 文本（added_count/task_guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AddMembersAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("member_ids", "成员 open_id 数组（如 [\"ou_xxx\"]）", Required = true)] string[] member_ids,
        [ToolParameter("role", "成员角色：assignee（负责人，默认）或 follower（关注人）")] string? role = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：平台原生幂等——相同键至多添加一次；省略时不保证幂等。幂等键不跨工具共享。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不添加（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
