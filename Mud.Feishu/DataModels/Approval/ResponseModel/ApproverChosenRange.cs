// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>提交人自选审批人的范围</para>
/// </summary>
public class ApproverChosenRange
{
    /// <summary>
    /// <para>指定范围</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：全公司范围</item>
    /// <item>1：指定角色范围</item>
    /// <item>2：指定用户范围</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("approver_range_type")]
    public int? ApproverRangeType { get; set; }

    /// <summary>
    /// <para>资源 ID。</para>
    /// <para>- approver_range_type 取值为 0 时，该参数为空。</para>
    /// <para>- approver_range_type 取值为 1 时，该参数取值为角色 ID。</para>
    /// <para>- approver_range_type 取值为 2 时，该参数取值为用户 open_id。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approver_range_ids")]
    public string[]? ApproverRangeIds { get; set; }
}