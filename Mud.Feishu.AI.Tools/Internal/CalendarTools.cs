// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Calendar;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Calendar 工具执行器（读面 <c>calendar.create_event</c> / <c>calendar.find_free_slots</c> /
/// <c>calendar.list_events</c>，WP5 / AT-F04；写面成环 4 个；T-1 补全 <c>calendar.agenda</c> /
/// <c>calendar.rsvp</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>时间语义铁律</b>：模型侧 RFC3339（必须带时区）；平台侧 <c>date_time</c> 字段直接接受
/// RFC3339 原文（保持调用方时区，不按服务器本地时区折算）。
/// </para>
/// <para>
/// <b>find_free_slots（R-1）</b>：body 为单 <c>user_id</c>/<c>room_id</c> 形态——二者必填其一、互斥。
/// </para>
/// </remarks>
internal sealed class CalendarTools(
    Mud.Feishu.IFeishuTenantV4CalendarEvent calendarEventClient,
    Mud.Feishu.IFeishuTenantV4Calendar calendarClient,
    IOptions<FeishuAgentOptions> options)
{
    /// <summary>timezone 缺省值（与 R4 §5.2 设计一致；可被模型显式覆盖）。</summary>
    private const string DefaultTimezone = "Asia/Shanghai";

    /// <summary>rsvp_status 闭集（平台取值）。</summary>
    private static readonly string[] RsvpStatuses = ["accept", "decline", "tentative"];

    private readonly Mud.Feishu.IFeishuTenantV4CalendarEvent _calendarEventClient = calendarEventClient
        ?? throw new ArgumentNullException(nameof(calendarEventClient));
    private readonly Mud.Feishu.IFeishuTenantV4Calendar _calendarClient = calendarClient
        ?? throw new ArgumentNullException(nameof(calendarClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>calendar.create_event：创建日程（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 同款）：<c>idempotency_key</c> → 直通平台查询参数（平台原生幂等）。</remarks>
    [ToolHandler(typeof(IFeishuTenantCalendarCreateEventTool))]
    public Task<ToolResult> CreateEventAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarCreateEvent, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarCreateEventArgs.Unpack(arguments);
            var timezone = args.Timezone ?? DefaultTimezone;

            // RFC3339 严格校验（带时区），并要求 end 晚于 start——两者都在下发前拦截。
            var startUtc = ParseRfc3339(args.Start, "start");
            var endUtc = ParseRfc3339(args.End, "end");
            if (endUtc <= startUtc)
            {
                throw new ArgumentException("end 须晚于 start");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/calendar/v4/calendars/{calendar_id}/events",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("calendar_id", args.CalendarId.Length), ("summary", args.Summary.Length),
                    ("start", args.Start.Length), ("end", args.End.Length)));
            }

            var request = new CreateCalendarEventRequest
            {
                Summary = args.Summary,
                Description = args.Description,
                StartTime = new CalendarTimeInfo { DateTime = args.Start, Timezone = timezone },
                EndTime = new CalendarTimeInfo { DateTime = args.End, Timezone = timezone },
            };

            var outcome = FeishuApiResultReader.Read(await _calendarEventClient
                .CreateCalendarEventAsync(args.CalendarId, request, idempotency_key: args.IdempotencyKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["event_id"] = data.Event?.EventId,
            });
        });
    }

    /// <summary>calendar.find_free_slots：查询单用户/会议室忙闲（user_id/room_id 二选一）。</summary>
    [ToolHandler(typeof(IFeishuTenantCalendarFindFreeSlotsTool))]
    public Task<ToolResult> FindFreeSlotsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarFindFreeSlots, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarFindFreeSlotsArgs.Unpack(arguments);

            ParseRfc3339(args.TimeMin, "time_min");
            ParseRfc3339(args.TimeMax, "time_max");
            if ((args.UserId is null) == (args.RoomId is null))
            {
                throw new ArgumentException("user_id 与 room_id 须二选一提供（freebusy 接口不接受用户列表，多人请逐人调用）");
            }

            var outcome = FeishuApiResultReader.Read(await _calendarClient
                .GetFreebusyCalendarAsync(
                    new GetFreebusyCalendarRequest
                    {
                        TimeMin = args.TimeMin,
                        TimeMax = args.TimeMax,
                        UserId = args.UserId,
                        RoomId = args.RoomId,
                        OnlyBusy = args.OnlyBusy,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectFreebusy);
        });
    }

    /// <summary>calendar.list_events：列出日程（分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantCalendarListEventsTool))]
    public Task<ToolResult> ListEventsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarListEvents, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarListEventsArgs.Unpack(arguments);

            // B1：fetch_all=true 时循环翻页（唯一翻页实现 ToolPagination）。
            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<GetCalendarEventPageListResult>(
                    async (token, ct) => FeishuApiResultReader.Read(await _calendarEventClient
                        .GetCalendarEventPageListAsync(args.CalendarId, page_size: PageSizes.CalendarEvents, page_token: token, cancellationToken: ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectEvents(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await _calendarEventClient
                .GetCalendarEventPageListAsync(args.CalendarId, page_size: PageSizes.CalendarEvents, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectEvents);
        });
    }

    /// <summary>calendar.agenda：时间窗内的日程实例视图（重复日程展开；平台 Query 为 Unix 秒）。</summary>
    [ToolHandler(typeof(IFeishuTenantCalendarAgendaTool))]
    public Task<ToolResult> AgendaAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarAgenda, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarAgendaArgs.Unpack(arguments);

            var startUtc = ParseRfc3339(args.Start, "start");
            var endUtc = ParseRfc3339(args.End, "end");
            if (endUtc <= startUtc)
            {
                throw new ArgumentException("end 须晚于 start");
            }

            // 时间语义铁律：模型侧 RFC3339 → 平台 Query 的 Unix 秒字符串（唯一转换点）。
            var outcome = FeishuApiResultReader.Read(await _calendarEventClient
                .GetInstanceViewCalendarEventAsync(
                    args.CalendarId,
                    startUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    endUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectAgenda);
        });
    }

    /// <summary>find_free_slots 投影：busy 段列表（start/end）+ time_min/time_max 回显。</summary>
    private static JsonObject ProjectFreebusy(GetFreebusyCalendarResult data)
    {
        var envelope = new JsonObject { ["busy"] = new JsonArray() };
        foreach (var freebusy in data.FreebusyLists ?? [])
        {
            envelope["busy"]!.AsArray().AddNode(new JsonObject
            {
                ["start"] = freebusy.StartTime,
                ["end"] = freebusy.EndTime,
            });
        }

        return envelope;
    }

    /// <summary>list_events 投影：items（event_id/summary/start/end）+ 翻页契约。</summary>
    private static JsonObject ProjectEvents(ApiPageListResult<CalendarEventListDetailInfo> data)
    {
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["event_id"] = item.EventId,
                ["summary"] = item.Summary,
                ["start"] = item.StartTime.DateTime ?? item.StartTime.Date ?? item.StartTime.Timestamp,
                ["end"] = item.EndTime.DateTime ?? item.EndTime.Date ?? item.EndTime.Timestamp,
            });
        }

        return envelope;
    }

    /// <summary>agenda 投影：items（event_id/summary/start/end/status）——重复日程的实例逐条展开。</summary>
    private static JsonObject ProjectAgenda(GetInstanceViewCalendarEventResult data)
    {
        // R-3：信封形态单源（ToolResultJsons.ItemsEnvelope，该接口无翻页契约）。
        var envelope = ToolResultJsons.ItemsEnvelope();

        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["event_id"] = item.EventId,
                ["summary"] = item.Summary,
                ["start"] = item.StartTime?.DateTime ?? item.StartTime?.Date ?? item.StartTime?.Timestamp,
                ["end"] = item.EndTime?.DateTime ?? item.EndTime?.Date ?? item.EndTime?.Timestamp,
                ["status"] = item.Status,
            });
        }

        return envelope;
    }

    // ────────── R7/WP4 写面成环 ──────────

    /// <summary>calendar.update_event：更新日程（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantCalendarUpdateEventTool))]
    public Task<ToolResult> UpdateEventAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarUpdateEvent, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarUpdateEventArgs.Unpack(arguments);

            // start/end 必须同时提供
            if ((args.Start is null) != (args.End is null))
            {
                throw new ArgumentException("start 与 end 必须同时提供（或同时省略）");
            }

            DateTimeOffset? startUtc = null, endUtc = null;
            if (args.Start is not null && args.End is not null)
            {
                startUtc = ParseRfc3339(args.Start, "start");
                endUtc = ParseRfc3339(args.End, "end");
                if (endUtc <= startUtc)
                {
                    throw new ArgumentException("end 须晚于 start");
                }
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", $"/open-apis/calendar/v4/calendars/{args.CalendarId}/events/{args.EventId}",
                    null,
                    ("summary", args.Summary?.Length ?? 0), ("description", args.Description?.Length ?? 0),
                    ("start", args.Start?.Length ?? 0), ("end", args.End?.Length ?? 0)));
            }

            var request = new UpdateCalendarEventRequest();
            if (args.Summary is not null)
                request.Summary = args.Summary;
            if (args.Description is not null)
                request.Description = args.Description;
            if (args.Start is not null && args.End is not null)
            {
                request.StartTime = new CalendarTimeInfo { DateTime = args.Start, Timezone = DefaultTimezone };
                request.EndTime = new CalendarTimeInfo { DateTime = args.End, Timezone = DefaultTimezone };
            }

            var outcome = FeishuApiResultReader.Read(await _calendarEventClient
                .UpdateCalendarEventAsync(args.CalendarId, args.EventId, request, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["event_id"] = args.EventId,
            });
        });
    }

    /// <summary>calendar.delete_event：取消日程（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantCalendarDeleteEventTool))]
    public Task<ToolResult> DeleteEventAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarDeleteEvent);
        return executor.RunAsync(async () =>
        {
            var args = CalendarDeleteEventArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", $"/open-apis/calendar/v4/calendars/{args.CalendarId}/events/{args.EventId}"));
            }

            var outcome = FeishuApiResultReader.Read(await _calendarEventClient
                .DeleteCalendarEventAsync(args.CalendarId, args.EventId, need_notification: true, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["deleted"] = true,
                ["event_id"] = args.EventId,
            });
        });
    }

    /// <summary>calendar.add_event_attendees：添加与会者（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantCalendarAddEventAttendeesTool))]
    public Task<ToolResult> AddEventAttendeesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarAddEventAttendees, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarAddEventAttendeesArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/calendar/v4/calendars/{args.CalendarId}/events/{args.EventId}/attendees",
                    null,
                    ("attendee_ids", args.AttendeeIds.Length)));
            }

            var attendees = args.AttendeeIds
                .Select(id => new CalendarEventAttendeeData { Type = "user", UserId = id })
                .ToArray();

            var request = new CreateCalendarEventAttendeeRequest { Attendees = attendees };

            var outcome = FeishuApiResultReader.Read(await _calendarEventClient
                .CreateCalendarEventAttendeeAsync(args.CalendarId, args.EventId, request, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["added_count"] = args.AttendeeIds.Length,
                ["event_id"] = args.EventId,
            });
        });
    }

    /// <summary>calendar.list_event_attendees：列出现有与会者（分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantCalendarListEventAttendeesTool))]
    public Task<ToolResult> ListEventAttendeesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarListEventAttendees, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarListEventAttendeesArgs.Unpack(arguments);

            // P-0 补齐：统一分页出口——聚合路径与单页路径共用同一次调用定义与同一份投影。
            async Task<ToolApiOutcome<ApiPageListResult<CalendarEventAttendeeInfoResult>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _calendarEventClient
                    .GetCalendarEventAttendeePageListAsync(args.CalendarId, args.EventId, page_size: PageSizes.CalendarEventAttendees, page_token: token, cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectAttendees, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>list_event_attendees 投影：items（attendee_id/name/type）+ 翻页契约。</summary>
    private static JsonObject ProjectAttendees(ApiPageListResult<CalendarEventAttendeeInfoResult> data)
    {
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["attendee_id"] = item.AttendeeId,
                ["name"] = item.DisplayName,
                ["type"] = item.Type,
                ["is_optional"] = item.IsOptional,
            });
        }

        return envelope;
    }

    // ────────── T-1：邀请回复 ──────────

    /// <summary>calendar.rsvp：回复日程邀请（accept/decline/tentative，<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantCalendarRsvpTool))]
    public Task<ToolResult> RsvpAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarRsvp);
        return executor.RunAsync(async () =>
        {
            var args = CalendarRsvpArgs.Unpack(arguments);

            var rsvpStatus = args.RsvpStatus.Trim();
            if (!RsvpStatuses.Contains(rsvpStatus, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"rsvp_status 仅支持 {string.Join("/", RsvpStatuses)}，实际: {args.RsvpStatus}");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/calendar/v4/calendars/{calendar_id}/events/{event_id}/reply",
                    null,
                    ("calendar_id", args.CalendarId.Length), ("event_id", args.EventId.Length), ("rsvp_status", rsvpStatus.Length)));
            }

            var failure = RequireNullDataSuccess(executor.ToolName, await _calendarEventClient
                .ReplyCalendarEventAsync(
                    args.CalendarId,
                    args.EventId,
                    new ReplyCalendarEventRequest { RsvpStatus = rsvpStatus },
                    cancellationToken)
                .ConfigureAwait(false));
            return failure ?? ToolResult.FromText(new JsonObject
            {
                ["replied"] = true,
                ["event_id"] = args.EventId,
                ["rsvp_status"] = rsvpStatus,
            }.ToJsonString());
        });
    }

    /// <summary>
    /// 解包 <see cref="FeishuNullDataApiResult"/>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>，T 不可推断）。
    /// </summary>
    /// <returns>成功返回 <see langword="null"/>；失败返回结构化错误结果（由调用方直接作为工具结果返回）。</returns>
    private static ToolResult? RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        return result.Code == 0
            ? null
            : ToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName, result.Code,
                $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, msg={result.Msg ?? "(无错误信息)"}"));
    }

    /// <summary>
    /// RFC3339 严格解析（带时区，同 ImTools 纪律：宽松解析会把无时区值按本地时区折算产生静默偏移）。
    /// </summary>
    private static DateTimeOffset ParseRfc3339(string value, string parameterName)
    {
        var trimmed = value.Trim();
        var hasOffset = trimmed.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
            || trimmed.IndexOf('+') > 0
            || trimmed.LastIndexOf('-') > 10; // 日期段的 '-'（位置 4/7）不算时区偏移

        if (!hasOffset
            || !DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ArgumentException(
                $"{parameterName} 需为带时区的 RFC3339 格式（如 2026-10-01T14:00:00+08:00 或 2026-10-01T14:00:00Z），实际: {trimmed}");
        }

        return parsed;
    }
}
