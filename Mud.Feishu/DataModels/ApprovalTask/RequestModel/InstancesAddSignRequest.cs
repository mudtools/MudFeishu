// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalTask;

/// <summary>
/// 审批任务加签请求体
/// </summary>
public class InstancesAddSignRequest
{
    /// <summary>
    /// <para>审批定义 Code 获取方式：</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例 Code </para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>操作用户的 user_id。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批任务 ID。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审核意见</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// <para>被加签人的 user_id，可以指定多个。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("add_sign_user_ids")]
    public string[] AddSignUserIds { get; set; } = [];

    /// <summary>
    /// <para>加签方式，可选值有：</para>
    /// <para>- 1：前加签，在当前操作用户之前审批。</para>
    /// <para>- 2：后加签，加签后自动通过当前审批，并流转至被加签人。</para>
    /// <para>- 3：并加签，和当前操作用户共同审批。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("add_sign_type")]
    public int AddSignType { get; set; }

    /// <summary>
    /// <para>仅在前加签、后加签时，需要填写该参数。可选值有：</para>
    /// <para>- 1： 或签，一名审批人同意或拒绝即可。</para>
    /// <para>- 2： 会签，需要所有审批人同意或拒绝。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_method")]
    public int? ApprovalMethod { get; set; }
}
