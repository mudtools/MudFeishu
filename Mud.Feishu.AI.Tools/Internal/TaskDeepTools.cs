// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Tasks;
using Mud.Feishu.DataModels.TasksList;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 任务深化工具执行器（T-3 成环：清单 8 / 搜索 1 / 提醒 2 / 重开 1，共 12 个）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软缺席按粒度分两档</b>：<see cref="Mud.Feishu.IFeishuTenantV2TaskList"/> 是整个执行器的注册锚
/// （缺席 ⇒ 12 个工具都不进注册表——白名单期 fail-fast）；其余侧客户端（任务 / 用户任务）
/// 缺席时对应工具在执行期给出"宿主须启用什么"的结构化错误（不影响 task 其它工具）。
/// </para>
/// <para>
/// <b>成员类型平台事实</b>：任务与清单成员对象的 <c>type</c> 合法值是 <c>user</c>（"open_id" 是
/// user_id_type 的取值、属于 <c>id</c> 字段，写成成员类型会被平台判非法）；清单成员缺省不带 role
/// （平台默认 viewer——最小权限），任务成员角色语义由既有 <c>task.add_members</c> 承担。
/// </para>
/// </remarks>
internal sealed class TaskDeepTools(
    Mud.Feishu.IFeishuTenantV2TaskList taskListClient,
    Mud.Feishu.IFeishuTenantV2Task? taskClient,
    Mud.Feishu.IFeishuUserV2Task? userTaskClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2TaskList _taskListClient = taskListClient
        ?? throw new ArgumentNullException(nameof(taskListClient));
    private readonly Mud.Feishu.IFeishuTenantV2Task? _taskClient = taskClient;
    private readonly Mud.Feishu.IFeishuUserV2Task? _userTaskClient = userTaskClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    // ────────── 清单（create / get / update / delete / member add / member remove / page list / tasks） ──────────

    /// <summary>task.tasklist_create：创建任务清单（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskTasklistCreateTool))]
    public Task<ToolResult> CreateTasklistAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskTasklistCreate);
        return executor.RunAsync(async () =>
        {
            var args = TaskTasklistCreateArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/task/v2/tasklists",
                    ToolDryRun.IdempotencyNote(null),
                    ("name", args.Name.Length),
                    ("member_ids", args.MemberIds?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _taskListClient
                .CreateTaskListAsync(
                    new CreateTaskListRequest
                    {
                        Name = args.Name,
                        Members = ToMembers(args.MemberIds),
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, static data => new JsonObject
            {
                ["tasklist_guid"] = data.Tasklist?.Guid,
                ["name"] = data.Tasklist?.Name,
                ["url"] = data.Tasklist?.Url,
            });
        });
    }

    /// <summary>task.tasklist_get：获取清单详情（<c>dry_run</c> 不适用——只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskTasklistGetTool))]
    public Task<ToolResult> GetTasklistAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskTasklistGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = TaskTasklistGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _taskListClient
                .GetTaskListByIdAsync(args.TasklistGuid, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, static data => new JsonObject
            {
                ["tasklist_guid"] = data.Tasklist?.Guid,
                ["name"] = data.Tasklist?.Name,
                ["owner_id"] = data.Tasklist?.Owner?.Id,
                ["member_ids"] = ToJsonArray(data.Tasklist?.Members?.Select(static m => m.Id).ToArray()),
                ["url"] = data.Tasklist?.Url,
            });
        });
    }

    /// <summary>task.tasklist_update：重命名清单（PATCH 按字段更新；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskTasklistUpdateTool))]
    public Task<ToolResult> UpdateTasklistAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskTasklistUpdate);
        return executor.RunAsync(async () =>
        {
            var args = TaskTasklistUpdateArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", $"/open-apis/task/v2/tasklists/{args.TasklistGuid}",
                    ToolDryRun.IdempotencyNote("PATCH 按字段更新天然幂等——相同字段重复写入结果一致"),
                    ("tasklist_guid", args.TasklistGuid.Length),
                    ("name", args.Name.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _taskListClient
                .UpdateTaskListByIdAsync(
                    args.TasklistGuid,
                    new UpdateTaskListRequest
                    {
                        Tasklist = new TaskListData { Name = args.Name },
                        UpdateFields = ["name"],
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["tasklist_guid"] = data.Tasklist?.Guid ?? args.TasklistGuid,
                ["updated"] = true,
            });
        });
    }

    /// <summary>task.tasklist_delete：删除清单（不可恢复；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskTasklistDeleteTool))]
    public Task<ToolResult> DeleteTasklistAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskTasklistDelete);
        return executor.RunAsync(async () =>
        {
            var args = TaskTasklistDeleteArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", $"/open-apis/task/v2/tasklists/{args.TasklistGuid}"));
            }

            var result = await _taskListClient
                .DeleteTaskListByIdAsync(args.TasklistGuid, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["tasklist_guid"] = args.TasklistGuid,
                ["deleted"] = true,
            });
        });
    }

    /// <summary>task.tasklist_member_add：向清单添加协作成员（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskTasklistMemberAddTool))]
    public Task<ToolResult> AddTasklistMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskTasklistMemberAdd);
        return executor.RunAsync(async () =>
        {
            var args = TaskTasklistMemberAddArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/task/v2/tasklists/{args.TasklistGuid}/add_members",
                    ToolDryRun.IdempotencyNote(null),
                    ("tasklist_guid", args.TasklistGuid.Length),
                    ("member_ids", args.MemberIds.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _taskListClient
                .AddTaskListMemberByIdAsync(
                    args.TasklistGuid,
                    new AddTaskListMemberRequest { Members = ToMembers(args.MemberIds) ?? [] },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["tasklist_guid"] = args.TasklistGuid,
                ["added_count"] = args.MemberIds.Length,
            });
        });
    }

    /// <summary>task.tasklist_member_remove：从清单移除协作成员（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskTasklistMemberRemoveTool))]
    public Task<ToolResult> RemoveTasklistMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskTasklistMemberRemove);
        return executor.RunAsync(async () =>
        {
            var args = TaskTasklistMemberRemoveArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/task/v2/tasklists/{args.TasklistGuid}/remove_members",
                    ToolDryRun.IdempotencyNote(null),
                    ("tasklist_guid", args.TasklistGuid.Length),
                    ("member_ids", args.MemberIds.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _taskListClient
                .RemoveTaskListMemberByIdAsync(
                    args.TasklistGuid,
                    new RemoveTaskListMemberRequest { Members = ToMembers(args.MemberIds) ?? [] },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["tasklist_guid"] = args.TasklistGuid,
                ["removed_count"] = args.MemberIds.Length,
            });
        });
    }

    /// <summary>task.tasklist_page_list：列出可读取的全部任务清单（分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskTasklistPageListTool))]
    public Task<ToolResult> GetTasklistPageListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskTasklistPageList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = TaskTasklistPageListArgs.Unpack(arguments);

            async Task<ToolApiOutcome<ApiPageListResult<TaskListInfo>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _taskListClient
                    .GetTaskListsPageListAsync(
                        page_size: PageSizes.TaskLists,
                        page_token: token,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectTaskLists, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>task.tasklist_tasks：分页列出清单内的任务（可按完成状态过滤）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskTasklistTasksTool))]
    public Task<ToolResult> GetTasklistTasksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskTasklistTasks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = TaskTasklistTasksArgs.Unpack(arguments);

            async Task<ToolApiOutcome<ApiPageListResult<TaskSummary>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _taskListClient
                    .GetTaskListPageListByIdAsync(
                        args.TasklistGuid,
                        page_size: PageSizes.TaskListTasks,
                        page_token: token,
                        completed: args.Completed,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectTaskSummaries, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    // ────────── 搜索（user 身份） ──────────

    /// <summary>task.search_tasks：搜索当前用户可见的任务（user 身份）。</summary>
    [ToolHandler(typeof(IFeishuUserTaskSearchTasksTool))]
    public Task<ToolResult> SearchTasksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskSearchTasks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(
                _userTaskClient, executor.ToolName, "IFeishuUserV2Task",
                "AddTaskApi 的用户侧客户端（用户令牌——宿主须提供当前用户身份）");
            var args = TaskSearchTasksArgs.Unpack(arguments);

            if (args.Query is { Length: > 50 })
            {
                throw new ArgumentException(
                    $"query 最长 50 个字符（平台限制），实际: {args.Query.Length} 个字符");
            }

            var request = new SearchTaskRequest
            {
                Query = args.Query,
                Filter = BuildSearchFilter(args),
            };

            async Task<ToolApiOutcome<SearchTaskResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await client
                    .SearchTasksAsync(
                        request,
                        page_size: PageSizes.TaskSearch,
                        page_token: token,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            // SearchTaskResult 自带 has_more/page_token 但不继承 ApiPageListResult，故走显式页状态重载。
            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectSearchTasks,
                static data => (data.HasMore == true, data.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    // ────────── 提醒 / 重开 ──────────

    /// <summary>task.set_reminder：为任务添加提醒（相对截止时间；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskSetReminderTool))]
    public Task<ToolResult> SetReminderAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskSetReminder);
        return executor.RunAsync(async () =>
        {
            var client = Require(_taskClient, executor.ToolName, "IFeishuTenantV2Task", "AddTaskApi 的任务客户端");
            var args = TaskSetReminderArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/task/v2/tasks/{args.TaskGuid}/add_reminders",
                    ToolDryRun.IdempotencyNote(null),
                    ("task_guid", args.TaskGuid.Length),
                    ("relative_fire_minute", args.RelativeFireMinute.ToString(CultureInfo.InvariantCulture).Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .AddTaskReminderByIdAsync(
                    args.TaskGuid,
                    new AddTaskReminderRequest
                    {
                        Reminders = [new AddTaskReminder { RelativeFireMinute = args.RelativeFireMinute }],
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            // reminders 回带 id 是 task.remove_reminder 的唯一 reminder_ids 来源（任务详情无读取工具）。
            return executor.FromApiUntruncated(outcome, data =>
            {
                var envelope = new JsonObject
                {
                    ["task_guid"] = args.TaskGuid,
                    ["reminder_added"] = true,
                };
                var reminders = new JsonArray();
                foreach (var reminder in data.Task?.Reminders ?? [])
                {
                    reminders.AddNode(new JsonObject
                    {
                        ["reminder_id"] = reminder.Id,
                        ["relative_fire_minute"] = reminder.RelativeFireMinute,
                    });
                }

                envelope["reminders"] = reminders;
                return envelope;
            });
        });
    }

    /// <summary>task.remove_reminder：移除任务的提醒（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskRemoveReminderTool))]
    public Task<ToolResult> RemoveReminderAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskRemoveReminder);
        return executor.RunAsync(async () =>
        {
            var client = Require(_taskClient, executor.ToolName, "IFeishuTenantV2Task", "AddTaskApi 的任务客户端");
            var args = TaskRemoveReminderArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/task/v2/tasks/{args.TaskGuid}/remove_reminders",
                    ToolDryRun.IdempotencyNote(null),
                    ("task_guid", args.TaskGuid.Length),
                    ("reminder_ids", args.ReminderIds.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .RemoveTaskReminderByIdAsync(
                    args.TaskGuid,
                    new RemoveReminderRequest { ReminderIds = args.ReminderIds },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["task_guid"] = args.TaskGuid,
                ["removed_count"] = args.ReminderIds.Length,
            });
        });
    }

    /// <summary>task.reopen：把已完成的任务重新打开（与 <c>task.complete_task</c> 互为逆操作；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantTaskReopenTool))]
    public Task<ToolResult> ReopenTaskAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.TaskReopen);
        return executor.RunAsync(async () =>
        {
            var client = Require(_taskClient, executor.ToolName, "IFeishuTenantV2Task", "AddTaskApi 的任务客户端");
            var args = TaskReopenArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", $"/open-apis/task/v2/tasks/{args.TaskGuid}",
                    ToolDryRun.IdempotencyNote("幂等——对已打开的任务重复调用不产生副作用"),
                    ("task_guid", args.TaskGuid.Length),
                    ("update_fields", "completed_at".Length)));
            }

            // completed_at=0 即"未完成"（平台契约：为 0 表示未完成），与 complete_task 写时间戳互为逆操作。
            var outcome = FeishuApiResultReader.Read(await client
                .UpdateTaskAsync(
                    args.TaskGuid,
                    new UpdateTaskRequest
                    {
                        Task = new UpdateTaskData { CompletedAt = "0" },
                        UpdateFields = ["completed_at"],
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["task_guid"] = args.TaskGuid,
                ["completed"] = false,
            });
        });
    }

    // ────────── 私有辅助 ──────────

    /// <summary>取软依赖客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private static T Require<T>(T? client, string toolName, string clientName, string api)
        where T : class
        => client ?? throw new ArgumentException(
            $"{toolName} 需要 {clientName}——宿主须启用 {api}；未启用时本工具不在工具列表中"
            + "（软缺席，不影响 task 其它工具）");

    /// <summary>解包 <c>FeishuNullDataApiResult</c>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>）。</summary>
    private static void RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            throw new ArgumentException(
                FeishuToolBinding.StructuredError(
                    toolName,
                    result.Code,
                    $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={result.Msg ?? "(无错误信息)"}"));
        }
    }

    /// <summary>成员入参：open_id 数组 → 平台成员对象（<c>type=user</c>；不带 role——清单平台默认 viewer）。</summary>
    /// <remarks>
    /// <b>平台事实</b>：成员对象的 <c>type</c> 合法值是 <c>user</c>/<c>chat</c>/<c>app</c>——
    /// "open_id" 是 user_id_type 的取值（属于 <c>id</c> 字段）而不是成员类型，写成 open_id 会被平台判非法。
    /// </remarks>
    private static TaskMember[]? ToMembers(string[]? memberIds)
        => memberIds is { Length: > 0 }
            ? memberIds.Select(static id => new TaskMember { Id = id, Type = "user" }).ToArray()
            : null;

    /// <summary>搜索过滤条件组装：任一过滤参数在场才产出 Filter（全空 = 不过滤，避免下发空过滤器）。</summary>
    private static TaskSearchFilter? BuildSearchFilter(TaskSearchTasksArgs args)
    {
        var hasDueRange = args.DueFrom is not null || args.DueTo is not null;
        if (args.AssigneeIds is not { Length: > 0 }
            && args.CreatorIds is not { Length: > 0 }
            && args.FollowerIds is not { Length: > 0 }
            && args.IsCompleted is null
            && !hasDueRange)
        {
            return null;
        }

        return new TaskSearchFilter
        {
            AssigneeIds = args.AssigneeIds,
            CreatorIds = args.CreatorIds,
            FollowerIds = args.FollowerIds,
            IsCompleted = args.IsCompleted,
            DueTime = hasDueRange
                ? new TaskSearchTimeRange { StartTime = args.DueFrom, EndTime = args.DueTo }
                : null,
        };
    }

    /// <summary>清单列表投影：items（tasklist_guid/name/url）+ 翻页契约。</summary>
    private static JsonObject ProjectTaskLists(ApiPageListResult<TaskListInfo> data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

        foreach (var tasklist in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["tasklist_guid"] = tasklist.Guid,
                ["name"] = tasklist.Name,
                ["url"] = tasklist.Url,
            });
        }

        return envelope;
    }

    /// <summary>清单内任务投影：items（task_guid/summary/due/completed_at）+ 翻页契约（与 list_my_tasks 同字段口径）。</summary>
    private static JsonObject ProjectTaskSummaries(ApiPageListResult<TaskSummary> data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

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

    /// <summary>搜索命中投影：items（task_guid/preview/app_link）+ total + 翻页契约。</summary>
    private static JsonObject ProjectSearchTasks(SearchTaskResult data)
    {
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

        foreach (var hit in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["task_guid"] = hit.Id,
                ["preview"] = ToolResultText.Truncate(hit.DisplayInfo, PageSizes.MessagePreviewLength),
                ["app_link"] = hit.MetaData?.AppLink,
            });
        }

        envelope["total"] = data.Total;
        return envelope;
    }

    /// <summary>字符串数组 → JSON 数组（AddNode 逐项，规避泛型 Add 的裁剪注解）；空/缺席归一为 null。</summary>
    private static JsonArray? ToJsonArray(string[]? values)
    {
        if (values is not { Length: > 0 })
        {
            return null;
        }

        var array = new JsonArray();
        foreach (var value in values)
        {
            array.AddNode(value);
        }

        return array;
    }
}
