// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 审批实例详情（以用户身份查询）中的当前审批节点信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class InstanceCurrentNodeInfo
{
    /// <summary>
    /// <para>当前审批节点 id</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    /// <summary>
    /// <para>当前审批节点名称</para>
    /// </summary>
    [JsonPropertyName("node_name")]
    public string? NodeName { get; set; }

    /// <summary>
    /// <para>审批方式</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>当前节点审批人</para>
    /// </summary>
    [JsonPropertyName("approvers")]
    public InstanceCurrentNodeApproverInfo[]? Approvers { get; set; }
}

/// <summary>
/// 当前审批节点审批人信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class InstanceCurrentNodeApproverInfo
{
    /// <summary>
    /// <para>任务 ID</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>任务对应的 userID</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
