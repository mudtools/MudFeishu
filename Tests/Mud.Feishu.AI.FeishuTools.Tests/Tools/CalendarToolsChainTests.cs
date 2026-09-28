// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.DataModels.Calendar;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// WP5（AT-F04 / AT-F05）日历三工具的<b>真实调用链路</b>用例：断言 method/path（经编译期契约）
/// 与 SDK 实参（路径/查询/请求体），以及参数非法 → 结构化错误负例。
/// </summary>
/// <remarks>
/// 验收标准沿用 R3：<b>"工具存在"不算通过</b>——每条工具都必须有抓取实参的链路用例。
/// </remarks>
public class CalendarToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV4CalendarEvent> _eventClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV4Calendar> _calendarClient = new();

    private CalendarTools CreateTools()
        => new(
            _eventClient.Object,
            _calendarClient.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    // ───────────────────── 契约事实（method / path） ─────────────────────

    /// <summary>method/path 的真相源是编译期契约（R4 §2.1）：执行器与契约同源，此处把二者锁在一起。</summary>
    [Theory]
    [InlineData("calendar.create_event", "POST", "/open-apis/calendar/v4/calendars/{calendar_id}/events")]
    [InlineData("calendar.find_free_slots", "POST", "/open-apis/calendar/v4/freebusy/list")]
    [InlineData("calendar.list_events", "GET", "/open-apis/calendar/v4/calendars/{calendar_id}/events")]
    public void Contracts_ShouldCarrySdkMethodAndRoute(string toolName, string httpMethod, string route)
    {
        FeishuToolContracts.ByToolName.Should().ContainKey(toolName);
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod, "契约的 http 与 SDK 符号派生一致");
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be("tenant");
    }

    // ───────────────────── calendar.create_event ─────────────────────

    [Fact]
    public async Task CreateEvent_ShouldMapRfc3339IntoTimes_AndPassIdempotencyKey()
    {
        string? capturedCalendarId = null;
        CreateCalendarEventRequest? captured = null;
        string? capturedKey = null;

        _eventClient
            .Setup(c => c.CreateCalendarEventAsync(
                It.IsAny<string>(), It.IsAny<CreateCalendarEventRequest>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string calendarId, CreateCalendarEventRequest request, string? idempotencyKey, string? _, CancellationToken __) =>
            {
                capturedCalendarId = calendarId;
                captured = request;
                capturedKey = idempotencyKey;
            })
            .ReturnsAsync(new FeishuApiResult<CalendarEventOopsResult>
            {
                Code = 0,
                Data = new CalendarEventOopsResult { Event = new CalendarEventInfo { EventId = "evt_1" } },
            });

        var result = await CreateTools().CreateEventAsync(
            Args(
                ("calendar_id", "feishu.cn_abc@group.calendar.feishu.cn"),
                ("summary", "周会"),
                ("start", "2026-10-01T14:00:00+08:00"),
                ("end", "2026-10-01T15:00:00+08:00"),
                ("description", "议题"),
                ("timezone", "Asia/Tokyo"),
                ("idempotency_key", "bill-42-event")),
            CancellationToken.None);

        result.ToString().Should().Contain("evt_1", "写工具回填创建结果（event_id）");
        capturedCalendarId.Should().Be("feishu.cn_abc@group.calendar.feishu.cn", "calendar_id 经 [Path] 下发");
        captured!.Summary.Should().Be("周会");
        captured.Description.Should().Be("议题");
        captured.StartTime.DateTime.Should().Be("2026-10-01T14:00:00+08:00", "模型侧 RFC3339 原文直通 date_time");
        captured.StartTime.Timezone.Should().Be("Asia/Tokyo", "显式 timezone 覆盖默认值");
        captured.EndTime.DateTime.Should().Be("2026-10-01T15:00:00+08:00");
        capturedKey.Should().Be("bill-42-event", "idempotency_key 必须直通平台查询参数（平台原生幂等）");
    }

    [Fact]
    public async Task CreateEvent_ShouldUseDefaultTimezone_WhenOmitted()
    {
        CreateCalendarEventRequest? captured = null;
        _eventClient
            .Setup(c => c.CreateCalendarEventAsync(
                It.IsAny<string>(), It.IsAny<CreateCalendarEventRequest>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreateCalendarEventRequest request, string? __, string? ___, CancellationToken ____) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CalendarEventOopsResult>
            {
                Code = 0,
                Data = new CalendarEventOopsResult { Event = new CalendarEventInfo { EventId = "evt_2" } },
            });

        await CreateTools().CreateEventAsync(
            Args(
                ("calendar_id", "cal_1"),
                ("summary", "周会"),
                ("start", "2026-10-01T14:00:00+08:00"),
                ("end", "2026-10-01T15:00:00+08:00")),
            CancellationToken.None);

        captured!.StartTime.Timezone.Should().Be("Asia/Shanghai", "R4 §5.2：缺省时区为 Asia/Shanghai");
    }

    [Fact]
    public async Task CreateEvent_WithEndBeforeStart_ShouldReturnStructuredError_WithoutCallingDownstream()
    {
        var result = await CreateTools().CreateEventAsync(
            Args(
                ("calendar_id", "cal_1"),
                ("summary", "周会"),
                ("start", "2026-10-01T15:00:00+08:00"),
                ("end", "2026-10-01T14:00:00+08:00")),
            CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] calendar.create_event");
        result.ToString().Should().Contain("end 须晚于 start");
        _eventClient.Verify(
            c => c.CreateCalendarEventAsync(
                It.IsAny<string>(), It.IsAny<CreateCalendarEventRequest>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "参数校验失败必须在下发前拦截");
    }

    [Fact]
    public async Task CreateEvent_WithoutOffsetInRfc3339_ShouldReturnStructuredError()
    {
        var result = await CreateTools().CreateEventAsync(
            Args(
                ("calendar_id", "cal_1"),
                ("summary", "周会"),
                ("start", "2026-10-01T14:00:00"),
                ("end", "2026-10-01T15:00:00")),
            CancellationToken.None);

        result.ToString().Should().Contain("带时区的 RFC3339", "无时区值按本地时区折算会产生静默偏移，必须拒绝");
        _eventClient.Verify(
            c => c.CreateCalendarEventAsync(
                It.IsAny<string>(), It.IsAny<CreateCalendarEventRequest>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEvent_DryRun_ShouldNotCallDownstream_AndEchoIdempotencyState()
    {
        var result = await CreateTools().CreateEventAsync(
            Args(
                ("calendar_id", "cal_1"),
                ("summary", "周会"),
                ("start", "2026-10-01T14:00:00+08:00"),
                ("end", "2026-10-01T15:00:00+08:00"),
                ("idempotency_key", "bill-1"),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("POST /open-apis/calendar/v4/calendars/{calendar_id}/events");
        text.Should().Contain("幂等键：provided", "预演不占坑（T4-1）");
        text.Should().NotContain("bill-1", "摘要不得回显幂等键原文");
        _eventClient.Verify(
            c => c.CreateCalendarEventAsync(
                It.IsAny<string>(), It.IsAny<CreateCalendarEventRequest>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── calendar.find_free_slots ─────────────────────

    [Fact]
    public async Task FindFreeSlots_ShouldPassTimeWindowAndUserId()
    {
        GetFreebusyCalendarRequest? captured = null;
        _calendarClient
            .Setup(c => c.GetFreebusyCalendarAsync(
                It.IsAny<GetFreebusyCalendarRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((GetFreebusyCalendarRequest request, string? _, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<GetFreebusyCalendarResult>
            {
                Code = 0,
                Data = new GetFreebusyCalendarResult
                {
                    FreebusyLists =
                    [
                        new Freebusy { StartTime = "2026-10-01T14:00:00+08:00", EndTime = "2026-10-01T14:30:00+08:00" },
                    ],
                },
            });

        var result = await CreateTools().FindFreeSlotsAsync(
            Args(
                ("time_min", "2026-10-01T00:00:00+08:00"),
                ("time_max", "2026-10-02T00:00:00+08:00"),
                ("user_id", "ou_1"),
                ("only_busy", true)),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TimeMin.Should().Be("2026-10-01T00:00:00+08:00");
        captured.TimeMax.Should().Be("2026-10-02T00:00:00+08:00");
        captured.UserId.Should().Be("ou_1");
        captured.RoomId.Should().BeNull("R-1：DTO 为单 user_id/room_id 形态");
        captured.OnlyBusy.Should().BeTrue();

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("busy").GetArrayLength().Should().Be(1, "投影回填 busy 段列表");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task FindFreeSlots_ShouldRejectNonExclusiveUserOrRoom(bool provideUser, bool provideRoom)
    {
        var items = new List<(string, object?)>
        {
            ("time_min", "2026-10-01T00:00:00+08:00"),
            ("time_max", "2026-10-02T00:00:00+08:00"),
        };
        if (provideUser)
        {
            items.Add(("user_id", "ou_1"));
        }

        if (provideRoom)
        {
            items.Add(("room_id", "omm_1"));
        }

        var result = await CreateTools().FindFreeSlotsAsync(items.ToDictionary(p => p.Item1, p => p.Item2), CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] calendar.find_free_slots");
        result.ToString().Should().Contain("二选一");
        _calendarClient.Verify(
            c => c.GetFreebusyCalendarAsync(It.IsAny<GetFreebusyCalendarRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── calendar.list_events ─────────────────────

    [Fact]
    public async Task ListEvents_ShouldPassCalendarIdAndPageToken_AndProjectItems()
    {
        string? capturedCalendarId = null;
        int capturedPageSize = 0;
        string? capturedPageToken = null;

        _eventClient
            .Setup(c => c.GetCalendarEventPageListAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string calendarId, int pageSize, string? pageToken, string? _, string? __, string? ___, string? ____, string? _____, CancellationToken ______) =>
            {
                capturedCalendarId = calendarId;
                capturedPageSize = pageSize;
                capturedPageToken = pageToken;
            })
            .ReturnsAsync(new FeishuApiResult<GetCalendarEventPageListResult>
            {
                Code = 0,
                Data = new GetCalendarEventPageListResult
                {
                    Items =
                    [
                        new CalendarEventListDetailInfo
                        {
                            EventId = "evt_a",
                            Summary = "评审",
                            StartTime = new CalendarTimeInfo { DateTime = "2026-10-01T10:00:00+08:00" },
                            EndTime = new CalendarTimeInfo { DateTime = "2026-10-01T11:00:00+08:00" },
                        },
                    ],
                    HasMore = true,
                    PageToken = "pt_next",
                },
            });

        var result = await CreateTools().ListEventsAsync(
            Args(("calendar_id", "cal_1"), ("page_token", "pt_prev")),
            CancellationToken.None);

        capturedCalendarId.Should().Be("cal_1");
        capturedPageSize.Should().Be(PageSizes.CalendarEvents, "分页尺寸是绑定层实现细节（模型不可见）");
        capturedPageToken.Should().Be("pt_prev");

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;
        root.GetProperty("has_more").GetBoolean().Should().BeTrue();
        root.GetProperty("page_token").GetString().Should().Be("pt_next");
        var item = root.GetProperty("items")[0];
        item.GetProperty("event_id").GetString().Should().Be("evt_a");
        item.GetProperty("summary").GetString().Should().Be("评审");
        item.GetProperty("start").GetString().Should().Be("2026-10-01T10:00:00+08:00");
    }

    [Fact]
    public async Task ListEvents_FailureCode_ShouldReturnStructuredError()
    {
        _eventClient
            .Setup(c => c.GetCalendarEventPageListAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetCalendarEventPageListResult>
            {
                Code = 190002,
                Msg = "calendar not found",
            });

        var result = await CreateTools().ListEventsAsync(Args(("calendar_id", "cal_1")), CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] calendar.list_events");
        result.ToString().Should().Contain("190002", "失败回填必须带飞书业务 code（错误分类消费）");
    }
}
