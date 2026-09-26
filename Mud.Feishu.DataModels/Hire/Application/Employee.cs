// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 入职信息-员工（入职相关响应共用）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class Employee
{
    /// <summary>
    /// <para>员工 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>入职状态：1 已入职 / 2 已离职</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_status")]
    public int? OnboardStatus { get; set; }

    /// <summary>
    /// <para>转正状态：1 未转正 / 2 已转正</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("conversion_status")]
    public int? ConversionStatus { get; set; }

    /// <summary>
    /// <para>实际入职时间（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_time")]
    public long? OnboardTime { get; set; }

    /// <summary>
    /// <para>预期转正时间（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("expected_conversion_time")]
    public long? ExpectedConversionTime { get; set; }

    /// <summary>
    /// <para>实际转正时间（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("actual_conversion_time")]
    public long? ActualConversionTime { get; set; }

    /// <summary>
    /// <para>离职时间（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("overboard_time")]
    public long? OverboardTime { get; set; }

    /// <summary>
    /// <para>离职原因</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("overboard_note")]
    public string? OverboardNote { get; set; }

    /// <summary>
    /// <para>办公地点 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_city_code")]
    public string? OnboardCityCode { get; set; }

    /// <summary>
    /// <para>入职部门 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    /// <summary>
    /// <para>直属上级用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leader")]
    public string? Leader { get; set; }

    /// <summary>
    /// <para>序列（职位类别）ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sequence")]
    public string? Sequence { get; set; }

    /// <summary>
    /// <para>职级 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("level")]
    public string? Level { get; set; }

    /// <summary>
    /// <para>人员类型 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employee_type")]
    public string? EmployeeType { get; set; }

    /// <summary>
    /// <para>招聘需求 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_requirement_id")]
    public string? JobRequirementId { get; set; }

    /// <summary>
    /// <para>飞书人事雇佣 ID（仅按投递 ID 查询入职信息接口返回）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("external_employment_id")]
    public string? ExternalEmploymentId { get; set; }
}
