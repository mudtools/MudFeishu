// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceShifts;

/// <summary>
/// <para>半天分割规则（仅飞书人事企业版可用）</para>
/// </summary>
public class ShiftMiddleTimeRule
{
    /// <summary>
    /// <para>半天分割类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：按全天班次时长（含休息）的中点分割</item>
    /// <item>1：按全天班次时长（不含休息）的中点分割</item>
    /// <item>2：按休息时间分割</item>
    /// <item>3：按固定时间点分割</item>
    /// </list></para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("middle_time_type")]
    public int? MiddleTimeType { get; set; }

    /// <summary>
    /// <para>固定分割时间点（middle_time_type 为 3 时有效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：12:00</para>
    /// </summary>
    [JsonPropertyName("fixed_middle_time")]
    public string? FixedMiddleTime { get; set; }
}