// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// WP5 日历工具接口集（AT-F04 / F-05）：<c>calendar.create_event</c> / <c>calendar.find_free_slots</c> /
// <c>calendar.list_events</c>。
// </summary>
// <remarks>
// <para>
// <b>时间语义铁律（R4 §5.2）</b>：模型侧一律 RFC3339（如 <c>2026-10-01T14:00:00+08:00</c>）；
// 平台侧形态由工具层转换——日历 v4 的 <c>date_time</c> 字段本身接受 RFC3339，任务 v2 的
// <c>timestamp</c> 是毫秒字符串（模型不感知）。混暴露两种格式会让模型把毫秒当秒产生静默错误。
// </para>
// <para>
// <b>find_free_slots 参数设计（R4.1 评审 R-1）</b>：<c>freebusy/list</c> 的 body 是
// 单 <c>user_id</c>/<c>room_id</c> 形态，不接受用户列表——多人场景由模型逐人调用。
// </para>
// </remarks>

/// <summary>工具接口：calendar.create_event（映射 <c>IFeishuTenantV4CalendarEvent.CreateCalendarEventAsync</c>）。</summary>
[FeishuTool("calendar.create_event",
    Description = "在指定日历上创建一个日程（start/end 为 RFC3339 时间，如 2026-10-01T14:00:00+08:00）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 calendar:calendar。",
    RequiredScopes = ["calendar:calendar"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4CalendarEvent) + "." + nameof(IFeishuTenantV4CalendarEvent.CreateCalendarEventAsync))]
