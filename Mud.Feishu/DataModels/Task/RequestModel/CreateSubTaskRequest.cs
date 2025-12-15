// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;

/// <summary>
/// 创建子任务请求体
/// </summary>
public class CreateSubTaskRequest
{
    /// <summary>
    /// <para>任务标题</para>
    /// <para>必填：是</para>
    /// <para>示例值：针对全年销售进行一次复盘</para>
    /// <para>最大长度：10000</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// <para>任务摘要</para>
    /// <para>必填：否</para>
    /// <para>示例值：需要事先阅读复盘总结文档</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>任务截止时间戳(ms)，截止时间戳和截止日期选择一个填写。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("due")]
    public TaskTime? Due { get; set; }

    /// <summary>
    /// <para>任务关联的第三方平台来源信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("origin")]
    public TaskOriginSrcData? Origin { get; set; }

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
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }

    /// <summary>
    /// <para>任务成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("members")]
    public TaskMember[]? Members { get; set; }


    /// <summary>
    /// <para>如果设置，则该任务为“重复任务”。该字段表示了重复任务的重复规则。详见[功能概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/task-v2/overview)中的“如何使用重复任务？”章节。</para>
    /// <para>必填：否</para>
    /// <para>示例值：FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR</para>
    /// <para>最大长度：1000</para>
    /// </summary>
    [JsonPropertyName("repeat_rule")]
    public string? RepeatRule { get; set; }

    /// <summary>
    /// <para>任务自定义完成规则。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_complete")]
    public TaskPlatformCompleteData? CustomComplete { get; set; }

    /// <summary>
    /// <para>任务所在清单的信息。如果设置，则表示创建的任务要直接加入到指定清单。</para>
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
    /// <para>任务的开始时间(ms)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start")]
    public TaskTime? Start { get; set; }

    /// <summary>
    /// <para>任务提醒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reminders")]
    public SubTaskReminder[]? Reminders { get; set; }

}
