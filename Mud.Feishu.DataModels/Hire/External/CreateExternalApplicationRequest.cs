// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 创建外部投递请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CreateExternalApplicationRequest
{
    /// <summary>
    /// <para>外部系统投递主键（仅用于幂等）：不传则不进行幂等校验；传入后同一 external_id 24 小时内仅可创建一次</para>
    /// <para>必填：否</para>
    /// <para>示例值：123</para>
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// <para>职位招聘类型：1 社招 / 2 校招</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("job_recruitment_type")]
    public int? JobRecruitmentType { get; set; }

    /// <summary>
    /// <para>职位名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：Senior Java</para>
    /// </summary>
    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// <para>简历来源</para>
    /// <para>必填：否</para>
    /// <para>示例值：lagou</para>
    /// </summary>
    [JsonPropertyName("resume_source")]
    public string? ResumeSource { get; set; }

    /// <summary>
    /// <para>阶段</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6960663240925956459</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>终止原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：Not match</para>
    /// </summary>
    [JsonPropertyName("termination_reason")]
    public string? TerminationReason { get; set; }

    /// <summary>
    /// <para>投递类型：1 HR 搜索 / 2 候选人自主投递 / 3 人才推荐 / 4 其他</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("delivery_type")]
    public int? DeliveryType { get; set; }

    /// <summary>
    /// <para>投递在外部系统终止时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278645</para>
    /// </summary>
    [JsonPropertyName("modify_time")]
    public long? ModifyTime { get; set; }

    /// <summary>
    /// <para>投递在外部系统创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278644</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public long? CreateTime { get; set; }

    /// <summary>
    /// <para>终止类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：health</para>
    /// </summary>
    [JsonPropertyName("termination_type")]
    public string? TerminationType { get; set; }
}
