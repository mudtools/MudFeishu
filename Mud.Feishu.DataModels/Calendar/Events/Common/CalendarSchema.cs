// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// <para>日程自定义信息，控制日程详情页的 UI 展示。不传值则默认为空。</para>
/// </summary>
public class CalendarSchema
{
    /// <summary>
    /// <para>UI 名称。</para>
    /// <para>**可选值有**：</para>
    /// <para>- ForwardIcon：日程转发按钮</para>
    /// <para>- MeetingChatIcon：会议群聊按钮</para>
    /// <para>- MeetingMinutesIcon：会议纪要按钮</para>
    /// <para>- MeetingVideo：视频会议区域</para>
    /// <para>- RSVP：接受、拒绝、待定区域</para>
    /// <para>- Attendee：参与者区域</para>
    /// <para>- OrganizerOrCreator：组织者或创建者区域</para>
    /// <para>必填：否</para>
    /// <para>示例值：ForwardIcon</para>
    /// </summary>
    [JsonPropertyName("ui_name")]
    public string? UiName { get; set; }

    /// <summary>
    /// <para>UI 项的状态。目前只支持选择 `hide`。</para>
    /// <para>必填：否</para>
    /// <para>示例值：hide</para>
    /// <para>可选值：<list type="bullet">
    /// <item>hide：隐藏显示</item>
    /// <item>readonly：只读</item>
    /// <item>editable：可编辑</item>
    /// <item>unknown：未知 UI 项自定义状态。该参数仅用于读取时兼容，不支持作为请求参数值传入</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("ui_status")]
    public string? UiStatus { get; set; }

    /// <summary>
    /// <para>按钮点击后跳转的链接。</para>
    /// <para>**注意**：兼容性参数，只读，因此暂不支持传入该请求参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://applink.feishu.cn/client/calendar/event/detail?calendarId=xxxxxx&amp;key=xxxxxx&amp;originalTime=xxxxxx&amp;startTime=xxxxxx</para>
    /// <para>最大长度：2000</para>
    /// </summary>
    [JsonPropertyName("app_link")]
    public string? AppLink { get; set; }
}