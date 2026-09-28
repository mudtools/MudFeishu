// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tools;

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
