// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效结果（v1）中的环节信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewDataStage
{
    /// <summary>
    /// <para>环节类型：leader_review（终评环节）/ communication_and_open_result（结果沟通环节）/ view_result（查看绩效结果环节）等</para>
    /// <para>示例值：leader_review</para>
    /// </summary>
    [JsonPropertyName("stage_type")]
    public string? StageType { get; set; }

    /// <summary>
    /// <para>环节状态：0（未开始）/ 1（待完成）/ 2（已截止）/ 3（已完成）/ 4（已复议）</para>
    /// </summary>
    [JsonPropertyName("progress")]
    public int? Progress { get; set; }

    /// <summary>
    /// <para>环节填写内容</para>
    /// </summary>
    [JsonPropertyName("data")]
    public PerformanceReviewDataDetail[]? Data { get; set; }
}
