// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceGroups;

/// <summary>
/// <para>加班打卡规则</para>
/// </summary>
public record AttendanceGroupOvertimeClockCfg
{
    /// <summary>
    /// <para>是否允许在非打卡时段申请打卡（仅灰度租户有效，如需使用请联系技术支持）</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("allow_punch_approval")]
    public bool? AllowPunchApproval { get; set; }

    /// <summary>
    /// <para>加班开始和结束需打卡（仅灰度租户有效，如需使用请联系技术支持）</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("need_clock_over_time_start_and_end")]
    public bool? NeedClockOverTimeStartAndEnd { get; set; }
}