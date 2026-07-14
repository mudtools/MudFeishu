// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// 日程实例响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CalendarEventInstanceResult
{
    /// <summary>
    /// <para>日程实例 ID。</para>
    /// <para>**注意**：重复日程实例的 ID 与其他日程 ID 不同，其 ID 包含了实例原始时间（Original time），数据格式为秒级时间戳。例如：`2cf525f0-1e67-4b04-ad4d-30b7f003903c_1713168000`，其中 `1713168000` 即为实例原始时间。</para>
    /// <para>必填：是</para>
    /// <para>示例值：75d28f9b-e35c-4230-8a83-4a661497db54_1602504000</para>
    /// </summary>
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// <para>日程主题。</para>
    /// <para>必填：否</para>
    /// <para>示例值：日程主题</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>日程描述。</para>
    /// <para>必填：否</para>
    /// <para>示例值：desc</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>日程开始时间。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public CalendarTimeInfo? StartTime { get; set; }

    /// <summary>
    /// <para>日程结束时间。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public CalendarTimeInfo? EndTime { get; set; }

    /// <summary>
    /// <para>日程状态。</para>
    /// <para>必填：否</para>
    /// <para>示例值：confirmed</para>
    /// <para>可选值：<list type="bullet">
    /// <item>tentative：未回应</item>
    /// <item>confirmed：已确认</item>
    /// <item>cancelled：日程已取消</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>日程是否是重复日程的例外日程。了解例外日程，可参见[例外日程](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar-event/introduction#71c5ec78)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_exception")]
    public bool? IsException { get; set; }

    /// <summary>
    /// <para>日程的 app_link，用于跳转到具体的某个日程。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://applink.larkoffice.com/client/calendar/event/detail?calendarId=7039673579105026066&amp;key=aeac9c56-aeb1-4179-a21b-02f278f59048&amp;originalTime=0&amp;startTime=1700496000</para>
    /// </summary>
    [JsonPropertyName("app_link")]
    public string? AppLink { get; set; }

    /// <summary>
    /// <para>日程地点。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("location")]
    public CalendarEventLocation? Location { get; set; }
}
