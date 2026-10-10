// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Attendance 考勤（R7 / A5 新域，8 个工具） ───────────────────────────
//
// 链路设计（"打卡 → 排班 → 统计 → 补卡"）：
//   query_my_flow / query_user_flow / get_flow（打卡流水）
//     → query_daily_shift（排班）/ query_stats_data（统计）/ get_shift（班次规则）
//     → submit_remedy（补卡申请，走审批流）
//
// ⚠️ 日期口径（对齐 SDK 线上字段，勿混用）：
//   · check_date_from / check_date_to / start_date / end_date / remedy_date = **yyyyMMdd 整数**（如 20240101）；
//   · check_time_from / check_time_to = **字符串**（秒级时间戳，如 "1705312800"）。

/// <summary>
/// 工具接口：attendance.query_my_flow（映射 <c>IFeishuUserV1AttendanceUserTask.QueryUserTaskAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A5："我的打卡结果"——user 身份。宿主须在 <c>AllowedIdentities</c> 放行 <c>user</c>，否则装配期 fail-fast。
/// </remarks>
[FeishuTool("attendance.query_my_flow",
    Description = "查询指定用户的考勤打卡结果（每日一条汇总，含上下班打卡时间与结果）。user 身份，仅能查当前登录用户自身数据。check_date_from/check_date_to 为 yyyyMMdd 整数日期（如 20240101）。需 attendance:task 权限；宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuUserV1AttendanceUserTask) + "." + nameof(IFeishuUserV1AttendanceUserTask.QueryUserTaskAsync))]
public interface IFeishuUserAttendanceQueryMyFlowTool
{
    /// <summary>查询打卡结果（"我的打卡"）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items 含上下班打卡时间/结果 + 翻页无关字段）。</returns>
    Task<string> QueryUserTaskAsync(
        [ToolParameter("employee_type", "员工 ID 类型：open_id / user_id / union_id", Required = true)] string employee_type,
        [ToolParameter("user_id", "用户 ID（与 employee_type 对应；user 身份下应传本人 ID）", Required = true)] string user_id,
        [ToolParameter("check_date_from", "查询起始日期，yyyyMMdd 整数（如 20240101）", Required = true)] int check_date_from,
        [ToolParameter("check_date_to", "查询结束日期，yyyyMMdd 整数（如 20240131）", Required = true)] int check_date_to,
        [ToolParameter("need_overtime_result", "是否返回加班时长结果（可选，默认 false）")] bool? need_overtime_result = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.query_user_flow（映射 <c>IFeishuTenantV1AttendanceUserFlows.QueryUserFlowAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A5（DP-A5-1 档位 ①）：⚠️ <b>PII 敏感</b>——按 user_id + 时间窗查<b>他人</b>打卡流水。
/// 默认不在任何白名单，需宿主显式加白名单；工具描述含后果句（结果将进入第三方模型上下文）。
/// </remarks>
[FeishuTool("attendance.query_user_flow",
    Description = "批量查询指定用户的考勤打卡流水（tenant 身份，可查他人）。check_time_from/check_time_to 为秒级时间戳字符串（如 \"1705312800\"）。⚠️ PII 敏感：打卡时间/地点属员工隐私数据，结果将进入第三方模型上下文，请确认已获合规授权。默认不启用，需宿主显式加白名单。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuTenantV1AttendanceUserFlows) + "." + nameof(IFeishuTenantV1AttendanceUserFlows.QueryUserFlowAsync))]
public interface IFeishuTenantAttendanceQueryUserFlowTool
{
    /// <summary>批量查询打卡流水。</summary>
    /// <returns>白名单投影后的 JSON 文本（items 含 user_id/check_time/location_name/check_result）。</returns>
    Task<string> QueryUserFlowAsync(
        [ToolParameter("employee_type", "员工 ID 类型：open_id / user_id / union_id", Required = true)] string employee_type,
        [ToolParameter("user_ids", "用户 ID 列表", Required = true)] string[] user_ids,
        [ToolParameter("check_time_from", "查询起始时间，秒级时间戳字符串（如 \"1705312800\"）", Required = true)] string check_time_from,
        [ToolParameter("check_time_to", "查询结束时间，秒级时间戳字符串（如 \"1705400000\"）", Required = true)] string check_time_to,
        [ToolParameter("include_terminated_user", "是否包含已离职用户（可选，默认 false）")] bool? include_terminated_user = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.get_flow（映射 <c>IFeishuTenantV1AttendanceUserFlows.GetUserFlowAsync</c>）。
/// </summary>
/// <remarks>R7 / A5（DP-A5-1 档位 ①）：⚠️ PII 敏感（单条打卡详情）。默认不启用，需宿主显式加白名单。</remarks>
[FeishuTool("attendance.get_flow",
    Description = "获取单条考勤打卡流水详情（tenant 身份）。⚠️ PII 敏感：打卡时间/地点属员工隐私数据，结果将进入第三方模型上下文。默认不启用，需宿主显式加白名单。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuTenantV1AttendanceUserFlows) + "." + nameof(IFeishuTenantV1AttendanceUserFlows.GetUserFlowAsync))]
public interface IFeishuTenantAttendanceGetFlowTool
{
    /// <summary>获取单条打卡流水。</summary>
    /// <returns>白名单投影后的 JSON 文本（user_id/check_time/location_name/check_result）。</returns>
    Task<string> GetUserFlowAsync(
        [ToolParameter("flow_id", "打卡流水 ID（user_flow_id，来自 attendance.query_user_flow 的 record_id）", Required = true)] string flow_id,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 user_id）")] string? employee_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.query_daily_shift（映射 <c>IFeishuTenantV1AttendanceUserDailyShifts.QueryUserDailyShiftAsync</c>）。
/// </summary>
[FeishuTool("attendance.query_daily_shift",
    Description = "查询指定用户的排班表（某天应上哪个班次，口径：回答\"我今天几点上班\"）。check_date_from/check_date_to 为 yyyyMMdd 整数日期。查到的 shift_id 可再调 attendance.get_shift 取班次时间规则。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuTenantV1AttendanceUserDailyShifts) + "." + nameof(IFeishuTenantV1AttendanceUserDailyShifts.QueryUserDailyShiftAsync))]
public interface IFeishuTenantAttendanceQueryDailyShiftTool
{
    /// <summary>查询排班表。</summary>
    /// <returns>白名单投影后的 JSON 文本（items 含 user_id/group_id/shift_id/month/day_no）。</returns>
    Task<string> QueryUserDailyShiftAsync(
        [ToolParameter("user_ids", "用户 ID 列表", Required = true)] string[] user_ids,
        [ToolParameter("check_date_from", "查询起始日期，yyyyMMdd 整数（如 20240101）", Required = true)] int check_date_from,
        [ToolParameter("check_date_to", "查询结束日期，yyyyMMdd 整数（如 20240131）", Required = true)] int check_date_to,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 user_id）")] string? employee_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.query_stats_data（映射 <c>IFeishuTenantV1AttendanceStats.QueryUserStatsDataAsync</c>）。
/// </summary>
[FeishuTool("attendance.query_stats_data",
    Description = "查询考勤统计报表数据（tenant 身份）。stats_type 取值 day（按日）/ month（按月）；start_date/end_date 为 yyyyMMdd 整数日期。user_ids 缺省时按当前考勤组统计。需 attendance:stats 权限。",
    RequiredScopes = ["attendance:stats"],
    Source = nameof(IFeishuTenantV1AttendanceStats) + "." + nameof(IFeishuTenantV1AttendanceStats.QueryUserStatsDataAsync))]
public interface IFeishuTenantAttendanceQueryStatsDataTool
{
    /// <summary>查询统计数据。</summary>
    /// <returns>白名单投影后的 JSON 文本（items 含 user_id/name/cells[{code,value}]）。</returns>
    Task<string> QueryUserStatsDataAsync(
        [ToolParameter("stats_type", "统计类型：day（按日）/ month（按月）", Required = true)] string stats_type,
        [ToolParameter("start_date", "起始日期，yyyyMMdd 整数（如 20240101）", Required = true)] int start_date,
        [ToolParameter("end_date", "结束日期，yyyyMMdd 整数（如 20240131）", Required = true)] int end_date,
        [ToolParameter("user_ids", "用户 ID 列表（可选；缺省按当前考勤组统计）")] string[]? user_ids = null,
        [ToolParameter("locale", "语言（可选：zh / en / ja）")] string? locale = null,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 user_id）")] string? employee_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.query_my_remedys（映射 <c>IFeishuTenantV1AttendanceRemedys.QueryUserTaskRemedyAsync</c>）。
/// </summary>
[FeishuTool("attendance.query_my_remedys",
    Description = "查询指定用户的补卡申请记录（tenant 身份）。check_time_from/check_time_to 为秒级时间戳字符串。需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    Source = nameof(IFeishuTenantV1AttendanceRemedys) + "." + nameof(IFeishuTenantV1AttendanceRemedys.QueryUserTaskRemedyAsync))]
public interface IFeishuTenantAttendanceQueryMyRemedysTool
{
    /// <summary>查询补卡记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（items 含 user_id/remedy_date/remedy_time/status/reason）。</returns>
    Task<string> QueryUserTaskRemedyAsync(
        [ToolParameter("user_ids", "用户 ID 列表", Required = true)] string[] user_ids,
        [ToolParameter("check_time_from", "查询起始时间，秒级时间戳字符串（如 \"1705312800\"）", Required = true)] string check_time_from,
        [ToolParameter("check_time_to", "查询结束时间，秒级时间戳字符串（如 \"1705400000\"）", Required = true)] string check_time_to,
        [ToolParameter("check_date_type", "时间口径（可选：PeriodTime=按补卡日期 / CreateTime=按创建时间，默认 PeriodTime）")] string? check_date_type = null,
        [ToolParameter("status", "审批状态过滤（可选，官方 status 枚举值；缺省返回全部）")] int? status = null,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 user_id）")] string? employee_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.submit_remedy（映射 <c>IFeishuTenantV1AttendanceRemedys.CreateUserTaskRemedyAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A5：补卡申请（写入考勤系统，状态为审批中）。<b>非幂等</b>——描述中声明"重复提交会造成重复申请"。
/// </remarks>
[FeishuTool("attendance.submit_remedy",
    Description = "提交补卡申请（写入飞书考勤系统，状态为审批中；走补卡审批流）。remedy_date 为 yyyyMMdd 整数日期，remedy_time 为 HH:mm 字符串。⚠️ 非幂等：重复提交会造成重复申请（无幂等键可用）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 attendance:task 权限。",
    RequiredScopes = ["attendance:task"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1AttendanceRemedys) + "." + nameof(IFeishuTenantV1AttendanceRemedys.CreateUserTaskRemedyAsync))]
public interface IFeishuTenantAttendanceSubmitRemedyTool
{
    /// <summary>创建补卡审批。</summary>
    /// <returns>白名单投影后的 JSON 文本（user_id/remedy_date/remedy_time/status/approval_id）。</returns>
    Task<string> CreateUserTaskRemedyAsync(
        [ToolParameter("user_id", "补卡申请人用户 ID", Required = true)] string user_id,
        [ToolParameter("remedy_date", "补卡日期，yyyyMMdd 整数（如 20240115）", Required = true)] int remedy_date,
        [ToolParameter("remedy_time", "补卡时间，HH:mm 字符串（如 \"09:00\"）", Required = true)] string remedy_time,
        [ToolParameter("punch_no", "第几次打卡（官方 punch_no：一天两次打卡时上午为 1、下午为 2；以考勤组配置为准）", Required = true)] int punch_no,
        [ToolParameter("work_type", "工作类型（官方 work_type：通常 1=工作日 / 2=休息日 / 3=节假日；以考勤组配置为准）", Required = true)] int work_type,
        [ToolParameter("reason", "补卡原因（如\"忘记打卡\"）", Required = true)] string reason,
        [ToolParameter("time", "补卡时间点（可选，官方 time 字段，格式同 remedy_time）")] string? time = null,
        [ToolParameter("employee_type", "员工 ID 类型（可选，默认 user_id）")] string? employee_type = null,
        [ToolParameter("dry_run", "仅预演不提交（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：attendance.get_shift（映射 <c>IFeishuTenantV1AttendanceShifts.GetShiftByIdAsync</c>）。
/// </summary>
[FeishuTool("attendance.get_shift",
    Description = "按 shift_id 获取班次规则详情（上下班时间、弹性规则等，tenant 身份）。shift_id 来自 attendance.query_daily_shift。需 attendance:rule 权限。",
    RequiredScopes = ["attendance:rule"],
    Source = nameof(IFeishuTenantV1AttendanceShifts) + "." + nameof(IFeishuTenantV1AttendanceShifts.GetShiftByIdAsync))]
public interface IFeishuTenantAttendanceGetShiftTool
{
    /// <summary>获取班次详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（shift_id/shift_name/punch_times/上下班时间规则）。</returns>
    Task<string> GetShiftByIdAsync(
        [ToolParameter("shift_id", "班次 ID（来自 attendance.query_daily_shift）", Required = true)] string shift_id,
        CancellationToken cancellationToken = default);
}
