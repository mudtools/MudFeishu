// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 审批实例任务信息
/// </summary>
public class InstanceTaskInfo
{
    /// <summary>
    /// <para>审批任务 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：1234</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批人的 user_id，自动通过、自动拒绝时该参数返回值为空。</para>
    /// <para>必填：是</para>
    /// <para>示例值：f7cb567e</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批人的 open_id，自动通过、自动拒绝时该参数返回值为空。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_123457</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>审批任务状态</para>
    /// <para>必填：是</para>
    /// <para>示例值：PENDING</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PENDING：审批中</item>
    /// <item>APPROVED：通过</item>
    /// <item>REJECTED：拒绝</item>
    /// <item>TRANSFERRED：已转交</item>
    /// <item>DONE：完成</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批任务所属的审批节点 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：46e6d96cfa756980907209209ec03b64</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    /// <summary>
    /// <para>审批任务所属的审批节点名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：开始</para>
    /// </summary>
    [JsonPropertyName("node_name")]
    public string? NodeName { get; set; }

    /// <summary>
    /// <para>审批任务所属的审批节点的自定义 ID。如果没设置自定义 ID，则不返回该参数值。</para>
    /// <para>必填：否</para>
    /// <para>示例值：manager</para>
    /// </summary>
    [JsonPropertyName("custom_node_id")]
    public string? CustomNodeId { get; set; }

    /// <summary>
    /// <para>审批方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：AND</para>
    /// <para>可选值：<list type="bullet">
    /// <item>AND：会签</item>
    /// <item>OR：或签</item>
    /// <item>AUTO_PASS：自动通过</item>
    /// <item>AUTO_REJECT：自动拒绝</item>
    /// <item>SEQUENTIAL：按顺序</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>审批任务的开始时间，毫秒级时间戳。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1564590532967</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批任务的完成时间，毫秒级时间戳。未完成时返回 0。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}
