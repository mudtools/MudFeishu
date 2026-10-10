// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// VideoConferencing 域工具（R6 / S3）：<c>vc.*</c> 10 个工具的投影、最小披露、dry-run 与边界。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域最需要锁住的是"最小披露"</b>：VC 的原始 DTO 带手机号/邮箱/员工号/内网 IP。
/// 投影白名单<b>刻意丢弃</b>这些字段，故用例显式断言"输出里没有这些值"——
/// 否则未来有人"顺手补全字段"时不会有任何信号。
/// </para>
/// <para>
/// 第二组断言是**平台约束前置化**：参会人接口必须给时段（而非 meeting_id）、
/// 邀请一次最多 10 人——这两条都在本地拦下，用例锁住"错误文案可读且不触下游"。
/// </para>
/// </remarks>
public class VcToolsTests
{
    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static VcTools CreateReadTools(
        Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingMeetinData>? meetinData = null,
        Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingMeeting>? meeting = null,
        Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingRecording>? recording = null,
        Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingReserves>? reserves = null,
        Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingRoom>? room = null)
        => new(
            meetinData?.Object, meeting?.Object, recording?.Object, reserves?.Object, room?.Object,
            Options.Create(AgentOptions()));

    private static VcWriteTools CreateWriteTools(
        Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingMeeting>? meeting = null,
        Mock<Mud.Feishu.IFeishuUserV1VideoConferencingMeeting>? userMeeting = null,
        Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingReserves>? reserves = null)
        => new(meeting?.Object, userMeeting?.Object, reserves?.Object, Options.Create(AgentOptions()));

    // ───────────────────── 只读：调用形状与最小披露 ─────────────────────

    [Fact]
    public async Task ListMeetings_ShouldCallDownstream_WithTimeRangeAndPageSize()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingMeetinData>();
        client.Setup(c => c.GetMeetingPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMeetingListResult>
            {
                Code = 0,
                Data = new GetMeetingListResult
                {
                    MeetingList =
                    [
                        new MeetingDataInfo
                        {
                            MeetingId = "705605196",
                            MeetingTopic = "周会",
                            MeetingStartTime = "2026-10-01T09:00:00+08:00",
                            MeetingEndTime = "2026-10-01T10:00:00+08:00",
                            MeetingDuration = "60",
                            NumberOfParticipants = "8",
                            Recording = true,
                            // 下面两个字段**不得**出现在投影里（最小披露）。
                            Email = "leak@example.com",
                            Mobile = "13800000000",
                        },
                    ],
                    HasMore = true,
                    PageToken = "pt_next",
                },
            });

        var result = await CreateReadTools(meetinData: client).ListMeetingsAsync(
            Args(("start_time", "2026-10-01T00:00:00+08:00"), ("end_time", "2026-10-02T00:00:00+08:00")),
            CancellationToken.None);

        var text = result.ToString()!;
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        var item = root.GetProperty("items")[0];

        item.GetProperty("meeting_id").GetString().Should().Be("705605196");
        item.GetProperty("topic").GetString().Should().Be("周会");
        item.GetProperty("recording").GetBoolean().Should().BeTrue();
        root.GetProperty("has_more").GetBoolean().Should().BeTrue();
        root.GetProperty("page_token").GetString().Should().Be("pt_next");

        text.Should().NotContain("leak@example.com", "会议列表投影必须做最小披露：不回邮箱");
        text.Should().NotContain("13800000000", "同上：不回手机号");

        client.Verify(
            c => c.GetMeetingPageListAsync(
                "2026-10-01T00:00:00+08:00", "2026-10-02T00:00:00+08:00", null, null, null, null, null, null, null,
                20, null, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "时间范围必须原样透传，默认每页 20 条");
    }

    [Fact]
    public async Task ListParticipants_ShouldProjectAttendance_WithoutContactFields()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingMeetinData>();
        client.Setup(c => c.GetParticipantPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetParticipantListResult>
            {
                Code = 0,
                Data = new GetParticipantListResult
                {
                    ParticipantList =
                    [
                        new MeetingParticipantInfo
                        {
                            ParticipantName = "张三",
                            UserId = "ou_zhangsan",
                            JoinTime = "2026-10-01T09:01:00+08:00",
                            LeaveTime = "2026-10-01T09:59:00+08:00",
                            TimeInMeeting = "58",
                            LeaveReason = "1",
                            Audio = true,
                            Video = false,
                            // 下面这些**不得**出现在投影里（最小披露）。
                            Email = "p@example.com",
                            Phone = "13900000000",
                            InternalIp = "10.0.0.7",
                        },
                    ],
                },
            });

        var result = await CreateReadTools(meetinData: client).ListParticipantsAsync(
            Args(
                ("meeting_no", "705605196"),
                ("meeting_start_time", "2026-10-01T09:00:00+08:00"),
                ("meeting_end_time", "2026-10-01T10:00:00+08:00")),
            CancellationToken.None);

        var text = result.ToString()!;
        using var document = JsonDocument.Parse(text);
        var item = document.RootElement.GetProperty("items")[0];

        item.GetProperty("name").GetString().Should().Be("张三");
        item.GetProperty("time_in_meeting").GetString().Should().Be("58");
        item.GetProperty("audio").GetBoolean().Should().BeTrue();

