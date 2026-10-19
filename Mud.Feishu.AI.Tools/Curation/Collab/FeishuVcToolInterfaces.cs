// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── VideoConferencing ───────────────────────────
//
// 本域对标官方 cli 的 lark-meeting / lark-vc skill。
// 策展口径（与 Okr 同款纪律，见 FeishuOkrToolInterfaces 的 remarks）：
//
// 1) **只策展"能形成完整链路"的工具**：查询（会议列表 / 详情 / 参会人 / 录制 / 预约 / 会议室）
//    + 四个高频且**参数面收敛**的写动作（设主持人 / 邀人 / 结束会议 / 删除预约）。
// 2) **不策展的接口**（每个都有具体理由）：
//    - `vc.apply_reserve` / `vc.update_reserve`：`ApplyReserveRequest` 含 10+ 嵌套字段
//      （跨时区 time_range、循环预约、会议室自动分配、会前设置…）。**部分策展会产出语义错误的预约**
//      （比"没有这个工具"更糟），需要单独设计参数面。
//    - `vc.kickout_member`：强管理动作（把参会人踢出会议），与"设主持人/结束会议"不同，
//      它没有可回滚的二次确认面（会议已在进行中），留给宿主按需自行策展。
//    - `vc.start_recording` / `vc.stop_recording`：录制启停涉及合规（《个人信息保护法》下的
//      告知义务），不应由模型在无人干预下触发。
//    - `vc.subscribe_meeting_event` / `unsubscribe`：是事件订阅的管理面，不是业务动作。
//    - `vc.get_note`：会议妙记已被 `minutes.get` 覆盖，**跨域去重**。
// 3) **身份轴真实存在**：`invite` / `end` 在 SDK 里是 **User** 接口
//    （`IFeishuUserV1VideoConferencingMeeting`）；宿主启用它们时必须在
//    `FeishuAgent:AllowedIdentities` 放行 `user`，否则**装配期 fail-fast**。
//    `set_host` / `delete_reserve` 等走 Tenant 接口（默认身份闭集内即可）。

