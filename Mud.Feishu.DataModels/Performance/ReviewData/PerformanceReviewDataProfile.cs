// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效结果（v1）中被评估人的评估数据
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewDataProfile
{
    /// <summary>
    /// <para>被评估人 ID（open_id / user_id 对象）</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public UserIdInfo? UserId { get; set; }

    /// <summary>
    /// <para>周期 ID</para>
    /// <para>示例值：6992035450862224940</para>
    /// </summary>
    [JsonPropertyName("semester_id")]
    public string? SemesterId { get; set; }

    /// <summary>
    /// <para>项目 ID</para>
    /// <para>示例值：6992035450862323244</para>
    /// </summary>
    [JsonPropertyName("activity_id")]
    public string? ActivityId { get; set; }

    /// <summary>
    /// <para>环节信息</para>
    /// </summary>
    [JsonPropertyName("stages")]
    public PerformanceReviewDataStage[]? Stages { get; set; }
}
