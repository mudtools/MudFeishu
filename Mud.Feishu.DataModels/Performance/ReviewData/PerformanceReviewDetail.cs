// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效详情数据（v2）中的评估题明细
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewDetail
{
    /// <summary>
    /// <para>评估题 ID，指评估内容中的每个评估项或填写项</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("field_id")]
    public string? FieldId { get; set; }

    /// <summary>
    /// <para>评估人 ID（open_id / user_id 对象）；如果开启了 360 匿名评估且是对全部查看者匿名，则不返回该值</para>
    /// </summary>
    [JsonPropertyName("reviewer_user_id")]
    public UserIdInfo? ReviewerUserId { get; set; }

    /// <summary>
    /// <para>该评估题的最后提交时间，毫秒级时间戳</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("submit_time")]
    public string? SubmitTime { get; set; }

    /// <summary>
    /// <para>评估项 ID（不包含子评估项）；option_id 或 score 有值的时候有值</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("indicator_id")]
    public string? IndicatorId { get; set; }

    /// <summary>
    /// <para>评估等级 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// <para>评分</para>
    /// <para>示例值：1.1</para>
    /// </summary>
    [JsonPropertyName("score")]
    public string? Score { get; set; }

    /// <summary>
    /// <para>填写项填写的文本</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// <para>标签填写题 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("tag_based_question_id")]
    public string? TagBasedQuestionId { get; set; }

    /// <summary>
    /// <para>标签填写项的内容</para>
    /// </summary>
    [JsonPropertyName("tag_text_item_data")]
    public PerformanceReviewTagText[]? TagTextItemData { get; set; }

    /// <summary>
    /// <para>绩效系数值</para>
    /// <para>示例值：1.1</para>
    /// </summary>
    [JsonPropertyName("perf_coefficient_value")]
    public string? PerfCoefficientValue { get; set; }

    /// <summary>
    /// <para>子评估项内容</para>
    /// </summary>
    [JsonPropertyName("sub_indicator_data")]
    public PerformanceReviewSubIndicator[]? SubIndicatorData { get; set; }

    /// <summary>
    /// <para>评估的目标数据，当评估内容是对目标（O）或关键举措（KR）评估时有值</para>
    /// </summary>
    [JsonPropertyName("objective_data")]
    public PerformanceReviewObjectiveData[]? ObjectiveData { get; set; }

    /// <summary>
    /// <para>评估的指标，当评估内容是对指标评估时有值</para>
    /// </summary>
    [JsonPropertyName("metric_data")]
    public PerformanceReviewMetricData[]? MetricData { get; set; }

    /// <summary>
    /// <para>终评环节填写内容的来源（仅终评环节的数据有值）：review（产生终评结果的评估型环节）/ calibaration（校准环节，飞书原始枚举值拼写如此）/ reconsideration（结果复议环节）</para>
    /// <para>示例值：review</para>
    /// </summary>
    [JsonPropertyName("leader_review_data_source")]
    public string? LeaderReviewDataSource { get; set; }

    /// <summary>
    /// <para>工作/总结类型的文本内容</para>
    /// </summary>
    [JsonPropertyName("multi_texts")]
    public string[]? MultiTexts { get; set; }

    /// <summary>
    /// <para>富文本格式的填写内容</para>
    /// </summary>
    [JsonPropertyName("richtext")]
    public string? Richtext { get; set; }

    /// <summary>
    /// <para>富文本格式的填写内容列表</para>
    /// </summary>
    [JsonPropertyName("multi_richtexts")]
    public string[]? MultiRichtexts { get; set; }

    /// <summary>
    /// <para>该评估题是否是首要评估项</para>
    /// </summary>
    [JsonPropertyName("is_principal_review_item")]
    public bool? IsPrincipalReviewItem { get; set; }
}
