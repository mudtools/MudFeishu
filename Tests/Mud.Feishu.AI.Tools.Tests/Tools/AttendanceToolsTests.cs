// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.AttendanceRemedys;
using Mud.Feishu.DataModels.AttendanceShifts;
using Mud.Feishu.DataModels.AttendanceStats;
using Mud.Feishu.DataModels.AttendanceUserDailyShifts;
using Mud.Feishu.DataModels.AttendanceUserFlows;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Attendance 考勤域工具（R7 / A5）：8 个工具的请求映射、日期口径、PII 纪律与补卡写面防护。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域最容易错的是日期口径</b>：<c>check_date_from/to</c> 是 <b>yyyyMMdd 整数</b>，
/// <c>check_time_from/to</c> 是<b>秒级时间戳字符串</b>——传错时飞书返回"无数据"而非报错，
/// 看起来像"员工没打卡"。故用例逐一锁定参数落点（<see cref="UserTasksQueryRequest"/> vs
/// <see cref="UserFlowsQueryRequest"/> 的字段名与类型）。
/// </para>
/// <para>
/// 第二组断言是 <b>PII 纪律</b>：<c>query_user_flow</c> / <c>get_flow</c> 的 Schema 描述必须含后果句，
/// 且 PII 集合由 <c>PiiToolDisciplineContractGuards</c> 与权威清单机械对齐。
/// </para>
/// </remarks>
public class AttendanceToolsTests
{
    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static AttendanceTools CreateTools(
        Mock<Mud.Feishu.IFeishuUserV1AttendanceUserTask>? userTask = null,
        Mock<Mud.Feishu.IFeishuTenantV1AttendanceUserFlows>? userFlows = null,
        Mock<Mud.Feishu.IFeishuTenantV1AttendanceUserDailyShifts>? dailyShifts = null,
        Mock<Mud.Feishu.IFeishuTenantV1AttendanceStats>? stats = null,
        Mock<Mud.Feishu.IFeishuTenantV1AttendanceRemedys>? remedys = null,
        Mock<Mud.Feishu.IFeishuTenantV1AttendanceShifts>? shifts = null)
        => new(
            Options.Create(AgentOptions()),
            userTask?.Object,
            userFlows?.Object,
            dailyShifts?.Object,
            stats?.Object,
            remedys?.Object,
            shifts?.Object);

    // ───────────────────── 契约面：身份 / 读写 / scope ─────────────────────

    [Fact]
    public void QueryMyFlow_Should_Be_User_Identity()
    {
        var contract = FeishuToolContracts.ByToolName[FeishuToolNames.AttendanceQueryMyFlow];

        contract.Identity.Should().Be("user",
            "attendance.query_my_flow 映射 user 令牌客户端（IFeishuUserV1AttendanceUserTask）：宿主须在 AllowedIdentities 放行 user");
        contract.IsWrite.Should().BeFalse();
        contract.RequiredScopes.Should().BeEquivalentTo(new[] { "attendance:task" });
    }

    [Fact]
    public void WriteTool_Should_Be_Write_And_HighRiskAware()
    {
        var contract = FeishuToolContracts.ByToolName[FeishuToolNames.AttendanceSubmitRemedy];

        contract.IsWrite.Should().BeTrue("补卡申请写入考勤系统（走审批流）——属写面，须 WriteAllowList + 授权门禁");
        contract.Identity.Should().Be("tenant");
        FeishuToolNames.IsWriteTool(FeishuToolNames.AttendanceSubmitRemedy).Should().BeTrue();
    }

    [Fact]
    public void PiiTools_Should_DeclareConsequencesInDescription()
    {
        foreach (var toolName in new[]
                 {
                     FeishuToolNames.AttendanceQueryUserFlow,
                     FeishuToolNames.AttendanceGetFlow,
                 })
        {
            var schema = FeishuToolSchemas.SchemaByToolName[toolName];
            schema.Should().Contain("PII", $"{toolName} 是 PII 敏感工具（DP-A5-1 档位 ①）");
            schema.Should().Contain("第三方模型上下文",
                $"{toolName} 的描述必须声明后果（结果进第三方模型上下文）");
            schema.Should().Contain("默认不启用", $"{toolName} 必须声明默认不启用（宿主显式加白名单）");
        }
    }

    // ───────────────────── 只读面：请求映射与日期口径 ─────────────────────

