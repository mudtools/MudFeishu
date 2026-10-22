// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Tasks;
using Mud.Feishu.DataModels.TasksList;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// T-3（task 域成环，12 个）的<b>真实调用链路</b>用例：清单 8 + 搜索 1 + 提醒 2 + 重开 1。
/// 断言 method/path（经编译期契约）与 SDK 实参（路径/查询/请求体），以及参数非法与软缺席的负例。
/// </summary>
public class TaskDeepToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV2TaskList> _taskListClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV2Task> _taskClient = new();
    private readonly Mock<Mud.Feishu.IFeishuUserV2Task> _userTaskClient = new();

    private TaskDeepTools CreateTools(bool withTaskClient = true, bool withUserTaskClient = true)
        => new(
            _taskListClient.Object,
            withTaskClient ? _taskClient.Object : null,
            withUserTaskClient ? _userTaskClient.Object : null,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static FeishuApiResult<TaskListOperationResult> TaskListOk(string? guid = "tl_1", string? name = "发布清单")
        => new()
        {
            Code = 0,
            Data = new TaskListOperationResult
            {
                Tasklist = new TaskListInfo { Guid = guid, Name = name, Url = "https://feishu.cn/tl_1" },
            },
        };

    // ───────────────────── 契约事实（method / path / identity） ─────────────────────

    [Theory]
    [InlineData("task.tasklist_create", "POST", "/open-apis/task/v2/tasklists", "tenant")]
    [InlineData("task.tasklist_get", "GET", "/open-apis/task/v2/tasklists/{tasklist_guid}", "tenant")]
    [InlineData("task.tasklist_update", "PATCH", "/open-apis/task/v2/tasklists/{tasklist_guid}", "tenant")]
    [InlineData("task.tasklist_delete", "DELETE", "/open-apis/task/v2/tasklists/{tasklist_guid}", "tenant")]
    [InlineData("task.tasklist_member_add", "POST", "/open-apis/task/v2/tasklists/{tasklist_guid}/add_members", "tenant")]
    [InlineData("task.tasklist_member_remove", "POST", "/open-apis/task/v2/tasklists/{tasklist_guid}/remove_members", "tenant")]
    [InlineData("task.tasklist_page_list", "GET", "/open-apis/task/v2/tasklists", "tenant")]
    [InlineData("task.tasklist_tasks", "GET", "/open-apis/task/v2/tasklists/{tasklist_guid}/tasks", "tenant")]
    [InlineData("task.search_tasks", "POST", "/open-apis/task/v2/tasks/search", "user")]
    [InlineData("task.set_reminder", "POST", "/open-apis/task/v2/tasks/{task_guid}/add_reminders", "tenant")]
    [InlineData("task.remove_reminder", "POST", "/open-apis/task/v2/tasks/{task_guid}/remove_reminders", "tenant")]
    public void Contracts_ShouldCarrySdkMethodRouteAndIdentity(string toolName, string httpMethod, string route, string identity)
    {
        FeishuToolContracts.ByToolName.Should().ContainKey(toolName);
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod);
        contract.Route.Should().Be(route, "契约 route 与 SDK 符号的 [Get]/[Post] 特性同源");
        contract.Identity.Should().Be(identity, "search_tasks 是唯一 user 身份的深化工具");
    }

    // ───────────────────── task.tasklist_create ─────────────────────

    [Fact]
    public async Task CreateTasklist_ShouldBuildMembersWithTypeUser_AndOmitRole()
    {
        CreateTaskListRequest? captured = null;
        _taskListClient
            .Setup(c => c.CreateTaskListAsync(
                It.IsAny<CreateTaskListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((CreateTaskListRequest request, string _, CancellationToken __) => captured = request)
            .ReturnsAsync(TaskListOk());

        var result = await CreateTools().CreateTasklistAsync(
            Args(("name", "发布清单"), ("member_ids", new[] { "ou_1", "ou_2" })),
            CancellationToken.None);

        result.ToString().Should().Contain("tl_1", "写工具回填创建结果（tasklist_guid）");
        captured!.Name.Should().Be("发布清单");
        captured.Members.Should().HaveCount(2);
        captured.Members![0].Id.Should().Be("ou_1");
        captured.Members[0].Type.Should().Be("user", "成员 type 合法值是 user——写 open_id 会被平台判非法");
        captured.Members[0].Role.Should().BeNull("清单成员不带 role（平台默认 viewer = 最小权限）");
    }

    [Fact]
    public async Task CreateTasklist_WithoutMemberIds_ShouldNotSendEmptyMembers()
    {
        CreateTaskListRequest? captured = null;
        _taskListClient
            .Setup(c => c.CreateTaskListAsync(
                It.IsAny<CreateTaskListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((CreateTaskListRequest request, string _, CancellationToken __) => captured = request)
            .ReturnsAsync(TaskListOk());

        await CreateTools().CreateTasklistAsync(Args(("name", "空成员清单")), CancellationToken.None);

        captured!.Members.Should().BeNull("省略 member_ids 时不得下发空成员数组");
    }

    [Fact]
    public async Task CreateTasklist_DryRun_ShouldNotTouchDownstream()
    {
        var result = await CreateTools().CreateTasklistAsync(
            Args(("name", "绝密清单名"), ("dry_run", true)), CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        _taskListClient.Verify(
            c => c.CreateTaskListAsync(It.IsAny<CreateTaskListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTasklist_MissingName_ShouldReturnStructuredError()
    {
        var result = await CreateTools().CreateTasklistAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.tasklist_create");
        result.ToString().Should().Contain("name");
    }

    // ───────────────────── task.tasklist_get ─────────────────────

    [Fact]
    public async Task GetTasklist_ShouldPassPathGuid_AndProjectOwnerAndMembers()
    {
        string? capturedGuid = null;
        _taskListClient
            .Setup(c => c.GetTaskListByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string guid, string _, CancellationToken __) => capturedGuid = guid)
            .ReturnsAsync(new FeishuApiResult<TaskListOperationResult>
            {
                Code = 0,
                Data = new TaskListOperationResult
                {
                    Tasklist = new TaskListInfo
                    {
                        Guid = "tl_1",
                        Name = "发布清单",
                        Owner = new TaskMember { Id = "ou_owner", Type = "user" },
                        Members = [new TaskMember { Id = "ou_1", Type = "user" }],
                    },
                },
            });

        var result = await CreateTools().GetTasklistAsync(Args(("tasklist_guid", "tl_1")), CancellationToken.None);

        capturedGuid.Should().Be("tl_1", "tasklist_guid 经 [Path] 下发");
        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;
        root.GetProperty("owner_id").GetString().Should().Be("ou_owner");
        root.GetProperty("member_ids")[0].GetString().Should().Be("ou_1");
    }

    [Fact]
    public async Task GetTasklist_MissingGuid_ShouldReturnStructuredError()
    {
        var result = await CreateTools().GetTasklistAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.tasklist_get");
        _taskListClient.Verify(
            c => c.GetTaskListByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── task.tasklist_update ─────────────────────

    [Fact]
    public async Task UpdateTasklist_ShouldSendNameAsSoleUpdateField()
    {
        string? capturedGuid = null;
        UpdateTaskListRequest? captured = null;
        _taskListClient
            .Setup(c => c.UpdateTaskListByIdAsync(
                It.IsAny<string>(), It.IsAny<UpdateTaskListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string guid, UpdateTaskListRequest request, string _, CancellationToken __) =>
            {
                capturedGuid = guid;
                captured = request;
            })
            .ReturnsAsync(TaskListOk(name: "改名后清单"));

        var result = await CreateTools().UpdateTasklistAsync(
            Args(("tasklist_guid", "tl_1"), ("name", "改名后清单")), CancellationToken.None);

        capturedGuid.Should().Be("tl_1");
        captured!.Tasklist!.Name.Should().Be("改名后清单");
        captured.UpdateFields.Should().ContainSingle(field => field == "name", "PATCH 白名单字段——不得顺手下发未请求的字段");
        result.ToString().Should().Contain("updated", "写操作回执");
    }

    [Fact]
    public async Task UpdateTasklist_DryRun_ShouldNotTouchDownstream()
    {
        var result = await CreateTools().UpdateTasklistAsync(
            Args(("tasklist_guid", "tl_1"), ("name", "改名"), ("dry_run", true)), CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        _taskListClient.Verify(
            c => c.UpdateTaskListByIdAsync(
                It.IsAny<string>(), It.IsAny<UpdateTaskListRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── task.tasklist_delete ─────────────────────

    [Fact]
    public async Task DeleteTasklist_ShouldAcceptNullDataSuccess_AndReportDeleted()
    {
        string? capturedGuid = null;
        _taskListClient
            .Setup(c => c.DeleteTaskListByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string guid, CancellationToken _) => capturedGuid = guid)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools().DeleteTasklistAsync(
            Args(("tasklist_guid", "tl_1")), CancellationToken.None);

        capturedGuid.Should().Be("tl_1");
        result.ToString().Should().Contain("deleted");
    }

    [Fact]
    public async Task DeleteTasklist_NonZeroCode_ShouldReturnStructuredError()
    {
        _taskListClient
            .Setup(c => c.DeleteTaskListByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 1069902, Msg = "tasklist not found" });

        var result = await CreateTools().DeleteTasklistAsync(
            Args(("tasklist_guid", "tl_missing")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.tasklist_delete");
        result.ToString().Should().Contain("1069902", "无 data 的成功判定也必须读 code，否则删除失败静默返回成功");
    }

    [Fact]
    public async Task DeleteTasklist_DryRun_ShouldNotTouchDownstream()
    {
        var result = await CreateTools().DeleteTasklistAsync(
            Args(("tasklist_guid", "tl_1"), ("dry_run", true)), CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        _taskListClient.Verify(
            c => c.DeleteTaskListByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ───────────────────── task.tasklist_member_add / remove ─────────────────────

    [Fact]
    public async Task AddTasklistMember_ShouldSendMemberObjectsWithTypeUser()
    {
        string? capturedGuid = null;
        AddTaskListMemberRequest? captured = null;
        _taskListClient
            .Setup(c => c.AddTaskListMemberByIdAsync(
                It.IsAny<string>(), It.IsAny<AddTaskListMemberRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string guid, AddTaskListMemberRequest request, string _, CancellationToken __) =>
            {
                capturedGuid = guid;
                captured = request;
            })
            .ReturnsAsync(TaskListOk());

        var result = await CreateTools().AddTasklistMemberAsync(
            Args(("tasklist_guid", "tl_1"), ("member_ids", new[] { "ou_1", "ou_2", "ou_3" })),
            CancellationToken.None);

        capturedGuid.Should().Be("tl_1");
        captured!.Members.Should().HaveCount(3);
        captured.Members![2].Type.Should().Be("user");
        result.ToString().Should().Contain("added_count");
    }

    [Fact]
    public async Task RemoveTasklistMember_ShouldSendMemberObjects_AndCountMatchesInput()
    {
        RemoveTaskListMemberRequest? captured = null;
        _taskListClient
            .Setup(c => c.RemoveTaskListMemberByIdAsync(
                It.IsAny<string>(), It.IsAny<RemoveTaskListMemberRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, RemoveTaskListMemberRequest request, string __, CancellationToken ___) => captured = request)
            .ReturnsAsync(TaskListOk());

        var result = await CreateTools().RemoveTasklistMemberAsync(
            Args(("tasklist_guid", "tl_1"), ("member_ids", new[] { "ou_9" })), CancellationToken.None);

        captured!.Members.Should().ContainSingle(m => m.Id == "ou_9" && m.Type == "user");
        result.ToString().Should().Contain("removed_count");
    }

    [Fact]
    public async Task RemoveTasklistMember_MissingMemberIds_ShouldReturnStructuredError()
    {
        var result = await CreateTools().RemoveTasklistMemberAsync(
            Args(("tasklist_guid", "tl_1")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.tasklist_member_remove");
        result.ToString().Should().Contain("member_ids");
    }

    // ───────────────────── task.tasklist_page_list / tasks ─────────────────────

    [Fact]
    public async Task TasklistPageList_ShouldBindOpaquePaging_AndProjectItems()
    {
        int capturedPageSize = 0;
        string? capturedPageToken = null;
        _taskListClient
            .Setup(c => c.GetTaskListsPageListAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((int pageSize, string? pageToken, string? _, CancellationToken __) =>
            {
                capturedPageSize = pageSize;
                capturedPageToken = pageToken;
            })
            .ReturnsAsync(new FeishuApiPageListResult<TaskListInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<TaskListInfo>
                {
                    Items = [new TaskListInfo { Guid = "tl_1", Name = "发布清单", Url = "https://feishu.cn/tl_1" }],
                    HasMore = true,
                    PageToken = "pt_next",
                },
            });

        var result = await CreateTools().GetTasklistPageListAsync(
            Args(("page_token", "pt_prev")), CancellationToken.None);

        capturedPageSize.Should().Be(PageSizes.TaskLists, "分页尺寸是实现细节，不暴露给模型");
        capturedPageToken.Should().Be("pt_prev");
        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;
        root.GetProperty("has_more").GetBoolean().Should().BeTrue();
        root.GetProperty("page_token").GetString().Should().Be("pt_next");
        root.GetProperty("items")[0].GetProperty("tasklist_guid").GetString().Should().Be("tl_1");
    }

    [Fact]
    public async Task TasklistTasks_ShouldForwardCompletedFilter_AndProjectDueTimestamp()
    {
        string? capturedGuid = null;
        bool? capturedCompleted = null;
        _taskListClient
            .Setup(c => c.GetTaskListPageListByIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string guid, int _, string? __, bool? completed, string? ___, string? ____, string? _____, CancellationToken ______) =>
            {
                capturedGuid = guid;
                capturedCompleted = completed;
            })
            .ReturnsAsync(new FeishuApiPageListResult<TaskSummary>
            {
                Code = 0,
                Data = new ApiPageListResult<TaskSummary>
                {
                    Items =
                    [
                        new TaskSummary
                        {
                            Guid = "g1",
                            Summary = "发布检查",
                            Due = new TaskTime { Timestamp = "1759312800000" },
                            CompletedAt = "0",
                        },
                    ],
                    HasMore = false,
                },
            });

        var result = await CreateTools().GetTasklistTasksAsync(
            Args(("tasklist_guid", "tl_1"), ("completed", false)), CancellationToken.None);

        capturedGuid.Should().Be("tl_1");
        capturedCompleted.Should().BeFalse("completed=false = 只取未完成任务（模型可见三态过滤）");
        using var document = JsonDocument.Parse(result.ToString()!);
        var item = document.RootElement.GetProperty("items")[0];
        item.GetProperty("task_guid").GetString().Should().Be("g1");
        item.GetProperty("due").GetString().Should().Be("1759312800000");
    }

    [Fact]
    public async Task TasklistTasks_MissingGuid_ShouldReturnStructuredError()
    {
        var result = await CreateTools().GetTasklistTasksAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.tasklist_tasks");
        _taskListClient.Verify(
            c => c.GetTaskListPageListByIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── task.search_tasks（user 身份） ─────────────────────

    [Fact]
    public async Task SearchTasks_ShouldKeepRfc3339VerbatimInFilter_AndProjectHits()
    {
        SearchTaskRequest? captured = null;
        _userTaskClient
            .Setup(c => c.SearchTasksAsync(
                It.IsAny<SearchTaskRequest>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SearchTaskRequest request, int _, string? __, string? ___, CancellationToken ____) => captured = request)
            .ReturnsAsync(new FeishuApiResult<SearchTaskResult>
            {
                Code = 0,
                Data = new SearchTaskResult
                {
                    Items =
                    [
                        new TaskSearchItemInfo
                        {
                            Id = "g1",
                            DisplayInfo = "发布检查",
                            MetaData = new TaskSearchMetaData { AppLink = "https://feishu.cn/task/g1" },
                        },
                    ],
                    Total = 1,
                    HasMore = false,
                },
            });

        var result = await CreateTools().SearchTasksAsync(
            Args(
                ("query", "发布"),
                ("assignee_ids", new[] { "ou_1" }),
                ("is_completed", false),
                ("due_from", "2026-10-01T00:00:00+08:00"),
                ("due_to", "2026-10-31T23:59:59+08:00")),
            CancellationToken.None);

        captured!.Query.Should().Be("发布");
        captured.Filter!.AssigneeIds.Should().Equal("ou_1");
        captured.Filter.IsCompleted.Should().BeFalse();
        captured.Filter.DueTime!.StartTime.Should().Be("2026-10-01T00:00:00+08:00",
            "搜索时间区间是 ISO8601 字段——工具层不得再转毫秒（与 task.due 的毫秒契约是两套平台口径）");
        captured.Filter.DueTime.EndTime.Should().Be("2026-10-31T23:59:59+08:00");

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;
        root.GetProperty("total").GetInt32().Should().Be(1);
        root.GetProperty("items")[0].GetProperty("task_guid").GetString().Should().Be("g1");
        root.GetProperty("items")[0].GetProperty("app_link").GetString().Should().Be("https://feishu.cn/task/g1");
    }

    [Fact]
    public async Task SearchTasks_WithoutAnyFilter_ShouldNotSendEmptyFilter()
    {
        SearchTaskRequest? captured = null;
        _userTaskClient
            .Setup(c => c.SearchTasksAsync(
                It.IsAny<SearchTaskRequest>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SearchTaskRequest request, int _, string? __, string? ___, CancellationToken ____) => captured = request)
            .ReturnsAsync(new FeishuApiResult<SearchTaskResult>
            {
                Code = 0,
                Data = new SearchTaskResult { Items = [], Total = 0, HasMore = false },
            });

        await CreateTools().SearchTasksAsync(Args(("query", "全部")), CancellationToken.None);

        captured!.Filter.Should().BeNull("全空过滤条件不得下发空对象（平台按空过滤器语义不确定）");
    }

    [Fact]
    public async Task SearchTasks_OverLongQuery_ShouldReturnStructuredError()
    {
        var result = await CreateTools().SearchTasksAsync(
            Args(("query", new string('字', 51))), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.search_tasks");
        result.ToString().Should().Contain("50", "平台硬限制必须在工具侧拦下，而不是让下游返回不可读错误");
        _userTaskClient.Verify(
            c => c.SearchTasksAsync(
                It.IsAny<SearchTaskRequest>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchTasks_WithoutUserClient_ShouldReturnStructuredError()
    {
        var result = await CreateTools(withUserTaskClient: false).SearchTasksAsync(
            Args(("query", "发布")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.search_tasks");
        result.ToString().Should().Contain("IFeishuUserV2Task", "软缺席到工具粒度：缺席时给出宿主须启用什么");
    }

    // ───────────────────── task.set_reminder / remove_reminder ─────────────────────

    [Fact]
    public async Task SetReminder_ShouldSendSingleRelativeMinute_AndReturnReminderId()
    {
        string? capturedGuid = null;
        AddTaskReminderRequest? captured = null;
        _taskClient
            .Setup(c => c.AddTaskReminderByIdAsync(
                It.IsAny<string>(), It.IsAny<AddTaskReminderRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string guid, AddTaskReminderRequest request, string _, CancellationToken __) =>
            {
                capturedGuid = guid;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<AddTaskReminderResult>
            {
                Code = 0,
                Data = new AddTaskReminderResult
                {
                    Task = new AddListTaskInfo
                    {
                        Guid = "g1",
                        Reminders = [new TaskReminderInfo { Id = "10", RelativeFireMinute = 30 }],
                    },
                },
            });

        var result = await CreateTools().SetReminderAsync(
            Args(("task_guid", "g1"), ("relative_fire_minute", 30)), CancellationToken.None);

        capturedGuid.Should().Be("g1");
        captured!.Reminders.Should().ContainSingle(r => r.RelativeFireMinute == 30);
        result.ToString().Should().Contain("reminder_id", "回带 id 是 remove_reminder 入参的唯一来源（任务详情无读取工具）");
    }

    [Fact]
    public async Task RemoveReminder_ShouldForwardIdArray()
    {
        RemoveReminderRequest? captured = null;
        _taskClient
            .Setup(c => c.RemoveTaskReminderByIdAsync(
                It.IsAny<string>(), It.IsAny<RemoveReminderRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, RemoveReminderRequest request, string __, CancellationToken ___) => captured = request)
            .ReturnsAsync(new FeishuApiResult<RemoveTaskReminderResult>
            {
                Code = 0,
                Data = new RemoveTaskReminderResult { Task = new AddListTaskInfo { Guid = "g1" } },
            });

        var result = await CreateTools().RemoveReminderAsync(
            Args(("task_guid", "g1"), ("reminder_ids", new[] { "10" })), CancellationToken.None);

        captured!.ReminderIds.Should().Equal("10");
        result.ToString().Should().Contain("removed_count");
        result.ToString().Should().NotContain("已移除", "平台对不存在的提醒也返回成功——回执不得声称业务效果");
    }

    [Fact]
    public async Task RemoveReminder_MissingIds_ShouldReturnStructuredError()
    {
        var result = await CreateTools().RemoveReminderAsync(Args(("task_guid", "g1")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.remove_reminder");
        result.ToString().Should().Contain("reminder_ids");
    }

    [Fact]
    public async Task SetReminder_DryRun_ShouldNotTouchDownstream()
    {
        var result = await CreateTools().SetReminderAsync(
            Args(("task_guid", "g1"), ("relative_fire_minute", 30), ("dry_run", true)), CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        _taskClient.Verify(
            c => c.AddTaskReminderByIdAsync(
                It.IsAny<string>(), It.IsAny<AddTaskReminderRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
