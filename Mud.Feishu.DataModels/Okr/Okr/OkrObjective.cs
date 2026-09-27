// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Okr;

/// <summary>
/// OKR 目标（Objective）
/// </summary>
public class OkrObjective
{
    /// <summary>
    /// <para>目标 id</para>
    /// <para>示例值：7073360513731690515</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>访问权限：0 无权限、1 有权限</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("permission")]
    public int? Permission { get; set; }

    /// <summary>
    /// <para>目标内容</para>
    /// <para>示例值：需求@刘三</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>目标进度记录内容</para>
    /// <para>示例值：红豆泥</para>
    /// </summary>
    [JsonPropertyName("progress_report")]
    public string? ProgressReport { get; set; }

    /// <summary>
    /// <para>目标得分（0 - 100）</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    /// <summary>
    /// <para>目标权重（0 - 100）</para>
    /// <para>示例值：40</para>
    /// </summary>
    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    /// <summary>
    /// <para>目标进度</para>
    /// </summary>
    [JsonPropertyName("progress_rate")]
    public OkrProgressRate? ProgressRate { get; set; }

    /// <summary>
    /// <para>目标的关键结果（KR）列表</para>
    /// </summary>
    [JsonPropertyName("kr_list")]
    public OkrObjectiveKr[]? KrList { get; set; }

    /// <summary>
    /// <para>与该目标对齐的目标列表</para>
    /// </summary>
    [JsonPropertyName("aligned_objective_list")]
    public OkrAlignedObjective[]? AlignedObjectiveList { get; set; }

    /// <summary>
    /// <para>该目标对其对齐的目标列表</para>
    /// </summary>
    [JsonPropertyName("aligning_objective_list")]
    public OkrAlignedObjective[]? AligningObjectiveList { get; set; }

    /// <summary>
    /// <para>该目标的进展记录列表</para>
    /// </summary>
    [JsonPropertyName("progress_record_list")]
    public OkrProgressRecordSimplify[]? ProgressRecordList { get; set; }

    /// <summary>
    /// <para>最近一次更新进度百分比的时间（毫秒时间戳字符串）</para>
    /// <para>示例值：1646907261326</para>
    /// </summary>
    [JsonPropertyName("progress_rate_percent_last_updated_time")]
    public string? ProgressRatePercentLastUpdatedTime { get; set; }

    /// <summary>
    /// <para>最近一次更新进度状态的时间（毫秒时间戳字符串）</para>
    /// <para>示例值：1646907261326</para>
    /// </summary>
    [JsonPropertyName("progress_rate_status_last_updated_time")]
    public string? ProgressRateStatusLastUpdatedTime { get; set; }

    /// <summary>
    /// <para>最近一次侧边栏新增或编辑进展的时间（毫秒时间戳字符串）</para>
    /// <para>示例值：1646907590448</para>
    /// </summary>
    [JsonPropertyName("progress_record_last_updated_time")]
    public string? ProgressRecordLastUpdatedTime { get; set; }

    /// <summary>
    /// <para>最近一次编辑进展/备注的时间（毫秒时间戳字符串）</para>
    /// <para>示例值：1646907387911</para>
    /// </summary>
    [JsonPropertyName("progress_report_last_updated_time")]
    public string? ProgressReportLastUpdatedTime { get; set; }

    /// <summary>
    /// <para>最近一次更新得分的时间（毫秒时间戳字符串）</para>
    /// <para>示例值：1646907590472</para>
    /// </summary>
    [JsonPropertyName("score_last_updated_time")]
    public string? ScoreLastUpdatedTime { get; set; }

    /// <summary>
    /// <para>截止时间（毫秒时间戳字符串）</para>
    /// <para>示例值：1648656000000</para>
    /// </summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }

    /// <summary>
    /// <para>目标中被 @ 的用户列表</para>
    /// </summary>
    [JsonPropertyName("mentioned_user_list")]
    public UserIdInfo[]? MentionedUserList { get; set; }
}
