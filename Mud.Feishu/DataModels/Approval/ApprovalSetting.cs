// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;


/// <summary>
/// <para>审批定义其他设置</para>
/// </summary>
public class ApprovalSetting
{
    /// <summary>
    /// <para>审批实例通过后允许撤回的时间，以秒为单位，默认 31 天，取值 0 为不可撤回。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("revert_interval")]
    public int? RevertInterval { get; set; }

    /// <summary>
    /// <para>是否支持审批通过第一个节点后撤回，默认为 1 表示支持，取值为 0 表示不支持。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("revert_option")]
    public int? RevertOption { get; set; }

    /// <summary>
    /// <para>审批被拒绝后的设置</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：默认设置，流程被终止</item>
    /// <item>1：退回至发起人，发起人可编辑流程后重新提交</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("reject_option")]
    public int? RejectOption { get; set; }

    /// <summary>
    /// <para>快捷审批配置项，开启后可在卡片上直接审批。</para>
    /// <para>**默认值**：1</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：禁用</item>
    /// <item>1：启用</item>
    /// </list></para>
    /// <para>默认值：1</para>
    /// </summary>
    [JsonPropertyName("quick_approval_option")]
    public int? QuickApprovalOption { get; set; }
}