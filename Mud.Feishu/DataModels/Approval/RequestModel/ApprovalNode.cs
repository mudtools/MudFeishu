// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>审批定义节点列表，用于设置审批流程所需要的各个节点，审批流程的始末固定为开始节点和结束节点，因此传值时需要将开始节点作为 list 第一个元素，结束节点作为 list 最后一个元素。</para>
/// <para>**说明**：API 方式不支持设置条件分支，如需设置条件分支请前往[飞书审批后台]创建审批定义。</para>
/// </summary>
public class ApprovalNode
{
    /// <summary>
    /// <para>节点 ID。</para>
    /// <para>- 开始节点的 ID 为 START</para>
    /// <para>- 结束节点的 ID 为 END</para>
    /// <para>开始和结束节点不需要指定 name、node_type 以及 approver。</para>
    /// <para>必填：是</para>
    /// <para>示例值：START</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>节点名称的国际化文案 Key，以 `@i18n@` 开头，长度不得少于 9 个字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@node_name</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>当前节点的审批方式。</para>
    /// <para>**注意**：当该参数取值为依次审批（SEQUENTIAL）时，审批人类型（approver.type）必须为发起人自选（Free）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：AND</para>
    /// <para>可选值：<list type="bullet">
    /// <item>AND：会签，需要所有审批人同意才会通过审批</item>
    /// <item>OR：或签，一名审批人同意即可通过审批</item>
    /// <item>SEQUENTIAL：依次审批，按照审批人顺序依次进行审批</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("node_type")]
    public string? NodeType { get; set; }

    /// <summary>
    /// <para>审批人列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approver")]
    public ApprovalApproverCcer[]? Approvers { get; set; }

    /// <summary>
    /// <para>抄送人列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ccer")]
    public ApprovalApproverCcer[]? Ccers { get; set; }

    /// <summary>
    /// <para>表单内的控件权限</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("privilege_field")]
    public FieldGroup? PrivilegeField { get; set; }


    /// <summary>
    /// <para>发起人自选审批人时，是否允许多选。</para>
    /// <para>- true：允许</para>
    /// <para>- false：不允许</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("approver_chosen_multi")]
    public bool? ApproverChosenMulti { get; set; }

    /// <summary>
    /// <para>发起人自选审批人时，可选择的范围。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approver_chosen_range")]
    public ApproverRange[]? ApproverChosenRanges { get; set; }



    /// <summary>
    /// <para>审批人为提交人本人时的操作</para>
    /// <para>必填：否</para>
    /// <para>示例值：STARTER</para>
    /// <para>可选值：<list type="bullet">
    /// <item>STARTER：提交人本人进行审批</item>
    /// <item>AUTO_PASS：自动通过</item>
    /// <item>SUPERVISOR：提交人的直属上级进行审批</item>
    /// <item>DEPARTMENT_MANAGER：提交人的直属部门负责人进行审批</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("starter_assignee")]
    public string? StarterAssignee { get; set; }
}