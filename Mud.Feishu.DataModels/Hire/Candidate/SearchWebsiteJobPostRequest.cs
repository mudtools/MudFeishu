// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 搜索官网职位列表请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class SearchWebsiteJobPostRequest
{
    /// <summary>
    /// <para>职位类别 ID 列表，最大 100 个，可通过获取职位类别列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_type_id_list")]
    public string[]? JobTypeIdList { get; set; }

    /// <summary>
    /// <para>职位城市列表，最大 100 个</para>
    /// <para>必填：否</para>
    /// <para>示例值：["CN_1"]</para>
    /// </summary>
    [JsonPropertyName("city_code_list")]
    public string[]? CityCodeList { get; set; }

    /// <summary>
    /// <para>职能分类列表，最大 100 个</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_function_id_list")]
    public string[]? JobFunctionIdList { get; set; }

    /// <summary>
    /// <para>职位科目列表，最大 100 个</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subject_id_list")]
    public string[]? SubjectIdList { get; set; }

    /// <summary>
    /// <para>关键词</para>
    /// <para>必填：否</para>
    /// <para>示例值：HR</para>
    /// </summary>
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    /// <summary>
    /// <para>最早更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278663</para>
    /// </summary>
    [JsonPropertyName("update_start_time")]
    public string? UpdateStartTime { get; set; }

    /// <summary>
    /// <para>最晚更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278663</para>
    /// </summary>
    [JsonPropertyName("update_end_time")]
    public string? UpdateEndTime { get; set; }

    /// <summary>
    /// <para>最早创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278663</para>
    /// </summary>
    [JsonPropertyName("create_start_time")]
    public string? CreateStartTime { get; set; }

    /// <summary>
    /// <para>最晚创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278663</para>
    /// </summary>
    [JsonPropertyName("create_end_time")]
    public string? CreateEndTime { get; set; }
}
