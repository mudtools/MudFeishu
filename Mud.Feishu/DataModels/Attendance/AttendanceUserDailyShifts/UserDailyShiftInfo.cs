// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceUserDailyShifts;

/// <summary>
/// <para>班表信息列表</para>
/// </summary>
public class UserDailyShiftInfo
{
    /// <summary>
    /// <para>考勤组 ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：6737202939523236110</para>
    /// </summary>
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// <para>班次 ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：6753520403404030215</para>
    /// </summary>
    [JsonPropertyName("shift_id")]
    public string ShiftId { get; set; } = string.Empty;

    /// <summary>
    /// <para>月份，格式yyyyMM</para>
    /// <para>必填：是</para>
    /// <para>示例值：202101</para>
    /// </summary>
    [JsonPropertyName("month")]
    public int Month { get; set; }

    /// <summary>
    /// <para>用户 ID，与employee_type对应</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>月内日期，最多31天</para>
    /// <para>必填：是</para>
    /// <para>示例值：21</para>
    /// </summary>
    [JsonPropertyName("day_no")]
    public int DayNo { get; set; }
}
