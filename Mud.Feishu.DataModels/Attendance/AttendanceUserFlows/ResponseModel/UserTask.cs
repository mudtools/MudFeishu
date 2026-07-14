// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceUserFlows;

/// <summary>
/// 打卡任务列表
/// </summary>
public class UserTask
{
    /// <summary>
    /// <para>打卡记录 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6709359313699356941</para>
    /// </summary>
    [JsonPropertyName("result_id")]
    public string ResultId { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID，对应employee_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户姓名</para>
    /// <para>必填：是</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("employee_name")]
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// <para>日期，格式为yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20190819</para>
    /// </summary>
    [JsonPropertyName("day")]
    public int Day { get; set; }

    /// <summary>
    /// <para>考勤组 ID（特别说明：1代表未加入考勤组）</para>
    /// <para>必填：是</para>
    /// <para>示例值：6737202939523236110</para>
    /// </summary>
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// <para>班次 ID（特别说明：9代表默认班次）</para>
    /// <para>必填：是</para>
    /// <para>示例值：6753520403404030215</para>
    /// </summary>
    [JsonPropertyName("shift_id")]
    public string ShiftId { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户考勤记录</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("records")]
    public UserTaskRecord[] Records { get; set; } = [];
}
