// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalQuery;

/// <summary>
/// 任务列表
/// </summary>
public class TaskUserSearchItem
{
    /// <summary>
    /// <para>任务所属的任务分组，如「待办」、「已办」等</para>
    /// <para>**可选值有**：</para>
    /// <para>1:待办审批,2:已办审批,3:已发起审批,17:未读知会,18:已读知会</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：待办审批</item>
    /// <item>2：已办审批</item>
    /// <item>3：已发起审批</item>
    /// <item>17：未读知会</item>
    /// <item>18：已读知会</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// <para>任务所属的用户 ID</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>任务题目</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>任务相关 URL</para>
    /// </summary>
    [JsonPropertyName("urls")]
    public TaskUserUrls? Urls { get; set; }


    /// <summary>
    /// <para>流程三方 ID，仅第三方流程，需要在当前租户、当前 APP 内唯一</para>
    /// </summary>
    [JsonPropertyName("process_external_id")]
    public string? ProcessExternalId { get; set; }

    /// <summary>
    /// <para>任务三方 ID，仅第三方流程，需要在当前流程实例内唯一</para>
    /// </summary>
    [JsonPropertyName("task_external_id")]
    public string? TaskExternalId { get; set; }

    /// <summary>
    /// <para>任务状态</para>
    /// <para>**可选值有**：</para>
    /// <para>1:待办,2:已办,17:未读,18:已读,33:处理中，标记完成用,34:撤回</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：待办</item>
    /// <item>2：已办</item>
    /// <item>17：未读</item>
    /// <item>18：已读</item>
    /// <item>33：处理中，标记完成用</item>
    /// <item>34：撤回</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>流程实例状态</para>
    /// <para>**可选值有**：</para>
    /// <para>0:无流程状态，不展示对应标签,1:流程实例流转中,2:已通过,3:已拒绝,4:已撤销,5:已终止</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：无流程状态，不展示对应标签</item>
    /// <item>1：流程实例流转中</item>
    /// <item>2：已通过</item>
    /// <item>3：已拒绝</item>
    /// <item>4：已撤销</item>
    /// <item>5：已终止</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("process_status")]
    public string? ProcessStatus { get; set; }

    /// <summary>
    /// <para>流程定义 Code</para>
    /// </summary>
    [JsonPropertyName("definition_code")]
    public string? DefinitionCode { get; set; }

    /// <summary>
    /// <para>发起人 ID 列表</para>
    /// </summary>
    [JsonPropertyName("initiators")]
    public string[]? Initiators { get; set; }

    /// <summary>
    /// <para>发起人姓名列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("initiator_names")]
    public string[]? InitiatorNames { get; set; }

    /// <summary>
    /// <para>任务 ID，全局唯一</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>流程 ID，全局唯一</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>流程 Code</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_code")]
    public string? ProcessCode { get; set; }

    /// <summary>
    /// <para>流程定义分组 ID</para>
    /// </summary>
    [JsonPropertyName("definition_group_id")]
    public string? DefinitionGroupId { get; set; }

    /// <summary>
    /// <para>流程定义分组名称</para>
    /// </summary>
    [JsonPropertyName("definition_group_name")]
    public string? DefinitionGroupName { get; set; }

    /// <summary>
    /// <para>流程定义 ID</para>
    /// </summary>
    [JsonPropertyName("definition_id")]
    public string? DefinitionId { get; set; }

    /// <summary>
    /// <para>流程定义名称</para>
    /// </summary>
    [JsonPropertyName("definition_name")]
    public string? DefinitionName { get; set; }
}
