// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;


/// <summary>
/// <para>要更新的任务数据，只需要设置出现在`update_fields`中的字段即可。如果`update_fields`设置了要变更一个字段名，但是`task`里没设置新的值，则表示将该字段清空。</para>
/// </summary>
public class UpdateTaskData
{
    /// <summary>
    /// <para>任务标题。如更新标题，不可将任务标题设为空。标题最大支持3000个utf8 字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：针对全年销售进行一次复盘</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>任务描述。描述最大支持3000个utf8字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：需要事先阅读复盘总结文档</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>任务截止时间。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("due")]
    public TaskDueTime? Due { get; set; }

    /// <summary>
    /// <para>调用者可以传入的任意附带到任务上的数据。在获取任务详情时会原样返回。</para>
    /// <para>必填：否</para>
    /// <para>示例值：dGVzdA==</para>
    /// <para>最大长度：65536</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>任务的完成时刻时间戳(ms)</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// <para>最大长度：20</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }

    /// <summary>
    /// <para>如果设置，则该任务为“重复任务”。</para>
    /// <para>必填：否</para>
    /// <para>示例值：FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR</para>
    /// <para>最大长度：1000</para>
    /// </summary>
    [JsonPropertyName("repeat_rule")]
    public string? RepeatRule { get; set; }

    /// <summary>
    /// <para>任务自定义完成配置。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_complete")]
    public TaskPlatformCompleteData? CustomComplete { get; set; }

    /// <summary>
    /// <para>任务的开始时间(ms)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start")]
    public TasksStartTime? Start { get; set; }

    /// <summary>
    /// <para>任务的完成模式。1 - 会签任务；2 - 或签任务</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>最大值：2</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("mode")]
    public int? Mode { get; set; }

    /// <summary>
    /// <para>是否是里程碑任务</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_milestone")]
    public bool? IsMilestone { get; set; }

    /// <summary>
    /// <para>自定义字段值。</para>
    /// <para>如要更新，每个字段的值根据字段类型填写相应的字段。</para>
    /// <para>* 当`type`为"number"时，应使用`number_value`字段，表示数字类型自定义字段的值；</para>
    /// <para>* 当`type`为"member"时，应使用`member_value`字段，表示人员类型自定义字段的值；</para>
    /// <para>* 当`type`为"datetime"时，应使用`datetime_value`字段，表示日期类型自定义字段的值；</para>
    /// <para>* 当`type`为"single_select"时，应使用`single_select_value`字段，表示单选类型自定义字段的值；</para>
    /// <para>* 当`type`为"multi_select"时，应使用`multi_select_value`字段，表示多选类型自定义字段的值；</para>
    /// <para>* 当`type`为"text"时，应使用`text_value`字段，表示文本类型自定义字段的值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_fields")]
    public InputCustomFieldValueUpdate[]? CustomFields { get; set; }
}