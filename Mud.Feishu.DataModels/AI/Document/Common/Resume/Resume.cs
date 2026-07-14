// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>简历信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class Resume
{
    /// <summary>
    /// <para>文件标识ID，依据文件内容自动生成</para>
    /// <para>必填：否</para>
    /// <para>示例值：825c59042dxxxxx3ff90b45xxxxx88</para>
    /// </summary>
    [JsonPropertyName("file_md5")]
    public string? FileMd5 { get; set; }

    /// <summary>
    /// <para>文本内容，当接口返回成功时，该字段才存在</para>
    /// <para>必填：否</para>
    /// <para>示例值：XX负责行政人事管理和日常事务...</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>经过排序后的文本内容，当接口返回成功时，该字段才存在</para>
    /// <para>必填：否</para>
    /// <para>示例值：XX负责行政人事管理和日常事务...</para>
    /// </summary>
    [JsonPropertyName("new_content")]
    public string? NewContent { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：zhangsan.1111@company.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>手机号码</para>
    /// <para>必填：否</para>
    /// <para>示例值：13600000000</para>
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// <para>手机号码是否虚拟号码</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("mobile_is_virtual")]
    public bool? MobileIsVirtual { get; set; }

    /// <summary>
    /// <para>手机号码国家编码</para>
    /// <para>必填：否</para>
    /// <para>示例值：86</para>
    /// </summary>
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    /// <summary>
    /// <para>教育经历</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("educations")]
    public ResumeEducation[]? Educations { get; set; }


    /// <summary>
    /// <para>职业经历</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("careers")]
    public ResumeCareer[]? Careers { get; set; }


    /// <summary>
    /// <para>项目经历</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("projects")]
    public ResumeProject[]? Projects { get; set; }


    /// <summary>
    /// <para>工作年限，为空表示工作年限未知，数字单位为年，整数</para>
    /// <para>必填：否</para>
    /// <para>示例值：5</para>
    /// <para>最大值：100</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("work_year")]
    public int? WorkYear { get; set; }

    /// <summary>
    /// <para>生日，格式YYYY-MM-DD</para>
    /// <para>必填：否</para>
    /// <para>示例值：1995-01-01</para>
    /// </summary>
    [JsonPropertyName("date_of_birth")]
    public string? DateOfBirth { get; set; }

    /// <summary>
    /// <para>性别</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：未知</item>
    /// <item>1：男性</item>
    /// <item>2：女性</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// <para>希望获得的职位列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxx岗位</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("willing_positions")]
    public string[]? WillingPositions { get; set; }

    /// <summary>
    /// <para>当前工作地点(城市)</para>
    /// <para>必填：否</para>
    /// <para>示例值：上海</para>
    /// </summary>
    [JsonPropertyName("current_location")]
    public string? CurrentLocation { get; set; }

    /// <summary>
    /// <para>希望工作地点列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("willing_locations")]
    public string[]? WillingLocations { get; set; }

    /// <summary>
    /// <para>家乡(城市)</para>
    /// <para>必填：否</para>
    /// <para>示例值：上海</para>
    /// </summary>
    [JsonPropertyName("home_location")]
    public string? HomeLocation { get; set; }

    /// <summary>
    /// <para>语言</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("languages")]
    public ResumeLanguage[]? Languages { get; set; }


    /// <summary>
    /// <para>获奖</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("awards")]
    public ResumeAward[]? Awards { get; set; }


    /// <summary>
    /// <para>证书</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("certificates")]
    public ResumeCertificate[]? Certificates { get; set; }


    /// <summary>
    /// <para>竞赛</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("competitions")]
    public ResumeCompetition[]? Competitions { get; set; }

    /// <summary>
    /// <para>自我评价</para>
    /// <para>必填：否</para>
    /// <para>示例值：我是一个...</para>
    /// </summary>
    [JsonPropertyName("self_evaluation")]
    public string? SelfEvaluation { get; set; }

    /// <summary>
    /// <para>链接列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("urls")]
    public string[]? Urls { get; set; }

    /// <summary>
    /// <para>社交链接</para>
    /// <para>必填：否</para>
    /// <para>最大长度：99</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("social_links")]
    public string[]? SocialLinks { get; set; }
}
