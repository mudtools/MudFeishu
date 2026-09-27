// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效详情数据（v2）中的环节信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewStage
{
    /// <summary>
    /// <para>环节 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("stage_id")]
    public string? StageId { get; set; }

    /// <summary>
    /// <para>环节类型：summarize_key_outputs（工作总结环节）/ review（评估型环节）/ communication_and_open_result（结果沟通环节）/ view_result（绩效结果查看环节）/ reconsideration（结果复议环节）/ leader_review（终评环节，特指最终的绩效结果数据）</para>
    /// <para>示例值：review</para>
    /// </summary>
    [JsonPropertyName("stage_type")]
    public string? StageType { get; set; }

    /// <summary>
    /// <para>评估型环节的执行人角色列表</para>
    /// </summary>
    [JsonPropertyName("review_stage_roles")]
    public string[]? ReviewStageRoles { get; set; }

    /// <summary>
    /// <para>该环节对应的环节模板的 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("template_id")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// <para>评估内容记录；多人评估的环节有多份记录，比如 360 评估环节。如果开启了 360 匿名评估且是对全部查看者匿名，评估记录数低于匿名下限则不返回 360 评估记录</para>
    /// </summary>
    [JsonPropertyName("records")]
    public PerformanceReviewRecord[]? Records { get; set; }

    /// <summary>
    /// <para>评估型环节的执行人角色：reviewee（被评估人）/ invited_reviewer（360°评估人）/ solid_line_leader（实线上级）/ dotted_line_leader（虚线上级）/ secondary_solid_line_leader（第二实线上级）/ direct_project_leader（项目直属上级）/ custom_review_role（自定义评估角色）/ metric_reviewer（指标评价人角色）</para>
    /// <para>示例值：reviewee</para>
    /// </summary>
    [JsonPropertyName("review_stage_role")]
    public string? ReviewStageRole { get; set; }
}