        text.Should().NotContain("p@example.com");
        text.Should().NotContain("13900000000");
        text.Should().NotContain("10.0.0.7", "参会人投影不含内网 IP（最小披露）");
    }

    [Fact]
    public async Task GetRecording_WhenNoRecording_ShouldReportUnavailable()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingRecording>();
        client.Setup(c => c.GetMeetingRecordingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMeetingRecordingResult>
            {
                Code = 0,
                Data = new GetMeetingRecordingResult { Recording = new MeetingRecording() },
            });

        var result = await CreateReadTools(recording: client).GetRecordingAsync(
            Args(("meeting_id", "705605196")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("available").GetBoolean().Should().BeFalse(
            "未录制时模型需要看到明确的 available=false，而不是一个空对象");
    }

    [Fact]
    public async Task ListRooms_ShouldProjectRoomInfo()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingRoom>();
        client.Setup(c => c.GetMeetingRoomsPageListAsync(
                It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<MeetingRoomInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<MeetingRoomInfo>
                {
                    Items =
                    [
                        new MeetingRoomInfo
                        {
                            RoomId = "omm_x",
                            DisplayId = "LM1",
                            Name = "大会议室",
                            Capacity = 20,
                            RoomLevelId = "omb_l1",
                            Path = ["omb_root", "omb_l1"],
                        },
                    ],
                },
            });

        var result = await CreateReadTools(room: client).ListRoomsAsync(Args(), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var item = document.RootElement.GetProperty("items")[0];
        item.GetProperty("room_id").GetString().Should().Be("omm_x");
        item.GetProperty("name").GetString().Should().Be("大会议室");
        item.GetProperty("capacity").GetInt32().Should().Be(20);
        item.GetProperty("path").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetMeeting_MissingId_ShouldReturnStructuredError()
    {
        var result = await CreateReadTools(meeting: new Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingMeeting>())
            .GetMeetingAsync(Args(), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] vc.get_meeting");
    }

    [Fact]
    public async Task ReadTool_ClientAbsent_ShouldFailFast_WithActionableError()
    {
        var result = await CreateReadTools().GetRecordingAsync(
            Args(("meeting_id", "705605196")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] vc.get_recording");
        text.Should().Contain("IFeishuTenantV1VideoConferencingRecording",
            "缺席必须给出可操作提示（哪个客户端），而不是裸 NullReferenceException");
    }

    // ───────────────────── 写面：dry-run / 平台约束前置化 ─────────────────────

    [Fact]
    public async Task DeleteReserve_DryRun_ShouldNotCallDownstream_AndReturnRoute()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1VideoConferencingReserves>();
        var tools = CreateWriteTools(reserves: client);

        var result = await tools.DeleteReserveAsync(
            Args(("reserve_id", "reserve_secret"), ("dry_run", true)), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("DELETE /open-apis/vc/v1/reserves/{reserve_id}");
        text.Should().NotContain("reserve_secret", "预演摘要不回显标识原文");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EndMeeting_DryRun_ShouldNotCallDownstream()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1VideoConferencingMeeting>();
        var tools = CreateWriteTools(userMeeting: client);

        var result = await tools.EndMeetingAsync(
            Args(("meeting_id", "705605196"), ("dry_run", true)), CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        result.ToString().Should().Contain("PATCH /open-apis/vc/v1/meetings/{meeting_id}/end");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InviteParticipants_MoreThanTen_ShouldBeRejectedWithoutCallingDownstream()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1VideoConferencingMeeting>();
        var tools = CreateWriteTools(userMeeting: client);
        var eleven = Enumerable.Range(0, 11).Select(i => $"ou_{i}").ToArray();

        var result = await tools.InviteParticipantsAsync(
            Args(("meeting_id", "705605196"), ("invitee_ids", eleven)), CancellationToken.None);

        result.ToString().Should().Contain("10",
            "平台硬约束（一次最多 10 人）必须在本地前置拦下，而不是让平台返错");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InviteParticipants_ShouldReturnPerUserResult()
    {
        InviteMeetingRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuUserV1VideoConferencingMeeting>();
        client.Setup(c => c.InviteMeetingAsync(
                It.IsAny<string>(), It.IsAny<InviteMeetingRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, InviteMeetingRequest, string?, CancellationToken>(
                (_, request, _, _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<InviteMeetingResult>
            {
                Code = 0,
                Data = new InviteMeetingResult
                {
                    InviteResults =
                    [
                        new MeetingInviteStatus { Id = "ou_1", UserType = 1, Status = 1 },
                        new MeetingInviteStatus { Id = "ou_2", UserType = 1, Status = 2 },
                    ],
                },
            });

        var result = await CreateWriteTools(userMeeting: client).InviteParticipantsAsync(
            Args(("meeting_id", "705605196"), ("invitee_ids", new[] { "ou_1", "ou_2" })),
            CancellationToken.None);

        captured!.Invitees.Should().HaveCount(2);
        captured.Invitees[0].Id.Should().Be("ou_1");
        captured.Invitees[0].UserType.Should().Be(1, "未给 user_type 时默认 1（飞书用户）");

        using var document = JsonDocument.Parse(result.ToString()!);
        var results = document.RootElement.GetProperty("invite_results");
        results[0].GetProperty("invited").GetBoolean().Should().BeTrue();
        results[1].GetProperty("invited").GetBoolean().Should().BeFalse(
            "邀请失败的人必须显式 invited=false（模型据此如实汇报，而不是笼统说'已邀请'）");
    }

    [Fact]
    public async Task WriteTool_ClientAbsent_ShouldFailFast()
    {
        var result = await CreateWriteTools().EndMeetingAsync(
            Args(("meeting_id", "705605196")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] vc.end_meeting");
        text.Should().Contain("IFeishuUserV1VideoConferencingMeeting",
            "user 身份工具的缺席提示要指明是哪个（用户身份）接口");
    }
}
