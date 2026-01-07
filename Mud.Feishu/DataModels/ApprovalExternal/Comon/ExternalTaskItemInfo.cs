// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>审批实例下的审批任务</para>
/// </summary>
public class ExternalTaskItemInfo
{
    /// <summary>
    /// <para>审批任务 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：310</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批任务状态</para>
    /// <para>必填：是</para>
    /// <para>示例值：PENDING</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PENDING：审批中</item>
    /// <item>APPROVED：审批流程结束，结果为同意</item>
    /// <item>REJECTED：审批流程结束，结果为拒绝</item>
    /// <item>TRANSFERRED：任务转交</item>
    /// <item>DONE：任务通过但审批人未操作；审批人看不到这个任务, 若想要看到, 可以通过抄送该人.</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批任务最后更新时间，单位 毫秒</para>
    /// <para>必填：是</para>
    /// <para>示例值：1621863215000</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string UpdateTime { get; set; } = string.Empty;
}