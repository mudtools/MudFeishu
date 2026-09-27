// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 外部数据源（datasource）配置信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollDatasource
{
    /// <summary>
    /// <para>数据源编码</para>
    /// <para>示例值：test_datasource__c</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// <para>数据源名称（多语言）</para>
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public I18nContent[]? I18nNames { get; set; }

    /// <summary>
    /// <para>启停用状态：1（已启用）/ 2（已停用）</para>
    /// </summary>
    [JsonPropertyName("active_status")]
    public int? ActiveStatus { get; set; }

    /// <summary>
    /// <para>数据源字段列表</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public PayrollDatasourceField[]? Fields { get; set; }

    /// <summary>
    /// <para>数据源描述（多语言）</para>
    /// </summary>
    [JsonPropertyName("i18n_description")]
    public I18nContent[]? I18nDescription { get; set; }

    /// <summary>
    /// <para>数据写入维度：1（算薪期间）/ 2（数据发生日期，灰度中）/ 3（自定义数据周期，灰度中）</para>
    /// </summary>
    [JsonPropertyName("data_period_type")]
    public int? DataPeriodType { get; set; }
}
