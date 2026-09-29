// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Tasks;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Task 双工具执行器（<c>task.create_task</c>（tenant）/ <c>task.list_my_tasks</c>（<b>user 身份</b>），
/// WP5 / AT-F17）。
/// </summary>
/// <remarks>
/// <para>
/// <b>时间语义铁律</b>：模型侧 RFC3339；任务 v2 的 <c>due.timestamp</c> 是<b>毫秒</b>字符串——
/// 转换在工具层完成（确定性纯函数），模型不感知毫秒（毫秒当秒会产生 1970 年静默错误）。
/// </para>
/// <para>
/// <b>list_my_tasks 的用户上下文</b>：执行链在 user 身份工具执行前把当前用户写入
/// <see cref="IFeishuCurrentUserContext"/>（AsyncLocal）并在 finally 中清理——
/// 泄漏会让后续请求误用上一个人的令牌（R4 §5.3，本方案最危险的一处）。
/// </para>
/// </remarks>
internal sealed class TaskTools(
    Mud.Feishu.IFeishuTenantV2Task taskClient,
    Mud.Feishu.IFeishuUserV2Task? userTaskClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2Task _taskClient = taskClient
        ?? throw new ArgumentNullException(nameof(taskClient));
    private readonly Mud.Feishu.IFeishuUserV2Task? _userTaskClient = userTaskClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>task.create_task：创建任务（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 同款）：<c>idempotency_key</c> → <c>CreateTaskRequest.ClientToken</c>（平台原生幂等）。</remarks>
    [FeishuToolHandler(typeof(IFeishuTaskCreateTaskTool))]
    public Task<FeishuToolResult> CreateTaskAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskCreateTask, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = TaskCreateTaskArgs.Unpack(arguments);

            var dueMs = args.Due is null ? null : ToUnixMilliseconds(args.Due);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/task/v2/tasks",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("summary", args.Summary.Length),
                    ("due", args.Due?.Length ?? 0),
                    ("assignee_ids", args.AssigneeIds?.Length ?? 0)));
            }

            var request = new CreateTaskRequest
            {
                Summary = args.Summary,
                Description = args.Description,
                ClientToken = args.IdempotencyKey,
            };
            if (dueMs is not null)
            {
                request.Due = new TaskTime { Timestamp = dueMs };
            }

            if (args.AssigneeIds is { Length: > 0 })
            {
                request.Members = args.AssigneeIds
                    .Select(static id => new TaskMemberInfo { Id = id, Type = "open_id", Role = "assignee" })
                    .ToArray();
            }

            var outcome = FeishuApiResultReader.Read(await _taskClient
                .CreateTaskAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["task_guid"] = data.Task?.Guid,
            });
        });
    }

    /// <summary>task.list_my_tasks：以用户令牌身份读取「我负责的」任务（分页）。</summary>
    /// <remarks>用户客户端缺席（宿主未注册 <c>IFeishuUserV2Task</c>）→ 结构化错误（该工具不应被启用）。</remarks>
    [FeishuToolHandler(typeof(IFeishuUserTaskListMyTasksTool))]
    public Task<FeishuToolResult> ListMyTasksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskListMyTasks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            if (_userTaskClient is null)
            {
                throw new ArgumentException(
                    "task.list_my_tasks 需要用户令牌客户端 IFeishuUserV2Task——宿主须启用 AddTaskApi 的用户侧客户端并提供当前用户身份");
            }

            var args = TaskListMyTasksArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _userTaskClient
                .GetTasksPageListByIdAsync(
                    page_size: PageSizes.TaskList,
                    page_token: args.PageToken,
                    completed: args.Completed,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectMyTasks);
        });
    }

    /// <summary>task.update_task：更新任务信息（summary/description/due，至少传一个；<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTaskUpdateTaskTool))]
    public Task<FeishuToolResult> UpdateTaskAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskUpdateTask);
        return executor.RunAsync(async () =>
        {
            var args = TaskUpdateTaskArgs.Unpack(arguments);

            // 构造 update_fields：只包含实际传入的字段
            var updateFields = new List<string>(3);
            var taskData = new UpdateTaskData();

            if (args.Summary is { Length: > 0 } summary)
            {
                updateFields.Add("summary");
                taskData.Summary = summary;
            }
            if (args.Description is { } description)
            {
                updateFields.Add("description");
                taskData.Description = description;
            }
            if (args.Due is { } due)
            {
                updateFields.Add("due");
                taskData.Due = new TaskTime { Timestamp = ToUnixMilliseconds(due) };
            }

            if (updateFields.Count == 0)
            {
                throw new ArgumentException("task.update_task 至少需提供一个要更新的字段（summary/description/due）");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", $"/open-apis/task/v2/tasks/{args.TaskGuid}",
                    ToolDryRun.IdempotencyNote("PATCH 按字段更新天然幂等——相同字段重复写入结果一致"),
                    ("task_guid", args.TaskGuid.Length),
                    ("update_fields", string.Join(",", updateFields).Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _taskClient
                .UpdateTaskAsync(
                    args.TaskGuid,
                    new UpdateTaskRequest { Task = taskData, UpdateFields = [.. updateFields] },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["task_guid"] = data.Task?.Guid,
            });
        });
    }

    /// <summary>task.complete_task：将任务标记为已完成（通过 update_task 设置 completed_at；<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>
    /// <para>
    /// 完成任务 = <c>UpdateTaskAsync(update_fields=["completed_at"])</c>，<c>completed_at</c> 设为当前毫秒时间戳。
    /// 不暴露 completed_at 参数给模型——当前时间由工具层注入（模型不感知时间戳语义）。
    /// </para>
    /// <para>
    /// 天然幂等：对已完成的任务重复调用 completed_at 不会产生副作用（只更新时间戳，不会"取消完成"）。
    /// </para>
    /// </remarks>
    [FeishuToolHandler(typeof(IFeishuTaskCompleteTaskTool))]
    public Task<FeishuToolResult> CompleteTaskAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskCompleteTask);
        return executor.RunAsync(async () =>
        {
            var args = TaskCompleteTaskArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", $"/open-apis/task/v2/tasks/{args.TaskGuid}",
                    ToolDryRun.IdempotencyNote("幂等——对已完成的任务重复调用不产生副作用"),
                    ("task_guid", args.TaskGuid.Length),
                    ("update_fields", "completed_at".Length)));
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                .ToString(CultureInfo.InvariantCulture);

            var outcome = FeishuApiResultReader.Read(await _taskClient
                .UpdateTaskAsync(
                    args.TaskGuid,
                    new UpdateTaskRequest
                    {
                        Task = new UpdateTaskData { CompletedAt = nowMs },
                        UpdateFields = ["completed_at"],
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["task_guid"] = data.Task?.Guid,
                ["completed"] = true,
            });
        });
    }

    /// <summary>list_my_tasks 投影：items（guid/summary/due/completed_at）+ 翻页契约。</summary>
    private static JsonObject ProjectMyTasks(ApiPageListResult<ListTaskInfo> data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

        foreach (var task in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["task_guid"] = task.Guid,
                ["summary"] = task.Summary,
                ["due"] = task.Due?.Timestamp,
                ["completed_at"] = task.CompletedAt,
            });
        }

        return envelope;
    }

    /// <summary>RFC3339 → 毫秒时间戳字符串（任务 v2 的 due.timestamp 形态；须带时区）。</summary>
    private static string ToUnixMilliseconds(string rfc3339)
    {
        var trimmed = rfc3339.Trim();
        var hasOffset = trimmed.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
            || trimmed.IndexOf('+') > 0
            || trimmed.LastIndexOf('-') > 10;

        if (!hasOffset
            || !DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ArgumentException(
                $"due 需为带时区的 RFC3339 格式（如 2026-10-01T18:00:00+08:00 或 2026-10-01T18:00:00Z），实际: {trimmed}");
        }

        return parsed.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
    }
}
