// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 薪资计划（响应形态）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ApplicationOfferSalaryPlan
{
    /// <summary>
    /// <para>币种，如 CNY</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// <para>基本工资，JSON 字符串 {"amount":"10000","period":2}，period：1 日薪，2 月薪</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("basic_salary")]
    public string? BasicSalary { get; set; }

    /// <summary>
    /// <para>试用期工资百分比</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("probation_salary_percentage")]
    public string? ProbationSalaryPercentage { get; set; }

    /// <summary>
    /// <para>年终奖月数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("award_salary_multiple")]
    public string? AwardSalaryMultiple { get; set; }

    /// <summary>
    /// <para>期权股数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("option_shares")]
    public string? OptionShares { get; set; }

    /// <summary>
    /// <para>季度奖金</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("quarterly_bonus")]
    public string? QuarterlyBonus { get; set; }

    /// <summary>
    /// <para>半年奖金</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("half_year_bonus")]
    public string? HalfYearBonus { get; set; }

    /// <summary>
    /// <para>年度现金总额</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("total_annual_cash")]
    public string? TotalAnnualCash { get; set; }

    /// <summary>
    /// <para>自定义字段值列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customize_info_list")]
    public OfferCustomValue[]? CustomizeInfoList { get; set; }
}
