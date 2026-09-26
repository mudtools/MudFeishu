// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效详情数据（v2）中合作项目中上级的评估记录信息，仅在「项目直属上级环节」有值
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceDirectProjectLeaderRecordInfo
{
    /// <summary>
    /// <para>评估人 ID（open_id / user_id 对象）</para>
    /// </summary>
    [JsonPropertyName("reviewer_id")]
    public UserIdInfo? ReviewerId { get; set; }

    /// <summary>
    /// <para>评估人作为直属项目上级所在的项目</para>
    /// </summary>
    [JsonPropertyName("cooperation_projects")]
    public PerformanceCooperationProject[]? CooperationProjects { get; set; }

    /// <summary>
    /// <para>评估依据的项目</para>
    /// </summary>
    [JsonPropertyName("review_depend_projects")]
    public PerformanceCooperationProject[]? ReviewDependProjects { get; set; }

    /// <summary>
    /// <para>共同参与的项目</para>
    /// </summary>
    [JsonPropertyName("participated_projects")]
    public PerformanceCooperationProject[]? ParticipatedProjects { get; set; }
}
