// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalTask;

/// <summary>
/// 退回审批任务请求体
/// </summary>
public class RollbackApprovalInstancesRequest
{

    /// <summary>
    /// <para>当前审批任务的审批人的用户 ID，ID 类型与查询参数 user_id_type 取值一致。</para>
    /// <para>必填：是</para>
    /// <para>示例值：893g4c45</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>当前需要回退的审批任务 ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：7026591166355210260</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// <para>退回原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：申请事项填写不具体，请重新填写</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// <para>扩展字段。</para>
    /// <para>**注意**：灰度参数，暂未开放使用。</para>
    /// <para>必填：否</para>
    /// <para>示例值：demo</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>需要退回到的任务 node_key。</para>
    /// <para>必填：是</para>
    /// <para>示例值：["START","APPROVAL_27997_285502","APPROVAL_462205_2734554"]</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("task_def_key_list")]
    public string[] TaskDefKeyList { get; set; } = [];
}
