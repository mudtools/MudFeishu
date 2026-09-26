// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.MDM;

/// <summary>
/// 国家/地区（country_region）主数据。提供飞书内通用地理位置国家/地区层级数据，支持多语言并定期更新，主要包括行政编码、名称等。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "MDM")]
public class MdmCountryRegion
{
    /// <summary>
    /// <para>主数据编码（系统生成的唯一永久代码，格式为 "MDCT+8位数字"）</para>
    /// <para>示例值：MDCT00000001</para>
    /// </summary>
    [JsonPropertyName("mdm_code")]
    public string? MdmCode { get; set; }

    /// <summary>
    /// <para>国家/地区名称</para>
    /// </summary>
    [JsonPropertyName("name")]
    public MdmI18nString? Name { get; set; }

    /// <summary>
    /// <para>三位字母代码，与 ISO 国家代码的三位代码一致</para>
    /// <para>示例值：AND</para>
    /// </summary>
    [JsonPropertyName("alpha_3_code")]
    public string? Alpha3Code { get; set; }

    /// <summary>
    /// <para>两位字母代码，与 ISO 国家代码的二位代码一致</para>
    /// <para>示例值：AD</para>
    /// </summary>
    [JsonPropertyName("alpha_2_code")]
    public string? Alpha2Code { get; set; }

    /// <summary>
    /// <para>数字代码，与 ISO 国家代码的 Numeric 代码一致</para>
    /// <para>示例值：20</para>
    /// </summary>
    [JsonPropertyName("numeric_code")]
    public string? NumericCode { get; set; }

    /// <summary>
    /// <para>国家/地区全称</para>
    /// </summary>
    [JsonPropertyName("full_name")]
    public MdmI18nString? FullName { get; set; }

    /// <summary>
    /// <para>国际电话区号</para>
    /// <para>示例值：+376</para>
    /// </summary>
    [JsonPropertyName("global_code")]
    public string? GlobalCode { get; set; }

    /// <summary>
    /// <para>所属大洲：1（亚洲）/ 2（欧洲）/ 3（非洲）/ 4（北美洲）/ 5（南美洲）/ 6（大洋洲）/ 7（南极洲）</para>
    /// </summary>
    [JsonPropertyName("continents")]
    public MdmEnumValue? Continents { get; set; }

    /// <summary>
    /// <para>状态：0（失效）/ 1（生效）</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
