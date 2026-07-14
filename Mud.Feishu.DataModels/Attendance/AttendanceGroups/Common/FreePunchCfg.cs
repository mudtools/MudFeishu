// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceGroups;

/// <summary>
/// <para>配置自由班制</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class AttendanceGroupFreePunchCfg
{
    /// <summary>
    /// <para>自由班制打卡开始时间，格式为x点x分，注意这里小时如果小于10点，是不需要补零的</para>
    /// <para>必填：是</para>
    /// <para>示例值：7:00</para>
    /// </summary>
    [JsonPropertyName("free_start_time")]
    public string FreeStartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>自由班制打卡结束时间，格式为x点x分，注意这里小时如果小于10点，是不需要补零的</para>
    /// <para>必填：是</para>
    /// <para>示例值：18:00</para>
    /// </summary>
    [JsonPropertyName("free_end_time")]
    public string FreeEndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>打卡的时间，为 7 位数字，每一位依次代表周一到周日，0 为不上班，1 为上班</para>
    /// <para>必填：是</para>
    /// <para>示例值：1111100</para>
    /// </summary>
    [JsonPropertyName("punch_day")]
    public int PunchDay { get; set; }

    /// <summary>
    /// <para>工作日不打卡是否记为缺卡，默认为空</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("work_day_no_punch_as_lack")]
    public bool? WorkDayNoPunchAsLack { get; set; }

    /// <summary>
    /// <para>工作日出勤是否需满足时长要求，默认为空</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("work_hours_demand")]
    public bool? WorkHoursDemand { get; set; }

    /// <summary>
    /// <para>每日工作时长（分钟），范围[0,1440]</para>
    /// <para>必填：否</para>
    /// <para>示例值：480</para>
    /// </summary>
    [JsonPropertyName("work_hours")]
    public int? WorkHours { get; set; }
}
