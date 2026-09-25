// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 创建外部面试请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CreateExternalInterviewRequest
{
    /// <summary>
    /// <para>外部系统面试主键（仅用于幂等）</para>
    /// <para>必填：否</para>
    /// <para>示例值：123</para>
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// <para>外部投递 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6960663240925956437</para>
    /// </summary>
    [JsonPropertyName("external_application_id")]
    public string? ExternalApplicationId { get; set; }

    /// <summary>
    /// <para>参与状态：1 未参与 / 2 参与 / 3 缺席</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("participate_status")]
    public int? ParticipateStatus { get; set; }

    /// <summary>
    /// <para>开始时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278638</para>
    /// </summary>
    [JsonPropertyName("begin_time")]
    public long? BeginTime { get; set; }

    /// <summary>
    /// <para>结束时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278639</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public long? EndTime { get; set; }

    /// <summary>
    /// <para>面试评价列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_assessments")]
    public ExternalInterviewAssessmentRequest[]? InterviewAssessments { get; set; }
}