/// <summary>工具接口：vc.list_meetings（映射 <c>IFeishuTenantV1VideoConferencingMeetinData.GetMeetingPageListAsync</c>）。</summary>
/// <remarks>
/// <b>「我最近开了哪些会」的首选入口</b>：返回**已结束/进行中会议的历史统计视角**
/// （会议号、主题、起止时间、时长、参会人数、是否录制），而非某个会议的实时详情。
/// 时间范围是<b>必填</b>（平台强制），故描述里写清格式以免模型反复试错。
/// </remarks>
[AgentTool("vc.list_meetings",
    Description = "按时间范围查询会议列表（历史/统计视角）：会议号、主题、起止时间、时长、参会人数、是否录制、是否外部会议。start_time/end_time 为必填的 ISO8601 带时区时间（如 2026-10-01T00:00:00+08:00），单次范围建议不超过 30 天。只读，需 vc:meeting:readonly。",
    RequiredScopes = ["vc:meeting:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV1VideoConferencingMeetinData) + "." + nameof(IFeishuTenantV1VideoConferencingMeetinData.GetMeetingPageListAsync))]
public interface IFeishuTenantVcListMeetingsTool
{
    /// <summary>列出会议。</summary>
    /// <returns>白名单投影后的 JSON 文本（items + has_more + page_token）。</returns>
    Task<string> ListMeetingsAsync(
        [ToolParameter("start_time", "查询起始时间（ISO8601 带时区，如 2026-10-01T00:00:00+08:00）", Required = true)] string start_time,
        [ToolParameter("end_time", "查询结束时间（ISO8601 带时区；范围建议 ≤30 天）", Required = true)] string end_time,
        [ToolParameter("meeting_status", "会议状态（可选）：1=进行中 2=未开始 3=已结束")] int? meeting_status = null,
        [ToolParameter("meeting_no", "9 位会议号（可选，精确过滤）")] string? meeting_no = null,
        [ToolParameter("user_id", "按参会人用户 ID 过滤（可选）")] string? user_id = null,
        [ToolParameter("room_id", "按会议室 ID 过滤（可选）")] string? room_id = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.get_meeting（映射 <c>IFeishuTenantV1VideoConferencingMeeting.GetMeetingAsync</c>）。</summary>
/// <remarks>
/// 与 <c>vc.list_meetings</c> 的分工：本工具给**单个会议的当前详情**（主题、会议号、入会链接、
/// 密码、主持人、参会人数、会议状态、会议产物），用于"这个会现在什么情况"。
/// </remarks>
[AgentTool("vc.get_meeting",
    Description = "按会议 ID（9 位会议号）获取单个会议的当前详情：主题、会议号、入会链接、密码、主持人、参会人数、累计参会人数、会议状态、关联会议产物。要历史列表用 vc.list_meetings；要参会人明细用 vc.list_participants。只读，需 vc:meeting:readonly。",
    RequiredScopes = ["vc:meeting:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV1VideoConferencingMeeting) + "." + nameof(IFeishuTenantV1VideoConferencingMeeting.GetMeetingAsync))]
public interface IFeishuTenantVcGetMeetingTool
{
    /// <summary>获取会议详情。</summary>
    Task<string> GetMeetingAsync(
        [ToolParameter("meeting_id", "会议 ID（9 位会议号）", Required = true)] string meeting_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.list_participants（映射 <c>IFeishuTenantV1VideoConferencingMeetinData.GetParticipantPageListAsync</c>）。</summary>
/// <remarks>
/// <b>为什么它的参数形状与 list_meetings 不同</b>：平台侧参会人明细接口以
/// 「会议时段 + 会议号」定位（而非 meeting_id），故 must-use 参数是
/// <c>meeting_no</c> + <c>meeting_start_time</c> + <c>meeting_end_time</c>。
/// 描述里显式写明这一点，否则模型会习惯性只传会议号而拿到空结果。
/// </remarks>
[AgentTool("vc.list_participants",
    Description = "按『会议号 + 会议时段』查询参会人明细列表：姓名、部门、用户 ID、入会/离会时间、在会时长、设备/IP/网络、音频视频共享状态、是否外部参会人。注意：本接口用 meeting_no(9 位会议号) + meeting_start_time/meeting_end_time 定位（不是 meeting_id），三者均必填。只读，需 vc:meeting:readonly。",
    RequiredScopes = ["vc:meeting:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV1VideoConferencingMeetinData) + "." + nameof(IFeishuTenantV1VideoConferencingMeetinData.GetParticipantPageListAsync))]
public interface IFeishuTenantVcListParticipantsTool
{
    /// <summary>列出参会人。</summary>
    Task<string> ListParticipantsAsync(
        [ToolParameter("meeting_no", "9 位会议号（来自 vc.list_meetings / vc.get_meeting 的会议号）", Required = true)] string meeting_no,
        [ToolParameter("meeting_start_time", "会议开始时间（ISO8601 带时区，须与该会议实际开始时间一致）", Required = true)] string meeting_start_time,
        [ToolParameter("meeting_end_time", "会议结束时间（ISO8601 带时区，须与该会议实际结束时间一致）", Required = true)] string meeting_end_time,
        [ToolParameter("meeting_status", "会议状态（可选）：1=进行中 2=未开始 3=已结束")] int? meeting_status = null,
        [ToolParameter("user_id", "按参会人用户 ID 过滤（可选）")] string? user_id = null,
        [ToolParameter("room_id", "按会议室 ID 过滤（可选）")] string? room_id = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.get_recording（映射 <c>IFeishuTenantV1VideoConferencingRecording.GetMeetingRecordingAsync</c>）。</summary>
/// <remarks>
/// <b>本域最容易误读的返回</b>：录制「有没有」在会议列表里（<c>recording</c> 布尔），
/// 而本工具返回的是**录制文件的下载地址与时长**。会议未录制或录制仍在处理时
/// 返回的是空记录，须如实告知用户，不要臆造链接。
/// </remarks>
[AgentTool("vc.get_recording",
    Description = "获取指定会议的录制文件信息（下载地址 url 与时长 duration）。会议未录制或录制仍在处理中时返回空记录——此时如实告知用户『该会议没有可用录制』，不要臆造链接。只读，需 vc:recording:readonly。",
    RequiredScopes = ["vc:recording:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV1VideoConferencingRecording) + "." + nameof(IFeishuTenantV1VideoConferencingRecording.GetMeetingRecordingAsync))]
public interface IFeishuTenantVcGetRecordingTool
{
    /// <summary>获取会议录制。</summary>
    Task<string> GetRecordingAsync(
        [ToolParameter("meeting_id", "会议 ID（9 位会议号）", Required = true)] string meeting_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.list_reserves（映射 <c>IFeishuTenantV1VideoConferencingReserves.GetReserveAsync</c>）。</summary>
/// <remarks>
/// 预约（Reserve）= 通过 API 预创建的会议，其 id 即会议号锚点。
/// 返回包含入会链接、密码、预约人、会议设置。删除预约用 <c>vc.delete_reserve</c>。
/// </remarks>
[AgentTool("vc.list_reserves",
    Description = "按预约 ID 获取预约（Reserve）详情：预约 ID、9 位会议号、入会链接、app_link、直播链接、密码、预约人、结束时间、失效状态、会议设置（主题/自动录制等）。只读，需 vc:reserve:readonly。",
    RequiredScopes = ["vc:reserve:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV1VideoConferencingReserves) + "." + nameof(IFeishuTenantV1VideoConferencingReserves.GetReserveAsync))]
public interface IFeishuTenantVcListReservesTool
{
    /// <summary>获取预约详情。</summary>
    Task<string> ListReservesAsync(
        [ToolParameter("reserve_id", "预约 ID", Required = true)] string reserve_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.list_rooms（映射 <c>IFeishuTenantV1VideoConferencingRoom.GetMeetingRoomsPageListAsync</c>）。</summary>
/// <remarks>
/// 「公司有哪些会议室、能坐多少人」是预约/找空档的前置查询。
/// 只策展<b>分页列表</b>（`/vc/v1/rooms`），不策展 `rooms/mget`（需要先已知 room_id 集合）
/// 与 `rooms/search`（User 身份，且与会话上下文无关）。
/// </remarks>
[AgentTool("vc.list_rooms",
    Description = "分页列出会议室：room_id、展示 ID（display_id）、名称、容纳人数、层级路径与所属层级 ID。找空档前先用它确认会议室范围（会议室忙闲请用 calendar.find_free_slots 传 room_id）。只读，需 vc:room:readonly。",
    RequiredScopes = ["vc:room:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV1VideoConferencingRoom) + "." + nameof(IFeishuTenantV1VideoConferencingRoom.GetMeetingRoomsPageListAsync))]
public interface IFeishuTenantVcListRoomsTool
{
    /// <summary>列出会议室。</summary>
    Task<string> ListRoomsAsync(
        [ToolParameter("room_level_id", "会议室层级 ID（可选，来自层级列表；省略则返回全部可见层级）")] string? room_level_id = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.set_host（映射 <c>IFeishuTenantV1VideoConferencingMeeting.SetHostMeetingAsync</c>）。</summary>
[AgentTool("vc.set_host",
    Description = "把进行中会议的主持人改设为指定用户（会中管理动作，立即生效）。host_user_id 为该用户的 open_id（先用 contact.resolve_user / contact.search_user 解析）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。建议先 dry_run 预演确认目标会议与用户。",
    RequiredScopes = ["vc:meeting"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1VideoConferencingMeeting) + "." + nameof(IFeishuTenantV1VideoConferencingMeeting.SetHostMeetingAsync))]
public interface IFeishuTenantVcSetHostTool
{
    /// <summary>设置会议主持人。</summary>
    Task<string> SetHostAsync(
        [ToolParameter("meeting_id", "会议 ID（9 位会议号，会议须进行中）", Required = true)] string meeting_id,
        [ToolParameter("host_user_id", "新主持人的 open_id", Required = true)] string host_user_id,
        [ToolParameter("user_type", "用户类型（可选，默认 1=飞书用户）")] int? user_type = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.invite_participants（映射 <c>IFeishuUserV1VideoConferencingMeeting.InviteMeetingAsync</c>）。</summary>
/// <remarks>
/// <b>身份轴</b>：SDK 中该方法是 **User** 接口（`IFeishuUserV1…`），
/// 宿主须在 `FeishuAgent:AllowedIdentities` 放行 `user`，否则装配期 fail-fast。
/// </remarks>
[AgentTool("vc.invite_participants",
    Description = "邀请用户加入进行中的会议（一次性最多 10 人，返回每人邀请是否成功）。invitee_ids 为 open_id 列表（先用 contact 解析）。**用户身份工具**：宿主须在 FeishuAgent:AllowedIdentities 放行 user，否则启动即报错。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。建议先 dry_run 预演确认会议与人员。",
    RequiredScopes = ["vc:meeting"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1VideoConferencingMeeting) + "." + nameof(IFeishuUserV1VideoConferencingMeeting.InviteMeetingAsync))]
public interface IFeishuUserVcInviteParticipantsTool
{
    /// <summary>邀请参会人。</summary>
    Task<string> InviteParticipantsAsync(
        [ToolParameter("meeting_id", "会议 ID（9 位会议号，会议须进行中）", Required = true)] string meeting_id,
        [ToolParameter("invitee_ids", "被邀请用户 open_id 列表（最多 10 人）", Required = true)] string[] invitee_ids,
        [ToolParameter("user_type", "用户类型（可选，默认 1=飞书用户）")] int? user_type = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.end_meeting（映射 <c>IFeishuUserV1VideoConferencingMeeting.EndMeetingAsync</c>）。</summary>
/// <remarks>
/// <b>身份轴同 invite</b>：SDK 中是 User 接口。
/// <b>为什么归 high-risk 之外但要求 dry_run 先行</b>：结束会议对进行中的会议是**不可逆**的
/// （余人会被移出），但方法名不含危险词，故风险派生结果为 <c>write</c>——
/// 安全闸由 L1 guidance 的"写工具一律先预演"承担（`vc.md`）。
/// </remarks>
[AgentTool("vc.end_meeting",
    Description = "结束一个进行中的会议（对所有参会人立即生效，不可逆——你会把所有人移出会议）。**用户身份工具**：宿主须在 FeishuAgent:AllowedIdentities 放行 user。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。**务必先 dry_run=true 预演并与用户确认会议号**。",
    RequiredScopes = ["vc:meeting"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1VideoConferencingMeeting) + "." + nameof(IFeishuUserV1VideoConferencingMeeting.EndMeetingAsync))]
public interface IFeishuUserVcEndMeetingTool
{
    /// <summary>结束会议。</summary>
    Task<string> EndMeetingAsync(
        [ToolParameter("meeting_id", "会议 ID（9 位会议号，会议须进行中）", Required = true)] string meeting_id,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.delete_reserve（映射 <c>IFeishuTenantV1VideoConferencingReserves.DeleteReserveAsync</c>）。</summary>
/// <remarks>
/// <c>high-risk-write</c> 由 profile 的危险词（<c>delete</c>）在编译期判定，非人工标注。
/// </remarks>
[AgentTool("vc.delete_reserve",
    Description = "删除一个预约（Reserve）——预约对应的会议号随即失效且不可恢复。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:reserve。**务必先 dry_run=true 预演（用 vc.list_reserves 确认对象）**。",
    RequiredScopes = ["vc:reserve"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1VideoConferencingReserves) + "." + nameof(IFeishuTenantV1VideoConferencingReserves.DeleteReserveAsync))]
public interface IFeishuTenantVcDeleteReserveTool
{
    /// <summary>删除预约。</summary>
    Task<string> DeleteReserveAsync(
        [ToolParameter("reserve_id", "预约 ID（先用 vc.list_reserves 确认）", Required = true)] string reserve_id,
        [ToolParameter("dry_run", "仅预演不删除（可选，默认 false）")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── 会议机器人（会中动作面） ───────────────────────────
//
// 策展口径：对标官方 cli 的 bot 能力——机器人**入会 / 离会 / 会中发消息**三个动作，
// 加一个 `vc.user_active_meeting` 作为 ID 解析入口。
// - **两种 ID 不可混用**（本组最大的坑）：入会用 9 位 <c>meeting_no</c>，
//   离会 / 会中消息必须用 JoinBot 返回的<b>长数字 meeting_id</b>。故 user_active_meeting
//   同时投影两种 ID，并在描述里写清配对关系，否则模型会拿会议号去离会。
// - **不策展 `vc.countdown_bot`**（会中倒计时）：参数面是 <c>action</c> 字符串 + 秒数字符串
//   （"30" / "600"）的组合，语义随 action 变化，需要单独设计；且倒计时属会中干扰动作。
// - **不策展 `vc.get_bot_events`**：会中事件流已由 WebSocket 事件面覆盖，跨域去重。
// - <c>vc.meeting_screenshot</c>（会议截图）**SDK 无对应接口**（VideoConferencing 模块
//   实测无 screenshot 族），已登记进「需补 SDK」清单，不入工具面。
// - 四个工具均走 **Tenant** 接口（<c>IFeishuTenantV1VideoConferencingBot</c>）：机器人以应用身份入会，
//   不涉及身份轴新增要求。

/// <summary>工具接口：vc.user_active_meeting（映射 <c>IFeishuTenantV1VideoConferencingBot.GetUserActiveMeetingAsync</c>）。</summary>
/// <remarks>
/// <b>本域唯一的「双 ID 出口」</b>：返回体同时给出 9 位会议号与长数字会议 ID，
/// 是 join（要会议号）与 leave/send_message（要长数字 ID）之间的翻译层。
/// </remarks>
[AgentTool("vc.user_active_meeting",
    Description = "查询指定用户当前正在参加的会议，返回 meeting_no（9 位会议号）与 meeting_id（长数字）两种 ID 及会议标题。⚠️ 本工具的用途是**解析 ID**：vc.join_meeting 用 meeting_no，而 vc.leave_meeting / vc.send_meeting_message 必须用长数字 meeting_id（或 vc.join_meeting 返回的 meeting.id）。以应用身份调用时 user_id 必填。只读，需 vc:meeting:readonly。",
    RequiredScopes = ["vc:meeting:readonly"],
    IsWrite = false,
    Source = nameof(IFeishuTenantV1VideoConferencingBot) + "." + nameof(IFeishuTenantV1VideoConferencingBot.GetUserActiveMeetingAsync))]
public interface IFeishuTenantVcUserActiveMeetingTool
{
    /// <summary>查询用户当前活跃会议。</summary>
    Task<string> GetUserActiveMeetingAsync(
        [ToolParameter("user_id", "目标用户 ID（应用身份调用时必填）", Required = true)] string user_id,
        [ToolParameter("user_id_type", "用户 ID 类型（可选：open_id / union_id / user_id，默认 open_id）")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.join_meeting（映射 <c>IFeishuTenantV1VideoConferencingBot.JoinBotAsync</c>）。</summary>
/// <remarks>
/// 返回体的 <c>meeting.id</c> 是后续 leave / 发消息的<b>唯一可用 ID</b>，投影时必须保留并显式提示；
/// 机器人入会本身对参会人可见（会中会出现机器人成员），故属写动作。
/// </remarks>
[AgentTool("vc.join_meeting",
    Description = "让会议机器人以 9 位会议号加入进行中的会议（会中可见机器人成员）。⚠️ 返回的 meeting.id 是<b>长数字</b>会议 ID——后续 vc.leave_meeting / vc.send_meeting_message 必须用它，不要复用这里的 meeting_no。会议设了密码时须传 password。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。",
    RequiredScopes = ["vc:meeting"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1VideoConferencingBot) + "." + nameof(IFeishuTenantV1VideoConferencingBot.JoinBotAsync))]
public interface IFeishuTenantVcJoinMeetingTool
{
    /// <summary>机器人入会。</summary>
    Task<string> JoinMeetingAsync(
        [ToolParameter("meeting_no", "9 位会议号（来自 vc.list_meetings / vc.get_meeting / vc.user_active_meeting）", Required = true)] string meeting_no,
        [ToolParameter("password", "会议密码（可选；会议开启密码入会时必填）")] string? password = null,
        [ToolParameter("action", "入会动作（可选）：1=仅加入已存在的会议（默认） / 2=发起并加入日程会议")] int? action = null,
        [ToolParameter("dry_run", "仅预演不入会（可选，默认 false）")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.leave_meeting（映射 <c>IFeishuTenantV1VideoConferencingBot.LeaveBotAsync</c>）。</summary>
/// <remarks>
/// 离会只影响机器人自身，<b>不会</b>结束会议（结束会议是 <c>vc.end_meeting</c>）——
/// 二者常模型混用，描述里对照写明。
/// </remarks>
[AgentTool("vc.leave_meeting",
    Description = "让会议机器人离开会议（只影响机器人自己，**不会结束会议**；要让所有人退出用 vc.end_meeting）。meeting_id 必须是 vc.join_meeting 返回的<b>长数字</b>会议 ID 或 vc.user_active_meeting 的 meeting_id，传 9 位会议号会失败。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。",
    RequiredScopes = ["vc:meeting"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1VideoConferencingBot) + "." + nameof(IFeishuTenantV1VideoConferencingBot.LeaveBotAsync))]
public interface IFeishuTenantVcLeaveMeetingTool
{
    /// <summary>机器人离会。</summary>
    Task<string> LeaveMeetingAsync(
        [ToolParameter("meeting_id", "长数字会议 ID（来自 vc.join_meeting 的 meeting.id 或 vc.user_active_meeting）", Required = true)] string meeting_id,
        [ToolParameter("dry_run", "仅预演不离会（可选，默认 false）")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：vc.send_meeting_message（映射 <c>IFeishuTenantV1VideoConferencingBot.SendBotMessageAsync</c>）。</summary>
/// <remarks>
/// 与 <c>im.send_message</c> 的分工是本域易错点：本工具发到<b>会议内的会中消息面板</b>，
/// 只有当时在会的人看得见；发到群聊/单聊要用 <c>im.send_message</c>。
/// 平台自带幂等 ID（uuid），故 <c>idempotency_key</c> 有真实落点。
/// </remarks>
[AgentTool("vc.send_meeting_message",
    Description = "会议机器人在**会中**发送文本消息或反馈表情（msg_type：text=会中文本 / reaction=表情 key）。⚠️ 只投递给当前会议内成员，不是聊天消息——要发群聊/单聊用 im.send_message。meeting_id 必须是长数字会议 ID（见 vc.join_meeting / vc.user_active_meeting）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。",
    RequiredScopes = ["vc:meeting"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1VideoConferencingBot) + "." + nameof(IFeishuTenantV1VideoConferencingBot.SendBotMessageAsync))]
public interface IFeishuTenantVcSendMeetingMessageTool
{
    /// <summary>会中发送消息。</summary>
    Task<string> SendMeetingMessageAsync(
        [ToolParameter("meeting_id", "长数字会议 ID（来自 vc.join_meeting 的 meeting.id 或 vc.user_active_meeting）", Required = true)] string meeting_id,
        [ToolParameter("msg_type", "消息类型（必填闭集：text=会中文本消息 / reaction=会中反馈表情）", Required = true)] string msg_type,
        [ToolParameter("content", "消息内容：msg_type=text 时传文本；reaction 时传表情 key", Required = true)] string content,
        [ToolParameter("idempotency_key", "幂等去重 ID（可选，形如 UUID）：平台按该键去重，省略时由服务端自动生成。幂等键不跨工具共享。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不发送（可选，默认 false）")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
