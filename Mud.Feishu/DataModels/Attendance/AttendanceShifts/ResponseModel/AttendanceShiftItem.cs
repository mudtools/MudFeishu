// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceShifts;

/// <summary>
/// <para>班次列表</para>
/// </summary>
public class AttendanceShiftItem
{
    /// <summary>
    /// <para>班次 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6919358778597097404</para>
    /// </summary>
    [JsonPropertyName("shift_id")]
    public string ShiftId { get; set; } = string.Empty;

    /// <summary>
    /// <para>班次名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：早班</para>
    /// </summary>
    [JsonPropertyName("shift_name")]
    public string ShiftName { get; set; } = string.Empty;

    /// <summary>
    /// <para>打卡次数</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("punch_times")]
    public int PunchTimes { get; set; }

    /// <summary>
    /// <para>无效字段，请勿使用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sub_shift_leader_ids")]
    public string[]? SubShiftLeaderIds { get; set; }

    /// <summary>
    /// <para>是否弹性打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_flexible")]
    public bool? IsFlexible { get; set; }

    /// <summary>
    /// <para>弹性打卡时间，单位：分钟，设置【上班最多可晚到】与【下班最多可早走】时间，如果不设置flexible_rule则生效</para>
    /// <para>必填：否</para>
    /// <para>示例值：60</para>
    /// </summary>
    [JsonPropertyName("flexible_minutes")]
    public int? FlexibleMinutes { get; set; }

    /// <summary>
    /// <para>弹性打卡时间设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("flexible_rule")]
    public FlexibleRule[]? FlexibleRules { get; set; }


    /// <summary>
    /// <para>不需要打下班卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("no_need_off")]
    public bool? NoNeedOff { get; set; }

    /// <summary>
    /// <para>打卡规则</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("punch_time_rule")]
    public PunchTimeRule[] PunchTimeRules { get; set; } = [];


    /// <summary>
    /// <para>晚走晚到规则（仅飞书人事企业版可用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("late_off_late_on_rule")]
    public ShiftLateOffLateOnRule[]? LateOffLateOnRules { get; set; }


    /// <summary>
    /// <para>休息规则</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("rest_time_rule")]
    public RestRule[]? RestTimeRules { get; set; }


    /// <summary>
    /// <para>加班时段（仅飞书人事企业版可用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("overtime_rule")]
    public OvertimeRule[]? OvertimeRules { get; set; }

    /// <summary>
    /// <para>日期类型，【是否弹性打卡 = ture】时，不可设置为“休息日” 可选值：1：工作日 2：休息日 示例值：（默认值）1</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("day_type")]
    public int? DayType { get; set; }

    /// <summary>
    /// <para>班外休息规则</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("overtime_rest_time_rule")]
    public RestRule[]? OvertimeRestTimeRules { get; set; }

    /// <summary>
    /// <para>晚到多久记为严重迟到，单位：分钟（优先级比原有字段高）</para>
    /// <para>必填：否</para>
    /// <para>示例值：40</para>
    /// </summary>
    [JsonPropertyName("late_minutes_as_serious_late")]
    public int? LateMinutesAsSeriousLate { get; set; }

    /// <summary>
    /// <para>半天分割规则（仅飞书人事企业版可用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("shift_middle_time_rule")]
    public ShiftMiddleTimeRule? ShiftMiddleTimeRule { get; set; }

    /// <summary>
    /// <para>晚走次日晚到配置规则</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("late_off_late_on_setting")]
    public ShiftLateOffLateOnSetting? LateOffLateOnSetting { get; set; }

}
