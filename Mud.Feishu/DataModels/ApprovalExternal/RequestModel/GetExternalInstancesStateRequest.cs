// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// 获取三方审批任务状态请求体
/// </summary>
public class GetExternalInstancesStateRequest
{

    /// <summary>
    /// <para>审批定义 Code，用于指定只获取这些定义下的数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：B7B65FFE-C2GC-452F-9F0F-9AA8352363D6</para>
    /// <para>最大长度：20</para>
    /// </summary>
    [JsonPropertyName("approval_codes")]
    public string[]? ApprovalCodes { get; set; }

    /// <summary>
    /// <para>审批实例 ID, 用于指定只获取这些实例下的数据，最多支持 20 个</para>
    /// <para>必填：否</para>
    /// <para>示例值：oa_159160304</para>
    /// </summary>
    [JsonPropertyName("instance_ids")]
    public string[]? InstanceIds { get; set; }

    /// <summary>
    /// <para>审批人 user_id，用于指定只获取这些用户的数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：112321</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }

    /// <summary>
    /// <para>审批任务状态，用于指定获取该状态下的数据</para>
    /// <para>必填：否</para>
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
    public string? Status { get; set; }
}