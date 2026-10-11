// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.AttendanceRemedys;
using Mud.Feishu.DataModels.AttendanceShifts;
using Mud.Feishu.DataModels.AttendanceStats;
using Mud.Feishu.DataModels.AttendanceUserDailyShifts;
using Mud.Feishu.DataModels.AttendanceUserFlows;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Attendance 考勤工具执行器（R7 / A5：8 个工具，只读 7 + 写 1）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：6 个考勤客户端均为可空注入——宿主未启用考勤 API（<c>AddAttendanceApi</c>）时，
/// 调用会得到"须启用考勤 API"的<b>可执行</b>结构化错误，不影响其它域工具。
/// </para>
/// <para>
/// <b>PII（DP-A5-1 档位 ①）</b>：<c>attendance.query_user_flow</c> / <c>attendance.get_flow</c>
/// 属 PII 敏感工具——策展但默认不启用，需宿主显式加白名单；描述已含后果句（结果进第三方模型上下文）。
/// </para>
/// <para>
/// <b>日期口径</b>：<c>check_date_from/to</c>、<c>start_date/end_date</c>、<c>remedy_date</c>
/// 为 <b>yyyyMMdd 整数</b>（如 20240101）；<c>check_time_from/to</c> 为<b>秒级时间戳字符串</b>。
/// 两种口径混用会被飞书静默判为"无数据"，故接口层描述逐一写明。
/// </para>
/// <para>
/// <b>二进制防线（A10）</b>：<c>IFeishuV1AttendanceUserSettings_Tenant.DownloadFileAsync</c>
/// 返回 <c>byte[]</c> → 不策展。
/// </para>
/// </remarks>
internal sealed class AttendanceTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuUserV1AttendanceUserTask? userTaskClient = null,
    Mud.Feishu.IFeishuTenantV1AttendanceUserFlows? userFlowsClient = null,
    Mud.Feishu.IFeishuTenantV1AttendanceUserDailyShifts? dailyShiftsClient = null,
    Mud.Feishu.IFeishuTenantV1AttendanceStats? statsClient = null,
    Mud.Feishu.IFeishuTenantV1AttendanceRemedys? remedysClient = null,
    Mud.Feishu.IFeishuTenantV1AttendanceShifts? shiftsClient = null)
{
    private readonly Mud.Feishu.IFeishuUserV1AttendanceUserTask? _userTaskClient = userTaskClient;
    private readonly Mud.Feishu.IFeishuTenantV1AttendanceUserFlows? _userFlowsClient = userFlowsClient;
    private readonly Mud.Feishu.IFeishuTenantV1AttendanceUserDailyShifts? _dailyShiftsClient = dailyShiftsClient;
    private readonly Mud.Feishu.IFeishuTenantV1AttendanceStats? _statsClient = statsClient;
    private readonly Mud.Feishu.IFeishuTenantV1AttendanceRemedys? _remedysClient = remedysClient;
    private readonly Mud.Feishu.IFeishuTenantV1AttendanceShifts? _shiftsClient = shiftsClient;
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    // ─────────────────────────── 打卡流水面（3 个） ───────────────────────────

    /// <summary>attendance.query_my_flow：查询本人打卡结果（user 身份）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserAttendanceQueryMyFlowTool))]
    public Task<FeishuToolResult> QueryUserTaskAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryMyFlow, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _userTaskClient
                ?? throw new ArgumentException(MissingClient(nameof(Mud.Feishu.IFeishuUserV1AttendanceUserTask), "用户令牌"));

            var args = AttendanceQueryMyFlowArgs.Unpack(arguments);

            var request = new UserTasksQueryRequest
            {
                UserIds = [args.UserId],
                CheckDateFrom = args.CheckDateFrom,
                CheckDateTo = args.CheckDateTo,
                NeedOvertimeResult = args.NeedOvertimeResult,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .QueryUserTaskAsync(request, employee_type: args.EmployeeType, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectUserTasks);
        });
    }

    /// <summary>attendance.query_user_flow：批量查询他人打卡流水（tenant 身份；PII 敏感）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAttendanceQueryUserFlowTool))]
    public Task<FeishuToolResult> QueryUserFlowAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryUserFlow, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _userFlowsClient
                ?? throw new ArgumentException(MissingClient(nameof(Mud.Feishu.IFeishuTenantV1AttendanceUserFlows), "租户令牌"));

            var args = AttendanceQueryUserFlowArgs.Unpack(arguments);

            var request = new UserFlowsQueryRequest
            {
                UserIds = [.. args.UserIds],
                CheckTimeFrom = args.CheckTimeFrom,
                CheckTimeTo = args.CheckTimeTo,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .QueryUserFlowAsync(
                    request,
                    include_terminated_user: args.IncludeTerminatedUser,
                    employee_type: args.EmployeeType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectUserFlows);
        });
    }

    /// <summary>attendance.get_flow：单条打卡流水详情（tenant 身份；PII 敏感）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAttendanceGetFlowTool))]
    public Task<FeishuToolResult> GetUserFlowAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceGetFlow, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _userFlowsClient
                ?? throw new ArgumentException(MissingClient(nameof(Mud.Feishu.IFeishuTenantV1AttendanceUserFlows), "租户令牌"));

            var args = AttendanceGetFlowArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetUserFlowAsync(
                    args.FlowId,
                    employee_type: args.EmployeeType ?? DefaultEmployeeType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectSingleFlow);
        });
    }

    // ─────────────────────────── 排班 / 统计 / 班次面（4 个） ───────────────────────────

    /// <summary>attendance.query_daily_shift：查询排班表（tenant 身份）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAttendanceQueryDailyShiftTool))]
    public Task<FeishuToolResult> QueryUserDailyShiftAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryDailyShift, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _dailyShiftsClient
                ?? throw new ArgumentException(MissingClient(nameof(Mud.Feishu.IFeishuTenantV1AttendanceUserDailyShifts), "租户令牌"));

            var args = AttendanceQueryDailyShiftArgs.Unpack(arguments);

            var request = new QueryUserDailyShiftsRequest
            {
                UserIds = [.. args.UserIds],
                CheckDateFrom = args.CheckDateFrom,
                CheckDateTo = args.CheckDateTo,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .QueryUserDailyShiftAsync(
                    request,
                    employee_type: args.EmployeeType ?? DefaultEmployeeType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectDailyShifts);
        });
    }

    /// <summary>attendance.query_stats_data：查询考勤统计报表（tenant 身份）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAttendanceQueryStatsDataTool))]
    public Task<FeishuToolResult> QueryUserStatsDataAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryStatsData, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _statsClient
                ?? throw new ArgumentException(MissingClient(nameof(Mud.Feishu.IFeishuTenantV1AttendanceStats), "租户令牌"));

            var args = AttendanceQueryStatsDataArgs.Unpack(arguments);

            ValidateStatsType(args.StatsType);

            var request = new QueryStatsDatasRequest
            {
                StatsType = args.StatsType,
                StartDate = args.StartDate,
                EndDate = args.EndDate,
                UserIds = args.UserIds is { Length: > 0 } ? [.. args.UserIds] : null,
                // Locale 是**非空** string（SDK 默认空串会走服务端默认语言），仅在显式传入时覆盖。
                Locale = args.Locale ?? string.Empty,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .QueryUserStatsDataAsync(
                    request,
                    employee_type: args.EmployeeType ?? DefaultEmployeeType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectStatsDatas);
        });
    }

    /// <summary>attendance.query_my_remedys：查询补卡记录（tenant 身份）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAttendanceQueryMyRemedysTool))]
    public Task<FeishuToolResult> QueryUserTaskRemedyAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryMyRemedys, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _remedysClient
                ?? throw new ArgumentException(MissingClient(nameof(Mud.Feishu.IFeishuTenantV1AttendanceRemedys), "租户令牌"));

            var args = AttendanceQueryMyRemedysArgs.Unpack(arguments);

            var request = new QueryUserRemedysRequest
            {
                UserIds = [.. args.UserIds],
                CheckTimeFrom = args.CheckTimeFrom,
                CheckTimeTo = args.CheckTimeTo,
                CheckDateType = args.CheckDateType,
                Status = args.Status,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .QueryUserTaskRemedyAsync(
                    request,
                    employee_type: args.EmployeeType ?? DefaultEmployeeType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectRemedies);
        });
    }

    /// <summary>attendance.get_shift：按 ID 获取班次规则详情（tenant 身份）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAttendanceGetShiftTool))]
    public Task<FeishuToolResult> GetShiftByIdAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceGetShift, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _shiftsClient
                ?? throw new ArgumentException(MissingClient(nameof(Mud.Feishu.IFeishuTenantV1AttendanceShifts), "租户令牌"));

            var args = AttendanceGetShiftArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetShiftByIdAsync(args.ShiftId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectShift);
        });
    }

    // ─────────────────────────── 写面（1 个：补卡申请） ───────────────────────────

    /// <summary>attendance.submit_remedy：提交补卡申请（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantAttendanceSubmitRemedyTool))]
    public Task<FeishuToolResult> CreateUserTaskRemedyAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceSubmitRemedy);
        return executor.RunAsync(async () =>
        {
            var client = _remedysClient
                ?? throw new ArgumentException(MissingClient(nameof(Mud.Feishu.IFeishuTenantV1AttendanceRemedys), "租户令牌"));

            var args = AttendanceSubmitRemedyArgs.Unpack(arguments);

            if (string.IsNullOrWhiteSpace(args.RemedyTime))
            {
                throw new ArgumentException("remedy_time 不能为空（格式 HH:mm，如 09:00）");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/attendance/v1/user_task_remedy",
                    ToolDryRun.IdempotencyNote(null),
                    ("user_id", args.UserId.Length),
                    ("remedy_date", 8),
                    ("remedy_time", args.RemedyTime.Length),
                    ("reason", args.Reason.Length)));
            }

            var request = new AttendanceRemedysRequest
            {
                UserId = args.UserId,
                RemedyDate = args.RemedyDate,
                RemedyTime = args.RemedyTime,
                PunchNo = args.PunchNo,
                WorkType = args.WorkType,
                Reason = args.Reason,
                Time = args.Time,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .CreateUserTaskRemedyAsync(
                    request,
                    employee_type: args.EmployeeType ?? DefaultEmployeeType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, ProjectSingleRemedy);
        });
    }

    // ─────────────────────────── 投影 ───────────────────────────

    /// <summary>query_my_flow 投影：items（result_id/user_id/employee_name/day + 上下班时间与结果）。</summary>
    private static JsonObject ProjectUserTasks(UserTasksQueryResult data)
    {
        var items = new JsonArray();
        foreach (var task in data.UserTaskResults ?? [])
        {
            var records = new JsonArray();
            foreach (var record in task.Records ?? [])
            {
                records.AddNode(new JsonObject
                {
                    ["check_in_time"] = record.CheckInRecord?.CheckTime,
                    ["check_in_result"] = record.CheckInResult,
                    ["check_out_time"] = record.CheckOutRecord?.CheckTime,
                    ["check_out_result"] = record.CheckOutResult,
                });
            }

            items.AddNode(new JsonObject
            {
                ["result_id"] = task.ResultId,
                ["user_id"] = task.UserId,
                ["employee_name"] = task.EmployeeName,
                ["day"] = task.Day,
                ["shift_id"] = task.ShiftId,
                ["records"] = records,
            });
        }

        var envelope = new JsonObject
        {
            ["items"] = items,
            ["count"] = items.Count,
        };
        AddIfAny(envelope, "invalid_user_ids", data.InvalidUserIds);
        AddIfAny(envelope, "unauthorized_user_ids", data.UnauthorizedUserIds);
        return envelope;
    }

    /// <summary>query_user_flow 投影：items（user_id/check_time/location_name/type/check_result）。</summary>
    private static JsonObject ProjectUserFlows(UserFlowsQueryResult data)
    {
        var items = new JsonArray();
        foreach (var flow in data.UserFlowResults ?? [])
        {
            items.AddNode(ProjectFlow(flow));
        }

        return new JsonObject
        {
            ["items"] = items,
            ["count"] = items.Count,
        };
    }

    /// <summary>get_flow 投影：单条打卡流水（含 record_id 供后续引用）。</summary>
    private static JsonObject ProjectSingleFlow(UserFlowInfo data) => ProjectFlow(data);

    /// <summary>打卡流水单条投影（白名单：时间/地点/类型/结果，不含照片 URL 与设备指纹）。</summary>
    private static JsonObject ProjectFlow(UserFlow flow) => new()
    {
        ["record_id"] = flow.RecordId,
        ["user_id"] = flow.UserId,
        ["check_time"] = flow.CheckTime,
        ["location_name"] = flow.LocationName,
        ["type"] = flow.Type,
        ["check_result"] = flow.CheckResult,
        ["comment"] = flow.Comment,
    };

    /// <summary>query_daily_shift 投影：items（user_id/group_id/shift_id/month/day_no）。</summary>
    private static JsonObject ProjectDailyShifts(UserDailyShiftsQueryResult data)
    {
        var items = new JsonArray();
        foreach (var shift in data.UserDailyShifts ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["user_id"] = shift.UserId,
                ["group_id"] = shift.GroupId,
                ["shift_id"] = shift.ShiftId,
                ["month"] = shift.Month,
                ["day_no"] = shift.DayNo,
            });
        }

        return new JsonObject
        {
            ["items"] = items,
            ["count"] = items.Count,
        };
    }

    /// <summary>query_stats_data 投影：items（user_id/name + 单元格 code/value）。</summary>
    private static JsonObject ProjectStatsDatas(QueryStatsDatasResult data)
    {
        var items = new JsonArray();
        foreach (var user in data.UserDatas ?? [])
        {
            var cells = new JsonArray();
            foreach (var cell in user.Datas ?? [])
            {
                cells.AddNode(new JsonObject
                {
                    ["code"] = cell.Code,
                    ["title"] = cell.Title,
                    ["value"] = cell.Value,
                });
            }

            items.AddNode(new JsonObject
            {
                ["user_id"] = user.UserId,
                ["name"] = user.Name,
                ["cells"] = cells,
            });
        }

        var envelope = new JsonObject
        {
            ["items"] = items,
            ["count"] = items.Count,
        };
        AddIfAny(envelope, "invalid_user_ids", data.InvalidUserList);
        return envelope;
    }

    /// <summary>query_my_remedys 投影：items（补卡记录白名单字段）。</summary>
    private static JsonObject ProjectRemedies(QueryUserRemedysResult data)
    {
        var items = new JsonArray();
        foreach (var remedy in data.UserRemedys ?? [])
        {
            items.AddNode(ProjectRemedy(remedy));
        }

        return new JsonObject
        {
            ["items"] = items,
            ["count"] = items.Count,
        };
    }

    /// <summary>submit_remedy 投影：单条补卡记录（含 approval_id 供审批链路引用）。</summary>
    private static JsonObject ProjectSingleRemedy(AttendanceRemedysResult data)
    {
        var remedy = data.UserRemedy;
        if (remedy is null)
        {
            return new JsonObject
            {
                ["submitted"] = false,
                ["message"] = "飞书未返回补卡记录载荷，请用 attendance.query_my_remedys 复核是否已写入。",
            };
        }

        var envelope = ProjectRemedy(remedy);
        envelope["submitted"] = true;
        return envelope;
    }

    /// <summary>补卡记录单条投影（白名单：日期/时间/类型/状态/原因/审批号）。</summary>
    private static JsonObject ProjectRemedy(UserTaskRemedyInfo remedy) => new()
    {
        ["user_id"] = remedy.UserId,
        ["remedy_date"] = remedy.RemedyDate,
        ["remedy_time"] = remedy.RemedyTime,
        ["punch_no"] = remedy.PunchNo,
        ["work_type"] = remedy.WorkType,
        ["status"] = remedy.Status,
        ["reason"] = remedy.Reason,
        ["approval_id"] = remedy.ApprovalId,
        ["create_time"] = remedy.CreateTime,
    };

    /// <summary>get_shift 投影：班次 ID/名称/打卡次数/弹性 + 上下班时间规则。</summary>
    private static JsonObject ProjectShift(GetAttendanceShiftsResult data)
    {
        var rules = new JsonArray();
        foreach (var rule in data.PunchTimeRules ?? [])
        {
            rules.AddNode(new JsonObject
            {
                ["on_time"] = rule.OnTime,
                ["off_time"] = rule.OffTime,
                ["late_minutes_as_late"] = rule.LateMinutesAsLate,
                ["early_minutes_as_early"] = rule.EarlyMinutesAsEarly,
                ["no_need_on"] = rule.NoNeedOn,
                ["no_need_off"] = rule.NoNeedOff,
            });
        }

        return new JsonObject
        {
            ["shift_id"] = data.ShiftId,
            ["shift_name"] = data.ShiftName,
            ["punch_times"] = data.PunchTimes,
            ["is_flexible"] = data.IsFlexible,
            ["flexible_minutes"] = data.FlexibleMinutes,
            ["day_type"] = data.DayType,
            ["punch_time_rules"] = rules,
        };
    }

    // ─────────────────────────── 校验辅助 ───────────────────────────

    /// <summary>默认员工 ID 类型（SDK 的 <c>Consts.User_Id_Type</c> 同值；模型未传时兜底）。</summary>
    private const string DefaultEmployeeType = "user_id";

    /// <summary>统计类型的取值闭集（官方：day / month）。</summary>
    private static readonly string[] ValidStatsTypes = ["day", "month"];

    /// <summary>校验 stats_type 落在闭集内（非法值 → 结构化 invalid_args，附全部合法值）。</summary>
    private static void ValidateStatsType(string statsType)
    {
        if (!ValidStatsTypes.Contains(statsType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"stats_type '{statsType}' 不合法。可用值：{string.Join(" / ", ValidStatsTypes)}");
        }
    }

    /// <summary>软依赖缺席时的可执行提示（宿主启用哪个 API）。</summary>
    private static string MissingClient(string clientName, string tokenKind)
        => $"本工具需要 {clientName}（{tokenKind}）——宿主须启用考勤 API（AddAttendanceApi）；"
           + "未启用时请改用其它域工具（软缺席，不影响其它域）";

    /// <summary>非空数组才写入信封（保持"缺省字段不出现"的紧凑形态）。</summary>
    private static void AddIfAny(JsonObject envelope, string key, string[]? values)
    {
        if (values is { Length: > 0 })
        {
            envelope[key] = new JsonArray([.. values.Select(static v => (JsonNode?)v)]);
        }
    }
}
