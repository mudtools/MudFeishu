// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 设置猎头保护期请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ProtectAgencyRequest
{
    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6962051712422398239</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>猎头供应商 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6898173495386147079</para>
    /// </summary>
    [JsonPropertyName("supplier_id")]
    public string? SupplierId { get; set; }

    /// <summary>
    /// <para>猎头顾问 ID，需与 user_id_type 类型一致</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_f476cb099ac9227c9bae09ce46112579</para>
    /// </summary>
    [JsonPropertyName("consultant_id")]
    public string? ConsultantId { get; set; }

    /// <summary>
    /// <para>保护期创建时间，毫秒时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1610695587000</para>
    /// </summary>
    [JsonPropertyName("protect_create_time")]
    public long? ProtectCreateTime { get; set; }

    /// <summary>
    /// <para>保护期过期时间，毫秒时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1626333987000</para>
    /// </summary>
    [JsonPropertyName("protect_expire_time")]
    public long? ProtectExpireTime { get; set; }

    /// <summary>
    /// <para>推荐语</para>
    /// <para>必填：否</para>
    /// <para>示例值：此候选人非常优秀，建议录用。</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// <para>当前薪资</para>
    /// <para>必填：否</para>
    /// <para>示例值：15k * 13</para>
    /// </summary>
    [JsonPropertyName("current_salary")]
    public string? CurrentSalary { get; set; }

    /// <summary>
    /// <para>预期薪资</para>
    /// <para>必填：否</para>
    /// <para>示例值：18k * 16</para>
    /// </summary>
    [JsonPropertyName("expected_salary")]
    public string? ExpectedSalary { get; set; }
}