    [Fact]
    public async Task QueryMyFlow_Should_Pass_YyyyMmdd_DateRange()
    {
        UserTasksQueryRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuUserV1AttendanceUserTask>();
        client.Setup(c => c.QueryUserTaskAsync(
                It.IsAny<UserTasksQueryRequest>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<UserTasksQueryRequest, bool?, bool?, string, CancellationToken>((r, _, _, _, _) => captured = r)
            .ReturnsAsync(new FeishuApiResult<UserTasksQueryResult>
            {
                Code = 0,
                Data = new UserTasksQueryResult
                {
                    UserTaskResults =
                    [
                        new UserTask { ResultId = "r1", UserId = "ou_1", EmployeeName = "张三", Day = 20240115 },
                    ],
                },
            });

        var result = await CreateTools(userTask: client).QueryUserTaskAsync(
            Args(
                ("employee_type", "open_id"),
                ("user_id", "ou_1"),
                ("check_date_from", 20240101),
                ("check_date_to", 20240131)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("\"employee_name\":\"张三\"");
        captured.Should().NotBeNull();
        captured!.CheckDateFrom.Should().Be(20240101, "check_date_from 是 yyyyMMdd 整数（不是秒级时间戳）");
        captured.CheckDateTo.Should().Be(20240131);
        captured.UserIds.Should().BeEquivalentTo(new[] { "ou_1" });
    }

    [Fact]
    public async Task QueryUserFlow_Should_Pass_UserIds_And_TimeWindow()
    {
        UserFlowsQueryRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceUserFlows>();
        client.Setup(c => c.QueryUserFlowAsync(
                It.IsAny<UserFlowsQueryRequest>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<UserFlowsQueryRequest, bool?, string, CancellationToken>((r, _, _, _) => captured = r)
            .ReturnsAsync(new FeishuApiResult<UserFlowsQueryResult>
            {
                Code = 0,
                Data = new UserFlowsQueryResult
                {
                    UserFlowResults = [new UserFlowInfo { UserId = "ou_1", CheckTime = "1705312800" }],
                },
            });

        await CreateTools(userFlows: client).QueryUserFlowAsync(
            Args(
                ("employee_type", "user_id"),
                ("user_ids", new[] { "ou_1", "ou_2" }),
                ("check_time_from", "1705312800"),
                ("check_time_to", "1705400000")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.UserIds.Should().BeEquivalentTo(new[] { "ou_1", "ou_2" });
        captured.CheckTimeFrom.Should().Be("1705312800", "check_time_from 是秒级时间戳字符串");
        captured.CheckTimeTo.Should().Be("1705400000");
    }

    [Fact]
    public async Task GetFlow_Should_Default_EmployeeType_To_UserId()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceUserFlows>();
        client.Setup(c => c.GetUserFlowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<UserFlowInfo>
            {
                Code = 0,
                Data = new UserFlowInfo { UserId = "ou_1", LocationName = "公司正门" },
            });

        await CreateTools(userFlows: client).GetUserFlowAsync(
            Args(("flow_id", "flow_1")), CancellationToken.None);

        client.Verify(
            c => c.GetUserFlowAsync("flow_1", "user_id", It.IsAny<CancellationToken>()),
            Times.Once,
            "employee_type 缺省回落到 SDK 约定值 user_id（不传空串——空串会被服务端判为非法）");
    }

    [Fact]
    public async Task QueryDailyShift_Should_Pass_YyyyMmdd_Range()
    {
        QueryUserDailyShiftsRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceUserDailyShifts>();
        client.Setup(c => c.QueryUserDailyShiftAsync(
                It.IsAny<QueryUserDailyShiftsRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<QueryUserDailyShiftsRequest, string, CancellationToken>((r, _, _) => captured = r)
            .ReturnsAsync(new FeishuApiResult<UserDailyShiftsQueryResult>
            {
                Code = 0,
                Data = new UserDailyShiftsQueryResult
                {
                    UserDailyShifts = [new UserDailyShiftInfo { UserId = "ou_1", ShiftId = "s1", DayNo = 15 }],
                },
            });

        await CreateTools(dailyShifts: client).QueryUserDailyShiftAsync(
            Args(
                ("user_ids", new[] { "ou_1" }),
                ("check_date_from", 20240101),
                ("check_date_to", 20240131)),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CheckDateFrom.Should().Be(20240101);
        captured.CheckDateTo.Should().Be(20240131);
    }

    [Fact]
    public async Task QueryStatsData_Should_RejectUnknownStatsType_WithoutCallingDownstream()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceStats>();

        var result = await CreateTools(stats: client).QueryUserStatsDataAsync(
            Args(
                ("stats_type", "yearly"),
                ("start_date", 20240101),
                ("end_date", 20240131)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] attendance.query_stats_data");
        text.Should().Contain("day / month", "非法取值必须回填合法值清单（F-8 可执行下一步）");
        client.Invocations.Should().BeEmpty("参数校验失败不得触达下游");
    }

    [Fact]
    public async Task QueryUserStatsData_Should_Accept_ClosedSetValues()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceStats>();
        client.Setup(c => c.QueryUserStatsDataAsync(
                It.IsAny<QueryStatsDatasRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<QueryStatsDatasResult>
            {
                Code = 0,
                Data = new QueryStatsDatasResult
                {
                    UserDatas = [new UserStatsData { UserId = "ou_1", Name = "张三" }],
                },
            });

        var result = await CreateTools(stats: client).QueryUserStatsDataAsync(
            Args(
                ("stats_type", "month"),
                ("start_date", 20240101),
                ("end_date", 20240131),
                ("user_ids", new[] { "ou_1" })),
            CancellationToken.None);

        result.ToString().Should().Contain("\"name\":\"张三\"");
        client.Verify(
            c => c.QueryUserStatsDataAsync(
                It.Is<QueryStatsDatasRequest>(r => r.StatsType == "month" && r.UserIds!.Length == 1),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetShift_Should_Project_PunchTimeRules()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceShifts>();
        client.Setup(c => c.GetShiftByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetAttendanceShiftsResult>
            {
                Code = 0,
                Data = new GetAttendanceShiftsResult
                {
                    ShiftId = "s1",
                    ShiftName = "早班",
                    PunchTimes = 2,
                    PunchTimeRules = [new PunchTimeRule { OnTime = "09:00", OffTime = "18:00" }],
                },
            });

        var result = await CreateTools(shifts: client).GetShiftByIdAsync(
            Args(("shift_id", "s1")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("\"shift_name\":\"早班\"");
        text.Should().Contain("\"on_time\":\"09:00\"");
    }

    // ───────────────────── 写面：补卡 ─────────────────────

    [Fact]
    public async Task SubmitRemedy_DryRun_Should_Not_Call_Downstream_And_Return_Route()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceRemedys>();

        var result = await CreateTools(remedys: client).CreateUserTaskRemedyAsync(
            Args(
                ("user_id", "ou_1"),
                ("remedy_date", 20240115),
                ("remedy_time", "09:00"),
                ("punch_no", 1),
                ("work_type", 1),
                ("reason", "忘记打卡"),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[dry_run]");
        text.Should().Contain("/open-apis/attendance/v1/user_task_remedy");
        text.Should().Contain("未调用下游接口");
        client.Invocations.Should().BeEmpty("dry_run 是「只预演」的硬约束——触达下游即缺陷");
    }

    [Fact]
    public async Task SubmitRemedy_Should_Pass_All_Required_Fields()
    {
        AttendanceRemedysRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceRemedys>();
        client.Setup(c => c.CreateUserTaskRemedyAsync(
                It.IsAny<AttendanceRemedysRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceRemedysRequest, string, CancellationToken>((r, _, _) => captured = r)
            .ReturnsAsync(new FeishuApiResult<AttendanceRemedysResult>
            {
                Code = 0,
                Data = new AttendanceRemedysResult
                {
                    UserRemedy = new UserTaskRemedyInfo { UserId = "ou_1", RemedyDate = 20240115, Status = 0 },
                },
            });

        var result = await CreateTools(remedys: client).CreateUserTaskRemedyAsync(
            Args(
                ("user_id", "ou_1"),
                ("remedy_date", 20240115),
                ("remedy_time", "09:00"),
                ("punch_no", 1),
                ("work_type", 1),
                ("reason", "忘记打卡")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.RemedyDate.Should().Be(20240115, "remedy_date 是 yyyyMMdd 整数");
        captured.RemedyTime.Should().Be("09:00", "remedy_time 是 HH:mm 字符串");
        captured.PunchNo.Should().Be(1);
        captured.WorkType.Should().Be(1);
        result.ToString().Should().Contain("\"submitted\":true", "写入成功后回填可识别的完成信号");
    }

    [Fact]
    public async Task SubmitRemedy_EmptyRemedyTime_Should_Return_StructuredError()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AttendanceRemedys>();

        var result = await CreateTools(remedys: client).CreateUserTaskRemedyAsync(
            Args(
                ("user_id", "ou_1"),
                ("remedy_date", 20240115),
                ("remedy_time", "  "),
                ("punch_no", 1),
                ("work_type", 1),
                ("reason", "忘记打卡")),
            CancellationToken.None);

        // 参数层双保险：Schema 声明 remedy_time 必填 ⇒ 空白值在**解包层**即被判为缺失；
        // 执行器另有一道"空值不得下发"的校验（防未来把必填改成可选时静默下发空串）。
        var text = result.ToString()!;
        text.Should().Contain("[tool_error] attendance.submit_remedy");
        text.Should().Contain("remedy_time");
        client.Invocations.Should().BeEmpty();
    }

    // ───────────────────── 软依赖：客户端缺席 → 可执行提示 ─────────────────────

    [Fact]
    public async Task ClientAbsent_Should_FailFast_With_Actionable_Error()
    {
        var result = await CreateTools().QueryUserFlowAsync(
            Args(
                ("employee_type", "user_id"),
                ("user_ids", new[] { "ou_1" }),
                ("check_time_from", "1705312800"),
                ("check_time_to", "1705400000")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] attendance.query_user_flow");
        text.Should().Contain("AddAttendanceApi",
            "客户端缺席必须给出可操作提示（宿主要启用哪个 API），而不是裸 NullReferenceException");
    }
}
