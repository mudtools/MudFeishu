// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 预览审批流程请求体
/// </summary>
public class PreviewInstanceRequest
{
    /// <summary>
    /// <para>审批定义 Code。</para>
    /// <para>**示例值**："7C468A54-8745-2245-9675-08B7C63E7A85"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// <para>用户 ID，ID 类型与查询参数 user_id_type 的取值一致。</para>
    /// <para>- 在创建审批实例之前预览审批流程，此处需要传入审批发起人的用户 ID。</para>
    /// <para>- 在创建审批实例之后预览某审批任务的后续流程，此处需要传入审批任务审批人 ID。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批发起人所属的部门 ID。</para>
    /// <para>**注意**：如果用户只属于一个部门，该参数选填。如果用户属于多个部门，则必须填其中一个部门 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>审批表单的控件 JSON 值。</para>
    /// <para>**注意**：在创建审批实例之前预览审批流程，该参数必填。</para>
    /// <para>**示例值**：[{\"id\":\"widget16256287451710001\", \"type\": \"number\", \"value\":\"43\"}]</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("form")]
    public string? Form { get; set; }

    /// <summary>
    /// <para>审批实例 Code。</para>
    /// <para>**示例值**："81D31358-93AF-92D6-7425-01A5D67C4E71"</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// <para>审批任务 ID。</para>
    /// <para>**注意**：在创建审批实例之后预览某审批任务的后续流程，该参数必填，并且 user_id 需要传入任务的审批人 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }
}
