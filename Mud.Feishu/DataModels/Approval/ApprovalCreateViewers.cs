// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>viewers 字段指定了哪些人能从审批应用的前台发起该审批。使用说明：</para>
/// <para>- 当 viewer_type 为 USER，需要填写 viewer_user_id</para>
/// <para>- 当 viewer_type 为 DEPARTMENT，需要填写 viewer_department_id</para>
/// <para>- 当 viewer_type 为 TENANT 或 NONE 时，无需填写 viewer_user_id 和 viewer_department_id</para>
/// <para>**注意**：列表最大长度为 200。</para>
/// </summary>
public class ApprovalCreateViewers
{
    /// <summary>
    /// <para>审批定义的可见范围</para>
    /// <para>必填：否</para>
    /// <para>示例值：USER</para>
    /// <para>可选值：<list type="bullet">
    /// <item>TENANT：当前企业内可见</item>
    /// <item>DEPARTMENT：指定部门可见</item>
    /// <item>USER：指定用户可见</item>
    /// <item>NONE：任何人都不可见</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("viewer_type")]
    public string? ViewerType { get; set; }

    /// <summary>
    /// <para>当 viewer_type 是 USER 时，需要通过该参数传入用户 ID，ID 类型与查询参数 user_id_type 取值一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：19a294c2</para>
    /// </summary>
    [JsonPropertyName("viewer_user_id")]
    public string? ViewerUserId { get; set; }

    /// <summary>
    /// <para>当 viewer_type 为DEPARTMENT，需要通过该参数传入部门 ID，ID 类型与查询参数 department_id_type 取值一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-ac9d697abfa990b715dcc33d58a62a9d</para>
    /// </summary>
    [JsonPropertyName("viewer_department_id")]
    public string? ViewerDepartmentId { get; set; }
}