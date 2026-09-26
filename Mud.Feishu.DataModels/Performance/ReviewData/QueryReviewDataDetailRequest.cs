// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 获取绩效详情数据（v2）请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class QueryReviewDataDetailRequest
{
    /// <summary>
    /// <para>评估周期 ID 列表，可通过获取周期接口获得（0~10 个）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["6992035450862224940"]</para>
    /// </summary>
    [JsonPropertyName("semester_ids")]
    public string[]? SemesterIds { get; set; }

    /// <summary>
    /// <para>被评估人 ID 列表，ID 类型与 user_id_type 的取值一致（0~10 个）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["ou_3245842393d09e9428ad4655da6e30b3"]</para>
    /// </summary>
    [JsonPropertyName("reviewee_user_ids")]
    public string[]? RevieweeUserIds { get; set; }

    /// <summary>
    /// <para>环节类型；stage_types 和 stage_ids 至少要传一个，不传默认不返回任何环节评估数据；如果同时传了环节 ID 和环节类型，优先返回环节 ID 对应的绩效数据。可选值：summarize_key_outputs（工作总结环节）/ review（评估型环节）/ communication_and_open_result（结果沟通环节）/ view_result（绩效结果查看环节）/ reconsideration（结果复议环节）/ leader_review（终评环节，特指最终的绩效结果数据）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["leader_review"]</para>
    /// </summary>
    [JsonPropertyName("stage_types")]
    public string[]? StageTypes { get; set; }

    /// <summary>
    /// <para>评估型环节的执行人角色；当传入的环节类型中有评估型环节时才生效，不传默认包含所有的执行人角色。可选值：reviewee（被评估人）/ invited_reviewer（360°评估人）/ solid_line_leader（实线上级）/ dotted_line_leader（虚线上级）/ secondary_solid_line_leader（第二实线上级）/ direct_project_leader（合作项目中的直属上级）/ custom_review_role（自定义评估角色）/ metric_reviewer（指标评价人）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["reviewee"]</para>
    /// </summary>
    [JsonPropertyName("review_stage_roles")]
    public string[]? ReviewStageRoles { get; set; }

    /// <summary>
    /// <para>环节 ID（0~50 个），可在绩效结果开通 / 绩效详情变更事件中获得；stage_types 和 stage_ids 至少要传一个，不传默认不返回任何环节评估数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7343513161666707459"]</para>
    /// </summary>
    [JsonPropertyName("stage_ids")]
    public string[]? StageIds { get; set; }

    /// <summary>
    /// <para>当要获取的绩效数据的环节类型包含终评环节时，可指定是否需要返回绩效终评数据的具体环节来源；不传则默认不返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("need_leader_review_data_source")]
    public bool? NeedLeaderReviewDataSource { get; set; }

    /// <summary>
    /// <para>可筛选出在此时间之后有内容提交的环节数据，毫秒级时间戳；不传默认返回所有时间提交的环节数据，包括未提交的环节数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：1630425599999</para>
    /// </summary>
    [JsonPropertyName("updated_later_than")]
    public string? UpdatedLaterThan { get; set; }

    /// <summary>
    /// <para>环节状态，不传默认包含所有状态（0~50 个）。查看绩效结果环节：0（已开通）/ 1（待确认）/ 2（已截止）/ 3（已确认）/ 4（已复议）；绩效结果复议环节：1（待完成）/ 2（已截止）/ 3（已完成）；其他环节类型：0（未开始）/ 1（待完成）/ 2（已截止）/ 3（已完成）</para>
    /// <para>必填：否</para>
    /// <para>示例值：[1]</para>
    /// </summary>
    [JsonPropertyName("stage_progresses")]
    public int[]? StageProgresses { get; set; }
}
