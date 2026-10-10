// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Attendance 考勤工具执行器（R7 / A5：8 个工具，只读 7 + 写 1）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：Attendance 客户端为可空注入——宿主未启用考勤 API 时执行器软缺席。
/// </para>
/// <para>
/// <b>PII（DP-A5-1 档位 ①）</b>：<c>attendance.query_user_flow</c> / <c>attendance.get_flow</c>
/// 属 PII 敏感工具——策展但默认不启用，需宿主显式加白名单。
/// </para>
/// </remarks>
internal sealed class AttendanceTools(
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

    [FeishuToolHandler(typeof(IFeishuUserAttendanceQueryMyFlowTool))]
    public Task<FeishuToolResult> QueryMyFlowAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryMyFlow);
        return executor.RunAsync(async () =>
        {
            var args = AttendanceQueryMyFlowArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await RequireUserTask(executor.ToolName)
                .QueryUserTaskAsync(args.CheckTimeFrom, args.CheckTimeTo, args.EmployeeType, args.UserId, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var items = new JsonArray();
                foreach (var item in data.UserTasks ?? [])
                {
                    items.AddNode(new JsonObject
                    {
                        ["flow_id"] = item.FlowId,
                        ["check_time"] = item.CheckTime,
                        ["location_name"] = item.LocationName,
                    });
                }

                return new JsonObject
                {
                    ["user_id"] = args.UserId,
                    ["items"] = items,
                    ["has_more"] = data.HasMore,
                    ["page_token"] = data.PageToken,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantAttendanceQueryUserFlowTool))]
    public Task<FeishuToolResult> QueryUserFlowAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryUserFlow);
        return executor.RunAsync(async () =>
        {
            var args = AttendanceQueryUserFlowArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await RequireUserFlows(executor.ToolName)
                .QueryUserFlowAsync(args.CheckTimeFrom, args.CheckTimeTo, args.EmployeeType, [.. args.UserIds], page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var items = new JsonArray();
                foreach (var item in data.UserFlowList ?? [])
                {
                    items.AddNode(new JsonObject
                    {
                        ["flow_id"] = item.FlowId,
                        ["user_id"] = item.UserId,
                        ["check_time"] = item.CheckTime,
                        ["location_name"] = item.LocationName,
                    });
                }

                return new JsonObject
                {
                    ["items"] = items,
                    ["has_more"] = data.HasMore,
                    ["page_token"] = data.PageToken,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantAttendanceGetFlowTool))]
    public Task<FeishuToolResult> GetFlowAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceGetFlow);
        return executor.RunAsync(async () =>
        {
            var args = AttendanceGetFlowArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await RequireUserFlows(executor.ToolName)
                .GetUserFlowAsync(args.FlowId, args.EmployeeType, args.UserId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                if (data is null)
                {
                    return new JsonObject
                    {
                        ["found"] = false,
                        ["flow_id"] = args.FlowId,
                        ["message"] = "未找到打卡记录。",
                    };
                }

                return new JsonObject
                {
                    ["found"] = true,
                    ["flow_id"] = data.FlowId,
                    ["user_id"] = data.UserId,
                    ["check_time"] = data.CheckTime,
                    ["location_name"] = data.LocationName,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantAttendanceQueryDailyShiftTool))]
    public Task<FeishuToolResult> QueryDailyShiftAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryDailyShift);
        return executor.RunAsync(async () =>
        {
            var args = AttendanceQueryDailyShiftArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await RequireDailyShifts(executor.ToolName)
                .QueryUserDailyShiftAsync(args.UserId, args.ShiftDate, employee_type: args.EmployeeType, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var items = new JsonArray();
                foreach (var item in data.UserDailyShifts ?? [])
                {
                    items.AddNode(new JsonObject
                    {
                        ["shift_id"] = item.ShiftId,
                        ["shift_name"] = item.ShiftName,
                        ["start_time"] = item.StartTime,
                        ["end_time"] = item.EndTime,
                    });
                }

                return new JsonObject
                {
                    ["user_id"] = args.UserId,
                    ["shift_date"] = args.ShiftDate,
                    ["items"] = items,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantAttendanceQueryStatsDataTool))]
    public Task<FeishuToolResult> QueryStatsDataAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryStatsData);
        return executor.RunAsync(async () =>
        {
            var args = AttendanceQueryStatsDataArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await RequireStats(executor.ToolName)
                .QueryUserStatsDataAsync(args.StatsType, args.StartTime, args.EndTime, [.. args.UserIds], employee_type: args.EmployeeType, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var items = new JsonArray();
                foreach (var item in data.UserStatsDatas ?? [])
                {
                    items.AddNode(new JsonObject
                    {
                        ["user_id"] = item.UserId,
                        ["stats_type"] = item.StatsType,
                        ["stats_value"] = item.StatsValue,
                    });
                }

                return new JsonObject
                {
                    ["stats_type"] = args.StatsType,
                    ["items"] = items,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantAttendanceQueryMyRemedysTool))]
    public Task<FeishuToolResult> QueryMyRemedysAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceQueryMyRemedys);
        return executor.RunAsync(async () =>
        {
            var args = AttendanceQueryMyRemedysArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await RequireRemedys(executor.ToolName)
                .QueryUserTaskRemedyAsync(args.CheckTimeFrom, args.CheckTimeTo, args.UserId, employee_type: args.EmployeeType, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var items = new JsonArray();
                foreach (var item in data.UserRemedys ?? [])
                {
                    items.AddNode(new JsonObject
                    {
                        ["remedy_id"] = item.RemedyId,
                        ["remedy_time"] = item.RemedyTime,
                        ["remedy_type"] = item.RemedyType,
                    });
                }

                return new JsonObject
                {
                    ["user_id"] = args.UserId,
                    ["items"] = items,
                    ["has_more"] = data.HasMore,
                    ["page_token"] = data.PageToken,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantAttendanceSubmitRemedyTool))]
    public Task<FeishuToolResult> SubmitRemedyAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceSubmitRemedy);
        return executor.RunAsync(async () =>
        {
            var args = AttendanceSubmitRemedyArgs.Unpack(arguments);

            // 补卡申请：非幂等——重复提交会造成重复申请。
            var request = new Mud.Feishu.DataModels.Attendance.CreateUserTaskRemedyRequest
            {
                UserId = args.UserId,
                RemedyTime = args.RemedyTime,
                RemedyType = args.RemedyType,
                EmployeeType = args.EmployeeType,
                Reason = args.Reason,
            };

            var outcome = FeishuApiResultReader.Read(await RequireRemedys(executor.ToolName)
                .CreateUserTaskRemedyAsync(request, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                return new JsonObject
                {
                    ["user_id"] = args.UserId,
                    ["remedy_id"] = data.RemedyId,
                    ["submitted"] = true,
                };
            });
        });
    }

    [FeishuToolHandler(typeof(IFeishuTenantAttendanceGetShiftTool))]
    public Task<FeishuToolResult> GetShiftAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.AttendanceGetShift);
        return executor.RunAsync(async () =>
        {
            var args = AttendanceGetShiftArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await RequireShifts(executor.ToolName)
                .GetShiftByIdAsync(args.ShiftId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data =>
            {
                var shift = data.Shift;
                if (shift is null)
                {
                    return new JsonObject
                    {
                        ["found"] = false,
                        ["shift_id"] = args.ShiftId,
                        ["message"] = "未找到班次规则。",
                    };
                }

                return new JsonObject
                {
                    ["found"] = true,
                    ["shift_id"] = shift.ShiftId,
                    ["shift_name"] = shift.ShiftName,
                };
            });
        });
    }

    private Mud.Feishu.IFeishuUserV1AttendanceUserTask RequireUserTask(string toolName)
        => _userTaskClient ?? throw new ArgumentException($"{toolName} 需要 IFeishuUserV1AttendanceUserTask——宿主须启用考勤（Attendance）API");

    private Mud.Feishu.IFeishuTenantV1AttendanceUserFlows RequireUserFlows(string toolName)
        => _userFlowsClient ?? throw new ArgumentException($"{toolName} 需要 IFeishuTenantV1AttendanceUserFlows——宿主须启用考勤（Attendance）API");

    private Mud.Feishu.IFeishuTenantV1AttendanceUserDailyShifts RequireDailyShifts(string toolName)
        => _dailyShiftsClient ?? throw new ArgumentException($"{toolName} 需要 IFeishuTenantV1AttendanceUserDailyShifts——宿主须启用考勤（Attendance）API");

    private Mud.Feishu.IFeishuTenantV1AttendanceStats RequireStats(string toolName)
        => _statsClient ?? throw new ArgumentException($"{toolName} 需要 IFeishuTenantV1AttendanceStats——宿主须启用考勤（Attendance）API");

    private Mud.Feishu.IFeishuTenantV1AttendanceRemedys RequireRemedys(string toolName)
        => _remedysClient ?? throw new ArgumentException($"{toolName} 需要 IFeishuTenantV1AttendanceRemedys——宿主须启用考勤（Attendance）API");

    private Mud.Feishu.IFeishuTenantV1AttendanceShifts RequireShifts(string toolName)
        => _shiftsClient ?? throw new ArgumentException($"{toolName} 需要 IFeishuTenantV1AttendanceShifts——宿主须启用考勤（Attendance）API");
}
