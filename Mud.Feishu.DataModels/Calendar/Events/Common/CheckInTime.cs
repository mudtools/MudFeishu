// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>日程签到开始时间。</para>
/// <para>**注意**：签到开始时间不能大于或者等于签到结束时间。</para>
/// </summary>
public class CheckInTime
{
    /// <summary>
    /// <para>偏移量(分钟)相对于的日程时间节点类型。</para>
    /// <para>必填：是</para>
    /// <para>示例值：before_event_start</para>
    /// <para>可选值：<list type="bullet">
    /// <item>before_event_start：日程开始前</item>
    /// <item>after_event_start：日程开始后</item>
    /// <item>after_event_end：日程结束后</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("time_type")]
    public string TimeType { get; set; } = string.Empty;

    /// <summary>
    /// <para>相对于日程开始或者结束的偏移量(分钟)。</para>
    /// <para>- 目前取值只能为列表[0, 5, 15, 30, 60]之一，0表示立即开始。</para>
    /// <para>- 当time_type为before_event_start，duration不能取0</para>
    /// <para>必填：是</para>
    /// <para>示例值：15</para>
    /// <para>最大值：60</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}