// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>预约会议请求体</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ApplyReserveRequest
{
    /// <summary>
    /// <para>预约到期时间（unix时间，单位sec），多人会议必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：1608888867</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>指定会议归属人，使用 tenant_access_token 时生效且必传，指定对象必须为同租户下的合法飞书用户</para>
    /// <para>使用 user_access_token 时，该参数不生效，设置归属人无意义</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_3ec3f6a28a0d08c45d895276e8e5e19b</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// <para>会议设置</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("meeting_settings")]
    public ReserveMeetingSetting MeetingSettings { get; set; } = new();


}
