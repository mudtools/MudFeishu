// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 周期任务的环节任务信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceStageTaskInfo
{
    /// <summary>
    /// <para>环节 ID</para>
    /// <para>示例值：7270346432875692033</para>
    /// </summary>
    [JsonPropertyName("stage_id")]
    public string? StageId { get; set; }

    /// <summary>
    /// <para>环节名称（中划线形态 zh-CN / en-US）</para>
    /// </summary>
    [JsonPropertyName("name")]
    public PerformanceI18nName? Name { get; set; }

    /// <summary>
    /// <para>环节截止时间，毫秒时间戳</para>
    /// <para>示例值：1717142244000</para>
    /// </summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }

    /// <summary>
    /// <para>未完成的任务数量</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("need_todo_count")]
    public int? NeedTodoCount { get; set; }

    /// <summary>
    /// <para>处理任务的系统页面链接</para>
    /// </summary>
    [JsonPropertyName("jump_url")]
    public string? JumpUrl { get; set; }

    /// <summary>
    /// <para>环节任务状态：need_todo（还有待完成的任务）/ overdue（剩余未完成的任务均已逾期）/ all_done（全部任务均已完成）/ stage_pause（环节被暂停）</para>
    /// <para>示例值：need_todo</para>
    /// </summary>
    [JsonPropertyName("stage_task_status")]
    public string? StageTaskStatus { get; set; }

    /// <summary>
    /// <para>任务分类：1（待完成）/ 2（已完成）/ 3（已逾期，此分类仅在租户系统设置为不允许逾期提交时存在）</para>
    /// </summary>
    [JsonPropertyName("task_option_id")]
    public int? TaskOptionId { get; set; }

    /// <summary>
    /// <para>已完成的任务数量</para>
    /// <para>示例值：99</para>
    /// </summary>
    [JsonPropertyName("finished_count")]
    public int? FinishedCount { get; set; }
}
