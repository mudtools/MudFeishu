// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>搜索过滤器。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class EventSearchFilter
{
    /// <summary>
    /// <para>搜索过滤项，日程搜索区间的开始时间。</para>
    /// <para>**注意**：start_time 和 end_time 不传值时，默认搜索近一个月内的日程。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public CalendarTimeInfo? StartTime { get; set; }


    /// <summary>
    /// <para>搜索过滤项，日程搜索区间的结束时间。</para>
    /// <para>**注意**：start_time 和 end_time 不传值时，默认搜索近一个月内的日程。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public CalendarTimeInfo? EndTime { get; set; }

    /// <summary>
    /// <para>搜索过滤项，日程参与人的用户 ID 列表。设置该字段后，被搜索到的日程中至少包含其中一个参与人。</para>
    /// <para>**注意**：用户 ID 类型和 user_id_type 的值保持一致，关于用户 ID 可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**默认值**：空，表示不设置该过滤项</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxxx</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }

    /// <summary>
    /// <para>搜索过滤项，会议室 ID 列表。设置该字段后，被搜索到的日程中至少包含其中一个会议室。</para>
    /// <para>**默认值**：空，表示不设置该过滤项</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxxx</para>
    /// </summary>
    [JsonPropertyName("room_ids")]
    public string[]? RoomIds { get; set; }

    /// <summary>
    /// <para>搜索过滤项，群 ID 列表。设置该字段后，被搜索到的日程中至少包含其中一个群。关于群 ID 可参见[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)。</para>
    /// <para>**默认值**：空，表示不设置该过滤项</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxxx</para>
    /// </summary>
    [JsonPropertyName("chat_ids")]
    public string[]? ChatIds { get; set; }
}
