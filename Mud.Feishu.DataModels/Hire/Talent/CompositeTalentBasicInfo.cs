// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才 v2 基础信息（获取人才 v2 响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CompositeTalentBasicInfo
{
    /// <summary>
    /// <para>姓名</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>手机号（v2 字段名，区别 v1 的 mobile）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_number")]
    public string? MobileNumber { get; set; }

    /// <summary>
    /// <para>手机号区号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_code")]
    public string? MobileCode { get; set; }

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
    /// <para>国籍编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("nationality_code")]
    public string? NationalityCode { get; set; }

    /// <summary>
    /// <para>性别：1 男 / 2 女 / 3 保密</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// <para>现居住地编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("current_location_code")]
    public string? CurrentLocationCode { get; set; }

    /// <summary>
    /// <para>籍贯地编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hometown_location_code")]
    public string? HometownLocationCode { get; set; }

    /// <summary>
    /// <para>期望工作地编码列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("preferred_location_code_list")]
    public string[]? PreferredLocationCodeList { get; set; }

    /// <summary>
    /// <para>家庭住址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("home_address")]
    public string? HomeAddress { get; set; }

    /// <summary>
    /// <para>证件类型：1 身份证 / 2 护照 / 3 港澳居民来往内地通行证 / 4 台湾居民来往大陆通行证 / 5 其他 / 6 港澳台居民居住证 / 9 台湾居民居住证</para>
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
    /// <para>生日（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("birthday")]
    public long? Birthday { get; set; }

    /// <summary>
    /// <para>婚姻状况：1 已婚 / 2 未婚</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("marital_status")]
    public int? MaritalStatus { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public TalentCustomizedDataChild[]? CustomizedDataList { get; set; }

    /// <summary>
    /// <para>户口所在地编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hukou_location_code")]
    public string? HukouLocationCode { get; set; }

    /// <summary>
    /// <para>更新时间（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>创建时间（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>人才隐私设置：1 隐藏 / 2 公开</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("confidential")]
    public int? Confidential { get; set; }
}
