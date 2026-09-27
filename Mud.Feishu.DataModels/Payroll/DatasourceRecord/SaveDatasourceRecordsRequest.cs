// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 创建 / 更新外部算薪数据请求体（记录的唯一标志通过业务主键判断：employment_id + payroll_period；已存在则覆盖更新，只更新传入字段）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class SaveDatasourceRecordsRequest
{
    /// <summary>
    /// <para>数据源 code，可从获取外部数据源配置信息接口或「飞书人事后台-设置-算薪数据设置-外部数据源配置」页面获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：yache19_8680__c</para>
    /// </summary>
    [JsonPropertyName("source_code")]
    public string? SourceCode { get; set; }

    /// <summary>
    /// <para>需保存的记录列表（1~50 条）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("records")]
    public PayrollDatasourceRecordToSave[]? Records { get; set; }
}
