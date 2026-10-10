// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Tasks;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// WP5（AT-F17）任务双工具的<b>真实调用链路</b>用例：<c>task.create_task</c>（tenant）与
/// <c>task.list_my_tasks</c>（<b>user 身份</b>，工具面首个非 tenant 工具）。
/// </summary>
/// <remarks>
/// <b>时间语义铁律</b>：模型侧只学 RFC3339，毫秒转换在工具层（确定性纯函数）——
/// 若把两种格式暴露给模型，必然出现"毫秒当秒"的 1970 年静默错误，故此处对转换结果做精确断言。
/// </remarks>
public class TaskToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV2Task> _taskClient = new();
    private readonly Mock<Mud.Feishu.IFeishuUserV2Task> _userTaskClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV2TaskComments> _taskCommentsClient = new();

    private TaskTools CreateTools(bool withUserClient = true, bool withCommentsClient = true)
        => new(
            _taskClient.Object,
            withUserClient ? _userTaskClient.Object : null,
            withCommentsClient ? _taskCommentsClient.Object : null,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    // ───────────────────── 契约事实 ─────────────────────

    [Theory]
    [InlineData("task.create_task", "POST", "/open-apis/task/v2/tasks", "tenant")]
    [InlineData("task.list_my_tasks", "GET", "/open-apis/task/v2/tasks", "user")]
    public void Contracts_ShouldCarryIdentityAndRoute(string toolName, string httpMethod, string route, string identity)
    {
        FeishuToolContracts.ByToolName.Should().ContainKey(toolName);
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod);
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be(identity, "user 身份由 Source 的 IFeishuUser* 派生（MUDFT016 守护）");
    }

    // ───────────────────── task.create_task ─────────────────────

    [Fact]
    public async Task CreateTask_ShouldConvertRfc3339IntoMilliseconds_AndMapMembersAndClientToken()
    {
        CreateTaskRequest? captured = null;
        _taskClient
            .Setup(c => c.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((CreateTaskRequest request, string _, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<TaskOperationResult>
            {
                Code = 0,
                Data = new TaskOperationResult { Task = new TaskInfo { Guid = "task_guid_1" } },
            });

        var result = await CreateTools().CreateTaskAsync(
            Args(
                ("summary", "写周报"),
                ("description", "本周"),
                ("due", "2026-10-01T18:00:00+08:00"),
                ("assignee_ids", new[] { "ou_1", "ou_2" }),
                ("idempotency_key", "bill-42-task")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Summary.Should().Be("写周报");
        captured.Description.Should().Be("本周");
        captured.ClientToken.Should().Be("bill-42-task", "idempotency_key → CreateTaskRequest.ClientToken（平台原生幂等）");

        var expectedMs = DateTimeOffset
            .Parse("2026-10-01T18:00:00+08:00", CultureInfo.InvariantCulture)
            .ToUnixTimeMilliseconds()
            .ToString(CultureInfo.InvariantCulture);
        captured.Due!.Timestamp.Should().Be(expectedMs, "模型只学 RFC3339；毫秒时间戳由工具层转换（毫秒当秒 = 1970 年静默错误）");

        captured.Members.Should().HaveCount(2);
        captured.Members![0].Id.Should().Be("ou_1");
        captured.Members[0].Type.Should().Be("open_id");
        captured.Members[0].Role.Should().Be("assignee");

        result.ToString().Should().Contain("task_guid_1");
    }

    [Fact]
    public async Task CreateTask_WithoutDue_ShouldNotFillDue()
    {
        CreateTaskRequest? captured = null;
        _taskClient
            .Setup(c => c.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((CreateTaskRequest request, string _, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<TaskOperationResult>
            {
                Code = 0,
                Data = new TaskOperationResult { Task = new TaskInfo { Guid = "task_guid_2" } },
            });

        await CreateTools().CreateTaskAsync(Args(("summary", "无截止")), CancellationToken.None);

        captured!.Due.Should().BeNull("省略 due 时不得下发空 TaskTime");
        captured.Members.Should().BeNull("省略 assignee_ids 时不得下发空成员数组");
        captured.ClientToken.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData("2026-10-01T18:00:00")]
    [InlineData("下周三")]
    public async Task CreateTask_WithNonRfc3339Due_ShouldReturnStructuredError(string due)
    {
        var result = await CreateTools().CreateTaskAsync(
            Args(("summary", "写周报"), ("due", due)),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.create_task");
        result.ToString().Should().Contain("带时区的 RFC3339");
        _taskClient.Verify(
            c => c.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTask_MissingSummary_ShouldReturnStructuredError()
    {
        var result = await CreateTools().CreateTaskAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.create_task");
        result.ToString().Should().Contain("summary");
    }

    // ───────────────────── task.list_my_tasks（user 身份） ─────────────────────

    [Fact]
    public async Task ListMyTasks_ShouldPassPagingArgs_AndProjectItems()
    {
        int capturedPageSize = 0;
        string? capturedPageToken = null;
        bool? capturedCompleted = null;

        _userTaskClient
            .Setup(c => c.GetTasksPageListByIdAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((int pageSize, string? pageToken, bool? completed, string? _, string? __, CancellationToken ___) =>
            {
                capturedPageSize = pageSize;
                capturedPageToken = pageToken;
                capturedCompleted = completed;
            })
            .ReturnsAsync(new FeishuApiPageListResult<ListTaskInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<ListTaskInfo>
                {
                    Items =
                    [
                        new ListTaskInfo { Guid = "g1", Summary = "写周报", Due = new TaskTime { Timestamp = "1759312800000" } },
                    ],
                    HasMore = true,
                    PageToken = "pt_next",
                },
            });

        var result = await CreateTools().ListMyTasksAsync(
            Args(("page_token", "pt_prev"), ("completed", true)),
            CancellationToken.None);

        capturedPageSize.Should().Be(PageSizes.TaskList, "分页尺寸是绑定层实现细节（模型不可见）");
        capturedPageToken.Should().Be("pt_prev");
        capturedCompleted.Should().BeTrue("completed 是模型可见参数（是否包含已完成任务）");

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;
        root.GetProperty("has_more").GetBoolean().Should().BeTrue();
        root.GetProperty("page_token").GetString().Should().Be("pt_next");
        root.GetProperty("items")[0].GetProperty("task_guid").GetString().Should().Be("g1");
        root.GetProperty("items")[0].GetProperty("summary").GetString().Should().Be("写周报");
    }

    [Fact]
    public async Task ListMyTasks_WithoutUserClient_ShouldReturnStructuredError()
    {
        var result = await CreateTools(withUserClient: false).ListMyTasksAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.list_my_tasks");
        result.ToString().Should().Contain("IFeishuUserV2Task", "用户客户端缺席必须给出可读原因（软缺席到工具粒度）");
        _userTaskClient.Verify(
            c => c.GetTasksPageListByIdAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ListMyTasks_FailureCode_ShouldReturnStructuredError()
    {
        _userTaskClient
            .Setup(c => c.GetTasksPageListByIdAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<ListTaskInfo> { Code = 99991663, Msg = "user token invalid" });

        var result = await CreateTools().ListMyTasksAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] task.list_my_tasks");
        result.ToString().Should().Contain("99991663");
    }
}
