// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>审批可见人列表，列表长度上限 200，只有在审批可见人列表内的用户，才可以在审批发起页看到该审批。若该参数不传值，则表示任何人不可见。</para>
/// </summary>
public class ApprovalCreateViewers
{
    /// <summary>
    /// <para>可见人类型，生效优先级NONE&gt;TENANT&gt;指定范围</para>
    /// <para>必填：否</para>
    /// <para>示例值：USER</para>
    /// <para>可选值：<list type="bullet">
    /// <item>TENANT：租户内可见</item>
    /// <item>DEPARTMENT：指定部门</item>
    /// <item>USER：指定用户</item>
    /// <item>NONE：任何人都不可见</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("viewer_type")]
    public string? ViewerType { get; set; }

    /// <summary>
    /// <para>当 viewer_type 取值为 USER 时，需指定用户 ID。ID 类型与查询参数 user_id_type 取值保持一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：19a294c2</para>
    /// </summary>
    [JsonPropertyName("viewer_user_id")]
    public string? ViewerUserId { get; set; }

    /// <summary>
    /// <para>当 viewer_type 取值为 DEPARTMENT 时，需指定部门 ID。ID 类型与查询参数 department_id_type 取值保持一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-ac9d697abfa990b715dcc33d58a62a9d</para>
    /// </summary>
    [JsonPropertyName("viewer_department_id")]
    public string? ViewerDepartmentId { get; set; }
}
