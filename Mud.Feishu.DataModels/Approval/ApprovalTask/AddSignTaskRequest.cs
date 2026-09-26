// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalTask;

/// <summary>
/// 加签审批任务请求体（以用户身份调用）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class AddSignTaskRequest
{
    /// <summary>
    /// <para>审批实例 code</para>
    /// <para>必填：是</para>
    /// <para>示例值：6A123516-FB88-470D-A428-9AF58B71B3C0</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string InstanceCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>任务 id</para>
    /// <para>必填：是</para>
    /// <para>示例值：6992353208872969234</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批意见，500 字符内</para>
    /// <para>必填：否</para>
    /// <para>示例值：同意</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// <para>被加签人 id，与 user_id_type 一致。</para>
    /// <para>必填：是</para>
    /// <para>示例值：["f7cb567e"]</para>
    /// </summary>
    [JsonPropertyName("add_sign_user_ids")]
    public string[] AddSignUserIds { get; set; } = [];

    /// <summary>
    /// <para>加签类型。可选值：1：前加签；2：后加签；3：并加签。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("add_sign_type")]
    public int? AddSignType { get; set; }

    /// <summary>
    /// <para>审批方式。仅在前/后加签时填写，可选值：1：或签；2：会签。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("approval_method")]
    public int? ApprovalMethod { get; set; }
}
