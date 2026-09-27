// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 维度引用对象的基础信息（dimension_value_lookup_info），当维度为引用类型字段才会有值
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollDimensionValueLookupInfo
{
    /// <summary>
    /// <para>引用对象类型，包括不仅限：company（公司主体）/ cost_center（成本中心）/ department（部门）/ pay_group（薪资组）/ employee_type（人员类型）/ job（职务）/ job_family（序列）/ job_level（职级）/ working_hours_type（工时制度）/ work_calendar（工作日历）/ location（地点）</para>
    /// <para>示例值：work_calendar</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>引用对象的 id，可根据相关 API 查询到对象的完整信息</para>
    /// <para>示例值：6961286846093788621</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>引用对象的 code（company/cost_center/department/job/job_family/job_level/location 等对象有 code）</para>
    /// <para>示例值：D1230011115</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}
