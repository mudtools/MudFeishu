// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才基本信息（获取人才 v1 详情/列表响应 basic_info 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TalentBasicInfo
{
    /// <summary>
    /// <para>姓名</para>
    /// <para>必填：否</para>
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
    /// <para>手机区号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_code")]
    public string? MobileCode { get; set; }

    /// <summary>
    /// <para>手机国家码</para>
    /// <para>必填：否</para>
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
    /// <para>工作年限</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("experience_years")]
    public int? ExperienceYears { get; set; }

    /// <summary>
    /// <para>年龄</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    /// <summary>
    /// <para>民族</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("nationality")]
    public TalentNationality? Nationality { get; set; }

    /// <summary>
    /// <para>性别：1 男 / 2 女 / 3 其他</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// <para>现居城市</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("current_city")]
    public TalentCityInfo? CurrentCity { get; set; }

    /// <summary>
    /// <para>家乡城市</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hometown_city")]
    public TalentCityInfo? HometownCity { get; set; }

    /// <summary>
    /// <para>期望城市列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("preferred_city_list")]
    public TalentCityInfo[]? PreferredCityList { get; set; }

    /// <summary>
    /// <para>证件类型：1 中国大陆居民身份证 / 2 护照 / 3 港澳居民来往内地通行证 / 4 台湾居民来往大陆通行证 / 5 其他 / 6 港澳台居民居住证 / 9 台湾居民居住证</para>
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
    /// <para>证件信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("identification")]
    public TalentIdentificationInfo? Identification { get; set; }

    /// <summary>
    /// <para>生日，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("birthday")]
    public long? Birthday { get; set; }

    /// <summary>
    /// <para>创建人 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    /// <summary>
    /// <para>婚姻状况：1 已婚 / 2 未婚</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("marital_status")]
    public int? MaritalStatus { get; set; }

    /// <summary>
    /// <para>现居住地</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("current_home_address")]
    public string? CurrentHomeAddress { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public TalentCustomizedDataChild[]? CustomizedDataList { get; set; }

    /// <summary>
    /// <para>最后更新时间，毫秒时间戳字符串</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("modify_time")]
    public string? ModifyTime { get; set; }

    /// <summary>
    /// <para>户口所在地编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hukou_location_code")]
    public string? HukouLocationCode { get; set; }
}
