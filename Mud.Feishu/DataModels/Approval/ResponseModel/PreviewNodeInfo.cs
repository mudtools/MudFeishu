// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 预览节点信息
/// </summary>
public class PreviewNodeInfo
{
    /// <summary>
    /// <para>审批人 ID 列表</para>
    /// </summary>
    [JsonPropertyName("user_id_list")]
    public string[]? UserIdList { get; set; }

    /// <summary>
    /// <para>审批结束抄送人 ID 列表</para>
    /// </summary>
    [JsonPropertyName("end_cc_id_list")]
    public string[]? EndCcIdList { get; set; }

    /// <summary>
    /// <para>审批节点 ID</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    /// <summary>
    /// <para>审批节点名称</para>
    /// </summary>
    [JsonPropertyName("node_name")]
    public string? NodeName { get; set; }

    /// <summary>
    /// <para>审批节点类型。可能值有：</para>
    /// <para>- AND：会签</para>
    /// <para>- OR：或签</para>
    /// <para>- AUTO_PASS：自动通过</para>
    /// <para>- AUTO_REJECT：自动拒绝</para>
    /// <para>- SEQUENTIAL：按顺序</para>
    /// </summary>
    [JsonPropertyName("node_type")]
    public string? NodeType { get; set; }

    /// <summary>
    /// <para>用户自定义节点 ID</para>
    /// </summary>
    [JsonPropertyName("custom_node_id")]
    public string? CustomNodeId { get; set; }

    /// <summary>
    /// <para>节点的说明信息</para>
    /// </summary>
    [JsonPropertyName("comments")]
    public string[]? Comments { get; set; }

    /// <summary>
    /// <para>审批人是否为空，若为空，则 user_id_list 为兜底审批人 ID 列表。</para>
    /// </summary>
    [JsonPropertyName("is_empty_logic")]
    public bool? IsEmptyLogic { get; set; }

    /// <summary>
    /// <para>是否发起人自选节点</para>
    /// </summary>
    [JsonPropertyName("is_approver_type_free")]
    public bool? IsApproverTypeFree { get; set; }

    /// <summary>
    /// <para>节点是否支持抄送人自选</para>
    /// </summary>
    [JsonPropertyName("has_cc_type_free")]
    public bool? HasCcTypeFree { get; set; }
}
