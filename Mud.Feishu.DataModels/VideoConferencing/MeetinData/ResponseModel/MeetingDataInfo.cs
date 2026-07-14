// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>会议列表信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MeetingDataInfo
{
    /// <summary>
    /// <para>9位会议号</para>
    /// <para>必填：否</para>
    /// <para>示例值：705605196</para>
    /// </summary>
    [JsonPropertyName("meeting_id")]
    public string? MeetingId { get; set; }

    /// <summary>
    /// <para>会议主题</para>
    /// <para>必填：否</para>
    /// <para>示例值：讨论会</para>
    /// </summary>
    [JsonPropertyName("meeting_topic")]
    public string? MeetingTopic { get; set; }

    /// <summary>
    /// <para>会议类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：全部类型（默认）</item>
    /// <item>2：视频会议</item>
    /// <item>3：本地投屏</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("meeting_type")]
    public int? MeetingType { get; set; }

    /// <summary>
    /// <para>组织者</para>
    /// <para>必填：否</para>
    /// <para>示例值：kehan</para>
    /// </summary>
    [JsonPropertyName("organizer")]
    public string? Organizer { get; set; }

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
    /// <para>示例值：92f879</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>工号</para>
    /// <para>必填：否</para>
    /// <para>示例值：202105149765</para>
    /// </summary>
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// <para>邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxx@163.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>手机</para>
    /// <para>必填：否</para>
    /// <para>示例值：021-673288</para>
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// <para>会议开始时间，格式见响应体示例</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.23 11:16:59 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("meeting_start_time")]
    public string? MeetingStartTime { get; set; }

    /// <summary>
    /// <para>会议结束时间，格式见响应体示例</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.23 11:18:51 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("meeting_end_time")]
    public string? MeetingEndTime { get; set; }

    /// <summary>
    /// <para>会议持续时间（秒），格式见响应体示例</para>
    /// <para>必填：否</para>
    /// <para>示例值：00:01:52</para>
    /// </summary>
    [JsonPropertyName("meeting_duration")]
    public string? MeetingDuration { get; set; }

    /// <summary>
    /// <para>参会人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("number_of_participants")]
    public string? NumberOfParticipants { get; set; }

    /// <summary>
    /// <para>累计入会设备数</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("number_of_devices")]
    public string? NumberOfDevices { get; set; }

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
    /// <para>录制</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("recording")]
    public bool? Recording { get; set; }

    /// <summary>
    /// <para>电话</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("telephone")]
    public bool? Telephone { get; set; }

    /// <summary>
    /// <para>关联会议室列表，只有待召开的会议支持该字段。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reserved_rooms")]
    public ReservedRoom[]? ReservedRooms { get; set; }


    /// <summary>
    /// <para>是否有关联文档和纪要</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("has_related_document")]
    public bool? HasRelatedDocument { get; set; }

    /// <summary>
    /// <para>是否使用AI纪要</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("ai_note")]
    public bool? AiNote { get; set; }

    /// <summary>
    /// <para>是否为外部会议</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_external")]
    public bool? IsExternal { get; set; }

    /// <summary>
    /// <para>会议子类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：全部类型（默认）</item>
    /// <item>1：投屏</item>
    /// <item>2：有线投屏</item>
    /// <item>3：妙享</item>
    /// <item>4：聊天室</item>
    /// <item>5：飞阅会</item>
    /// <item>6：企业电话</item>
    /// <item>7：ip电话</item>
    /// <item>8：webniar会议</item>
    /// <item>9：离线会议</item>
    /// <item>10：妙记会议</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("meeting_subtype")]
    public int? MeetingSubtype { get; set; }

    /// <summary>
    /// <para>唯一会议ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7529416531681214468</para>
    /// </summary>
    [JsonPropertyName("meeting_instance_id")]
    public string? MeetingInstanceId { get; set; }

    /// <summary>
    /// <para>网络研讨会观众人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("number_of_webinar_viewers")]
    public string? NumberOfWebinarViewers { get; set; }
}
