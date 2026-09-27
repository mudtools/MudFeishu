// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效结果（v1）中的环节填写内容
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewDataDetail
{
    /// <summary>
    /// <para>评估模板 ID</para>
    /// <para>示例值：6982759008789366313</para>
    /// </summary>
    [JsonPropertyName("template_id")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// <para>评估内容 ID</para>
    /// <para>示例值：6982759009396508196</para>
    /// </summary>
    [JsonPropertyName("unit_id")]
    public string? UnitId { get; set; }

    /// <summary>
    /// <para>评估字段 ID</para>
    /// <para>示例值：6982759006887888417</para>
    /// </summary>
    [JsonPropertyName("field_id")]
    public string? FieldId { get; set; }

    /// <summary>
    /// <para>评估人 ID（open_id / user_id 对象）</para>
    /// </summary>
    [JsonPropertyName("reviewer_user_id")]
    public UserIdInfo? ReviewerUserId { get; set; }

    /// <summary>
    /// <para>最后提交时间，毫秒时间戳</para>
    /// <para>示例值：1627977100000</para>
    /// </summary>
    [JsonPropertyName("submit_time")]
    public string? SubmitTime { get; set; }

    /// <summary>
    /// <para>评估项 ID；当 option_id 或 score 有值时有值</para>
    /// <para>示例值：6966127279593784876</para>
    /// </summary>
    [JsonPropertyName("indicator_id")]
    public string? IndicatorId { get; set; }

    /// <summary>
    /// <para>评估项结果等级 ID；当前评估项是评级型评估项数据时有值</para>
    /// <para>示例值：6966127279593653804</para>
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// <para>评分型评估项填写内容；当前评估项是评分型评估项数据时有值</para>
    /// </summary>
    [JsonPropertyName("score")]
    public string? Score { get; set; }

    /// <summary>
    /// <para>填写项填写内容；当前评估项是填写项数据时有值</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// <para>绩效系数值</para>
    /// <para>示例值：4.00</para>
    /// </summary>
    [JsonPropertyName("perf_coefficient_result")]
    public string? PerfCoefficientResult { get; set; }

    /// <summary>
    /// <para>富文本格式的填写内容</para>
    /// </summary>
    [JsonPropertyName("richtext")]
    public string? Richtext { get; set; }
}
