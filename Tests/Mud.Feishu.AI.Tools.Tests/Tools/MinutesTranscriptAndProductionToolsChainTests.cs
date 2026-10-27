// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

using Mud.Feishu.DataModels.Minutes;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 妙记新增面（<c>minutes.get_transcript</c> + 生产面 <c>clip</c> / <c>upload</c> /
/// <c>subscribe</c> / <c>unsubscribe</c>）的链路用例。
/// </summary>
/// <remarks>
/// <para>
/// <b>逐字稿走受控下载出口</b>：字节交宿主落盘、结果只回 <c>local_path</c>，且逐字稿是<b>交付物</b>
/// （与上传类"临时附件"相反）——执行器不得调用 Cleanup，故此处把清理次数也锁成断言。
/// </para>
/// <para>
/// <b>剪辑时间段的文本 DSL 是最大风险面</b>：宽松解析会把「2026-10-01T14:00:00」按本地时区折算，
/// 剪错时间段而平台不会报错。因此必须锁：① 无时区值在下发前被拒；② 下发的字符串与模型给的
/// 逐字相同（不做任何格式化，否则本地时区差异会写进请求体）。
/// </para>
/// </remarks>
public class MinutesTranscriptAndProductionToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute> _tenantClient = new();
    private readonly Mock<Mud.Feishu.IFeishuUserV1MinutesMinute> _userClient = new();
    private readonly List<string> _stagedPaths = [];

    private int _stageCalls;
    private int _cleanupCalls;
    private string? _lastUrl;
    private string? _lastFileName;
    private byte[]? _lastContent;

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private MinutesTranscriptTools CreateTranscriptTools(IFeishuAttachmentStager? stager = null)
        => new(_tenantClient.Object, stager ?? CreateStager().Object);

    private MinutesUserWriteTools CreateUserWriteTools() => new(_userClient.Object);

    /// <summary>宿主落盘器替身：写出真实临时文件并记录清理调用（下载产物不应触发清理）。</summary>
    private Mock<IFeishuAttachmentStager> CreateStager(bool allow = true)
    {
        var stager = new Mock<IFeishuAttachmentStager>();
        stager
            .Setup(s => s.StageAsync(It.IsAny<AttachmentSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttachmentSource source, CancellationToken _) =>
            {
                _stageCalls++;
                _lastUrl = source.Url;
                _lastFileName = source.FileName;
                _lastContent = source.Content?.ToArray();
                if (!allow)
                {
                    return null;
                }

                var path = Path.Combine(Path.GetTempPath(), $"mud-feishu-minutes-test-{Guid.NewGuid():N}.txt");
                File.WriteAllBytes(path, _lastContent ?? []);
                _stagedPaths.Add(path);

                return new StagedAttachment(
                    path,
                    _lastContent?.LongLength ?? 0,
                    "text/plain",
                    () =>
                    {
                        _cleanupCalls++;
                        return ValueTask.CompletedTask;
                    });
            });
        return stager;
    }

    /// <summary>测试自身清场：交付物不进执行器清理链，由用例事后删除。</summary>
    private void DeleteStagedFiles()
    {
        foreach (var path in _stagedPaths)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // ───────────────────── 契约事实（method / path / 身份 / 风险） ─────────────────────

    [Theory]
    [InlineData("minutes.get_transcript", "GET", "/open-apis/minutes/v1/minutes/{minute_token}/transcript", "tenant", "read", "minutes:minutes.transcript:export")]
    [InlineData("minutes.clip", "POST", "/open-apis/minutes/v1/minutes/{minute_token}/clip", "user", "write", "minutes:minutes.clip:write")]
    [InlineData("minutes.upload", "POST", "/open-apis/minutes/v1/minutes/upload", "user", "write", "minutes:minutes.upload:write")]
    [InlineData("minutes.subscribe", "POST", "/open-apis/minutes/v1/minutes/subscription", "user", "write", "minutes:minutes.basic:read")]
    [InlineData("minutes.unsubscribe", "POST", "/open-apis/minutes/v1/minutes/unsubscription", "user", "write", "minutes:minutes.basic:read")]
    public void Contracts_ShouldCarrySdkMethodAndRoute(string toolName, string httpMethod, string route, string identity, string risk, string scope)
    {
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod);
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be(identity, "生产面只有用户身份形态：宿主须放行 user，否则运行期被平台拒绝");
        contract.Risk.Should().Be(risk == "read" ? FeishuToolRisk.Read : FeishuToolRisk.Write);
        contract.IsWrite.Should().Be(risk != "read", "订阅/退订改的是事件流向，属写面");
        contract.RequiredScopes.Should().Contain(scope);
    }

    // ───────────────────── minutes.get_transcript ─────────────────────

    [Fact]
    public async Task GetTranscript_ShouldStageBytes_AndReturnPathOnly()
    {
        string? capturedToken = null;
        bool? capturedSpeaker = null;
        bool? capturedTimestamp = null;
        string? capturedFormat = null;
        var payload = Bytes("TRANSCRIPT-BYTES-MARKER");

        _tenantClient
            .Setup(c => c.GetMinuteTranscriptAsync(
                It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string token, bool? speaker, bool? timestamp, string? format, CancellationToken _) =>
            {
                capturedToken = token;
                capturedSpeaker = speaker;
                capturedTimestamp = timestamp;
                capturedFormat = format;
            })
            .ReturnsAsync(payload);

        try
        {
            var text = (await CreateTranscriptTools().GetMinuteTranscriptAsync(
                Args(("minute_token", "obcn1"), ("file_format", "srt"), ("need_speaker", true), ("need_timestamp", true)),
                CancellationToken.None)).ToString()!;

            capturedToken.Should().Be("obcn1", "minute_token 经 [Path] 下发");
            capturedFormat.Should().Be("srt", "格式经 [Query] 透传（srt 自带时间轴）");
            capturedSpeaker.Should().BeTrue();
            capturedTimestamp.Should().BeTrue();
            _stageCalls.Should().Be(1, "字节必须经宿主落盘（no-bytes-in-context）");
            _lastUrl.Should().BeNull("逐字稿来源是内存字节，不是 URL");
            _lastFileName.Should().Be("minute-obcn1.srt", "落盘建议名带 token 与真实格式");
            _lastContent.Should().Equal(payload);

            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var localPath = root.GetProperty("local_path").GetString();
            File.Exists(localPath).Should().BeTrue("逐字稿是交付物：落盘后留在宿主路径");
            root.GetProperty("size").GetInt64().Should().Be(payload.LongLength);
            root.GetProperty("minute_token").GetString().Should().Be("obcn1");
            _cleanupCalls.Should().Be(0, "与上传类临时文件相反——交付物不进清理链");
            text.Should().NotContain("TRANSCRIPT-BYTES-MARKER", "结果只回路径/大小/类型，逐字稿原文不进上下文");
        }
        finally
        {
            DeleteStagedFiles();
        }
    }

    [Fact]
    public async Task GetTranscript_WhenFormatNotInClosedSet_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateTranscriptTools().GetMinuteTranscriptAsync(
            Args(("minute_token", "obcn1"), ("file_format", "docx")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.get_transcript");
        text.Should().Contain("file_format 仅支持 txt/srt");
        _tenantClient.VerifyNoOtherCalls();
        _stageCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetTranscript_WhenJsonErrorBody_ShouldRejectWithoutStaging()
    {
        _tenantClient
            .Setup(c => c.GetMinuteTranscriptAsync(
                It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bytes("""{"code":1770001,"msg":"minute not found"}"""));

        var text = (await CreateTranscriptTools().GetMinuteTranscriptAsync(
            Args(("minute_token", "missing")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.get_transcript");
        text.Should().Contain("错误体防线");
        text.Should().Contain("1770001", "HTTP 200 + JSON 错误体会伪装成逐字稿文件");
        _stageCalls.Should().Be(0, "错误体不得落盘（否则交付路径上多一个内容是错误 JSON 的假逐字稿）");
    }

    [Fact]
    public async Task GetTranscript_WhenEmptyContent_ShouldReturnStructuredError()
    {
        _tenantClient
            .Setup(c => c.GetMinuteTranscriptAsync(
                It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<byte>());

        var text = (await CreateTranscriptTools().GetMinuteTranscriptAsync(
            Args(("minute_token", "obcn1")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.get_transcript");
        text.Should().Contain("平台未返回逐字稿内容", "空响应体要给出可执行的下一步（转写未完成 / 无导出权限）");
        _stageCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetTranscript_WhenHostRejectsStaging_ShouldReportDeliveryFailure_NotSkipStaging()
    {
        _tenantClient
            .Setup(c => c.GetMinuteTranscriptAsync(
                It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Bytes("TRANSCRIPT-OK"));

        var text = (await CreateTranscriptTools(CreateStager(allow: false).Object).GetMinuteTranscriptAsync(
            Args(("minute_token", "obcn1")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.get_transcript");
        text.Should().Contain("宿主安全域", "安全域由宿主实现，SDK 只如实回填（不假装能校验）");
        text.Should().Contain("文件未交付", "落盘被拒 = 交付失败，不得降级为『跳过落盘、回一段文字』");
        _stageCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetTranscript_WhenMinutesClientMissing_ShouldTellHostWhatToEnable()
    {
        var tools = new MinutesTranscriptTools(null, CreateStager().Object);

        var text = (await tools.GetMinuteTranscriptAsync(
            Args(("minute_token", "obcn1")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.get_transcript");
        text.Should().Contain("IFeishuTenantV1MinutesMinute", "软缺席必须说清缺哪个客户端，否则宿主无从排查");
        _stageCalls.Should().Be(0, "客户端缺席不得留下空文件");
    }

    // ───────────────────── minutes.clip ─────────────────────

    [Fact]
    public async Task Clip_ShouldPassTimeRangesVerbatim_AndReturnMinuteUrl()
    {
        string? capturedToken = null;
        ClipMinuteRequest? captured = null;

        _userClient
            .Setup(c => c.ClipMinuteAsync(It.IsAny<string>(), It.IsAny<ClipMinuteRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string token, ClipMinuteRequest request, CancellationToken _) =>
            {
                capturedToken = token;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<MinuteUrlResult>
            {
                Code = 0,
                Data = new MinuteUrlResult { MinuteUrl = "https://bytedance.larkoffice.com/minutes/obcnNew" },
            });

        var text = (await CreateUserWriteTools().ClipMinuteAsync(
            Args(
                ("minute_token", "obcn1"),
                ("time_ranges", new[] { "2026-10-01T14:00:00+08:00|2026-10-01T14:05:00+08:00", " 2026-10-01T15:00:00Z | 2026-10-01T15:30:00Z " }),
                ("title", "决议片段")),
            CancellationToken.None)).ToString()!;

        capturedToken.Should().Be("obcn1");
        captured!.TimeRanges.Should().HaveCount(2);
        captured.TimeRanges[0].StartTime.Should().Be("2026-10-01T14:00:00+08:00");
        captured.TimeRanges[0].EndTime.Should().Be("2026-10-01T14:05:00+08:00");
        captured.TimeRanges[1].StartTime.Should().Be("2026-10-01T15:00:00Z",
            "只 Trim、不重格式化——按本地时区折算会把剪辑点整体平移，而平台不会报错");
        captured.Title.Should().Be("决议片段");

        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("minute_url").GetString().Should().StartWith("https://");
        document.RootElement.GetProperty("requested_range_count").GetInt32().Should().Be(2,
            "异步生成：成功只代表『已提交』，请求段数让模型能核对回执");
    }

    [Fact]
    public async Task Clip_WhenRangeHasNoTimezone_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateUserWriteTools().ClipMinuteAsync(
            Args(("minute_token", "obcn1"), ("time_ranges", new[] { "2026-10-01T14:00:00|2026-10-01T14:05:00" })),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.clip");
        text.Should().Contain("需为带时区的 RFC3339 格式");
        _userClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Clip_WhenRangeShorterThanOneSecond_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateUserWriteTools().ClipMinuteAsync(
            Args(("minute_token", "obcn1"), ("time_ranges", new[] { "2026-10-01T14:00:00+08:00|2026-10-01T14:00:00.500+08:00" })),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.clip");
        text.Should().Contain("1000 毫秒", "平台要求每段 >1000ms，本地拦下可读错误");
        _userClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Clip_WhenSegmentNotTwoParts_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateUserWriteTools().ClipMinuteAsync(
            Args(("minute_token", "obcn1"), ("time_ranges", new[] { "2026-10-01T14:00:00+08:00" })),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.clip");
        text.Should().Contain("两段", "DSL 格式错误必须点名是第几段——多段入参只说『格式不对』无法自我纠正");
        _userClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Clip_WhenMoreThanFiftyRanges_ShouldReject_WithoutCallingDownstream()
    {
        var ranges = Enumerable.Range(0, 51)
            .Select(static i => $"2026-10-01T14:00:00+08:00|2026-10-01T14:0{i % 10}:00+08:00")
            .ToArray();

        var text = (await CreateUserWriteTools().ClipMinuteAsync(
            Args(("minute_token", "obcn1"), ("time_ranges", ranges)), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.clip");
        text.Should().Contain("一次最多 50 段");
        _userClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Clip_DryRun_StillValidatesDsl_AndDoesNotTouchDownstream()
    {
        var invalid = (await CreateUserWriteTools().ClipMinuteAsync(
            Args(("minute_token", "obcn1"), ("time_ranges", new[] { "2026-10-01T14:00:00|2026-10-01T14:05:00" }), ("dry_run", true)),
            CancellationToken.None)).ToString()!;
        invalid.Should().Contain("[tool_error] minutes.clip", "校验先于预演：预演通过、实调失败毫无价值");

        var valid = (await CreateUserWriteTools().ClipMinuteAsync(
            Args(
                ("minute_token", "obcn1"),
                ("time_ranges", new[] { "2026-10-01T14:00:00+08:00|2026-10-01T14:05:00+08:00" }),
                ("title", "决议片段"),
                ("dry_run", true)),
            CancellationToken.None)).ToString()!;

        valid.Should().StartWith("[dry_run]");
        valid.Should().Contain("POST /open-apis/minutes/v1/minutes/obcn1/clip");
        valid.Should().NotContain("决议片段", "预演摘要只回字段长度，不回标题原文");
        _userClient.VerifyNoOtherCalls();
    }

    // ───────────────────── minutes.upload ─────────────────────

    [Fact]
    public async Task Upload_ShouldSendFileTokenOnly_AndReturnMinuteUrl()
    {
        UploadMinuteRequest? captured = null;

        _userClient
            .Setup(c => c.UploadMinuteAsync(It.IsAny<UploadMinuteRequest>(), It.IsAny<CancellationToken>()))
            .Callback((UploadMinuteRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<MinuteUrlResult>
            {
                Code = 0,
                Data = new MinuteUrlResult { MinuteUrl = "https://bytedance.larkoffice.com/minutes/obcnGenerated" },
            });

        var text = (await CreateUserWriteTools().UploadMinuteAsync(
            Args(("file_token", "boxcn_audio")), CancellationToken.None)).ToString()!;

        captured!.FileToken.Should().Be("boxcn_audio", "导入只交云盘 token——字节由平台自己搬运");
        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("minute_url").GetString().Should().Be("https://bytedance.larkoffice.com/minutes/obcnGenerated");
        document.RootElement.TryGetProperty("requested_range_count", out _).Should().BeFalse("非剪辑路径不带上段数噪声");
    }

    [Fact]
    public async Task Upload_DryRun_ShouldPreviewUploadRoute_AndNotTouchDownstream()
    {
        var text = (await CreateUserWriteTools().UploadMinuteAsync(
            Args(("file_token", "boxcn_audio"), ("dry_run", true)), CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/minutes/v1/minutes/upload");
        text.Should().NotContain("boxcn_audio");
        _userClient.VerifyNoOtherCalls();
    }

    // ───────────────────── minutes.subscribe / unsubscribe ─────────────────────

    [Fact]
    public async Task Subscribe_ShouldDefaultToClosedSetEventType_AndReturnSubscribedReceipt()
    {
        MinuteSubscriptionRequest? captured = null;

        _userClient
            .Setup(c => c.SubscribeMinuteAsync(It.IsAny<MinuteSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .Callback((MinuteSubscriptionRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var text = (await CreateUserWriteTools().SubscribeMinuteAsync(Args(), CancellationToken.None)).ToString()!;

        captured!.EventType.Should().Be("minutes.minute.generated_v1", "缺省即平台唯一可选值，模型可不传");
        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("subscribed").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("event_type").GetString().Should().Be("minutes.minute.generated_v1");
        _userClient.Verify(
            c => c.UnsubscribeMinuteAsync(It.IsAny<MinuteSubscriptionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never, "订阅不得走退订端点（两端 path 不同，写错即静默退订）");
    }

    [Fact]
    public async Task Unsubscribe_ShouldReturnUnsubscribedReceipt_NotSubscribed()
    {
        MinuteSubscriptionRequest? captured = null;
        _userClient
            .Setup(c => c.UnsubscribeMinuteAsync(It.IsAny<MinuteSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .Callback((MinuteSubscriptionRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var text = (await CreateUserWriteTools().UnsubscribeMinuteAsync(
            Args(("event_type", "minutes.minute.generated_v1")), CancellationToken.None)).ToString()!;

        captured!.EventType.Should().Be("minutes.minute.generated_v1");
        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("unsubscribed").GetBoolean().Should().BeTrue();
        document.RootElement.TryGetProperty("subscribed", out _).Should().BeFalse(
            "两端回执键刻意不同——共用 subscribed 会让模型把退订读成订阅成功");
    }

    [Fact]
    public async Task Subscribe_WhenEventTypeNotInClosedSet_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateUserWriteTools().SubscribeMinuteAsync(
            Args(("event_type", "minutes.minute.deleted_v1")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.subscribe");
        text.Should().Contain("event_type 仅支持 minutes.minute.generated_v1");
        _userClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_DryRun_ShouldPreviewSubscriptionRoute_AndNotTouchDownstream()
    {
        var text = (await CreateUserWriteTools().SubscribeMinuteAsync(
            Args(("dry_run", true)), CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/minutes/v1/minutes/subscription");
        _userClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_WhenPlatformFails_ShouldReturnStructuredErrorWithCode()
    {
        _userClient
            .Setup(c => c.SubscribeMinuteAsync(It.IsAny<MinuteSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 100, Msg = "no permission" });

        var text = (await CreateUserWriteTools().SubscribeMinuteAsync(Args(), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.subscribe");
        text.Should().Contain("100");
    }

    [Fact]
    public async Task UserWriteSurface_WhenClientMissing_ShouldNameTheUserIdentityRequirement()
    {
        var tools = new MinutesUserWriteTools();

        var text = (await tools.ClipMinuteAsync(
            Args(("minute_token", "obcn1"), ("time_ranges", new[] { "2026-10-01T14:00:00+08:00|2026-10-01T14:05:00+08:00" })),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] minutes.clip");
        text.Should().Contain("放行 user 身份", "生产面只有用户身份形态——缺席原因必须写到可执行的粒度");
    }
}
