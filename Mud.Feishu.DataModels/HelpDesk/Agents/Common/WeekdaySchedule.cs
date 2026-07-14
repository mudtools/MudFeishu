// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;


/// <summary>
/// <para>工作日程列表</para>
/// </summary>
public class WeekdaySchedule
{
    /// <summary>
    /// <para>开始时间, format 00:00 - 23:59</para>
    /// <para>必填：否</para>
    /// <para>示例值：00:00</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>结束时间, format 00:00 - 23:59</para>
    /// <para>必填：否</para>
    /// <para>示例值：24:00</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>星期几, 1 - Monday, 2 - Tuesday, 3 - Wednesday, 4 - Thursday, 5 - Friday, 6 - Saturday, 7 - Sunday, 9 - Everyday, 10 - Weekday, 11 - Weekend</para>
    /// <para>必填：否</para>
    /// <para>示例值：9</para>
    /// </summary>
    [JsonPropertyName("weekday")]
    public int? Weekday { get; set; }
}