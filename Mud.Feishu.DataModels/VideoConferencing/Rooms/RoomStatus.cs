// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>会议室状态</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RoomStatus
{
    /// <summary>
    /// <para>是否启用会议室</para>
    /// <para>必填：是</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    /// <summary>
    /// <para>会议室未来状态为启用或禁用（请忽略，该字段用于查询接口的返回值）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("schedule_status")]
    public bool? ScheduleStatus { get; set; }

    /// <summary>
    /// <para>禁用开始时间（unix时间，单位sec）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1652356050</para>
    /// </summary>
    [JsonPropertyName("disable_start_time")]
    public string? DisableStartTime { get; set; }

    /// <summary>
    /// <para>禁用结束时间（unix时间，单位sec，数值0表示永久禁用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1652442450</para>
    /// </summary>
    [JsonPropertyName("disable_end_time")]
    public string? DisableEndTime { get; set; }

    /// <summary>
    /// <para>禁用原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：测试占用</para>
    /// </summary>
    [JsonPropertyName("disable_reason")]
    public string? DisableReason { get; set; }

    /// <summary>
    /// <para>联系人列表，id类型由user_id_type参数决定</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_3ec3f6a28a0d08c45d895276e8e5e19b"]</para>
    /// </summary>
    [JsonPropertyName("contact_ids")]
    public string[]? ContactIds { get; set; }

    /// <summary>
    /// <para>是否在禁用时发送通知给预定了该会议室的员工</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("disable_notice")]
    public bool? DisableNotice { get; set; }

    /// <summary>
    /// <para>是否在恢复启用时发送通知给联系人</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("resume_notice")]
    public bool? ResumeNotice { get; set; }
}
