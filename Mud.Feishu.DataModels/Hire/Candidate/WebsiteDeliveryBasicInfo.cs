// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 官网投递基本信息（简历信息子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class WebsiteDeliveryBasicInfo
{
    /// <summary>
    /// <para>国籍，可通过查询地点列表接口获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：CN_112</para>
    /// </summary>
    [JsonPropertyName("nationality_id")]
    public string? NationalityId { get; set; }

    /// <summary>
    /// <para>开始工作时间，秒级时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1625648596</para>
    /// </summary>
    [JsonPropertyName("start_work_time")]
    public long? StartWorkTime { get; set; }

    /// <summary>
    /// <para>家庭住址</para>
    /// <para>必填：否</para>
    /// <para>示例值：成都</para>
    /// </summary>
    [JsonPropertyName("current_home_address")]
    public string? CurrentHomeAddress { get; set; }

    /// <summary>
    /// <para>家乡城市编码，可通过查询地点列表接口获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：CT_159</para>
    /// </summary>
    [JsonPropertyName("hometown_city_code")]
    public string? HometownCityCode { get; set; }

    /// <summary>
    /// <para>手机国家码，可通过查询地点列表接口获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：CN_1</para>
    /// </summary>
    [JsonPropertyName("mobile_country_code")]
    public string? MobileCountryCode { get; set; }

    /// <summary>
    /// <para>身份证件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("identification")]
    public WebsiteDeliveryIdentification? Identification { get; set; }

    /// <summary>
    /// <para>婚姻状况：1 已婚 / 2 未婚</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("marital_status")]
    public int? MaritalStatus { get; set; }

    /// <summary>
    /// <para>电话</para>
    /// <para>必填：否</para>
    /// <para>示例值：182900291190</para>
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// <para>所在城市编码，可通过查询地点列表接口获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：CT_11</para>
    /// </summary>
    [JsonPropertyName("current_city_code")]
    public string? CurrentCityCode { get; set; }

    /// <summary>
    /// <para>工作年限</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("experience_years")]
    public int? ExperienceYears { get; set; }

    /// <summary>
    /// <para>性别：1 男 / 2 女 / 3 其他</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// <para>出生日期，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609430400</para>
    /// </summary>
    [JsonPropertyName("birthday")]
    public long? Birthday { get; set; }

    /// <summary>
    /// <para>姓名</para>
    /// <para>必填：是</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>意向城市列表，可通过查询地点列表接口获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：["CT_1"]</para>
    /// </summary>
    [JsonPropertyName("preferred_city_code_list")]
    public string[]? PreferredCityCodeList { get; set; }

    /// <summary>
    /// <para>简历来源，可通过获取简历来源列表接口获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：6982104077248219436</para>
    /// </summary>
    [JsonPropertyName("resume_source_id")]
    public string? ResumeSourceId { get; set; }

    /// <summary>
    /// <para>年龄</para>
    /// <para>必填：否</para>
    /// <para>示例值：25</para>
    /// </summary>
    [JsonPropertyName("age")]
    public int? Age { get; set; }

    /// <summary>
    /// <para>自定义字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data")]
    public WebsiteDeliveryCustomizedData[]? CustomizedData { get; set; }

    /// <summary>
    /// <para>邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：foo@bytedance.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
