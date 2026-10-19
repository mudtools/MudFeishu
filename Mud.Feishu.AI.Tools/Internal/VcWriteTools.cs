// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// VideoConferencing 写入工具执行器（R6 / S3）：<c>vc.set_host</c> / <c>vc.invite_participants</c> /
/// <c>vc.end_meeting</c> / <c>vc.delete_reserve</c> + 会议机器人三枚
/// （<c>vc.join_meeting</c> / <c>vc.leave_meeting</c> / <c>vc.send_meeting_message</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>前四个写工具都是"会中/会前管理动作"</b>：改主持人、邀人、结束会议、删预约。
/// 机器人类三枚则是<b>机器人自身的动作</b>（入会/离会/会中发言），走同一个
/// <c>IFeishuTenantV1VideoConferencingBot</c> 客户端（租户身份，无新增身份轴要求）。
/// 它们与"创建会议"不同——后者（<c>vc.apply_reserve</c>）需要 10+ 嵌套字段
/// （时区/循环/会议室分配/会前设置），**有意不策展**（部分策展会产出语义错误的预约，
/// 比没有该工具更糟），理由记录在 <c>FeishuVcToolInterfaces.cs</c> 的域头注释与 R6 主文档 §12.6。
/// </para>
/// <para>
/// <b>身份轴</b>：<c>invite</c> / <c>end</c> 走 SDK 的 <b>User</b> 接口
/// （<c>IFeishuUserV1VideoConferencingMeeting</c>），宿主须在 <c>AllowedIdentities</c> 放行 <c>user</c>。
/// </para>
/// <para>
/// <b>dry-run 纪律</b>：<c>dry_run=true</c> 时<b>不调用下游</b>，只回 method/path 模板与字段长度摘要；
/// <b>不回显 meeting_id / reserve_id 原文</b>（dry-run 不得成为回显通道）。
/// </para>
/// </remarks>
internal sealed class VcWriteTools(
    Mud.Feishu.IFeishuTenantV1VideoConferencingMeeting? vcMeetingClient,
    Mud.Feishu.IFeishuUserV1VideoConferencingMeeting? vcUserMeetingClient,
    Mud.Feishu.IFeishuTenantV1VideoConferencingReserves? vcReservesClient,
    Mud.Feishu.IFeishuTenantV1VideoConferencingBot? vcBotClient,
    IOptions<FeishuAgentOptions> options)
{
    /// <summary>一次性邀请人数上限（平台硬约束：最多 10 人）。</summary>
    private const int MaxInvitees = 10;

    /// <summary>默认用户类型（1 = 飞书用户）。</summary>
    private const int DefaultUserType = 1;

    /// <summary>机器人入会方式（平台当前仅支持 1 = 会议号入会）。</summary>
    private const int JoinTypeByMeetingNo = 1;

    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingMeeting? _meetingClient = vcMeetingClient;
    private readonly Mud.Feishu.IFeishuUserV1VideoConferencingMeeting? _userMeetingClient = vcUserMeetingClient;
    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingReserves? _reservesClient = vcReservesClient;
    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingBot? _botClient = vcBotClient;
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>vc.set_host：改设进行中会议的主持人（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantVcSetHostTool))]
    public Task<ToolResult> SetHostAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcSetHost, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _meetingClient
                ?? throw new ArgumentException("vc.set_host 需要 IFeishuTenantV1VideoConferencingMeeting——宿主须启用 VC 域客户端");
            var args = VcSetHostArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/vc/v1/meetings/{meeting_id}/set_host",
                    null,
                    ("meeting_id", args.MeetingId.Length),
                    ("host_user_id", args.HostUserId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .SetHostMeetingAsync(
                    args.MeetingId,
                    new SetHostMeetingRequest
                    {
                        HostUser = new MeetingUser
                        {
                            Id = args.HostUserId,
                            UserType = args.UserType ?? DefaultUserType,
                        },
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["meeting_id"] = args.MeetingId,
                ["host_user_id"] = data.HostUser?.Id ?? args.HostUserId,
            });
        });
    }

    /// <summary>vc.invite_participants：邀请用户入会（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuUserVcInviteParticipantsTool))]
    public Task<ToolResult> InviteParticipantsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcInviteParticipants, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _userMeetingClient
                ?? throw new ArgumentException(
                    "vc.invite_participants 需要 IFeishuUserV1VideoConferencingMeeting（用户身份接口）——宿主须启用 VC 域客户端");
            var args = VcInviteParticipantsArgs.Unpack(arguments);

            // 平台硬约束（最多 10 人）：本地拦下比让平台返回语焉不详的错误更可读。
            if (args.InviteeIds.Length > MaxInvitees)
            {
                throw new ArgumentException(
                    $"invitee_ids 一次最多 {MaxInvitees} 人（平台限制），实际 {args.InviteeIds.Length} 人——请分批邀请");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/vc/v1/meetings/{meeting_id}/invite",
                    null,
                    ("meeting_id", args.MeetingId.Length),
                    ("invitee_ids", args.InviteeIds.Length)));
            }

            var invitees = args.InviteeIds
                .Select(id => new MeetingUser { Id = id, UserType = args.UserType ?? DefaultUserType })
                .ToArray();

            var outcome = FeishuApiResultReader.Read(await client
                .InviteMeetingAsync(
                    args.MeetingId,
                    new InviteMeetingRequest { Invitees = invitees },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data =>
            {
                var result = new JsonObject
                {
                    ["meeting_id"] = args.MeetingId,
                    ["invite_results"] = new JsonArray(),
                };
                foreach (var status in data.InviteResults ?? [])
                {
                    result["invite_results"]!.AsArray().AddNode(new JsonObject
                    {
                        ["user_id"] = status.Id,
                        ["user_type"] = status.UserType,
                        ["invited"] = status.Status == 1,
                    });
                }

                return result;
            });
        });
    }

    /// <summary>vc.end_meeting：结束进行中的会议（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuUserVcEndMeetingTool))]
    public Task<ToolResult> EndMeetingAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcEndMeeting);
        return executor.RunAsync(async () =>
        {
            var client = _userMeetingClient
                ?? throw new ArgumentException(
                    "vc.end_meeting 需要 IFeishuUserV1VideoConferencingMeeting（用户身份接口）——宿主须启用 VC 域客户端");
            var args = VcEndMeetingArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/vc/v1/meetings/{meeting_id}/end",
                    null,
                    ("meeting_id", args.MeetingId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .EndMeetingAsync(args.MeetingId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["ended"] = true,
                ["meeting_id"] = args.MeetingId,
            });
        });
    }

    /// <summary>vc.delete_reserve：删除预约（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantVcDeleteReserveTool))]
    public Task<ToolResult> DeleteReserveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcDeleteReserve);
        return executor.RunAsync(async () =>
        {
            var client = _reservesClient
                ?? throw new ArgumentException("vc.delete_reserve 需要 IFeishuTenantV1VideoConferencingReserves——宿主须启用 VC 域客户端");
            var args = VcDeleteReserveArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/vc/v1/reserves/{reserve_id}",
                    null,
                    ("reserve_id", args.ReserveId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .DeleteReserveAsync(args.ReserveId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["deleted"] = true,
                ["reserve_id"] = args.ReserveId,
            });
        });
    }

    // ────────── 会议机器人（入会 / 离会 / 会中消息） ──────────

    /// <summary>vc.join_meeting：机器人以会议号入会（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantVcJoinMeetingTool))]
    public Task<ToolResult> JoinMeetingAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcJoinMeeting, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireBotClient(executor.ToolName);
            var args = VcJoinMeetingArgs.Unpack(arguments);

            if (args.Action is not (null or 1 or 2))
            {
                throw new ArgumentException($"action '{args.Action}': 入会动作只支持 1（仅加入已存在的会议）/ 2（发起并加入日程会议）");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/vc/v1/bots/join",
                    "入会成功后请保存返回的长数字 meeting.id——离会与会中消息只用它。" + ToolDryRun.IdempotencyNote(null),
                    ("meeting_no", args.MeetingNo.Length),
                    ("password", args.Password?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .JoinBotAsync(
                    new JoinBotRequest
                    {
                        JoinType = JoinTypeByMeetingNo,
                        JoinIdentify = new JoinIdentify { MeetingNo = args.MeetingNo },
                        Password = args.Password,
                        Action = args.Action,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["meeting_id"] = data.Meeting?.Id,
                ["meeting_no"] = data.Meeting?.MeetingNo,
                ["topic"] = data.Meeting?.Topic,
                ["start_time"] = data.Meeting?.StartTime,
                ["bot_user_id"] = data.JoinUser?.Id,
                ["note"] = data.Meeting?.Id is null
                    ? "机器人已入会，但平台未返回长数字 meeting_id——离会前请用 vc.user_active_meeting 取回"
                    : "离会（vc.leave_meeting）与会中消息（vc.send_meeting_message）请用上面的 meeting_id（长数字），不要用它反推会议号",
            });
        });
    }

    /// <summary>vc.leave_meeting：机器人离会（只影响机器人自身，不结束会议）。</summary>
    [ToolHandler(typeof(IFeishuTenantVcLeaveMeetingTool))]
    public Task<ToolResult> LeaveMeetingAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcLeaveMeeting, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireBotClient(executor.ToolName);
            var args = VcLeaveMeetingArgs.Unpack(arguments);

            RequireLongMeetingId(executor.ToolName, args.MeetingId);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/vc/v1/bots/leave",
                    null,
                    ("meeting_id", args.MeetingId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .LeaveBotAsync(new LeaveBotRequest { MeetingId = args.MeetingId }, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["left"] = true,
                ["bot_user_id"] = data.LeaveUser?.Id,
                ["note"] = "机器人已离会，**会议本身仍在进行**；要结束会议用 vc.end_meeting",
            });
        });
    }

    /// <summary>vc.send_meeting_message：会中发送文本消息或反馈表情（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantVcSendMeetingMessageTool))]
    public Task<ToolResult> SendMeetingMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcSendMeetingMessage, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = RequireBotClient(executor.ToolName);
            var args = VcSendMeetingMessageArgs.Unpack(arguments);

            if (args.MsgType is not ("text" or "reaction"))
            {
                throw new ArgumentException($"msg_type '{args.MsgType}' 不合法。可用值：text（会中文本消息） / reaction（会中反馈表情，content 传表情 key）");
            }

            RequireLongMeetingId(executor.ToolName, args.MeetingId);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/vc/v1/bots/message",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("meeting_id", args.MeetingId.Length),
                    ("msg_type", args.MsgType.Length),
                    ("content", args.Content.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await client
                .SendBotMessageAsync(
                    new MessageBotRequest
                    {
                        MeetingId = args.MeetingId,
                        MsgType = args.MsgType,
                        Content = args.Content,
                        Uuid = args.IdempotencyKey,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["sent"] = true,
                ["uuid"] = data.Uuid,
                ["note"] = data.Uuid is null
                    ? "平台未回传幂等 ID；如需重发请用相同 idempotency_key 复核"
                    : "已投递到会中消息面板（仅当时在会的成员可见）",
            });
        });
    }

    /// <summary>取会议机器人客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么）。</summary>
    private IFeishuTenantV1VideoConferencingBot RequireBotClient(string toolName)
        => _botClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV1VideoConferencingBot——宿主须启用 AddVideoConferencingApi 的会议机器人客户端");

    /// <summary>
    /// 拦截"把 9 位会议号当长数字 meeting_id 用"：离会与发消息的 meeting_id 是机器人入会后平台给出的
    /// 长数字 ID，平台对会议号只会返回一个语焉不详的参数错误。
    /// </summary>
    private static void RequireLongMeetingId(string toolName, string meetingId)
    {
        if (meetingId.Length == 9 && meetingId.All(static c => c is >= '0' and <= '9'))
        {
            throw new ArgumentException(
                $"{toolName} 的 meeting_id 需要**长数字会议 ID**，收到的是 9 位会议号——"
                + "请用 vc.join_meeting 返回的 meeting_id，或先用 vc.user_active_meeting 按参会人取回");
        }
    }
}
