// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 创建外部 Offer 请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CreateExternalOfferRequest
{
    /// <summary>
    /// <para>外部系统 Offer 主键（仅用于幂等）：不传则不进行幂等校验；传入后同一 external_id 24 小时内仅可创建一次</para>
    /// <para>必填：否</para>
    /// <para>示例值：123</para>
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// <para>外部投递 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：7395015673275697419</para>
    /// </summary>
    [JsonPropertyName("external_application_id")]
    public string? ExternalApplicationId { get; set; }

    /// <summary>
    /// <para>Offer 创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1721899352428</para>
    /// </summary>
    [JsonPropertyName("biz_create_time")]
    public string? BizCreateTime { get; set; }

    /// <summary>
    /// <para>Offer 负责人姓名</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// <para>Offer 状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：Sent</para>
    /// </summary>
    [JsonPropertyName("offer_status")]
    public string? OfferStatus { get; set; }

    /// <summary>
    /// <para>Offer 详情附件 ID 列表（0~20 个），可通过创建附件接口返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7404675264888097068"]</para>
    /// </summary>
    [JsonPropertyName("attachment_id_list")]
    public string[]? AttachmentIdList { get; set; }
}
