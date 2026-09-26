// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 试卷
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class EcoExamPaperData
{
    /// <summary>
    /// <para>试卷 ID（由调用方自定义，即创建试卷列表时传入的 ID）</para>
    /// <para>必填：是</para>
    /// <para>示例值：7147998241542539527</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>试卷名称；长度不应超过 255 字符，超出部分将被截断</para>
    /// <para>必填：是</para>
    /// <para>示例值：春季测评</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>笔试时长（分钟）</para>
    /// <para>必填：否</para>
    /// <para>示例值：30</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public int? Duration { get; set; }

    /// <summary>
    /// <para>试卷题目数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：30</para>
    /// </summary>
    [JsonPropertyName("question_count")]
    public int? QuestionCount { get; set; }

    /// <summary>
    /// <para>笔试开始时间，毫秒时间戳。留空或不传表示不限制开始时间；若同时传入 end_time，开始时间必须小于结束时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1658676234053</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>笔试结束时间，毫秒时间戳。留空或不传表示不限制结束时间；若同时传入 start_time，结束时间必须大于开始时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1672444800000</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}
