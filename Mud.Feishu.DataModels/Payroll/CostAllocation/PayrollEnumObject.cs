// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 枚举对象（enum_object）：算薪项汇总维度为特定枚举值时返回枚举值 ID 与 Key
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollEnumObject
{
    /// <summary>
    /// <para>枚举对象 ID</para>
    /// <para>示例值：7188920315914207276</para>
    /// </summary>
    [JsonPropertyName("enum_value_id")]
    public string? EnumValueId { get; set; }

    /// <summary>
    /// <para>枚举对象，如 workCalendar（工作日历）/ location（地点）/ company（公司主体）/ costCenter（成本中心）/ department（部门）/ employeeType（人员类型）/ job（职务）/ jobFamily（序列）/ jobLevel（职级）/ workingHoursType（工时制度）</para>
    /// <para>示例值：company</para>
    /// </summary>
    [JsonPropertyName("enum_key")]
    public string? EnumKey { get; set; }
}
