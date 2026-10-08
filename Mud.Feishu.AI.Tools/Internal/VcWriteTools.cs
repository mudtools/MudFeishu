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
/// VideoConferencing 写入工具执行器（R6 / S3）：<c>vc.set_host</c> / <c>vc.invite_participants</c> /
/// <c>vc.end_meeting</c> / <c>vc.delete_reserve</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>四个写工具都是"会中/会前管理动作"</b>：改主持人、邀人、结束会议、删预约。
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
    IOptions<FeishuAgentOptions> options)
{
    /// <summary>一次性邀请人数上限（平台硬约束：最多 10 人）。</summary>
    private const int MaxInvitees = 10;

    /// <summary>默认用户类型（1 = 飞书用户）。</summary>
    private const int DefaultUserType = 1;

    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingMeeting? _meetingClient = vcMeetingClient;
    private readonly Mud.Feishu.IFeishuUserV1VideoConferencingMeeting? _userMeetingClient = vcUserMeetingClient;
    private readonly Mud.Feishu.IFeishuTenantV1VideoConferencingReserves? _reservesClient = vcReservesClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>vc.set_host：改设进行中会议的主持人（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantVcSetHostTool))]
    public Task<FeishuToolResult> SetHostAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcSetHost, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = _meetingClient
                ?? throw new ArgumentException("vc.set_host 需要 IFeishuTenantV1VideoConferencingMeeting——宿主须启用 VC 域客户端");
            var args = VcSetHostArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
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
    [FeishuToolHandler(typeof(IFeishuUserVcInviteParticipantsTool))]
    public Task<FeishuToolResult> InviteParticipantsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return FeishuToolResult.FromText(ToolDryRun.Describe(
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
    [FeishuToolHandler(typeof(IFeishuUserVcEndMeetingTool))]
    public Task<FeishuToolResult> EndMeetingAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
                return FeishuToolResult.FromText(ToolDryRun.Describe(
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
    [FeishuToolHandler(typeof(IFeishuTenantVcDeleteReserveTool))]
    public Task<FeishuToolResult> DeleteReserveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.VcDeleteReserve);
        return executor.RunAsync(async () =>
        {
            var client = _reservesClient
                ?? throw new ArgumentException("vc.delete_reserve 需要 IFeishuTenantV1VideoConferencingReserves——宿主须启用 VC 域客户端");
            var args = VcDeleteReserveArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
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
}
