// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>日程提醒列表。不传值则默认为空。</para>
/// </summary>
public class CalendarReminder
{
    /// <summary>
    /// <para>日程提醒时间的偏移量。</para>
    /// <para>- 正数时表示在日程开始前 X 分钟提醒。</para>
    /// <para>- 负数时表示在日程开始后 X 分钟提醒。</para>
    /// <para>**注意**：新建或更新日程时传入该字段，仅对当前身份生效，不会对日程的其他参与人生效。</para>
    /// <para>必填：否</para>
    /// <para>示例值：5</para>
    /// <para>最大值：20160</para>
    /// <para>最小值：-20160</para>
    /// </summary>
    [JsonPropertyName("minutes")]
    public int? Minutes { get; set; }
}