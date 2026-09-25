// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 外部面试评价信息（创建外部面试响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ExternalInterviewAssessment
{
    /// <summary>
    /// <para>外部面评 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6989181065243969836</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>外部系统面评主键（仅用于幂等）</para>
    /// <para>必填：否</para>
    /// <para>示例值：123</para>
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// <para>面试官姓名</para>
    /// <para>必填：否</para>
    /// <para>示例值：shaojiale</para>
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// <para>面试结果：1 不通过 / 2 通过 / 3 待定</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("conclusion")]
    public int? Conclusion { get; set; }

    /// <summary>
    /// <para>评价维度列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assessment_dimension_list")]
    public ExternalInterviewAssessmentDimension[]? AssessmentDimensionList { get; set; }

    /// <summary>
    /// <para>综合记录</para>
    /// <para>必填：否</para>
    /// <para>示例值：hello world</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>外部面试 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6960663240925956436</para>
    /// </summary>
    [JsonPropertyName("external_interview_id")]
    public string? ExternalInterviewId { get; set; }
}
