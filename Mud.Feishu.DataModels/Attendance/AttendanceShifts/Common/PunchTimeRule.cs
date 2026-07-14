// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceShifts;

/// <summary>
/// <para>打卡规则</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class PunchTimeRule
{
    /// <summary>
    /// <para>上班时间</para>
    /// <para>必填：是</para>
    /// <para>示例值：9:00</para>
    /// </summary>
    [JsonPropertyName("on_time")]
    public string OnTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>下班时间。如果下班时间跨天，则需要在 24 小时的基础上累加时间。例如，第二天凌晨 2 点取值为 26:00</para>
    /// <para>必填：是</para>
    /// <para>示例值：18:00</para>
    /// </summary>
    [JsonPropertyName("off_time")]
    public string OffTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>晚到多久记为迟到。单位：分钟</para>
    /// <para>必填：是</para>
    /// <para>示例值：30</para>
    /// </summary>
    [JsonPropertyName("late_minutes_as_late")]
    public int LateMinutesAsLate { get; set; }

    /// <summary>
    /// <para>晚到多久记为缺卡。单位：分钟</para>
    /// <para>必填：是</para>
    /// <para>示例值：60</para>
    /// </summary>
    [JsonPropertyName("late_minutes_as_lack")]
    public int LateMinutesAsLack { get; set; }

    /// <summary>
    /// <para>最早多久可打上班卡。最大值为 720。单位：分钟</para>
    /// <para>必填：是</para>
    /// <para>示例值：60</para>
    /// </summary>
    [JsonPropertyName("on_advance_minutes")]
    public int OnAdvanceMinutes { get; set; }

    /// <summary>
    /// <para>早退多久记为早退。单位：分钟</para>
    /// <para>必填：是</para>
    /// <para>示例值：30</para>
    /// </summary>
    [JsonPropertyName("early_minutes_as_early")]
    public int EarlyMinutesAsEarly { get; set; }

    /// <summary>
    /// <para>早退多久记为缺卡。单位：分钟</para>
    /// <para>必填：是</para>
    /// <para>示例值：60</para>
    /// </summary>
    [JsonPropertyName("early_minutes_as_lack")]
    public int EarlyMinutesAsLack { get; set; }

    /// <summary>
    /// <para>最晚多久可打下班卡。最大值为 960。单位：分钟</para>
    /// <para>必填：是</para>
    /// <para>示例值：60</para>
    /// </summary>
    [JsonPropertyName("off_delay_minutes")]
    public int OffDelayMinutes { get; set; }

    /// <summary>
    /// <para>晚到多久记为严重迟到。单位：分钟</para>
    /// <para>必填：否</para>
    /// <para>示例值：40</para>
    /// </summary>
    [JsonPropertyName("late_minutes_as_serious_late")]
    public int? LateMinutesAsSeriousLate { get; set; }

    /// <summary>
    /// <para>true为不需要打上班卡，这里需要特别注意，第一段打卡规则须为false。后续可按需配置</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("no_need_on")]
    public bool? NoNeedOn { get; set; }

    /// <summary>
    /// <para>true为不需要打下班卡。默认为false，需要下班打卡（优先级高于data.shift.no_need_off）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("no_need_off")]
    public bool? NoNeedOff { get; set; }
}
