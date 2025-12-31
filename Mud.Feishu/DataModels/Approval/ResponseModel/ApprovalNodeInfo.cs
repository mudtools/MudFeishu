// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 审批定义节点信息
/// </summary>
public class ApprovalNodeInfo
{
    /// <summary>
    /// <para>节点名称</para>
    /// <para>示例值：Approval</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>是否为发起人自选节点。取值为 true 表示发起审批时需要提交人自选审批人。</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("need_approver")]
    public bool NeedApprover { get; set; }

    /// <summary>
    /// <para>节点 ID</para>
    /// <para>示例值：46e6d96cfa756980907209209ec03b64</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// <para>节点自定义 ID，如果没有设置则不返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：46e6d96cfa756980907209209ec03b64</para>
    /// </summary>
    [JsonPropertyName("custom_node_id")]
    public string? CustomNodeId { get; set; }

    /// <summary>
    /// <para>审批方式</para>
    /// <para>示例值：AND</para>
    /// <para>可选值：<list type="bullet">
    /// <item>AND：会签</item>
    /// <item>OR：或签</item>
    /// <item>SEQUENTIAL：依次审批</item>
    /// <item>CC_NODE：抄送节点</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("node_type")]
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// <para>选择方式是否支持多选。流程的开始、结束节点该值无意义。</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("approver_chosen_multi")]
    public bool ApproverChosenMulti { get; set; }

    /// <summary>
    /// <para>提交人自选审批人的范围</para>
    /// </summary>
    [JsonPropertyName("approver_chosen_range")]
    public ApproverChosenRange[]? ApproverChosenRanges { get; set; }

    /// <summary>
    /// <para>审批同意时是否需要手写签名。</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("require_signature")]
    public bool? RequireSignature { get; set; }
}
