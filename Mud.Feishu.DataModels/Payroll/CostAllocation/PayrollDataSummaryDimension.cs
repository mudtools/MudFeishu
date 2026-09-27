// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 数据维度汇总（data_summary_dimension）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollDataSummaryDimension
{
    /// <summary>
    /// <para>层级</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("dimension_level")]
    public int? DimensionLevel { get; set; }

    /// <summary>
    /// <para>类型：1（公司主体）/ 2（成本中心）/ 3（部门）/ 4（薪资组）/ 5（人员类型）/ 6（雇佣状态）/ 7（转正状态）/ 8（职务）/ 9（序列）/ 10（职级）/ 11（工时制度）/ 12（合同类型）/ 13（算薪项）/ 100（自定义维度）</para>
    /// </summary>
    [JsonPropertyName("dimension_type")]
    public int? DimensionType { get; set; }

    /// <summary>
    /// <para>维度 ID，需要根据 dimension_type 再次转换（如 dimension_type=1 时表示公司主体 ID；=13 时表示算薪项 ID）</para>
    /// <para>示例值：6823630319749580306</para>
    /// </summary>
    [JsonPropertyName("dimension_value_id")]
    public string? DimensionValueId { get; set; }

    /// <summary>
    /// <para>枚举对象：算薪项汇总维度时，当算薪项是特定枚举值，会使用该字段返回枚举值 ID 以及枚举值 Key</para>
    /// </summary>
    [JsonPropertyName("enum_dimension")]
    public PayrollEnumObject? EnumDimension { get; set; }

    /// <summary>
    /// <para>维度引用对象的基础信息，当维度为引用类型字段才会有值</para>
    /// </summary>
    [JsonPropertyName("dimension_value_lookup_info")]
    public PayrollDimensionValueLookupInfo? DimensionValueLookupInfo { get; set; }

    /// <summary>
    /// <para>维度名称（多语言），算薪项、自定义维度使用</para>
    /// </summary>
    [JsonPropertyName("dimension_names")]
    public I18nContent[]? DimensionNames { get; set; }

    /// <summary>
    /// <para>数据维度表头（多语言），算薪项、自定义维度使用</para>
    /// </summary>
    [JsonPropertyName("dimension_titles")]
    public I18nContent[]? DimensionTitles { get; set; }
}
