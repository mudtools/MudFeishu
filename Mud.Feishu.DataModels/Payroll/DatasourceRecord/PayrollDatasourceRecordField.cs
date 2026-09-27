// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 外部算薪数据记录的字段值（datasource_record_field）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollDatasourceRecordField
{
    /// <summary>
    /// <para>数据源字段编码，请确保字段存在且是启用的</para>
    /// <para>必填：是</para>
    /// <para>示例值：employment_id</para>
    /// </summary>
    [JsonPropertyName("field_code")]
    public string? FieldCode { get; set; }

    /// <summary>
    /// <para>字段值，通过 string 传输，不允许输入空字符串，请确保字段的值符合类型对应的约束（金额 "12.23"；文本不超过 500 字符；日期 "yyyy-mm-dd"；算薪期间 "yyyy-mm"；百分比 "10" 代表 10%）</para>
    /// <para>必填：是</para>
    /// <para>示例值：123</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>字段类型：1（金额）/ 2（数值）/ 3（文本）/ 4（日期）/ 5（百分比）。保存时不需要传入此字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_type")]
    public int? FieldType { get; set; }
}
