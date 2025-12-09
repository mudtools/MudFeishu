// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;

/// <summary>
/// <para>返回的任务列表</para>
/// </summary>
public class ListTaskInfo
{
    /// <summary>
    /// <para>任务guid，任务的唯一ID</para>   
    /// <para>示例值：83912691-2e43-47fc-94a4-d512e03984fa</para>
    /// </summary>
    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    /// <summary>
    /// <para>任务标题</para>   
    /// <para>示例值：进行销售年中总结</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>任务备注</para>   
    /// <para>示例值：进行销售年中总结</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>任务截止时间</para>   
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("due")]
    public TaskDueTime? Due { get; set; }

    /// <summary>
    /// <para>任务的提醒配置列表。目前每个任务最多有1个。</para>
    /// </summary>
    [JsonPropertyName("reminders")]
    public TaskReminderInfo[]? Reminders { get; set; }

    /// <summary>
    /// <para>任务创建者</para>
    /// </summary>
    [JsonPropertyName("creator")]
    public TaskMember? Creator { get; set; }

    /// <summary>
    /// <para>任务成员列表</para>
    /// </summary>
    [JsonPropertyName("members")]
    public TaskMember[]? Members { get; set; }

    /// <summary>
    /// <para>任务完成的时间戳(ms)</para>
    /// <para>示例值：1675742789470</para>
    /// <para>最大长度：20</para>
    /// </summary>
    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }

    /// <summary>
    /// <para>任务的附件列表</para>
    /// </summary>
    [JsonPropertyName("attachments")]
    public TaskAttachment[]? Attachments { get; set; }

    /// <summary>
    /// <para>任务关联的第三方平台来源信息。创建是设置后就不可更改。</para>
    /// </summary>
    [JsonPropertyName("origin")]
    public TaskOrigin? Origin { get; set; }

    /// <summary>
    /// <para>任务附带的自定义数据。</para>
    /// <para>示例值：dGVzdA==</para>
    /// <para>最大长度：65536</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>任务所属清单的名字。调用者只能看到有权限访问的清单的列表。</para>
    /// </summary>
    [JsonPropertyName("tasklists")]
    public TaskInTaskListInfo[]? Tasklists { get; set; }

    /// <summary>
    /// <para>如果任务为重复任务，返回重复任务的配置</para>
    /// <para>示例值：FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR</para>
    /// </summary>
    [JsonPropertyName("repeat_rule")]
    public string? RepeatRule { get; set; }

    /// <summary>
    /// <para>如果当前任务为某个任务的子任务，返回父任务的guid</para>
    /// <para>示例值：e297ddff-06ca-4166-b917-4ce57cd3a7a0</para>
    /// </summary>
    [JsonPropertyName("parent_task_guid")]
    public string? ParentTaskGuid { get; set; }

    /// <summary>
    /// <para>任务的模式。1 - 会签任务；2 - 或签任务</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("mode")]
    public int? Mode { get; set; }

    /// <summary>
    /// <para>任务创建的来源</para>
    /// <para>示例值：6</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：未知来源</item>
    /// <item>1：任务中心</item>
    /// <item>2：群组任务/消息转任务</item>
    /// <item>6：通过开放平台以tenant_access_token授权创建的任务</item>
    /// <item>7：通过开放平台以user_access_token授权创建的任务</item>
    /// <item>8：文档任务</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("source")]
    public int? Source { get; set; }

    /// <summary>
    /// <para>任务的自定义完成配置</para>
    /// </summary>
    [JsonPropertyName("custom_complete")]
    public TaskPlatformCompleteData? CustomComplete { get; set; }

    /// <summary>
    /// <para>任务界面上的代码</para>
    /// <para>示例值：t6272302</para>
    /// <para>最大长度：20</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>任务创建时间戳(ms)</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>任务最后一次更新的时间戳(ms)</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>
    /// <para>任务的状态，支持"todo"和"done"两种状态</para>
    /// <para>示例值：todo</para>
    /// <para>最大长度：20</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>任务的分享链接</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>任务的开始时间</para>
    /// </summary>
    [JsonPropertyName("start")]
    public TasksStartTime? Start { get; set; }


    /// <summary>
    /// <para>该任务的子任务的个数。</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("subtask_count")]
    public int? SubtaskCount { get; set; }

    /// <summary>
    /// <para>是否是里程碑任务</para>

    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_milestone")]
    public bool? IsMilestone { get; set; }

    /// <summary>
    /// <para>任务的自定义字段值</para>

    /// </summary>
    [JsonPropertyName("custom_fields")]
    public CustomFieldValue[]? CustomFields { get; set; }


    /// <summary>
    /// <para>任务依赖</para>
    /// </summary>
    [JsonPropertyName("dependencies")]
    public TaskDependency[]? Dependencies { get; set; }
}