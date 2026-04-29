// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 创建请假日程响应体
/// </summary>
public class CreateTimeoffEventResult
{
    /// <summary>
    /// <para>请假日程 ID。后续可以使用该 ID 删除请假日程。</para>
    /// <para>必填：是</para>
    /// <para>示例值：timeoff:XXXXXX-XXXX-0917-1623-aa493d591a39-XXXXXX</para>
    /// </summary>
    [JsonPropertyName("timeoff_event_id")]
    public string TimeoffEventId { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID。关于用户 ID 可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_XXXXXXXXXX</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>时区信息。</para>
    /// <para>必填：是</para>
    /// <para>示例值：Asia/Shanghai</para>
    /// </summary>
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    /// <summary>
    /// <para>请假开始时间。可能返回的时间格式：</para>
    /// <para>- 秒级时间戳，例如 `1609430400`</para>
    /// <para>- 日期，例如 `2021-01-01`</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-01</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>请假结束时间。可能返回的时间格式：</para>
    /// <para>- 秒级时间戳，例如 `1609430400`</para>
    /// <para>- 日期，例如 `2021-01-01`</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-01</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>请假日程标题。</para>
    /// <para>必填：否</para>
    /// <para>示例值：请假中(全天) / 1-Day Time Off</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>请假日程描述。</para>
    /// <para>必填：否</para>
    /// <para>示例值：若删除此日程，飞书中相应的“请假”标签将自动消失，而请假系统中的休假申请不会被撤销。</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}