// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 操作候选人入职请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TransferOnboardRequest
{
    /// <summary>
    /// <para>实际入职时间（毫秒时间戳），不传默认当前时间且不能晚于当前时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("actual_onboard_time")]
    public long? ActualOnboardTime { get; set; }

    /// <summary>
    /// <para>预期转正时间（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("expected_conversion_time")]
    public long? ExpectedConversionTime { get; set; }

    /// <summary>
    /// <para>招聘需求 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_requirement_id")]
    public string? JobRequirementId { get; set; }

    /// <summary>
    /// <para>操作者用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public string? OperatorId { get; set; }

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
}
