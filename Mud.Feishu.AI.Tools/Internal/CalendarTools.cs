// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Calendar;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Calendar 三工具执行器（<c>calendar.create_event</c> / <c>calendar.find_free_slots</c> /
/// <c>calendar.list_events</c>，WP5 / AT-F04）。
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

    private readonly Mud.Feishu.IFeishuTenantV4CalendarEvent _calendarEventClient = calendarEventClient
        ?? throw new ArgumentNullException(nameof(calendarEventClient));
    private readonly Mud.Feishu.IFeishuTenantV4Calendar _calendarClient = calendarClient
        ?? throw new ArgumentNullException(nameof(calendarClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly int _maxAutoFetchItems = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchPages;

    /// <summary>calendar.create_event：创建日程（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 同款）：<c>idempotency_key</c> → 直通平台查询参数（平台原生幂等）。</remarks>
    [FeishuToolHandler(typeof(IFeishuTenantCalendarCreateEventTool))]
    public Task<FeishuToolResult> CreateEventAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return FeishuToolResult.FromText(ToolDryRun.Describe(
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
    [FeishuToolHandler(typeof(IFeishuTenantCalendarFindFreeSlotsTool))]
    public Task<FeishuToolResult> FindFreeSlotsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
    [FeishuToolHandler(typeof(IFeishuTenantCalendarListEventsTool))]
    public Task<FeishuToolResult> ListEventsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

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

    // ────────── R7/WP4 写面成环 ──────────

    /// <summary>calendar.update_event：更新日程（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantCalendarUpdateEventTool))]
    public Task<FeishuToolResult> UpdateEventAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return FeishuToolResult.FromText(ToolDryRun.Describe(
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
    [FeishuToolHandler(typeof(IFeishuTenantCalendarDeleteEventTool))]
    public Task<FeishuToolResult> DeleteEventAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarDeleteEvent);
        return executor.RunAsync(async () =>
        {
            var args = CalendarDeleteEventArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
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
    [FeishuToolHandler(typeof(IFeishuTenantCalendarAddEventAttendeesTool))]
    public Task<FeishuToolResult> AddEventAttendeesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarAddEventAttendees, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarAddEventAttendeesArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
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
    [FeishuToolHandler(typeof(IFeishuTenantCalendarListEventAttendeesTool))]
    public Task<FeishuToolResult> ListEventAttendeesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarListEventAttendees, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarListEventAttendeesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _calendarEventClient
                .GetCalendarEventAttendeePageListAsync(args.CalendarId, args.EventId, page_size: PageSizes.CalendarEventAttendees, page_token: args.PageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectAttendees);
        });
    }

    /// <summary>list_event_attendees 投影：items（attendee_id/name/type）+ 翻页契约。</summary>
    private static JsonObject ProjectAttendees(ApiPageListResult<CalendarEventAttendeeInfoResult> data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

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
