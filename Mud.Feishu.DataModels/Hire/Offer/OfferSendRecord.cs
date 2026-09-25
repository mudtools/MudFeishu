// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// Offer 发送记录
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class OfferSendRecord
{
    /// <summary>
    /// <para>Offer 发送记录 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_send_record_id")]
    public string? OfferSendRecordId { get; set; }

    /// <summary>
    /// <para>操作人用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_user_id")]
    public string? OperatorUserId { get; set; }

    /// <summary>
    /// <para>发送时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("send_time")]
    public string? SendTime { get; set; }

    /// <summary>
    /// <para>Offer 信函状态：1 已创建，2 已接受，3 已拒绝，4 已过期，5 已作废</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_letter_status")]
    public int? OfferLetterStatus { get; set; }

    /// <summary>
    /// <para>邮件信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("email_info")]
    public OfferEmailInfo? EmailInfo { get; set; }

    /// <summary>
    /// <para>接受信息列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("acceptance_list")]
    public OfferAcceptance[]? AcceptanceList { get; set; }

    /// <summary>
    /// <para>Offer 文件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_file_list")]
    public OfferFile[]? OfferFileList { get; set; }

    /// <summary>
    /// <para>签署信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_signature_info")]
    public OfferSignatureInfo? OfferSignatureInfo { get; set; }
}
