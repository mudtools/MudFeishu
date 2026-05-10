// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceShifts;

/// <summary>
/// <para>晚走次日晚到配置规则</para>
/// </summary>
public class ShiftLateOffLateOnSetting
{
    /// <summary>
    /// <para>当日晚走时间计算规则</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：弹性规则</item>
    /// <item>1：固定规则</item>
    /// </list></para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("late_off_base_on_time_type")]
    public int? LateOffBaseOnTimeType { get; set; }

    /// <summary>
    /// <para>次日晚到时间计算规则</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：固定规则</item>
    /// <item>1：弹性规则</item>
    /// </list></para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("late_on_base_on_time_type")]
    public int? LateOnBaseOnTimeType { get; set; }
}