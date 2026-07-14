// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceApprovals;

/// <summary>
/// 通知审批状态更新请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class UpdateApprovalInfosRequest
{
    /// <summary>
    /// <para>审批实例 ID，获取方式：1）</para>
    /// <para>必填：是</para>
    /// <para>示例值：6737202939523236113</para>
    /// </summary>
    [JsonPropertyName("approval_id")]
    public string ApprovalId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批类型</para>
    /// <para>- `leave`：请假</para>
    /// <para>- `out`：外出</para>
    /// <para>- `overtime`：加班</para>
    /// <para>- `trip`：出差</para>
    /// <para>- `remedy`：补卡</para>
    /// <para>必填：是</para>
    /// <para>示例值：remedy</para>
    /// </summary>
    [JsonPropertyName("approval_type")]
    public string ApprovalType { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批状态</para>
    /// <para>- `1`：不通过</para>
    /// <para>- `2`：通过</para>
    /// <para>- `4`：撤销</para>
    /// <para>**注意**</para>
    /// <para>- **请假、外出、加班、出差**只支持传**撤销**</para>
    /// <para>- **补卡**支持传**不通过、通过和撤销**</para>
    /// <para>必填：是</para>
    /// <para>示例值：4</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }
}
