// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;

/// <summary>
/// 创建任务请求体
/// </summary>
public class CreateTaskRequest
{
    /// <summary>
    /// <para>任务标题。不能为空，支持最大3000个utf8字符。</para>
    /// <para>必填：是</para>
    /// <para>示例值：针对全年销售进行一次复盘</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// <para>任务摘要。支持最大3000个utf8字符。</para>
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
    /// <para>任务关联的第三方平台来源信息，用于来源信息在飞书任务界面的展示。只能创建任务时设置，一旦设置后就不可变更。详见[功能概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/task-v2/overview)中的“ 如何使用Origin? ”章节。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("origin")]
    public TaskOriginSrcData? Origin { get; set; }


    /// <summary>
    /// <para>调用者可以传入的任意附带到任务上的数据。在获取任务详情时会原样返回。如果是二进制数据可以使用Base64编码。</para>
    /// <para>必填：否</para>
    /// <para>示例值：dGVzdA==</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>任务的完成时刻时间戳(ms)。不填写或者设为0表示创建一个未完成任务；填写一个具体的时间戳表示创建一个已完成任务。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }

    /// <summary>
    /// <para>任务成员列表，包括负责人和关注人。不填写表示任务无成员。单次请求支持最大50个成员（去重后）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("members")]
    public TaskMemberInfo[]? Members { get; set; }

    /// <summary>
    /// <para>重复任务规则。如果设置，则该任务为“重复任务”。</para>
    /// <para>必填：否</para>
    /// <para>示例值：FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR</para>
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
    /// <para>任务所在清单的信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tasklists")]
    public TaskInTaskListInfo[]? Tasklists { get; set; }

    /// <summary>
    /// <para>幂等token。如果提供则触发后端实现幂等行为。</para>
    /// <para>必填：否</para>
    /// <para>示例值：daa2237f-8310-4707-a83b-52c8a81e0fb7</para>
    /// </summary>
    [JsonPropertyName("client_token")]
    public string? ClientToken { get; set; }

    /// <summary>
    /// <para>任务的开始时间(ms),。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start")]
    public TasksStartTime? Start { get; set; }


    /// <summary>
    /// <para>任务提醒。要设置提醒必须同时设置任务的截止时间。一个任务最多只能设置1个提醒。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reminders")]
    public TaskReminder[]? Reminders { get; set; }



    /// <summary>
    /// <para>任务完成模式, 1 - 会签任务; 2 - 或签任务</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("mode")]
    public int? Mode { get; set; }

    /// <summary>
    /// <para>是否是里程碑任务</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_milestone")]
    public bool? IsMilestone { get; set; }

    /// <summary>
    /// <para>自定义字段值。可以在创建任务的同时设置一个或多个自定义字段的值。要设置值的自定义字段必须关联于任务要加入的清单(通过`tasklists`字段设置），否则将无法设置。</para>
    /// <para>每个字段的值根据字段类型填写相应的字段。</para>
    /// <para>* 当`type`为"number"时，应使用`number_value`字段，表示数字类型自定义字段的值；</para>
    /// <para>* 当`type`为"member"时，应使用`member_value`字段，表示人员类型自定义字段的值；</para>
    /// <para>* 当`type`为"datetime"时，应使用`datetime_value`字段，表示日期类型自定义字段的值；</para>
    /// <para>* 当`type`为"single_select"时，应使用`single_select_value`字段，表示单选类型自定义字段的值；</para>
    /// <para>* 当`type`为"multi_select"时，应使用`multi_select_value`字段，表示多选类型自定义字段的值；</para>
    /// <para>* 当`type`为“text”时，应使用`text_value`字段，表示文本字段类型的值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_fields")]
    public InputCustomFieldValue[]? CustomFields { get; set; }


    /// <summary>
    /// <para>如果希望设置任务来源为文档，则设置此字段</para>
    /// <para>- 和extra互斥，同时设置时报错</para>
    /// <para>- 和origin互斥，同时设置时报错</para>
    /// <para>- 和custom_complete互斥，同时设置时报错</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("docx_source")]
    public TaskDocxSource? DocxSource { get; set; }


    /// <summary>
    /// <para>正数协议每日提醒</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("positive_reminders")]
    public TaskReminder[]? PositiveReminders { get; set; }
}