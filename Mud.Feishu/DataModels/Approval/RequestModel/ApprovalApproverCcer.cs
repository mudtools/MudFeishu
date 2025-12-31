// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>审批人列表</para>
/// </summary>
public class ApprovalApproverCcer
{
    /// <summary>
    /// <para>审批人类型。使用说明：</para>
    /// <para>- 该参数取值为 Supervisor、SupervisorTopDown、DepartmentManager 、DepartmentManagerTopDown 这 4 种时，需要在 level 参数中填写对应的级数。例如：由下往上三级主管审批，该参数取值 Supervisor 、level 参数取值 3。</para>
    /// <para>- 该参数取值为 Personal 时，需要填写对应的 user_id ，用于指定用户。</para>
    /// <para>- 该参数取值为 Free 时，无需指定 user_id 和 level。</para>
    /// <para>必填：是</para>
    /// <para>示例值：Supervisor</para>
    /// <para>可选值：<list type="bullet">
    /// <item>Supervisor：主管审批（由下往上）</item>
    /// <item>SupervisorTopDown：主管审批（从上往下）</item>
    /// <item>DepartmentManager：部门负责人审批（由下往上）</item>
    /// <item>DepartmentManagerTopDown：部门负责人审批（从上往下）</item>
    /// <item>Personal：指定成员</item>
    /// <item>Free：发起人自选</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID。</para>
    /// <para>- type 取值 Personal 时需要通过该参数设置指定的用户。</para>
    /// <para>- ID 类型与查询参数 user_id_type 取值一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：f7cb567e</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批级数。当 type 取值为 Supervisor、SupervisorTopDown、DepartmentManager、DepartmentManagerTopDown 这 4 种时，需要在 level 中填写对应的级数。例如：由下往上三级主管审批，level 取值 3。</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("level")]
    public string? Level { get; set; }
}