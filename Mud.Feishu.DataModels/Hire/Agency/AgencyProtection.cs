// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 猎头保护期信息（查询猎头保护期响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class AgencyProtection
{
    /// <summary>
    /// <para>保护期类型：1 人才保护期 / 2 职位保护期</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("protection_type")]
    public int? ProtectionType { get; set; }

    /// <summary>
    /// <para>保护期类型为职位保护期（2）时，返回职位保护所在的投递 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6930815272790114323</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>保护期开始时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1700023694629</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>保护期过期时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1700023694630</para>
    /// </summary>
    [JsonPropertyName("expire_time")]
    public string? ExpireTime { get; set; }

    /// <summary>
    /// <para>推荐的猎头供应商 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6930815272790114324</para>
    /// </summary>
    [JsonPropertyName("agency_supplier_id")]
    public string? AgencySupplierId { get; set; }

    /// <summary>
    /// <para>推荐的猎头供应商名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agency_supplier_name")]
    public I18nName? AgencySupplierName { get; set; }

    /// <summary>
    /// <para>推荐的猎头顾问 ID，与入参 user_id_type 类型一致；仅作唯一标识，猎头顾问属于猎头供应商租户，需切换租户后才能查询其详情</para>
    /// <para>必填：否</para>
    /// <para>示例值：6930815272790114324</para>
    /// </summary>
    [JsonPropertyName("agency_supplier_user_id")]
    public string? AgencySupplierUserId { get; set; }

    /// <summary>
    /// <para>推荐的猎头顾问名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agency_supplier_user_name")]
    public I18nName? AgencySupplierUserName { get; set; }
}
