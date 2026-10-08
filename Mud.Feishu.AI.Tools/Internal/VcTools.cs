// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// VideoConferencing 只读工具执行器（R6 / S3）：<c>vc.list_meetings</c> / <c>vc.get_meeting</c> /
/// <c>vc.list_participants</c> / <c>vc.get_recording</c> / <c>vc.list_reserves</c> / <c>vc.list_rooms</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域投影的核心是"最小披露"</b>：VC 的会议/参会人 DTO 带大量
/// <b>个人与网络标识</b>（<c>email</c> / <c>mobile</c> / <c>phone</c> / <c>employee_id</c> /
/// <c>internal_ip</c> / <c>public_ip</c> / <c>app_version</c>）。
/// 会议场景下模型只需要「谁参加了、什么时候进出的、待了多久」，
/// 不需要手机号与内网 IP —— 故投影<b>显式丢弃</b>这些字段（白名单的取舍点，而非遗漏）。
/// </para>
/// <para>
/// <b>软缺席</b>：五个客户端全部可空；缺席时对应工具返回结构化错误，
/// 而不是让整个 <c>vc.*</c> 域从注册表消失（对齐 <c>MinutesTools</c>/<c>OkrTools</c> 的纪律）。
/// </para>
/// </remarks>
internal sealed class VcTools(
    Mud.Feishu.IFeishuTenantV1VideoConferencingMeetinData? vcMeetinDataClient,
    Mud.Feishu.IFeishuTenantV1VideoConferencingMeeting? vcMeetingClient,
    Mud.Feishu.IFeishuTenantV1VideoConferencingRecording? vcRecordingClient,
    Mud.Feishu.IFeishuTenantV1VideoConferencingReserves? vcReservesClient,
    Mud.Feishu.IFeishuTenantV1VideoConferencingRoom? vcRoomClient,
    IOptions<FeishuAgentOptions> options)
{
    /// <summary>默认每页条数（官方上限 100；取 20 控量）。</summary>
    private const int DefaultPageSize = 20;

    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingMeetinData? _meetinDataClient = vcMeetinDataClient;
    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingMeeting? _meetingClient = vcMeetingClient;
    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingRecording? _recordingClient = vcRecordingClient;
    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingReserves? _reservesClient = vcReservesClient;
    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingRoom? _roomClient = vcRoomClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>vc.list_meetings：按时间范围查询会议列表。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantVcListMeetingsTool))]
    public Task<FeishuToolResult> ListMeetingsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcListMeetings, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _meetinDataClient
                ?? throw new ArgumentException("vc.list_meetings 需要 IFeishuTenantV1VideoConferencingMeetinData——宿主须启用 VC 域客户端");
            var args = VcListMeetingsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetMeetingPageListAsync(
                    args.StartTime,
                    args.EndTime,
                    meeting_status: args.MeetingStatus,
                    meeting_no: args.MeetingNo,
                    user_id: args.UserId,
                    room_id: args.RoomId,
                    page_size: args.PageSize ?? DefaultPageSize,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectMeetings);
        });
    }

    /// <summary>vc.get_meeting：获取单个会议详情。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantVcGetMeetingTool))]
    public Task<FeishuToolResult> GetMeetingAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcGetMeeting, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _meetingClient
                ?? throw new ArgumentException("vc.get_meeting 需要 IFeishuTenantV1VideoConferencingMeeting——宿主须启用 VC 域客户端");
            var args = VcGetMeetingArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetMeetingAsync(args.MeetingId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectMeeting);
        });
    }

    /// <summary>vc.list_participants：按会议号与时段时间查询参会人明细。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantVcListParticipantsTool))]
    public Task<FeishuToolResult> ListParticipantsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcListParticipants, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _meetinDataClient
                ?? throw new ArgumentException("vc.list_participants 需要 IFeishuTenantV1VideoConferencingMeetinData——宿主须启用 VC 域客户端");
            var args = VcListParticipantsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetParticipantPageListAsync(
                    args.MeetingStartTime,
                    args.MeetingEndTime,
                    args.MeetingNo,
                    meeting_status: args.MeetingStatus,
                    user_id: args.UserId,
                    room_id: args.RoomId,
                    page_size: args.PageSize ?? DefaultPageSize,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectParticipants);
        });
    }

    /// <summary>vc.get_recording：获取会议录制文件信息。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantVcGetRecordingTool))]
    public Task<FeishuToolResult> GetRecordingAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcGetRecording, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _recordingClient
                ?? throw new ArgumentException("vc.get_recording 需要 IFeishuTenantV1VideoConferencingRecording——宿主须启用 VC 域客户端");
            var args = VcGetRecordingArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetMeetingRecordingAsync(args.MeetingId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectRecording);
        });
    }

    /// <summary>vc.list_reserves：获取预约详情。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantVcListReservesTool))]
    public Task<FeishuToolResult> ListReservesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcListReserves, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _reservesClient
                ?? throw new ArgumentException("vc.list_reserves 需要 IFeishuTenantV1VideoConferencingReserves——宿主须启用 VC 域客户端");
            var args = VcListReservesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetReserveAsync(args.ReserveId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectReserve);
        });
    }

    /// <summary>vc.list_rooms：分页列出会议室。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantVcListRoomsTool))]
    public Task<FeishuToolResult> ListRoomsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcListRooms, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _roomClient
                ?? throw new ArgumentException("vc.list_rooms 需要 IFeishuTenantV1VideoConferencingRoom——宿主须启用 VC 域客户端");
            var args = VcListRoomsArgs.Unpack(arguments);

            // ⚠️ 该接口返回 FeishuApiPageListResult<T>（page_size 非空 + 独立 page_token），
            // 与其余 VC 接口的 FeishuApiResult<T> 形状不同——解包后 T = ApiPageListResult<MeetingRoomInfo>。
            var outcome = FeishuApiResultReader.Read(await client
                .GetMeetingRoomsPageListAsync(
                    room_level_id: args.RoomLevelId,
                    page_size: args.PageSize ?? DefaultPageSize,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectRooms);
        });
    }

    // ────────── 投影（白名单 + 最小披露 + 翻页契约） ──────────

    private static JsonObject ProjectMeetings(GetMeetingListResult data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.MeetingList ?? [])
        {
            // 有意的取舍：`email` / `mobile` / `employee_id` / `department` 不投影——
            // 会议列表场景不需要个人信息，投影越少越安全（最小披露）。
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["meeting_id"] = item.MeetingId,
                ["topic"] = item.MeetingTopic,
                ["meeting_type"] = item.MeetingType,
                ["organizer"] = item.Organizer,
                ["start_time"] = item.MeetingStartTime,
                ["end_time"] = item.MeetingEndTime,
                ["duration"] = item.MeetingDuration,
                ["participant_count"] = item.NumberOfParticipants,
                ["recording"] = item.Recording,
                ["is_external"] = item.IsExternal,
                ["meeting_instance_id"] = item.MeetingInstanceId,
            });
        }

        return envelope;
    }

    private static JsonObject ProjectMeeting(MeetingResult data)
    {
        var meeting = data.Meeting;
        var envelope = new JsonObject
        {
            ["has_related_artifacts"] = data.RelatedArtifacts is not null,
        };
        if (meeting is null)
        {
            return envelope;
        }

        envelope["meeting_id"] = meeting.Id;
        envelope["meeting_no"] = meeting.MeetingNo;
        envelope["topic"] = meeting.Topic;
        envelope["url"] = meeting.Url;
        envelope["password"] = meeting.Password;
        envelope["meeting_connect"] = meeting.MeetingConnect;
        envelope["note_id"] = meeting.NoteId;
        envelope["create_time"] = meeting.CreateTime;
        envelope["start_time"] = meeting.StartTime;
        envelope["end_time"] = meeting.EndTime;
        envelope["status"] = meeting.Status;
        envelope["host_user_id"] = meeting.HostUser?.Id;
        envelope["participant_count"] = meeting.ParticipantCount;
        envelope["participant_count_accumulated"] = meeting.ParticipantCountAccumulated;
        return envelope;
    }

    private static JsonObject ProjectParticipants(GetParticipantListResult data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.ParticipantList ?? [])
        {
            // 有意的取舍：`email` / `phone` / `employee_id` / `internal_ip` / `public_ip` /
            // `app_version` 不投影——参会人明细只需要"谁、何时进出、待了多久、音视频是否开启"。
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["name"] = item.ParticipantName,
                ["user_id"] = item.UserId,
                ["department"] = item.Department,
                ["meeting_room_id"] = item.MeetingRoomId,
                ["join_time"] = item.JoinTime,
                ["leave_time"] = item.LeaveTime,
                ["time_in_meeting"] = item.TimeInMeeting,
                ["leave_reason"] = item.LeaveReason,
                ["accept_status"] = item.AcceptStatus,
                ["is_external"] = item.IsExternal,
                ["audio"] = item.Audio,
                ["video"] = item.Video,
                ["sharing"] = item.Sharing,
            });
        }

        return envelope;
    }

    private static JsonObject ProjectRecording(GetMeetingRecordingResult data)
        => new()
        {
            ["url"] = data.Recording?.Url,
            ["duration"] = data.Recording?.Duration,
            ["available"] = !string.IsNullOrEmpty(data.Recording?.Url),
        };

    private static JsonObject ProjectReserve(GetReserveResult data)
    {
        var reserve = data.Reserve;
        if (reserve is null)
        {
            return new JsonObject();
        }

        return new JsonObject
        {
            ["reserve_id"] = reserve.Id,
            ["meeting_no"] = reserve.MeetingNo,
            ["url"] = reserve.Url,
            ["app_link"] = reserve.AppLink,
            ["live_link"] = reserve.LiveLink,
            ["password"] = reserve.Password,
            ["end_time"] = reserve.EndTime,
            ["expire_status"] = reserve.ExpireStatus,
            ["reserve_user_id"] = reserve.ReserveUserId,
            ["topic"] = reserve.MeetingSettings?.Topic,
            ["auto_record"] = reserve.MeetingSettings?.AutoRecord,
            ["meeting_connect"] = reserve.MeetingSettings?.MeetingConnect,
        };
    }

    private static JsonObject ProjectRooms(ApiPageListResult<MeetingRoomInfo> data)
    {
        var envelope = PageHeader(data);
        foreach (var item in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["room_id"] = item.RoomId,
                ["display_id"] = item.DisplayId,
                ["name"] = item.Name,
                ["capacity"] = item.Capacity,
                ["description"] = item.Description,
                ["room_level_id"] = item.RoomLevelId,
                ["path"] = item.Path is null ? null : new JsonArray([.. item.Path.Select(static p => (JsonNode?)JsonValue.Create(p))]),
            });
        }

        return envelope;
    }

    /// <summary>翻页信封（items + has_more + 可选 page_token）。</summary>
    private static JsonObject PageHeader(ApiPageListResult data)
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

        return envelope;
    }
}
