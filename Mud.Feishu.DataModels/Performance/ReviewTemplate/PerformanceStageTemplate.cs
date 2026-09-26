// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效模板环节模板信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceStageTemplate
{
    /// <summary>
    /// <para>环节模板 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("template_id")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// <para>环节模板对应的环节名称</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>环节类型：summarize_key_outputs（工作总结环节）/ review（评估型环节）/ communication_and_open_result（结果沟通环节）/ view_result（绩效结果查看环节）/ calibration（校准环节）/ reconsideration（结果复议环节）</para>
    /// <para>示例值：review</para>
    /// </summary>
    [JsonPropertyName("stage_type")]
    public string? StageType { get; set; }

    /// <summary>
    /// <para>评估型环节的执行人角色：reviewee（被评估人）/ invited_reviewer（360°评估人）/ solid_line_leader（实线上级）/ dotted_line_leader（虚线上级）/ secondary_solid_line_leader（第二实线上级）/ direct_project_leader（项目直属上级）/ custom_review_role（自定义评估角色）</para>
    /// <para>示例值：reviewee</para>
    /// </summary>
    [JsonPropertyName("review_stage_role")]
    public string? ReviewStageRole { get; set; }

    /// <summary>
    /// <para>评估型环节评估模式</para>
    /// <para>示例值：cooperate,independent</para>
    /// </summary>
    [JsonPropertyName("review_stage_data_write_mode")]
    public string? ReviewStageDataWriteMode { get; set; }
}
