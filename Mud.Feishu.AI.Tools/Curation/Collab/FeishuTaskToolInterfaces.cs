// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// 任务工具接口集：<c>task.create_task</c>（tenant）与 <c>task.list_my_tasks</c>
// （<b>user 身份</b>——工具面首个非 tenant 工具）。
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

/// <summary>工具接口：task.create_task（映射 <c>IFeishuTenantV2Task.CreateTaskAsync</c> / <c>CreateSubTaskAsync</c>）。</summary>
/// <remarks>
/// 子任务 = 带父任务锚点的任务：<c>parent_task_guid</c> 缺省走 <c>POST /tasks</c>，
/// 传入时走 <c>POST /tasks/{guid}/subtasks</c>（字段面完全一致）。
/// </remarks>
[AgentTool("task.create_task",
    Description = "创建一条任务（summary 必填；due 为 RFC3339 时间由工具层转换为毫秒时间戳；assignee_ids 为被指派人 open_id 列表）。传 parent_task_guid 则创建该任务的子任务（同一入口，字段面一致）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Task) + "." + nameof(IFeishuTenantV2Task.CreateTaskAsync))]
public interface IFeishuTenantTaskCreateTaskTool
{
    /// <summary>创建任务（或子任务）。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_guid；子任务为 subtask_guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateTaskAsync(
        [ToolParameter("summary", "任务标题", Required = true)] string summary,
        [ToolParameter("description", "任务描述（可选）")] string? description = null,
        [ToolParameter("due", "截止时间（RFC3339，如 2026-10-01T18:00:00+08:00；可选）")] string? due = null,
        [ToolParameter("assignee_ids", "被指派人 open_id 数组（可选；如 [\"ou_xxx\"]）")] string[]? assignee_ids = null,
        [ToolParameter("parent_task_guid", "父任务 GUID（可选）：传入即创建该任务的子任务")] string? parent_task_guid = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：平台原生幂等——相同键至多创建一次任务；省略时不保证幂等。建议由调用方给出稳定值，不要用随机数。幂等键不跨工具共享。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.list_my_tasks（映射 <c>IFeishuUserV2Task.GetTasksPageListByIdAsync</c>）。</summary>
[AgentTool("task.list_my_tasks",
    Description = "以用户令牌身份读取「我负责的」任务列表（按用户在任务界面的自定义排序返回；可翻页）。要求宿主已提供当前用户身份与该用户的用户令牌。只读，需 task:task:readonly。",
    RequiredScopes = ["task:task:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuUserV2Task) + "." + nameof(IFeishuUserV2Task.GetTasksPageListByIdAsync))]
public interface IFeishuUserTaskListMyTasksTool
{
    /// <summary>列出我的任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（items + 翻页契约）。</returns>
    Task<string> ListMyTasksAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("completed", "是否包含已完成任务（可选，默认不包含）")] bool? completed = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Task 写（2 个，动作面） ───────────────────────────

/// <summary>工具接口：task.update_task（映射 <c>IFeishuTenantV2Task.UpdateTaskAsync</c>）。</summary>
[AgentTool("task.update_task",
    Description = "更新任务信息（summary/description/due/completed，至少传一个要更新的字段）。completed=true 标记完成、false 重新打开（与 task.complete_task 同一路径的简写入口）。task_guid 来自 task.create_task 或 task.list_my_tasks。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Task) + "." + nameof(IFeishuTenantV2Task.UpdateTaskAsync),

    // 至少要更新一个字段——跨参数约束，required 表达不了（会让成员全部必填）。
    // 成员集与执行器的运行时校验（TaskTools.UpdateTaskAsync）严格一致，不多不少。
    // 注意：task.complete_task 共用同一 SDK 方法但**不加** AnyOf——它只传 completed_at，
    // 语义是"固定字段写入"，不是"开放字段任选其一"。
    AnyOf = ["summary|description|due|completed"])]
public interface IFeishuTenantTaskUpdateTaskTool
{
    /// <summary>更新任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateTaskAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("summary", "任务标题（可选更新；如更新不可为空）")] string? summary = null,
        [ToolParameter("description", "任务描述（可选更新）")] string? description = null,
        [ToolParameter("due", "截止时间（RFC3339，如 2026-10-01T18:00:00+08:00；可选更新）")] string? due = null,
        [ToolParameter("completed", "完成状态（可选）：true=标记完成；false=重新打开（清除完成时间）")] bool? completed = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.complete_task（映射 <c>IFeishuTenantV2Task.UpdateTaskAsync</c>，update_fields=["completed_at"]）。</summary>
[AgentTool("task.complete_task",
    Description = "将任务标记为已完成（通过 update_task 设置 completed_at 字段）。task_guid 来自 task.create_task 或 task.list_my_tasks。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Task) + "." + nameof(IFeishuTenantV2Task.UpdateTaskAsync))]
public interface IFeishuTenantTaskCompleteTaskTool
{
    /// <summary>完成任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_guid/completed=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CompleteTaskAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("dry_run", "仅预演不完成（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Task 写面成环（4 个） ───────────────────────────

/// <summary>工具接口：task.delete_task（映射 <c>IFeishuTenantV2Task.DeleteTaskByIdAsync</c>）。</summary>
[AgentTool("task.delete_task",
    Description = "删除指定任务（删除后任务无法再被获取到）。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。建议先 dry_run 预演确认。task_guid 来自 task.create_task 或 task.list_my_tasks。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Task) + "." + nameof(IFeishuTenantV2Task.DeleteTaskByIdAsync))]
public interface IFeishuTenantTaskDeleteTaskTool
{
    /// <summary>删除任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteTaskAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.add_comment（映射 <c>IFeishuTenantV2TaskComments.CreateCommentAsync</c>）。</summary>
[AgentTool("task.add_comment",
    Description = "为指定任务添加评论（content 必填，最长 3000 个 utf8 字符）。可通过 reply_to_comment_id 回复已有评论。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2TaskComments) + "." + nameof(IFeishuTenantV2TaskComments.CreateCommentAsync))]
public interface IFeishuTenantTaskAddCommentTool
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
[AgentTool("task.add_members",
    Description = "向指定任务添加成员（负责人或关注人，member_ids 为 open_id 数组，role 指定角色 assignee 或 follower）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Task) + "." + nameof(IFeishuTenantV2Task.AddMembersByIdAsync))]
public interface IFeishuTenantTaskAddMembersTool
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

// ─────────────────────────── Task 成环（12 个） ───────────────────────────
//
// task.upload_attachment 不在策展面：SDK 的 UploadAttachmentAsync 在 [FormContent] 请求体之外还带
// [Query] user_id_type，工具面上传校验（生成器槽位 008：上传方法的非二进制参数必须自身是表单体）
// 判其不可映射为 format:binary；待对齐 Drive 上传形态后方可入策展。

/// <summary>工具接口：task.tasklist_create（映射 <c>IFeishuTenantV2TaskList.CreateTaskListAsync</c>）。</summary>
[AgentTool("task.tasklist_create",
    Description = "创建一个任务清单（清单用于组织同一项目的多个任务）。member_ids 为协作成员 open_id 列表（可选）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2TaskList) + "." + nameof(IFeishuTenantV2TaskList.CreateTaskListAsync))]
public interface IFeishuTenantTaskTasklistCreateTool
{
    /// <summary>创建清单。</summary>
    /// <returns>白名单投影后的 JSON 文本（tasklist_guid/name/url）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateTasklistAsync(
        [ToolParameter("name", "清单名称（最多 100 个字符）", Required = true)] string name,
        [ToolParameter("member_ids", "协作成员 open_id 数组（可选；如 [\"ou_xxx\"]）")] string[]? member_ids = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.tasklist_get（映射 <c>IFeishuTenantV2TaskList.GetTaskListByIdAsync</c>）。</summary>
[AgentTool("task.tasklist_get",
    Description = "获取清单详情（清单名/所有者/成员）。tasklist_guid 来自 task.tasklist_page_list 或 task.tasklist_create。只读，需 task:task:readonly。",
    RequiredScopes = ["task:task:readonly"],
    Source = nameof(IFeishuTenantV2TaskList) + "." + nameof(IFeishuTenantV2TaskList.GetTaskListByIdAsync))]
public interface IFeishuTenantTaskTasklistGetTool
{
    /// <summary>获取清单详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（tasklist_guid/name/owner_id/member_ids/url）。</returns>
    Task<string> GetTasklistAsync(
        [ToolParameter("tasklist_guid", "任务清单全局唯一 GUID（来自 task.tasklist_page_list）", Required = true)] string tasklist_guid,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.tasklist_update（映射 <c>IFeishuTenantV2TaskList.UpdateTaskListByIdAsync</c>）。</summary>
/// <remarks>仅支持改名——平台换所有人（update_fields=["owner"]）的线上形态需要 <c>tasklist.owner</c> 字段，当前 SDK DTO（TaskListData）未暴露该字段。</remarks>
[AgentTool("task.tasklist_update",
    Description = "重命名一个任务清单。tasklist_guid 来自 task.tasklist_page_list。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2TaskList) + "." + nameof(IFeishuTenantV2TaskList.UpdateTaskListByIdAsync))]
public interface IFeishuTenantTaskTasklistUpdateTool
{
    /// <summary>重命名清单。</summary>
    /// <returns>白名单投影后的 JSON 文本（tasklist_guid/updated=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateTasklistAsync(
        [ToolParameter("tasklist_guid", "任务清单全局唯一 GUID（来自 task.tasklist_page_list）", Required = true)] string tasklist_guid,
        [ToolParameter("name", "清单新名称（最多 100 个字符）", Required = true)] string name,
        [ToolParameter("dry_run", "仅预演不修改（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.tasklist_delete（映射 <c>IFeishuTenantV2TaskList.DeleteTaskListByIdAsync</c>）。</summary>
[AgentTool("task.tasklist_delete",
    Description = "删除指定清单（删除后清单不可恢复，清单内的任务本身不受影响）。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。建议先 dry_run 预演确认。tasklist_guid 来自 task.tasklist_page_list。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2TaskList) + "." + nameof(IFeishuTenantV2TaskList.DeleteTaskListByIdAsync))]
public interface IFeishuTenantTaskTasklistDeleteTool
{
    /// <summary>删除清单。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true/tasklist_guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteTasklistAsync(
        [ToolParameter("tasklist_guid", "任务清单全局唯一 GUID（来自 task.tasklist_page_list）", Required = true)] string tasklist_guid,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.tasklist_member_add（映射 <c>IFeishuTenantV2TaskList.AddTaskListMemberByIdAsync</c>）。</summary>
[AgentTool("task.tasklist_member_add",
    Description = "向清单添加协作成员（member_ids 为 open_id 数组）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2TaskList) + "." + nameof(IFeishuTenantV2TaskList.AddTaskListMemberByIdAsync))]
public interface IFeishuTenantTaskTasklistMemberAddTool
{
    /// <summary>添加清单成员。</summary>
    /// <returns>白名单投影后的 JSON 文本（added_count/tasklist_guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AddTasklistMemberAsync(
        [ToolParameter("tasklist_guid", "任务清单全局唯一 GUID（来自 task.tasklist_page_list）", Required = true)] string tasklist_guid,
        [ToolParameter("member_ids", "成员 open_id 数组（如 [\"ou_xxx\"]）", Required = true)] string[] member_ids,
        [ToolParameter("dry_run", "仅预演不添加（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.tasklist_member_remove（映射 <c>IFeishuTenantV2TaskList.RemoveTaskListMemberByIdAsync</c>）。</summary>
[AgentTool("task.tasklist_member_remove",
    Description = "从清单移除协作成员（member_ids 为 open_id 数组；移除不存在的成员平台直接返回成功）。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。建议先 dry_run 预演确认。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2TaskList) + "." + nameof(IFeishuTenantV2TaskList.RemoveTaskListMemberByIdAsync))]
public interface IFeishuTenantTaskTasklistMemberRemoveTool
{
    /// <summary>移除清单成员。</summary>
    /// <returns>白名单投影后的 JSON 文本（removed_count/tasklist_guid）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> RemoveTasklistMemberAsync(
        [ToolParameter("tasklist_guid", "任务清单全局唯一 GUID（来自 task.tasklist_page_list）", Required = true)] string tasklist_guid,
        [ToolParameter("member_ids", "要移除的成员 open_id 数组（如 [\"ou_xxx\"]）", Required = true)] string[] member_ids,
        [ToolParameter("dry_run", "仅预演不移除（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.tasklist_page_list（映射 <c>IFeishuTenantV2TaskList.GetTaskListsPageListAsync</c>）。</summary>
[AgentTool("task.tasklist_page_list",
    Description = "列出调用身份可读取的全部任务清单（返回清单摘要，可翻页）。只读，需 task:task:readonly。",
    RequiredScopes = ["task:task:readonly"],
    Source = nameof(IFeishuTenantV2TaskList) + "." + nameof(IFeishuTenantV2TaskList.GetTaskListsPageListAsync))]
public interface IFeishuTenantTaskTasklistPageListTool
{
    /// <summary>列出清单。</summary>
    /// <returns>白名单投影后的 JSON 文本（items + 翻页契约）。</returns>
    Task<string> GetTasklistPageListAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.tasklist_tasks（映射 <c>IFeishuTenantV2TaskList.GetTaskListPageListByIdAsync</c>）。</summary>
[AgentTool("task.tasklist_tasks",
    Description = "分页列出指定清单内的任务（返回任务摘要；可按完成状态过滤，可翻页）。tasklist_guid 来自 task.tasklist_page_list。只读，需 task:task:readonly。",
    RequiredScopes = ["task:task:readonly"],
    Source = nameof(IFeishuTenantV2TaskList) + "." + nameof(IFeishuTenantV2TaskList.GetTaskListPageListByIdAsync))]
public interface IFeishuTenantTaskTasklistTasksTool
{
    /// <summary>列出清单内的任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（items + 翻页契约）。</returns>
    Task<string> GetTasklistTasksAsync(
        [ToolParameter("tasklist_guid", "任务清单全局唯一 GUID（来自 task.tasklist_page_list）", Required = true)] string tasklist_guid,
        [ToolParameter("completed", "完成状态过滤（可选：true=只返回已完成 / false=只返回未完成；不填不过滤）")] bool? completed = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.search_tasks（映射 <c>IFeishuUserV2Task.SearchTasksAsync</c>，user 身份）。</summary>
[AgentTool("task.search_tasks",
    Description = "基于关键词与过滤条件搜索当前用户可见的任务（如'找关于合同的未完成任务'）。要求宿主已提供当前用户身份与该用户的用户令牌。只读，需 task:task:readonly。",
    RequiredScopes = ["task:task:readonly"],
    Source = nameof(IFeishuUserV2Task) + "." + nameof(IFeishuUserV2Task.SearchTasksAsync))]
public interface IFeishuUserTaskSearchTasksTool
{
    /// <summary>搜索任务。</summary>
    /// <returns>白名单投影后的 JSON 文本（items + total + 翻页契约）。</returns>
    Task<string> SearchTasksAsync(
        [ToolParameter("query", "搜索关键字（可选，长度 0~50 字符；与过滤条件可组合）")] string? query = null,
        [ToolParameter("assignee_ids", "按负责人 open_id 过滤（可选，数组）")] string[]? assignee_ids = null,
        [ToolParameter("creator_ids", "按创建人 open_id 过滤（可选，数组）")] string[]? creator_ids = null,
        [ToolParameter("follower_ids", "按关注人 open_id 过滤（可选，数组）")] string[]? follower_ids = null,
        [ToolParameter("is_completed", "按完成状态过滤（可选：true=已完成 / false=未完成；不填不过滤）")] bool? is_completed = null,
        [ToolParameter("due_from", "按截止时间起始过滤（RFC3339，如 2026-10-01T00:00:00+08:00；可选）")] string? due_from = null,
        [ToolParameter("due_to", "按截止时间截止过滤（RFC3339；可选）")] string? due_to = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.set_reminder（映射 <c>IFeishuTenantV2Task.AddTaskReminderByIdAsync</c>）。</summary>
[AgentTool("task.set_reminder",
    Description = "为任务添加提醒（提醒时刻 = 截止时间 - relative_fire_minute 分钟；任务必须已有截止时间；每个任务最多 1 个提醒）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Task) + "." + nameof(IFeishuTenantV2Task.AddTaskReminderByIdAsync))]
public interface IFeishuTenantTaskSetReminderTool
{
    /// <summary>添加任务提醒。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_guid/reminder_added + reminders[]：reminder_id/relative_fire_minute）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> SetReminderAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("relative_fire_minute", "相对截止时间的提前分钟数（如 30=截止前 30 分钟提醒）", Required = true)] int relative_fire_minute,
        [ToolParameter("dry_run", "仅预演不添加（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：task.remove_reminder（映射 <c>IFeishuTenantV2Task.RemoveTaskReminderByIdAsync</c>）。</summary>
[AgentTool("task.remove_reminder",
    Description = "移除任务的提醒（reminder_ids 来自任务详情；移除不存在的提醒平台直接返回成功）。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 task:task。建议先 dry_run 预演确认。",
    RequiredScopes = ["task:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2Task) + "." + nameof(IFeishuTenantV2Task.RemoveTaskReminderByIdAsync))]
public interface IFeishuTenantTaskRemoveReminderTool
{
    /// <summary>移除任务提醒。</summary>
    /// <returns>白名单投影后的 JSON 文本（task_guid/removed_count）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> RemoveReminderAsync(
        [ToolParameter("task_guid", "任务全局唯一 ID（来自 task.create_task 或 task.list_my_tasks）", Required = true)] string task_guid,
        [ToolParameter("reminder_ids", "要移除的提醒 ID 数组（如 [\"10\"]，取自 task.set_reminder 返回的 reminders[].reminder_id）", Required = true)] string[] reminder_ids,
        [ToolParameter("dry_run", "仅预演不移除（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