public interface IFeishuTenantCalendarCreateEventTool
{
    /// <summary>创建日程。</summary>
    /// <returns>白名单投影后的 JSON 文本（event_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateEventAsync(
        [ToolParameter("calendar_id", "日历 ID（形如 feishu.cn_xxx@group.calendar.feishu.cn）", Required = true)] string calendar_id,
        [ToolParameter("summary", "日程标题", Required = true)] string summary,
        [ToolParameter("start", "开始时间（RFC3339，如 2026-10-01T14:00:00+08:00）", Required = true)] string start,
        [ToolParameter("end", "结束时间（RFC3339，须晚于 start）", Required = true)] string end,
        [ToolParameter("description", "日程描述（可选）")] string? description = null,
        [ToolParameter("timezone", "时区（可选，默认 Asia/Shanghai；仅对无时区偏移的时间生效）")] string? timezone = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：平台原生幂等——相同键在同一应用与日历维度下至多创建一次日程；省略时不保证幂等。建议由调用方给出稳定值，不要用随机数。幂等键不跨工具共享。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：calendar.find_free_slots（映射 <c>IFeishuTenantV4Calendar.GetFreebusyCalendarAsync</c>）。</summary>
/// <remarks>R-1：body 为单 user_id/room_id 形态，不接受用户列表；多人场景由模型逐人调用。</remarks>
[FeishuTool("calendar.find_free_slots",
    Description = "查询一个用户主日历或一间会议室在指定时间窗内的忙闲（time_min/time_max 为 RFC3339；user_id 与 room_id 二选一）。多人场景请逐人调用。只读，需 calendar:calendar:readonly。",
    RequiredScopes = ["calendar:calendar:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV4Calendar) + "." + nameof(IFeishuTenantV4Calendar.GetFreebusyCalendarAsync),

    // R5 / B-6：user_id 与 room_id 二选一是跨参数约束，required 表达不了（会让两者都必填）。
    // 渲染为 "anyOf":[{"required":["user_id"]},{"required":["room_id"]}]，让约束结构化进入模型可见契约。
    AnyOf = ["user_id|room_id"])]
public interface IFeishuTenantCalendarFindFreeSlotsTool
{
    /// <summary>查询忙闲。</summary>
    /// <returns>白名单投影后的 JSON 文本（busy 段列表）。</returns>
    Task<string> FindFreeSlotsAsync(
        [ToolParameter("time_min", "查询窗口起点（RFC3339，含时区）", Required = true)] string time_min,
        [ToolParameter("time_max", "查询窗口终点（RFC3339，须晚于起点）", Required = true)] string time_max,
        [ToolParameter("user_id", "用户 open_id（与 room_id 二选一）")] string? user_id = null,
        [ToolParameter("room_id", "会议室 ID（与 user_id 二选一）")] string? room_id = null,
        [ToolParameter("only_busy", "仅返回忙碌时段（可选，默认 false）")] bool? only_busy = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：calendar.list_events（映射 <c>IFeishuTenantV4CalendarEvent.GetCalendarEventPageListAsync</c>）。</summary>
[FeishuTool("calendar.list_events",
    Description = "列出指定日历上的日程（按开始时间返回，含 summary/start/end/event_id），可翻页。只读，需 calendar:calendar:readonly。",
    RequiredScopes = ["calendar:calendar:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV4CalendarEvent) + "." + nameof(IFeishuTenantV4CalendarEvent.GetCalendarEventPageListAsync))]
public interface IFeishuTenantCalendarListEventsTool
{
    /// <summary>列出日程。</summary>
    /// <returns>白名单投影后的 JSON 文本（items + 翻页契约）。</returns>
    Task<string> ListEventsAsync(
        [ToolParameter("calendar_id", "日历 ID（形如 feishu.cn_xxx@group.calendar.feishu.cn）", Required = true)] string calendar_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Calendar 写面成环（R7/WP4，4 个） ───────────────────────────

/// <summary>工具接口：calendar.update_event（映射 <c>IFeishuTenantV4CalendarEvent.UpdateCalendarEventAsync</c>）。</summary>
[FeishuTool("calendar.update_event",
    Description = "更新指定日程的信息（summary/description/start/end 等字段，至少传一个要更新的字段）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 calendar:calendar。start/end 为 RFC3339 时间。",
    RequiredScopes = ["calendar:calendar"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4CalendarEvent) + "." + nameof(IFeishuTenantV4CalendarEvent.UpdateCalendarEventAsync))]
public interface IFeishuTenantCalendarUpdateEventTool
{
    /// <summary>更新日程。</summary>
    /// <returns>白名单投影后的 JSON 文本（event_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateEventAsync(
        [ToolParameter("calendar_id", "日历 ID（形如 feishu.cn_xxx@group.calendar.feishu.cn）", Required = true)] string calendar_id,
        [ToolParameter("event_id", "日程 ID（来自 calendar.create_event 或 calendar.list_events）", Required = true)] string event_id,
        [ToolParameter("summary", "日程标题（可选更新）")] string? summary = null,
        [ToolParameter("description", "日程描述（可选更新）")] string? description = null,
        [ToolParameter("start", "开始时间（RFC3339，如 2026-10-01T14:00:00+08:00；可选更新，须与 end 同时提供）")] string? start = null,
        [ToolParameter("end", "结束时间（RFC3339，须晚于 start；可选更新，须与 start 同时提供）")] string? end = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：calendar.delete_event（映射 <c>IFeishuTenantV4CalendarEvent.DeleteCalendarEventAsync</c>）。</summary>
[FeishuTool("calendar.delete_event",
    Description = "取消（删除）指定日程——取消日程会通知所有与会者。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 calendar:calendar。建议先 dry_run 预演确认。",
    RequiredScopes = ["calendar:calendar"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4CalendarEvent) + "." + nameof(IFeishuTenantV4CalendarEvent.DeleteCalendarEventAsync))]
public interface IFeishuTenantCalendarDeleteEventTool
{
    /// <summary>取消日程。</summary>
    /// <returns>白名单投影后的 JSON 文本（deleted=true）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> DeleteEventAsync(
        [ToolParameter("calendar_id", "日历 ID（形如 feishu.cn_xxx@group.calendar.feishu.cn）", Required = true)] string calendar_id,
        [ToolParameter("event_id", "日程 ID（来自 calendar.create_event 或 calendar.list_events）", Required = true)] string event_id,
        [ToolParameter("dry_run", "仅预演不取消（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：calendar.add_event_attendees（映射 <c>IFeishuTenantV4CalendarEvent.CreateCalendarEventAttendeeAsync</c>）。</summary>
[FeishuTool("calendar.add_event_attendees",
    Description = "向指定日程添加与会者（attendee_ids 为 open_id 数组，每位参会人会收到日程邀请）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 calendar:calendar。",
    RequiredScopes = ["calendar:calendar"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV4CalendarEvent) + "." + nameof(IFeishuTenantV4CalendarEvent.CreateCalendarEventAttendeeAsync))]
public interface IFeishuTenantCalendarAddEventAttendeesTool
{
    /// <summary>添加与会者。</summary>
    /// <returns>白名单投影后的 JSON 文本（added_count）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> AddEventAttendeesAsync(
        [ToolParameter("calendar_id", "日历 ID（形如 feishu.cn_xxx@group.calendar.feishu.cn）", Required = true)] string calendar_id,
        [ToolParameter("event_id", "日程 ID（来自 calendar.create_event 或 calendar.list_events）", Required = true)] string event_id,
        [ToolParameter("attendee_ids", "与会者 open_id 数组（如 [\"ou_xxx\"]）", Required = true)] string[] attendee_ids,
        [ToolParameter("dry_run", "仅预演不添加（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：calendar.list_event_attendees（映射 <c>IFeishuTenantV4CalendarEvent.GetCalendarEventAttendeePageListAsync</c>）。</summary>
[FeishuTool("calendar.list_event_attendees",
    Description = "分页列出指定日程的与会者（attendee_id/name/type/is_optional），可翻页。只读，需 calendar:calendar:readonly。",
    RequiredScopes = ["calendar:calendar:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV4CalendarEvent) + "." + nameof(IFeishuTenantV4CalendarEvent.GetCalendarEventAttendeePageListAsync))]
public interface IFeishuTenantCalendarListEventAttendeesTool
{
    /// <summary>列出现有与会者。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> ListEventAttendeesAsync(
        [ToolParameter("calendar_id", "日历 ID（形如 feishu.cn_xxx@group.calendar.feishu.cn）", Required = true)] string calendar_id,
        [ToolParameter("event_id", "日程 ID（来自 calendar.create_event 或 calendar.list_events）", Required = true)] string event_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
