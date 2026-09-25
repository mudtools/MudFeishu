// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 更新 Offer 响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class UpdateOfferResult
{
    /// <summary>
    /// <para>Offer ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_id")]
    public string? OfferId { get; set; }

    /// <summary>
    /// <para>Offer 申请表模板 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("schema_id")]
    public string? SchemaId { get; set; }

    /// <summary>
    /// <para>Offer 基本信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("basic_info")]
    public OfferBasicInfo? BasicInfo { get; set; }

    /// <summary>
    /// <para>薪资信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("salary_info")]
    public OfferSalaryInfo? SalaryInfo { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_info_list")]
    public OfferCustomizedInfo[]? CustomizedInfoList { get; set; }
}
