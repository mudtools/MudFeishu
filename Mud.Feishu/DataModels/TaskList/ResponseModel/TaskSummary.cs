// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Task;

namespace Mud.Feishu.DataModels.TaskList;

/// <summary>
/// 任务摘要数据
/// </summary>
public class TaskSummary
{
    /// <summary>
    /// <para>任务GUID</para>
    /// <para>必填：否</para>
    /// <para>示例值：e297ddff-06ca-4166-b917-4ce57cd3a7a0</para>
    /// </summary>
    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    /// <summary>
    /// <para>任务标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：年终总结</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>任务完成的时间戳(ms)，为0表示未完成</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }

    /// <summary>
    /// <para>任务开始时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start")]
    public TaskTime? Start { get; set; }

    /// <summary>
    /// <para>任务截止时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("due")]
    public TaskTime? Due { get; set; }

    /// <summary>
    /// <para>任务成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("members")]
    public TaskMemberInfo[]? Members { get; set; }

    /// <summary>
    /// <para>子任务的个数</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("subtask_count")]
    public int? SubtaskCount { get; set; }
}
