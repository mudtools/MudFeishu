// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Performance;

/// <summary>
/// 绩效环节变更信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceStageChange
{
    /// <summary>
    /// <para>发生变更的环节 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("stage_id")]
    public string? StageId { get; set; }

    /// <summary>
    /// <para>环节类型。枚举值有：</para>
    /// <para>- summarize_key_outputs：工作总结环节</para>
    /// <para>- review：评估环节</para>
    /// <para>- communication_and_open_result：结果沟通环节</para>
    /// <para>- view_result：绩效结果查看环节</para>
    /// <para>- reconsideration：结果复议环节</para>
    /// <para>- leader_review：最终评估流程（特指最终绩效结果数据）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("stage_type")]
    public string? StageType { get; set; }

    /// <summary>
    /// <para>评估流程中执行人的角色。枚举值有：</para>
    /// <para>- reviewee：被评估人</para>
    /// <para>- invited_reviewer：360° 评估人</para>
    /// <para>- solid_line_leader：实线经理</para>
    /// <para>- dotted_line_leader：虚线经理</para>
    /// <para>- secondary_solid_line_leader：第二实线经理</para>
    /// <para>- direct_project_leader：项目直接负责人</para>
    /// <para>- custom_review_role：自定义评估角色</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("review_stage_role")]
    public string? ReviewStageRole { get; set; }
}
