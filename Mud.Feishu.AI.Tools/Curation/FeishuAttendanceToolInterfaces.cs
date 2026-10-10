// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Attendance 考勤（R7 / A5 新域） ───────────────────────────

/// <summary>
/// 工具接口：attendance.query_my_flow（映射 <c>IFeishuUserV1AttendanceUserTask.QueryUserTaskAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A5："我的打卡记录"——user 身份只能查自己。宿主须在 <c>AllowedIdentities</c> 放行 <c>user</c>。
/// </remarks>
[FeishuTool("attendance.query_my_flow",
    Description = "查询当前用户的考勤打卡记录（user 身份，只能查自己）。需 attendance:task 权限。宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuUserV1AttendanceUserTask) + "." + nameof(IFeishuUserV1AttendanceUserTask.QueryUserTaskAsync))]
public interface IFeishuUserAttendanceQueryMyFlowTool
{
    Task<string> QueryUserTaskAsync(
        [ToolParameter("employee_type", "员工 ID 类型：open_id / user_id / union_id", Required = true)] string employee_type,
        [ToolParameter("user_id", "用户 ID（与 employee_type 对应）", Required = true)] string user_id,
        [ToolParameter("check_time_from", "打卡起始时间（Unix 秒）", Required = true)] string check_time_from,
        [ToolParameter("check_time_to", "打卡结束时间（Unix 秒）", Required = true)] string check_time_to,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.query_user_flow（映射 <c>IFeishuTenantV1AttendanceUserFlows.QueryUserFlowAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A5：⚠️ <b>PII 敏感</b>——按 user_id + 时间窗查他人打卡。默认不启用，需宿主显式加白名单。
/// 工具描述含后果句：结果将进入第三方模型上下文。
/// </remarks>
[FeishuTool("attendance.query_user_flow",
    Description = "按用户 ID 批量查询考勤打卡记录（tenant 身份，可查他人）。⚠️ PII 敏感：考勤数据属员工隐私，结果将进入第三方模型上下文，请确认合规要求。默认不启用，需宿主显式加白名单。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuTenantV1AttendanceUserFlows) + "." + nameof(IFeishuTenantV1AttendanceUserFlows.QueryUserFlowAsync))]
public interface IFeishuTenantAttendanceQueryUserFlowTool
{
    Task<string> QueryUserFlowAsync(
        [ToolParameter("employee_type", "员工 ID 类型：open_id / user_id / union_id", Required = true)] string employee_type,
        [ToolParameter("user_ids", "用户 ID 数组", Required = true)] string[] user_ids,
        [ToolParameter("check_time_from", "打卡起始时间（Unix 秒）", Required = true)] string check_time_from,
        [ToolParameter("check_time_to", "打卡结束时间（Unix 秒）", Required = true)] string check_time_to,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.get_flow（映射 <c>IFeishuTenantV1AttendanceUserFlows.GetUserFlowAsync</c>）。
/// </summary>
[FeishuTool("attendance.get_flow",
    Description = "获取单条考勤打卡详情（tenant 身份）。⚠️ PII 敏感：考勤数据属员工隐私。默认不启用。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuTenantV1AttendanceUserFlows) + "." + nameof(IFeishuTenantV1AttendanceUserFlows.GetUserFlowAsync))]
public interface IFeishuTenantAttendanceGetFlowTool
{
    Task<string> GetUserFlowAsync(
        [ToolParameter("flow_id", "打卡记录 ID", Required = true)] string flow_id,
        [ToolParameter("employee_type", "员工 ID 类型：open_id / user_id / union_id", Required = true)] string employee_type,
        [ToolParameter("user_id", "用户 ID", Required = true)] string user_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.query_daily_shift（映射 <c>IFeishuTenantV1AttendanceUserDailyShifts.QueryUserDailyShiftAsync</c>）。
/// </summary>
[FeishuTool("attendance.query_daily_shift",
    Description = "查询用户排班信息（某天几点上班等，tenant 身份）。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuTenantV1AttendanceUserDailyShifts) + "." + nameof(IFeishuTenantV1AttendanceUserDailyShifts.QueryUserDailyShiftAsync))]
public interface IFeishuTenantAttendanceQueryDailyShiftTool
{
    Task<string> QueryUserDailyShiftAsync(
        [ToolParameter("user_id", "用户 ID", Required = true)] string user_id,
        [ToolParameter("shift_date", "查询日期（yyyy-MM-dd）", Required = true)] string shift_date,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 open_id）")] string? employee_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.query_stats_data（映射 <c>IFeishuTenantV1AttendanceStats.QueryUserStatsDataAsync</c>）。
/// </summary>
[FeishuTool("attendance.query_stats_data",
    Description = "查询考勤统计报表数据（tenant 身份）。需 attendance:stats 权限。",
    RequiredScopes = ["attendance:stats"],
    Source = nameof(IFeishuTenantV1AttendanceStats) + "." + nameof(IFeishuTenantV1AttendanceStats.QueryUserStatsDataAsync))]
public interface IFeishuTenantAttendanceQueryStatsDataTool
{
    Task<string> QueryUserStatsDataAsync(
        [ToolParameter("stats_type", "统计类型（如 day/month）", Required = true)] string stats_type,
        [ToolParameter("start_time", "统计起始时间（Unix 秒）", Required = true)] string start_time,
        [ToolParameter("end_time", "统计结束时间（Unix 秒）", Required = true)] string end_time,
        [ToolParameter("user_ids", "用户 ID 数组", Required = true)] string[] user_ids,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 open_id）")] string? employee_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.query_my_remedys（映射 <c>IFeishuTenantV1AttendanceRemedys.QueryUserTaskRemedyAsync</c>）。
/// </summary>
[FeishuTool("attendance.query_my_remedys",
    Description = "查询补卡申请记录（tenant 身份）。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuTenantV1AttendanceRemedys) + "." + nameof(IFeishuTenantV1AttendanceRemedys.QueryUserTaskRemedyAsync))]
public interface IFeishuTenantAttendanceQueryMyRemedysTool
{
    Task<string> QueryUserTaskRemedyAsync(
        [ToolParameter("user_id", "用户 ID", Required = true)] string user_id,
        [ToolParameter("check_time_from", "查询起始时间（Unix 秒）", Required = true)] string check_time_from,
        [ToolParameter("check_time_to", "查询结束时间（Unix 秒）", Required = true)] string check_time_to,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 open_id）")] string? employee_type = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.submit_remedy（映射 <c>IFeishuTenantV1AttendanceRemedys.CreateUserTaskRemedyAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A5：补卡申请（走审批流）。<b>非幂等</b>——描述中声明"重复提交会造成重复申请"。
/// </remarks>
[FeishuTool("attendance.submit_remedy",
    Description = "提交补卡申请（走审批流）。⚠️ 非幂等：重复提交会造成重复申请。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1AttendanceRemedys) + "." + nameof(IFeishuTenantV1AttendanceRemedys.CreateUserTaskRemedyAsync))]
public interface IFeishuTenantAttendanceSubmitRemedyTool
{
    Task<string> CreateUserTaskRemedyAsync(
        [ToolParameter("user_id", "申请人用户 ID", Required = true)] string user_id,
        [ToolParameter("remedy_time", "补卡时间（Unix 秒）", Required = true)] string remedy_time,
        [ToolParameter("remedy_type", "补卡类型（如 absent/miss_punch 等）", Required = true)] string remedy_type,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 open_id）")] string? employee_type = null,
        [ToolParameter("reason", "补卡原因（可选）")] string? reason = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.get_shift（映射 <c>IFeishuTenantV1AttendanceShifts.GetShiftByIdAsync</c>）。
/// </summary>
[FeishuTool("attendance.get_shift",
    Description = "按 shift_id 获取班次规则详情（tenant 身份）。需 attendance:rule 权限。",
    RequiredScopes = ["attendance:rule"],
    Source = nameof(IFeishuTenantV1AttendanceShifts) + "." + nameof(IFeishuTenantV1AttendanceShifts.GetShiftByIdAsync))]
public interface IFeishuTenantAttendanceGetShiftTool
{
    Task<string> GetShiftByIdAsync(
        [ToolParameter("shift_id", "班次 ID", Required = true)] string shift_id,
        CancellationToken cancellationToken = default);
}
