// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 外部数据源字段（datasource_field）信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollDatasourceField
{
    /// <summary>
    /// <para>数据源字段编码</para>
    /// <para>示例值：test__c</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// <para>数据源字段名称（多语言）</para>
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public I18nContent[]? I18nNames { get; set; }

    /// <summary>
    /// <para>字段类型：1（金额）/ 2（数值）/ 3（文本）/ 4（日期）/ 5（百分比）</para>
    /// </summary>
    [JsonPropertyName("field_type")]
    public int? FieldType { get; set; }

    /// <summary>
    /// <para>字段启停用状态：1（已启用）/ 2（已停用）</para>
    /// </summary>
    [JsonPropertyName("active_status")]
    public int? ActiveStatus { get; set; }

    /// <summary>
    /// <para>数据源字段描述（多语言）</para>
    /// </summary>
    [JsonPropertyName("i18n_description")]
    public I18nContent[]? I18nDescription { get; set; }

    /// <summary>
    /// <para>保留小数位数。目前只有 number、money 类型字段需要设置保留小数</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("decimal_places")]
    public int? DecimalPlaces { get; set; }
}
