// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 取消候选人入职请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CancelOnboardRequest
{
    /// <summary>
    /// <para>终止类型：1 我们拒绝了候选人 / 22 候选人拒绝了我们 / 27 其他</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("termination_type")]
    public int? TerminationType { get; set; }

    /// <summary>
    /// <para>终止具体原因 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("termination_reason_id_list")]
    public string[]? TerminationReasonIdList { get; set; }

    /// <summary>
    /// <para>终止原因备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("termination_reason_notes")]
    public string? TerminationReasonNotes { get; set; }
}
