// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 批量查询外部算薪数据记录请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class QueryDatasourceRecordsRequest
{
    /// <summary>
    /// <para>数据源 code</para>
    /// <para>必填：是</para>
    /// <para>示例值：yache19_8680__c</para>
    /// </summary>
    [JsonPropertyName("source_code")]
    public string? SourceCode { get; set; }

    /// <summary>
    /// <para>指定查询的数据源字段 code（1~200 个）。不传时默认返回所有数据源字段；传入时系统会默认返回 employment_id、payroll_period 字段的值</para>
    /// <para>必填：否</para>
    /// <para>示例值：["yache41_8680__c"]</para>
    /// </summary>
    [JsonPropertyName("selected_fields")]
    public string[]? SelectedFields { get; set; }

    /// <summary>
    /// <para>查询条件列表（最大 100 个），多个条件之间为 And 关系。支持的条件：employment_id（最多 100 个，IsAnyOf）；时间范围条件必传——算薪期间维度 payroll_period（IsAnyOf，最多 2 个月）、数据发生日期维度（灰度中）occur_day（InDateRange，不超过 90 天）、自定义数据周期维度（灰度中）custom_start 与 custom_end（均必传，InDateRange，不超过 90 天）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_filters")]
    public PayrollDatasourceRecordFieldFilter[]? FieldFilters { get; set; }
}
