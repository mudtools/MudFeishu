// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 外部算薪数据记录的查询条件（datasource_record_field_filter）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollDatasourceRecordFieldFilter
{
    /// <summary>
    /// <para>字段编码</para>
    /// <para>必填：是</para>
    /// <para>示例值：employment_id</para>
    /// </summary>
    [JsonPropertyName("field_code")]
    public string? FieldCode { get; set; }

    /// <summary>
    /// <para>包含的字段值列表（1~500 个）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["123"]</para>
    /// </summary>
    [JsonPropertyName("field_values")]
    public string[]? FieldValues { get; set; }

    /// <summary>
    /// <para>查询操作符，不传默认为 IsAnyOf：1（IsAnyOf 包含查询，被查询记录的字段值被 field_values 列表包含即可）/ 2（InDateRange 日期范围查询，field_values 长度必须为 2，类似 [startDate, endDate]，前后都是闭区间；日期格式 "2024-01-02"，仅 occur_day、custom_start、custom_end 字段支持，且时间范围不超过 90 天）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("operator")]
    public int? Operator { get; set; }
}
