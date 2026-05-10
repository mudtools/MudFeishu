// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceUserDailyShifts;

/// <summary>
/// <para>临时班表信息列表（数量限制50以内）</para>
/// </summary>
public class UserTmpDailyShift
{
    /// <summary>
    /// <para>考勤组 ID，获取方式：1）[创建或修改考勤组](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/attendance-v1/group/create) 2）[按名称查询考勤组](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/attendance-v1/group/search) 3）[获取打卡结果](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/attendance-v1/user_task/query)</para>
    /// <para>必填：是</para>
    /// <para>示例值：6737202939523236110</para>
    /// </summary>
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID，与employee_type对应</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>日期，格式：yyyymmdd，如20240120</para>
    /// <para>必填：是</para>
    /// <para>示例值：20240120</para>
    /// </summary>
    [JsonPropertyName("date")]
    public int Date { get; set; }

    /// <summary>
    /// <para>班次名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：临时早班</para>
    /// </summary>
    [JsonPropertyName("shift_name")]
    public string ShiftName { get; set; } = string.Empty;

    /// <summary>
    /// <para>打卡规则</para>
    /// <para>必填：是</para>
    /// <para>最大长度：6</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("punch_time_simple_rules")]
    public PunchTimeSimpleRule[] PunchTimeSimpleRules { get; set; } = [];

}

/// <summary>
/// <para>打卡规则</para>
/// </summary>
public class PunchTimeSimpleRule
{
    /// <summary>
    /// <para>上班时间，格式HH：MM</para>
    /// <para>必填：是</para>
    /// <para>示例值：9：00</para>
    /// </summary>
    [JsonPropertyName("on_time")]
    public string OnTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>下班时间，格式HH：MM。如需表示次日2点，则填入"26：00"</para>
    /// <para>必填：是</para>
    /// <para>示例值：18：00</para>
    /// </summary>
    [JsonPropertyName("off_time")]
    public string OffTime { get; set; } = string.Empty;
}