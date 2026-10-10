// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.DataModels.Minutes;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Minutes 妙记域工具（R5 / F-11 + R7 / A3）：5 个工具的链路实参、逐字稿分窗口与参数负例。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类的核心是"逐字稿窗口"</b>（R7/A3 改造）：首版把 <c>Transcript</c> 按预览长度截断并回填
/// <c>transcript_truncated</c>，模型<b>永远拿不到</b>后半段（没有任何参数能取回来）。
/// 改造后逐字稿按 <c>transcript_offset</c>/<c>transcript_limit</c> 分窗口返回，
/// 并回填 <c>transcript_total_length</c>/<c>has_more</c>/<c>next_offset</c> 作为续读依据——
/// 本类用"第 N 页内容 == 总长切片"锁定这条语义，防止参数再次变成"声明了但没人读"。
/// </para>
/// <para>
/// <b>参数校验前置</b>：窗口参数越界在调用下游之前拒绝（<c>validation/invalid_args</c>），
/// 用例同时断言<b>下游零调用</b>（否则"本地拒绝"只是文案）。
/// </para>
/// </remarks>
public class MinutesToolsTests
{
    private const int DefaultWindow = 10000;
    private const int MaxWindow = 50000;

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static MinutesReadTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute>? client = null)
        => new(client?.Object);

    private static Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute> ClientWithArtifacts(GetMinuteArtifactsResult data)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute>();
        client.Setup(c => c.GetMinuteArtifactsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMinuteArtifactsResult> { Code = 0, Data = data });
        return client;
    }

    private static string Transcript(int length)
    {
        // 内容随下标循环（切片错位会立刻在用例里看出来）。
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = (char)('A' + (i % 26));
        }

        return new string(buffer);
    }

    /// <summary>把工具结果文本解析回 JSON 对象（断言字段值而不是字符串包含，避免"文案碰巧一样"的假绿）。</summary>
    private static JsonObject Payload(FeishuToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    // ───────────────────── 契约面 ─────────────────────

    [Fact]
    public void MinutesTools_Should_Be_Read_Only_With_Minutes_Scope()
    {
        foreach (var toolName in new[]
                 {
                     FeishuToolNames.MinutesGet,
                     FeishuToolNames.MinutesGetArtifacts,
                     FeishuToolNames.MinutesSearch,
                     FeishuToolNames.MinutesGetStatistics,
                     FeishuToolNames.MinutesGetMedia,
                 })
        {
            var contract = FeishuToolContracts.ByToolName[toolName];
            contract.IsWrite.Should().BeFalse($"{toolName} 是只读工具（本域无写面）");
            contract.Identity.Should().Be("tenant", $"{toolName} 走租户令牌客户端");
            contract.RequiredScopes.Should().BeEquivalentTo(new[] { "minutes:minutes:readonly" });
        }
    }

    [Fact]
    public void GetArtifacts_Should_Expose_Transcript_Window_Parameters()
    {
        var schema = FeishuToolSchemas.SchemaByToolName[FeishuToolNames.MinutesGetArtifacts];

        schema.Should().Contain("transcript_offset", "逐字稿窗口的续读游标必须对模型可见（否则参数形同虚设）");
        schema.Should().Contain("transcript_limit");
        schema.Should().NotContain("transcript_truncated");
    }

    // ───────────────────── 逐字稿分窗口（核心） ─────────────────────

    [Fact]
    public async Task GetArtifacts_Should_Window_Transcript_With_Offset()
    {
        var transcript = Transcript(25000);
        var client = ClientWithArtifacts(new GetMinuteArtifactsResult
        {
            Summary = "季度复盘",
            Transcript = transcript,
        });

        var result = await CreateTools(client).GetMinuteArtifactsAsync(
            Args(("minute_token", "obcn123"), ("transcript_offset", 10000), ("transcript_limit", 10000)),
            CancellationToken.None);

        var payload = Payload(result);

        payload["transcript"]!.GetValue<string>().Should().Be(transcript.Substring(10000, 10000),
            "第 2 页内容必须等于总长切片（参数被忽略时这里会错位）");
        payload["transcript_total_length"]!.GetValue<int>().Should().Be(25000);
        payload["transcript_offset"]!.GetValue<int>().Should().Be(10000);
        payload["transcript_limit"]!.GetValue<int>().Should().Be(10000);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
        payload["next_offset"]!.GetValue<int>().Should().Be(20000, "续读游标必须指向下一段起点");
    }

    [Fact]
    public async Task GetArtifacts_Should_Close_Window_On_Last_Page()
    {
        var transcript = Transcript(15000);
        var client = ClientWithArtifacts(new GetMinuteArtifactsResult { Transcript = transcript });

        var result = await CreateTools(client).GetMinuteArtifactsAsync(
            Args(("minute_token", "obcn123"), ("transcript_offset", 10000), ("transcript_limit", 10000)),
            CancellationToken.None);

        var payload = Payload(result);

        payload["transcript"]!.GetValue<string>().Should().Be(transcript.Substring(10000, 5000),
            "末页只返回剩余部分（不足一个窗口）");
        payload["has_more"]!.GetValue<bool>().Should().BeFalse();
        payload["next_offset"].Should().BeNull("读完后不再给续读游标");
    }

    [Fact]
    public async Task GetArtifacts_Should_Default_To_First_Window()
    {
        var transcript = Transcript(12000);
        var client = ClientWithArtifacts(new GetMinuteArtifactsResult { Transcript = transcript });

        var result = await CreateTools(client).GetMinuteArtifactsAsync(
            Args(("minute_token", "obcn123")),
            CancellationToken.None);

        var payload = Payload(result);

        payload["transcript_offset"]!.GetValue<int>().Should().Be(0);
        payload["transcript_limit"]!.GetValue<int>().Should().Be(DefaultWindow, "默认窗口 1 万字符");
        payload["transcript"]!.GetValue<string>().Should().Be(transcript.Substring(0, DefaultWindow));
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task GetArtifacts_Should_Flag_Offset_Out_Of_Range()
    {
        var client = ClientWithArtifacts(new GetMinuteArtifactsResult { Transcript = Transcript(100) });

        var result = await CreateTools(client).GetMinuteArtifactsAsync(
            Args(("minute_token", "obcn123"), ("transcript_offset", 5000)),
            CancellationToken.None);

        var payload = Payload(result);

        // 超界不抛错（模型可能按上一轮游标恰好读到末尾），但必须如实标记——
        // 否则"空串"会被模型读成"这段逐字稿就是空的"。
        payload["transcript"]!.GetValue<string>().Should().BeEmpty();
        payload["offset_out_of_range"]!.GetValue<bool>().Should().BeTrue();
        payload["has_more"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public void ComputeTranscriptWindow_Should_Split_By_Offset_And_Limit()
    {
        var transcript = Transcript(250);

        var first = MinutesReadTools.ComputeTranscriptWindow(transcript, null, 100);
        first.Text.Should().Be(transcript.Substring(0, 100));
        first.TotalLength.Should().Be(250);
        first.HasMore.Should().BeTrue();
        first.NextOffset.Should().Be(100);

        var second = MinutesReadTools.ComputeTranscriptWindow(transcript, first.NextOffset, 100);
        second.Text.Should().Be(transcript.Substring(100, 100));
        second.OffsetOutOfRange.Should().BeFalse();

        var last = MinutesReadTools.ComputeTranscriptWindow(transcript, second.NextOffset, 100);
        last.Text.Should().Be(transcript.Substring(200, 50));
        last.HasMore.Should().BeFalse();
        last.NextOffset.Should().BeNull();
    }

    // ───────────────────── 参数负例（本地拒绝，下游零调用） ─────────────────────

    [Fact]
    public async Task GetArtifacts_Should_Reject_Negative_Offset_Without_Calling_Downstream()
    {
        var client = ClientWithArtifacts(new GetMinuteArtifactsResult());

        var result = await CreateTools(client).GetMinuteArtifactsAsync(
            Args(("minute_token", "obcn123"), ("transcript_offset", -1)),
            CancellationToken.None);

        result.Error.Should().NotBeNull("参数越界必须产出结构化错误载荷（B2）");
        result.Error!.Category.Should().Be("validation");
        result.Error.Subtype.Should().Be(ToolErrorSubtype.InvalidArgs);
        result.ToString().Should().Contain("transcript_offset");
        client.Invocations.Should().BeEmpty("本地拒绝不得消耗一次下游调用（限频 5 次/秒）");
    }

    [Fact]
    public async Task GetArtifacts_Should_Reject_Over_Max_Window()
    {
        var client = ClientWithArtifacts(new GetMinuteArtifactsResult());

        var result = await CreateTools(client).GetMinuteArtifactsAsync(
            Args(("minute_token", "obcn123"), ("transcript_limit", MaxWindow + 1)),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("validation");
        result.ToString().Should().Contain(MaxWindow.ToString(System.Globalization.CultureInfo.InvariantCulture));
        client.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetArtifacts_Should_Reject_Non_Integer_Limit()
    {
        var client = ClientWithArtifacts(new GetMinuteArtifactsResult());

        var result = await CreateTools(client).GetMinuteArtifactsAsync(
            Args(("minute_token", "obcn123"), ("transcript_limit", "abc")),
            CancellationToken.None);

        result.Error.Should().NotBeNull("整数参数无法解析时不得静默降级为默认值");
        client.Invocations.Should().BeEmpty();
    }

    // ───────────────────── 搜索（A3：找会议入口） ─────────────────────

    [Fact]
    public async Task Search_Should_Map_Keyword_And_TimeRange()
    {
        SearchMinutesRequest? captured = null;
        string? capturedPageToken = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute>();
        client.Setup(c => c.SearchMinutesAsync(
                It.IsAny<SearchMinutesRequest>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<SearchMinutesRequest, int?, string?, CancellationToken>((request, _, pageToken, _) =>
            {
                captured = request;
                capturedPageToken = pageToken;
            })
            .ReturnsAsync(new FeishuApiResult<SearchMinutesResult>
            {
                Code = 0,
                Data = new SearchMinutesResult
                {
                    Items =
                    [
                        new MinutesSearchItem
                        {
                            Token = "obcn1",
                            DisplayInfo = "季度复盘会",
                            MetaData = new MinutesMeta { Description = "复盘", AppLink = "https://example/minutes/obcn1" },
                        },
                    ],
                    Total = 1,
                    HasMore = true,
                    PageToken = "next-token",
                    Notice = "结果已被截断",
                },
            });

        var result = await CreateTools(client).SearchMinutesAsync(
            Args(
                ("query", "季度复盘"),
                ("owner_ids", new[] { "ou_1" }),
                ("create_time_start", "2026-03-01T00:00:00+08:00"),
                ("create_time_end", "2026-03-21T00:00:00+08:00"),
                ("page_token", "prev-token")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Query.Should().Be("季度复盘");
        captured.Filter.Should().NotBeNull();
        captured.Filter!.OwnerIds.Should().BeEquivalentTo(new[] { "ou_1" });
        captured.Filter.CreateTime!.StartTime.Should().Be("2026-03-01T00:00:00+08:00");
        captured.Filter.CreateTime.EndTime.Should().Be("2026-03-21T00:00:00+08:00");
        capturedPageToken.Should().Be("prev-token", "翻页游标必须原样回传");

        var payload = Payload(result);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
        payload["page_token"]!.GetValue<string>().Should().Be("next-token");
        payload["items"]!.AsArray()[0]!["minute_token"]!.GetValue<string>().Should().Be("obcn1");
    }

    [Fact]
    public async Task Search_Should_Reject_Missing_Query()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute>();

        var result = await CreateTools(client).SearchMinutesAsync(
            Args(("owner_ids", new[] { "ou_1" })),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("validation");
        client.Invocations.Should().BeEmpty();
    }

    // ───────────────────── 媒体与统计（A3 新增工具） ─────────────────────

    [Fact]
    public async Task GetMedia_Should_Return_Url_Only()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute>();
        client.Setup(c => c.GetMinuteMediaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMinuteMediaResult>
            {
                Code = 0,
                Data = new GetMinuteMediaResult { DownloadUrl = "https://example.com/minutes/obcn1.mp4" },
            });

        var result = await CreateTools(client).GetMinuteMediaAsync(
            Args(("minute_token", "obcn1")),
            CancellationToken.None);

        var text = result.ToString();
        var payload = Payload(result);

        payload["download_url"]!.GetValue<string>().Should().Be("https://example.com/minutes/obcn1.mp4");

        // A10 二进制防线：只回填 URL，且永不出现 base64 载荷。
        payload.ContainsKey("content").Should().BeFalse();
        payload.ContainsKey("bytes").Should().BeFalse();
        text.Should().NotContain("base64");
        text.Length.Should().BeLessThan(1000, "媒体结果只能是链接文本");
    }

    [Fact]
    public async Task GetStatistics_Should_Project_Uv_Pv_And_Visits()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1MinutesMinute>();
        client.Setup(c => c.GetMinuteStatisticsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMinuteStatisticsResult>
            {
                Code = 0,
                Data = new GetMinuteStatisticsResult
                {
                    Statistics = new MinuteStatistics
                    {
                        UserViewCount = "12",
                        PageViewCount = "30",
                        UserViewList = [new UserViewDetail { UserId = "ou_1", ViewTime = "1700000000" }],
                    },
                },
            });

        var result = await CreateTools(client).GetMinuteStatisticsAsync(
            Args(("minute_token", "obcn1")),
            CancellationToken.None);

        var payload = Payload(result);

        payload["uv"]!.GetValue<string>().Should().Be("12");
        payload["pv"]!.GetValue<string>().Should().Be("30");
        payload["visit_list"]!.AsArray()[0]!["user_id"]!.GetValue<string>().Should().Be("ou_1");
    }

    // ───────────────────── 软依赖（客户端缺席） ─────────────────────

    [Fact]
    public async Task Tools_Should_Report_Actionable_Error_When_Minutes_Client_Missing()
    {
        var result = await CreateTools(null).GetMinuteArtifactsAsync(
            Args(("minute_token", "obcn1")),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("validation");
        result.ToString().Should().Contain("IFeishuTenantV1MinutesMinute",
            "软缺席时给出的错误必须告诉宿主该启用什么");
    }
}
