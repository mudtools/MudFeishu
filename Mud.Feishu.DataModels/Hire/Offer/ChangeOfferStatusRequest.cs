// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 更新 Offer 状态请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ChangeOfferStatusRequest
{
    /// <summary>
    /// <para>Offer 状态，本接口可选值：2 审批中，3 审批已撤回，4 审批通过，5 审批不通过，6 已发送，7 被候选人接受，8 被候选人拒绝，9 已失效，10 已创建</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("offer_status")]
    public int? OfferStatus { get; set; }

    /// <summary>
    /// <para>过期时间，格式 YYYY-MM-DD；offer_status = 6 时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("expiration_date")]
    public string? ExpirationDate { get; set; }

    /// <summary>
    /// <para>终止原因 ID 列表，最多 50 个；offer_status = 8 时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("termination_reason_id_list")]
    public string[]? TerminationReasonIdList { get; set; }

    /// <summary>
    /// <para>终止备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("termination_reason_note")]
    public string? TerminationReasonNote { get; set; }

    /// <summary>
    /// <para>撤销终止类型：1 我们拒绝了候选人，22 候选人拒绝了我们，27 其他（文档未列出，Go SDK 有此字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cancel_offer_termination_type")]
    public int? CancelOfferTerminationType { get; set; }
}
