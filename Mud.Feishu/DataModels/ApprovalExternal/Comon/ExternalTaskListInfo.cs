// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>返回数据</para>
/// </summary>
public record ExternalTaskList
{
    /// <summary>
    /// <para>审批实例 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：29075</para>
    /// </summary>
    [JsonPropertyName("instance_id")]
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批的id</para>
    /// <para>必填：是</para>
    /// <para>示例值：fwwweffff33111133xxx</para>
    /// </summary>
    [JsonPropertyName("approval_id")]
    public string ApprovalId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批对应的 approval_code</para>
    /// <para>必填：是</para>
    /// <para>示例值：B7B65FFE-C2GC-452F-9F0F-9AA8352363D6</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例当前的状态</para>
    /// <para>必填：是</para>
    /// <para>示例值：PENDING</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PENDING：审批中</item>
    /// <item>APPROVED：审批流程结束，结果为同意</item>
    /// <item>REJECTED：审批流程结束，结果为拒绝</item>
    /// <item>CANCELED：审批发起人撤回</item>
    /// <item>DELETED：审批被删除</item>
    /// <item>HIDDEN：状态隐藏(不显示状态)</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例最后更新时间，单位 毫秒</para>
    /// <para>必填：是</para>
    /// <para>示例值：1621863215000</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string UpdateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例下的审批任务</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tasks")]
    public ExternalTaskItemInfo[]? Tasks { get; set; }

}