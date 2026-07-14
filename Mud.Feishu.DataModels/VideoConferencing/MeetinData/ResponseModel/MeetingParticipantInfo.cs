// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>会议中的参与人</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MeetingParticipantInfo
{
    /// <summary>
    /// <para>参会者</para>
    /// <para>必填：否</para>
    /// <para>示例值：kehan</para>
    /// </summary>
    [JsonPropertyName("participant_name")]
    public string? ParticipantName { get; set; }

    /// <summary>
    /// <para>部门</para>
    /// <para>必填：否</para>
    /// <para>示例值：development</para>
    /// </summary>
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    /// <summary>
    /// <para>用户ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：8efq90</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>会议室ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：omm_8efq90</para>
    /// </summary>
    [JsonPropertyName("meeting_room_id")]
    public string? MeetingRoomId { get; set; }

    /// <summary>
    /// <para>工号</para>
    /// <para>必填：否</para>
    /// <para>示例值：202205789</para>
    /// </summary>
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// <para>电话</para>
    /// <para>必填：否</para>
    /// <para>示例值：021-883889</para>
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// <para>邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxx@163.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>设备</para>
    /// <para>必填：否</para>
    /// <para>示例值：windows</para>
    /// <para>默认值：windows</para>
    /// </summary>
    [JsonPropertyName("device")]
    public string? Device { get; set; }

    /// <summary>
    /// <para>客户端版本</para>
    /// <para>必填：否</para>
    /// <para>示例值：5.26.0-alpha.38</para>
    /// </summary>
    [JsonPropertyName("app_version")]
    public string? AppVersion { get; set; }

    /// <summary>
    /// <para>公网IP</para>
    /// <para>必填：否</para>
    /// <para>示例值：27.xx.xx.183</para>
    /// </summary>
    [JsonPropertyName("public_ip")]
    public string? PublicIp { get; set; }

    /// <summary>
    /// <para>内网IP</para>
    /// <para>必填：否</para>
    /// <para>示例值：192.xx.xx.13</para>
    /// </summary>
    [JsonPropertyName("internal_ip")]
    public string? InternalIp { get; set; }

    /// <summary>
    /// <para>代理服务</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("use_rtc_proxy")]
    public bool? UseRtcProxy { get; set; }

    /// <summary>
    /// <para>位置</para>
    /// <para>必填：否</para>
    /// <para>示例值：东莞</para>
    /// </summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    /// <summary>
    /// <para>网络类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：wifi</para>
    /// </summary>
    [JsonPropertyName("network_type")]
    public string? NetworkType { get; set; }

    /// <summary>
    /// <para>连接类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：udp</para>
    /// </summary>
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    /// <summary>
    /// <para>麦克风</para>
    /// <para>必填：否</para>
    /// <para>示例值：麦克风阵列 (Realtek(R) Audio)</para>
    /// <para>默认值：mic</para>
    /// </summary>
    [JsonPropertyName("microphone")]
    public string? Microphone { get; set; }

    /// <summary>
    /// <para>扬声器</para>
    /// <para>必填：否</para>
    /// <para>示例值：扬声器 (Realtek(R) Audio)</para>
    /// </summary>
    [JsonPropertyName("speaker")]
    public string? Speaker { get; set; }

    /// <summary>
    /// <para>摄像头</para>
    /// <para>必填：否</para>
    /// <para>示例值：HD Camera</para>
    /// </summary>
    [JsonPropertyName("camera")]
    public string? Camera { get; set; }

    /// <summary>
    /// <para>音频</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("audio")]
    public bool? Audio { get; set; }

    /// <summary>
    /// <para>视频</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("video")]
    public bool? Video { get; set; }

    /// <summary>
    /// <para>共享</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("sharing")]
    public bool? Sharing { get; set; }

    /// <summary>
    /// <para>入会时间，格式见响应体示例</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.23 11:16:59 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("join_time")]
    public string? JoinTime { get; set; }

    /// <summary>
    /// <para>离会时间，格式见响应体示例</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.23 11:18:51 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("leave_time")]
    public string? LeaveTime { get; set; }

    /// <summary>
    /// <para>参会时长（秒），格式见响应体示例</para>
    /// <para>必填：否</para>
    /// <para>示例值：00:01:52</para>
    /// </summary>
    [JsonPropertyName("time_in_meeting")]
    public string? TimeInMeeting { get; set; }

    /// <summary>
    /// <para>离会原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：主持人结束会议</para>
    /// </summary>
    [JsonPropertyName("leave_reason")]
    public string? LeaveReason { get; set; }

    /// <summary>
    /// <para>日程响应状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：接受</item>
    /// <item>2：拒绝</item>
    /// <item>3：待确认</item>
    /// <item>4：未响应</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("accept_status")]
    public int? AcceptStatus { get; set; }

    /// <summary>
    /// <para>是否为外部参会人</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_external")]
    public bool? IsExternal { get; set; }

    /// <summary>
    /// <para>网络研讨会中的角色，"0"为嘉宾，"3"为观众</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("webinar_user_role")]
    public string? WebinarUserRole { get; set; }
}
