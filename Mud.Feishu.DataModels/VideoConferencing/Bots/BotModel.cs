// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 设置会中倒计时请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/bot/countdown"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class CountdownBotRequest
{
    /// <summary>
    /// <para>会议的唯一标识，需传入 Join 接口返回的长数字 meeting_id，而非 9 位会议号。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_id")]
    public string? MeetingId { get; set; }

    /// <summary>
    /// <para>操作类型，可选值：set（设置）、prolong（延长）、end_in_advance（提前结束）、close_window（关闭倒计时窗口）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// <para>倒计时时长，单位为分钟。action 为 set 或 prolong 时必填；超过 24 小时时，set 报错、prolong 按 24 小时处理。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    /// <summary>
    /// <para>倒计时结束时是否播放提示音，仅 action 为 set 时生效。</para>
    /// <para>必填：否</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("need_play_audio_at_end")]
    public bool? NeedPlayAudioAtEnd { get; set; }

    /// <summary>
    /// <para>结束前的提醒时间，单位为分钟，仅 action 为 set 时生效，需大于 0 且小于 duration。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reminder_before_end")]
    public string? ReminderBeforeEnd { get; set; }
}

/// <summary>
/// 入会标识
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class JoinIdentify
{
    /// <summary>
    /// <para>9 位会议号。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_no")]
    public string? MeetingNo { get; set; }
}

/// <summary>
/// 机器人入会请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/bot/join"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class JoinBotRequest
{
    /// <summary>
    /// <para>入会方式，当前仅支持 1（会议号入会）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("join_type")]
    public int? JoinType { get; set; }

    /// <summary>
    /// <para>入会标识。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("join_identify")]
    public JoinIdentify? JoinIdentify { get; set; }

    /// <summary>
    /// <para>会议密码。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// <para>邀请-入会链路的关联标识，来自「邀请机器人入会」事件，响应邀请时原样回传。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("call_id")]
    public string? CallId { get; set; }

    /// <summary>
    /// <para>入会动作，1 表示仅加入已存在的会议（默认），2 表示发起并加入日程会议。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("action")]
    public int? Action { get; set; }
}

/// <summary>
/// 机器人入会响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/bot/join"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class JoinBotResult
{
    /// <summary>
    /// <para>入会后的会议信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting")]
    public BotMeetingInfo? Meeting { get; set; }

    /// <summary>
    /// <para>入会成员信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("join_user")]
    public MeetingUser? JoinUser { get; set; }
}

/// <summary>
/// 机器人入会后的会议信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class BotMeetingInfo
{
    /// <summary>
    /// <para>会议 ID（长数字形式）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>9 位会议号。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_no")]
    public string? MeetingNo { get; set; }

    /// <summary>
    /// <para>会议开始时间（秒级时间戳）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>会议主题。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }
}

/// <summary>
/// 机器人离会请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/bot/leave"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class LeaveBotRequest
{
    /// <summary>
    /// <para>目标会议的唯一标识，需传入 Join 接口返回的长数字 meeting_id，而非 9 位会议号。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_id")]
    public string? MeetingId { get; set; }
}

/// <summary>
/// 机器人离会响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/bot/leave"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class LeaveBotResult
{
    /// <summary>
    /// <para>离开会议的用户信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leave_user")]
    public MeetingUser? LeaveUser { get; set; }
}

/// <summary>
/// 机器人在会中发送消息请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/bot/message"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MessageBotRequest
{
    /// <summary>
    /// <para>会议的唯一标识，需传入长数字 meeting_id。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_id")]
    public string? MeetingId { get; set; }

    /// <summary>
    /// <para>消息类型，可选值：text（会中文本消息）、reaction（会中反馈表情）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("msg_type")]
    public string? MsgType { get; set; }

    /// <summary>
    /// <para>消息内容。msg_type 为 text 时传文本内容，为 reaction 时传表情 key。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>幂等去重 ID，不传时由服务端自动生成。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }
}

/// <summary>
/// 机器人在会中发送消息响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/bot/message"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MessageBotResult
{
    /// <summary>
    /// <para>本次实际使用的幂等 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }
}

/// <summary>
/// 会中事件条目
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class BotEvent
{
    /// <summary>
    /// <para>事件 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    /// <summary>
    /// <para>事件类型。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }

    /// <summary>
    /// <para>事件发生时间，RFC3339 格式。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_time")]
    public string? EventTime { get; set; }
}

/// <summary>
/// 获取用户活跃会议响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/bot/user_active_meeting"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class UserActiveMeetingResult
{
    /// <summary>
    /// <para>活跃会议集合。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meetings")]
    public UserActiveMeetingInfo[]? Meetings { get; set; }
}

/// <summary>
/// 用户活跃会议信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class UserActiveMeetingInfo
{
    /// <summary>
    /// <para>9 位会议号。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_no")]
    public string? MeetingNo { get; set; }

    /// <summary>
    /// <para>会议 ID（长数字形式）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_id")]
    public string? MeetingId { get; set; }

    /// <summary>
    /// <para>会议标题。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_title")]
    public string? MeetingTitle { get; set; }
}
