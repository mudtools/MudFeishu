// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Acs;

/// <summary>
/// 智能门禁记录（用户在门禁考勤机上成功开门或打卡后生成）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Acs")]
public class AcsAccessRecord
{
    /// <summary>
    /// <para>门禁记录 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6939433228970082591</para>
    /// </summary>
    [JsonPropertyName("access_record_id")]
    public string? AccessRecordId { get; set; }

    /// <summary>
    /// <para>门禁记录所属用户 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>门禁设备 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6939433228970082593</para>
    /// </summary>
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// <para>是否是打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_clock_in")]
    public bool? IsClockIn { get; set; }

    /// <summary>
    /// <para>访问时间，单位秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：1624520221</para>
    /// </summary>
    [JsonPropertyName("access_time")]
    public string? AccessTime { get; set; }

    /// <summary>
    /// <para>识别方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：FA</para>
    /// </summary>
    [JsonPropertyName("access_type")]
    public string? AccessType { get; set; }

    /// <summary>
    /// <para>识别相关数据，根据 access_type 不同，取值不同</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"has_access_photo":true}</para>
    /// </summary>
    [JsonPropertyName("access_data")]
    public string? AccessData { get; set; }

    /// <summary>
    /// <para>是否开门</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_door_open")]
    public bool? IsDoorOpen { get; set; }
}
