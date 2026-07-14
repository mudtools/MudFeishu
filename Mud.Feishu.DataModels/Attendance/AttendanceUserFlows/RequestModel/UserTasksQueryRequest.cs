// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceUserFlows;

/// <summary>
/// 查询打卡结果请求体
/// </summary>
public class UserTasksQueryRequest
{
    /// <summary>
    /// <para>employee_no 或 employee_id 列表，对应employee_type，长度不超过 50</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[] UserIds { get; set; } = [];

    /// <summary>
    /// <para>查询的起始工作日，格式为yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20190817</para>
    /// </summary>
    [JsonPropertyName("check_date_from")]
    public int CheckDateFrom { get; set; }

    /// <summary>
    /// <para>查询的结束工作日，格式为yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20190820</para>
    /// </summary>
    [JsonPropertyName("check_date_to")]
    public int CheckDateTo { get; set; }

    /// <summary>
    /// <para>是否需要加班班段打卡结果；当need_overtime_result=true时，会返回加班班段，加班班段通过task_shift_type=1标识，加班班段上下班与正常班段相连时会出现共用record_id情况。例如：9-18为正常班次，18-19为加班班次，打卡结果中records 会出现两段，分别为9-18，18-19 且两段上下班record_id相同（check_in_record_id和check_out_record_id相同）。非相连加班班次正常分段返回。当need_overtime_result=false时，仅返回正常班段且task_shift_type=0。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("need_overtime_result")]
    public bool? NeedOvertimeResult { get; set; }
}
