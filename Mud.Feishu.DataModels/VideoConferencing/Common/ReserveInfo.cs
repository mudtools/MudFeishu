// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 预约数据
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ReserveInfo
{
    /// <summary>
    /// <para>预约ID（预约的唯一标识，非会议ID，会议ID仅在会议开始后才生成）</para>
    /// <para>必填：否</para>
    /// <para>示例值：6911188411934973028</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>9位会议号（飞书用户可通过输入9位会议号快捷入会）</para>
    /// <para>必填：否</para>
    /// <para>示例值：112000358</para>
    /// </summary>
    [JsonPropertyName("meeting_no")]
    public string? MeetingNo { get; set; }

    /// <summary>
    /// <para>会议密码</para>
    /// <para>必填：否</para>
    /// <para>示例值：971024</para>
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// <para>会议链接（飞书用户可通过点击会议链接快捷入会）</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://vc.feishu.cn/j/337736498</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>APPLink用于唤起飞书APP入会。"{?}"为占位符，用于配置入会参数，使用时需替换具体值：0表示关闭，1表示打开。preview为入会前的设置页，mic为麦克风，speaker为扬声器，camera为摄像头</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://applink.feishu.cn/client/videochat/open?source=openplatform&amp;action=join&amp;idtype=reservationid&amp;id={?}&amp;preview={?}&amp;mic={?}&amp;speaker={?}&amp;camera={?}</para>
    /// </summary>
    [JsonPropertyName("app_link")]
    public string? AppLink { get; set; }

    /// <summary>
    /// <para>会议转直播链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://meetings.feishu.cn/s/1gub381l4gglv</para>
    /// </summary>
    [JsonPropertyName("live_link")]
    public string? LiveLink { get; set; }

    /// <summary>
    /// <para>预约到期时间（unix时间，单位sec）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1608883322</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>过期状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：未过期</item>
    /// <item>2：已过期</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("expire_status")]
    public int? ExpireStatus { get; set; }

    /// <summary>
    /// <para>预约人ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_3ec3f6a28a0d08c45d895276e8e5e19b</para>
    /// </summary>
    [JsonPropertyName("reserve_user_id")]
    public string? ReserveUserId { get; set; }

    /// <summary>
    /// <para>会议设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_settings")]
    public ReserveMeetingSettingInfo? MeetingSettings { get; set; }

}
