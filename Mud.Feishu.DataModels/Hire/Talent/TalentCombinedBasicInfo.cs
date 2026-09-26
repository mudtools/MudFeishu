// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才基本信息（创建/更新人才请求子对象）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TalentCombinedBasicInfo
{
    /// <summary>
    /// <para>姓名（创建人才时必填，更新人才时非必填）</para>
    /// <para>必填：是（创建时）/ 否（更新时）</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>手机号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// <para>手机号区号</para>
    /// <para>必填：否</para>
    /// <para>示例值：86</para>
    /// </summary>
    [JsonPropertyName("mobile_country_code")]
    public string? MobileCountryCode { get; set; }

    /// <summary>
    /// <para>邮箱</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>证件类型：1 中国大陆居民身份证 / 2 护照 / 3 港澳居民来往内地通行证 / 4 台湾居民来往大陆通行证 / 5 其他 / 6 港澳台居民居住证（大陆签发）/ 9 台湾居民居住证</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("identification_type")]
    public int? IdentificationType { get; set; }

    /// <summary>
    /// <para>证件号码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("identification_number")]
    public string? IdentificationNumber { get; set; }

    /// <summary>
    /// <para>证件信息对象</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("identification")]
    public TalentIdentificationInfo? Identification { get; set; }

    /// <summary>
    /// <para>参加工作时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：4136534400000</para>
    /// </summary>
    [JsonPropertyName("start_work_time")]
    public string? StartWorkTime { get; set; }

    /// <summary>
    /// <para>生日，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：4136534400000</para>
    /// </summary>
    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }

    /// <summary>
    /// <para>性别：1 男 / 2 女 / 3 其他</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// <para>国籍编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("nationality_id")]
    public string? NationalityId { get; set; }

    /// <summary>
    /// <para>现居地编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("current_city_code")]
    public string? CurrentCityCode { get; set; }

    /// <summary>
    /// <para>籍贯编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hometown_city_code")]
    public string? HometownCityCode { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data")]
    public TalentCustomizedDataObjectValue[]? CustomizedData { get; set; }
}
