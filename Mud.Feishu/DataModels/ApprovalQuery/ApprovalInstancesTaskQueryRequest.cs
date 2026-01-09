// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalQuery;

/// <summary>
/// 查询任务列表请求体
/// </summary>
public class ApprovalInstancesTaskQueryRequest
{
    /// <summary>
    /// <para>任务审批人 ID，ID 类型与查询参数 user_id_type 保持一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：lwiu098wj</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批定义 Code。</para>
    /// <para>**注意**：</para>
    /// <para>- user_id、approval_code、instance_code、instance_external_id、group_external_id 不能同时为空。</para>
    /// <para>- approval_code 和 group_external_id 查询结果取并集。</para>
    /// <para>必填：否</para>
    /// <para>示例值：EB828003-9FFE-4B3F-AA50-2E199E2ED942</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// <para>审批实例 Code。</para>
    /// <para>**注意**：</para>
    /// <para>- user_id、approval_code、instance_code、instance_external_id、group_external_id 不能同时为空。</para>
    /// <para>- instance_code 和 instance_external_id 查询结果取并集。</para>
    /// <para>必填：否</para>
    /// <para>示例值：EB828003-9FFE-4B3F-AA50-2E199E2ED943</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// <para>审批实例的第三方 ID。</para>
    /// <para>**注意**：</para>
    /// <para>- user_id、approval_code、instance_code、instance_external_id、group_external_id 不能同时为空。</para>
    /// <para>- instance_code 和 instance_external_id 查询结果取并集。</para>
    /// <para>必填：否</para>
    /// <para>示例值：EB828003-9FFE-4B3F-AA50-2E199E2ED976</para>
    /// </summary>
    [JsonPropertyName("instance_external_id")]
    public string? InstanceExternalId { get; set; }

    /// <summary>
    /// <para>审批定义分组的第三方 ID。</para>
    /// <para>**注意**：</para>
    /// <para>- user_id、approval_code、instance_code、instance_external_id、group_external_id 不能同时为空。</para>
    /// <para>- approval_code 和 group_external_id 查询结果取并集。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1234567</para>
    /// </summary>
    [JsonPropertyName("group_external_id")]
    public string? GroupExternalId { get; set; }

    /// <summary>
    /// <para>审批任务标题。</para>
    /// <para>**说明**：仅第三方审批存在审批任务标题。</para>
    /// <para>必填：否</para>
    /// <para>示例值：test</para>
    /// </summary>
    [JsonPropertyName("task_title")]
    public string? TaskTitle { get; set; }

    /// <summary>
    /// <para>审批任务状态。</para>
    /// <para>**注意**：若不设置则查询全部状态，若不在集合中，则报错。</para>
    /// <para>必填：否</para>
    /// <para>示例值：PENDING</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PENDING：审批中</item>
    /// <item>REJECTED：已拒绝</item>
    /// <item>APPROVED：已通过</item>
    /// <item>TRANSFERRED：已转交</item>
    /// <item>DONE：已完成</item>
    /// <item>RM_REPEAT：去重</item>
    /// <item>PROCESSED：已处理</item>
    /// <item>ALL：所有状态</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("task_status")]
    public string? TaskStatus { get; set; }

    /// <summary>
    /// <para>任务查询开始时间，Unix 毫秒时间戳。与 task_start_time_to 参数构成时间段查询条件，仅会返回在该时间段内的审批任务。</para>
    /// <para>**注意**：查询时间跨度不得大于 30 天，开始和结束时间必须同时设置或者同时不设置。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1547654251506</para>
    /// </summary>
    [JsonPropertyName("task_start_time_from")]
    public string? TaskStartTimeFrom { get; set; }

    /// <summary>
    /// <para>任务查询结束时间，Unix 毫秒时间戳。与 task_start_time_from 参数构成时间段查询条件，仅会返回在该时间段内的审批任务。</para>
    /// <para>**注意**：查询时间跨度不得大于 30 天，开始和结束时间必须同时设置或者同时不设置。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1547654251506</para>
    /// </summary>
    [JsonPropertyName("task_start_time_to")]
    public string? TaskStartTimeTo { get; set; }

    /// <summary>
    /// <para>语言。</para>
    /// <para>必填：否</para>
    /// <para>示例值：zh-CN</para>
    /// <para>可选值：<list type="bullet">
    /// <item>zh-CN：中文</item>
    /// <item>en-US：英文</item>
    /// <item>ja-JP：日文</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// <para>查询多种状态的任务，当填写此参数时，task_status 参数将失效。</para>
    /// <para>**可选值有**：</para>
    /// <para>- `PENDING`：审批中</para>
    /// <para>- `REJECTED`：拒绝</para>
    /// <para>- `APPROVED`：通过</para>
    /// <para>- `TRANSFERRED`：转交</para>
    /// <para>- `DONE`：已完成</para>
    /// <para>- `RM_REPEAT`：去重</para>
    /// <para>- `PROCESSED`：已处理</para>
    /// <para>必填：否</para>
    /// <para>示例值：PENDING</para>
    /// </summary>
    [JsonPropertyName("task_status_list")]
    public string[]? TaskStatusList { get; set; }

    /// <summary>
    /// <para>按任务时间排序</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：按审批任务更新时间（update_time）倒排。</item>
    /// <item>1：按审批任务更新时间（update_time）正排。</item>
    /// <item>2：按审批任务开始时间（start_time）倒排。</item>
    /// <item>3：按审批任务开始时间（start_time）正排。</item>
    /// </list></para>
    /// <para>默认值：2</para>
    /// </summary>
    [JsonPropertyName("order")]
    public int? Order { get; set; }
}