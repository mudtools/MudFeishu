// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Calendar;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Internal;

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

    /// <summary>calendar.create_event：创建日程（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 同款）：<c>idempotency_key</c> → 直通平台查询参数（平台原生幂等）。</remarks>
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
    public Task<FeishuToolResult> ListEventsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.CalendarListEvents, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = CalendarListEventsArgs.Unpack(arguments);

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
