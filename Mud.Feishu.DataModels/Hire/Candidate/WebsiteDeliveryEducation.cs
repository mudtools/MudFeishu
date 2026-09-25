// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 官网投递教育经历（简历信息子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class WebsiteDeliveryEducation
{
    /// <summary>
    /// <para>学历类型：1 中国大陆非全日制 / 2 全日制统招 / 3 成人教育 / 4 自考 / 5 其他</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("education_type")]
    public int? EducationType { get; set; }

    /// <summary>
    /// <para>结束时间，毫秒时间戳；「至今」传 -1（投递创建成功后可正常查看，编辑时需填具体时间）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278663</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public long? EndTime { get; set; }

    /// <summary>
    /// <para>结束时间（新），毫秒时间戳，不支持「至今」传值，建议使用本字段避免模糊毕业时间影响候选人筛选</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278663</para>
    /// </summary>
    [JsonPropertyName("end_time_v2")]
    public long? EndTimeV2 { get; set; }

    /// <summary>
    /// <para>专业</para>
    /// <para>必填：否</para>
    /// <para>示例值：汉语言文学</para>
    /// </summary>
    [JsonPropertyName("field_of_study")]
    public string? FieldOfStudy { get; set; }

    /// <summary>
    /// <para>学校</para>
    /// <para>必填：否</para>
    /// <para>示例值：香港中文大学</para>
    /// </summary>
    [JsonPropertyName("school")]
    public string? School { get; set; }

    /// <summary>
    /// <para>开始时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609430400</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public long? StartTime { get; set; }

    /// <summary>
    /// <para>专业排名：5 前 5% / 10 前 10% / 20 前 20% / 30 前 30% / 50 前 50% / 51 其他</para>
    /// <para>必填：否</para>
    /// <para>示例值：5</para>
    /// </summary>
    [JsonPropertyName("academic_ranking")]
    public int? AcademicRanking { get; set; }

    /// <summary>
    /// <para>自定义字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data")]
    public WebsiteDeliveryCustomizedData[]? CustomizedData { get; set; }

    /// <summary>
    /// <para>学位：1 小学 / 2 初中 / 3 中专 / 4 高中 / 5 大专 / 6 本科 / 7 硕士 / 8 博士 / 9 其他</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("degree")]
    public int? Degree { get; set; }
}
